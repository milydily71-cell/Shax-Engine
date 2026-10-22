using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Shax;

/// <summary>Metallic-roughness material settings shared by editor, scene and render backends.</summary>
public sealed class PbrMaterialSettings
{
    public string Name { get; set; } = "Default Material";
    public float BaseColorR { get; set; } = 0.18f;
    public float BaseColorG { get; set; } = 0.62f;
    public float BaseColorB { get; set; } = 0.95f;
    public float BaseColorA { get; set; } = 1f;
    public float Metallic { get; set; } = 0.08f;
    public float Roughness { get; set; } = 0.28f;
    public float AmbientOcclusion { get; set; } = 1f;
    public float NormalScale { get; set; } = 1f;

    public void Validate()
    {
        ValidateUnit(BaseColorR, nameof(BaseColorR)); ValidateUnit(BaseColorG, nameof(BaseColorG));
        ValidateUnit(BaseColorB, nameof(BaseColorB)); ValidateUnit(BaseColorA, nameof(BaseColorA));
        ValidateUnit(Metallic, nameof(Metallic)); ValidateUnit(Roughness, nameof(Roughness));
        ValidateUnit(AmbientOcclusion, nameof(AmbientOcclusion));
        if (float.IsNaN(NormalScale) || float.IsInfinity(NormalScale) || NormalScale < 0f || NormalScale > 4f)
            throw new ArgumentOutOfRangeException(nameof(NormalScale), "Normal scale must be finite and between 0 and 4.");
    }

    private static void ValidateUnit(float value, string name)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
            throw new ArgumentOutOfRangeException(name, "PBR channel values must be finite and between 0 and 1.");
    }
}

/// <summary>Runtime graphics features determined from the actual active context, not requested EGL version.</summary>
public readonly struct RenderCapabilities
{
    public int Major { get; }
    public int Minor { get; }
    public bool SupportsGles32 { get; }
    public bool SupportsComputeShaders { get; }
    public bool SupportsGeometryShaders { get; }

    public RenderCapabilities(int major, int minor)
    {
        if (major != 3 || (minor != 0 && minor != 2))
            throw new ArgumentOutOfRangeException(nameof(major), "Shax only accepts GLES 3.2 or GLES 3.0; GLES 3.1 is deliberately rejected.");
        Major = major; Minor = minor;
        SupportsGles32 = major > 3 || (major == 3 && minor >= 2);
        SupportsComputeShaders = SupportsGles32;
        SupportsGeometryShaders = SupportsGles32;
    }

    public static bool TryParse(string? version, out RenderCapabilities capabilities)
    {
        capabilities = default;
        if (string.IsNullOrWhiteSpace(version)) return false;
        var marker = version.IndexOf("OpenGL ES ", StringComparison.Ordinal);
        if (marker < 0) return false;
        var numbers = version.Substring(marker + 10).Split('.');
        if (numbers.Length < 2 || !int.TryParse(new string(numbers[0].TakeWhile(char.IsDigit).ToArray()), out var major)
            || !int.TryParse(new string(numbers[1].TakeWhile(char.IsDigit).ToArray()), out var minor)) return false;
        try { capabilities = new RenderCapabilities(major, minor); return true; }
        catch (ArgumentOutOfRangeException) { return false; }
    }
}

/// <summary>Reference-count-free cache with explicit ownership: cached values are disposed on replacement/clear.</summary>
public sealed class RenderResourceCache<TKey, TValue> : IDisposable where TKey : notnull where TValue : class, IDisposable
{
    private readonly Dictionary<TKey, TValue> _items = new();
    private bool _disposed;
    public int Count => _items.Count;

    public TValue GetOrCreate(TKey key, Func<TKey, TValue> factory)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(RenderResourceCache<TKey, TValue>));
        if (_items.TryGetValue(key, out var value)) return value;
        value = factory(key) ?? throw new InvalidOperationException("Resource factory returned null.");
        _items.Add(key, value); return value;
    }

    public bool TryGet(TKey key, out TValue? value) => _items.TryGetValue(key, out value);

    public bool Remove(TKey key)
    {
        if (!_items.Remove(key, out var value)) return false;
        value.Dispose(); return true;
    }

    public void Clear()
    {
        foreach (var value in _items.Values) value.Dispose();
        _items.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        Clear(); _disposed = true;
    }
}

public sealed class GlbImportResult
{
    public string SourcePath { get; init; } = string.Empty;
    public string AssetPath { get; init; } = string.Empty;
    public string Json { get; init; } = string.Empty;
    public int MeshCount { get; init; }
    public int MaterialCount { get; init; }
    public int TextureCount { get; init; }
    public int ImageCount { get; init; }
    public int NodeCount { get; init; }
    public int AnimationCount { get; init; }
    public int BufferCount { get; init; }
    public long FileSize { get; init; }
}

