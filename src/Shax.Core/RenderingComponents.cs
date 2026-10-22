namespace Shax;

public enum LightKind { Directional, Point, Spot }

public sealed class Camera
{
    public float FieldOfViewDegrees { get; set; } = 55f;
    public float NearClip { get; set; } = 0.1f;
    public float FarClip { get; set; } = 1000f;
    public bool IsPrimary { get; set; }
}

public sealed class Light
{
    public LightKind Kind { get; set; } = LightKind.Directional;
    public System.Numerics.Vector3 Color { get; set; } = System.Numerics.Vector3.One;
    public float Intensity { get; set; } = 1f;
    public float Range { get; set; } = 10f;
}

public sealed class Material
{
    public System.Numerics.Vector4 BaseColor { get; set; } = new(0.2f, 0.65f, 0.95f, 1f);
    public float Metallic { get; set; }
    public float Roughness { get; set; } = 0.7f;
}

public sealed class MeshRenderer : IDisposable
{
    private IDisposable? _meshResource;
    private IDisposable? _materialResource;
    public MeshData? Mesh { get; set; }
    public Material Material { get; set; } = new();
    public bool OwnsMeshResource { get; set; }
    public bool OwnsMaterialResource { get; set; }

    public void BindResources(IDisposable? meshResource, IDisposable? materialResource, bool ownsMesh, bool ownsMaterial)
    {
        ReleaseOwnedResources();
        _meshResource = meshResource;
        _materialResource = materialResource;
        OwnsMeshResource = ownsMesh;
        OwnsMaterialResource = ownsMaterial;
    }

    public void Dispose() => ReleaseOwnedResources();

    private void ReleaseOwnedResources()
    {
        if (OwnsMeshResource) _meshResource?.Dispose();
        if (OwnsMaterialResource && !ReferenceEquals(_meshResource, _materialResource)) _materialResource?.Dispose();
        _meshResource = null; _materialResource = null;
        OwnsMeshResource = OwnsMaterialResource = false;
    }
}
