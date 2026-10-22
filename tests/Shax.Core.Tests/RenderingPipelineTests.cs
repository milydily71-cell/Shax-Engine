using System.Text;
using Shax;
using Xunit;

namespace Shax.Core.Tests;

public sealed class RenderingPipelineTests
{
    [Fact]
    public void PbrMaterialRejectsOutOfRangeAndNonFiniteValues()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PbrMaterialSettings { Metallic = 1.5f }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new PbrMaterialSettings { Roughness = float.NaN }.Validate());
        new PbrMaterialSettings().Validate();
    }

    [Theory]
    [InlineData("OpenGL ES 3.2 V@123", true, 2)]
    [InlineData("OpenGL ES 3.0 V@123", true, 0)]
    [InlineData("OpenGL ES 3.1 V@123", false, 0)]
    [InlineData("OpenGL ES 2.0", false, 0)]
    [InlineData("not a GL version", false, 0)]
    public void ParsesActiveGraphicsCapabilities(string version, bool expected, int minor)
    {
        var success = RenderCapabilities.TryParse(version, out var capabilities);
        Assert.Equal(expected, success);
        if (success) Assert.Equal(minor, capabilities.Minor);
    }

    [Fact]
    public void GlbImporterValidatesAndStagesAsset()
    {
        var root = Path.Combine(Path.GetTempPath(), "shax-glb-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var source = Path.Combine(root, "sample.glb");
        var json = Encoding.UTF8.GetBytes("{\"asset\":{\"version\":\"2.0\"},\"meshes\":[{}],\"materials\":[{}],\"nodes\":[{}]}");
        var paddedLength = (json.Length + 3) & ~3;
        using (var stream = File.Create(source))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(0x46546C67u);
            writer.Write(2u);
            writer.Write((uint)(12 + 8 + paddedLength));
            writer.Write((uint)paddedLength);
            writer.Write(0x4E4F534Au);
            writer.Write(json);
            for (var i = json.Length; i < paddedLength; i++) writer.Write((byte)0x20);
        }

        try
        {
            var inspected = GlbAssetImporter.Inspect(source);
            Assert.Equal(1, inspected.MeshCount);
            Assert.Equal(1, inspected.MaterialCount);
            var imported = GlbAssetImporter.Import(source, Path.Combine(root, "Assets"));
            Assert.True(File.Exists(imported.AssetPath));
            Assert.Equal(inspected.FileSize, new FileInfo(imported.AssetPath).Length);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void ResourceCacheDisposesOwnedValuesOnClear()
    {
        var cache = new RenderResourceCache<string, TestResource>();
        cache.GetOrCreate("cube", _ => new TestResource());
        Assert.Equal(1, cache.Count);
        cache.Clear();
        Assert.Equal(0, cache.Count);
        Assert.True(TestResource.LastDisposed);
    }


    [Fact]
    public void MaterialSettingsRoundTripThroughAtomicStore()
    {
        var root = Path.Combine(Path.GetTempPath(), "shax-material-test-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(root, "material.json");
        try
        {
            var material = new PbrMaterialSettings { Name = "Chrome", Metallic = 0.92f, Roughness = 0.12f };
            PbrMaterialStore.SaveAtomic(path, material);
            var restored = PbrMaterialStore.Load(path);
            Assert.Equal("Chrome", restored.Name);
            Assert.Equal(0.92f, restored.Metallic, 3);
            Assert.Equal(0.12f, restored.Roughness, 3);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void BuiltInScriptIdeExamplesCompileInTheAotSafeSubset()
    {
        Assert.NotEmpty(ScriptSamples.All);
        foreach (var example in ScriptSamples.All)
        {
            var result = CSharpSubsetRuntime.Compile(example.Value);
            Assert.True(result.Success, example.Key + ": " + string.Join("; ", result.Diagnostics.Select(d => d.Message)));
        }
    }

    private sealed class TestResource : IDisposable
    {
        public static bool LastDisposed { get; private set; }
        public TestResource() => LastDisposed = false;
        public void Dispose() => LastDisposed = true;
    }
}