/// <summary>
/// Validates GLB 2.0 containers, extracts the JSON chunk and stages imported assets atomically.
/// Mesh decoding/GPU upload and skeletal animation evaluation are backend follow-ups.
/// </summary>
public static class GlbAssetImporter
{
    private const uint Magic = 0x46546C67;
    private const uint JsonChunk = 0x4E4F534A;
    private const uint BinChunk = 0x004E4942;
    private const uint MaximumAssetBytes = 512u * 1024u * 1024u;

    public static GlbImportResult Inspect(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A GLB path is required.", nameof(path));
        using var stream = File.OpenRead(path);
        if (stream.Length < 20 || stream.Length > MaximumAssetBytes) throw new InvalidDataException("GLB size is outside supported bounds.");
        using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        if (reader.ReadUInt32() != Magic) throw new InvalidDataException("Not a binary glTF (GLB) file.");
        var version = reader.ReadUInt32();
        var declaredLength = reader.ReadUInt32();
        if (version != 2) throw new InvalidDataException($"GLB version {version} is unsupported; expected version 2.");
        if (declaredLength != stream.Length) throw new InvalidDataException("GLB declared length does not match the file length.");

        string? json = null;
        long consumed = 12;
        while (consumed + 8 <= stream.Length)
        {
            uint chunkLength = reader.ReadUInt32(), chunkType = reader.ReadUInt32();
            consumed += 8;
            if (chunkLength > stream.Length - consumed) throw new InvalidDataException("GLB chunk extends beyond the file.");
            if (chunkType == JsonChunk)
            {
                if (json is not null) throw new InvalidDataException("GLB contains multiple JSON chunks.");
                var bytes = reader.ReadBytes(checked((int)chunkLength));
                json = Encoding.UTF8.GetString(bytes).TrimEnd('\0', ' ', '\t', '\r', '\n');
            }
            else if (chunkType == BinChunk)
            {
                stream.Seek(chunkLength, SeekOrigin.Current);
            }
            else
            {
                stream.Seek(chunkLength, SeekOrigin.Current);
            }
            consumed += chunkLength;
        }
        if (consumed != stream.Length || json is null) throw new InvalidDataException("GLB must contain a valid JSON chunk and aligned chunks.");
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        if (!root.TryGetProperty("asset", out var asset) || !asset.TryGetProperty("version", out var assetVersion)
            || assetVersion.GetString() is not string v || !v.StartsWith("2.", StringComparison.Ordinal))
            throw new InvalidDataException("GLB JSON does not declare glTF asset version 2.x.");
        return new GlbImportResult
        {
            SourcePath = Path.GetFullPath(path), Json = json, FileSize = stream.Length,
            MeshCount = Count(root, "meshes"), MaterialCount = Count(root, "materials"),
            TextureCount = Count(root, "textures"), ImageCount = Count(root, "images"),
            NodeCount = Count(root, "nodes"), AnimationCount = Count(root, "animations"),
            BufferCount = Count(root, "buffers")
        };
    }

    public static GlbImportResult Import(string sourcePath, string assetDirectory)
    {
        var inspected = Inspect(sourcePath);
        Directory.CreateDirectory(assetDirectory);
        var safeName = Path.GetFileNameWithoutExtension(sourcePath);
        foreach (var invalid in Path.GetInvalidFileNameChars()) safeName = safeName.Replace(invalid, '_');
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "imported_asset";
        var destination = Path.Combine(assetDirectory, safeName + ".glb");
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.Copy(sourcePath, temporary, overwrite: false);
            if (File.Exists(destination)) File.Delete(destination);
            File.Move(temporary, destination);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
        return new GlbImportResult
        {
            SourcePath = inspected.SourcePath, AssetPath = destination, Json = inspected.Json, FileSize = inspected.FileSize,
            MeshCount = inspected.MeshCount, MaterialCount = inspected.MaterialCount, TextureCount = inspected.TextureCount,
            ImageCount = inspected.ImageCount, NodeCount = inspected.NodeCount, AnimationCount = inspected.AnimationCount,
            BufferCount = inspected.BufferCount
        };
    }

    private static int Count(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array ? value.GetArrayLength() : 0;
}


/// <summary>Crash-resistant JSON persistence for editor PBR material settings.</summary>
public static class PbrMaterialStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static void SaveAtomic(string path, PbrMaterialSettings settings)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A material path is required.", nameof(path));
        if (settings is null) throw new ArgumentNullException(nameof(settings));
        settings.Validate();
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new IOException("Material path has no directory.");
        Directory.CreateDirectory(directory);
        var temporary = fullPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, Options), new UTF8Encoding(false));
            File.Move(temporary, fullPath, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static PbrMaterialSettings Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A material path is required.", nameof(path));
        if (!File.Exists(path)) return new PbrMaterialSettings();
        var value = JsonSerializer.Deserialize<PbrMaterialSettings>(File.ReadAllText(path), Options)
            ?? throw new InvalidDataException("Material settings JSON is empty or invalid.");
        value.Validate();
        return value;
    }
}
