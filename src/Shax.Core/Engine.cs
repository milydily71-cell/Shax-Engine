namespace Shax
{
    /// <summary>Small public entry point for projects and editor integrations.</summary>
    public static class Engine
    {
        public const string Version = "0.2.0";
        public static Scene CreateScene(string name = "Scene") => new(name);
        public static MeshData CreateCube() => PrimitiveGeometry.CreateCube();
    }

    public static class MathUtil
    {
        public static float DegreesToRadians(float degrees) => degrees * (MathF.PI / 180f);
        public static float RadiansToDegrees(float radians) => radians * (180f / MathF.PI);
    }
}

namespace ShaxEngine
{
    /// <summary>Convenience entry point for projects using the ShaxEngine namespace.</summary>
    public static class Engine
    {
        public const string Version = Shax.Engine.Version;
        public static Shax.Scene CreateScene(string name = "Scene") => Shax.Engine.CreateScene(name);
        public static Shax.MeshData CreateCube() => Shax.Engine.CreateCube();
    }

    public static class Primitives
    {
        public static Shax.MeshData Cube() => Shax.PrimitiveGeometry.CreateCube();
    }
}
