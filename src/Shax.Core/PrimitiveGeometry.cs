namespace Shax;

/// <summary>CPU-side mesh data. Renderer-specific GPU upload is handled by the graphics backend.</summary>
public sealed class MeshData
{
    public float[] Positions { get; }
    public uint[] Indices { get; }
    public MeshData(float[] positions, uint[] indices) { Positions = positions; Indices = indices; }
}

public static class PrimitiveGeometry
{
    /// <summary>Creates a unit cube centered at the origin, using 8 shared vertices.</summary>
    public static MeshData CreateCube()
    {
        float[] positions =
        {
            -0.5f, -0.5f, -0.5f,  0.5f, -0.5f, -0.5f,
             0.5f,  0.5f, -0.5f, -0.5f,  0.5f, -0.5f,
            -0.5f, -0.5f,  0.5f,  0.5f, -0.5f,  0.5f,
             0.5f,  0.5f,  0.5f, -0.5f,  0.5f,  0.5f
        };
        uint[] indices =
        {
            0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4, 3, 7, 6, 3, 6, 2,
            0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5
        };
        return new MeshData(positions, indices);
    }
}
