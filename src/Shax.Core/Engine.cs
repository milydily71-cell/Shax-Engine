namespace Shax
{
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
    /// <summary>Entity type for projects that prefer the ShaxEngine namespace.</summary>
    public class Entity : Shax.Entity
    {
        public Entity(string name = "Entity") : base(name) { }
    }

    /// <summary>Behaviour base compatible with the core lifecycle.</summary>
    public abstract class ShaxBehaviour : Shax.ShaxBehaviour { }

    public class Transform : Shax.Transform { }

    public sealed class Scene : Shax.Scene
    {
        public Scene(string name = "Scene") : base(name) { }
        public override Shax.Entity CreateEntity(string name = "Entity")
        {
            var entity = new Entity(name);
            RegisterEntity(entity);
            return entity;
        }
    }

    public static class Engine
    {
        public const string Version = Shax.Engine.Version;
        public static Scene CreateScene(string name = "Scene") => new(name);
        public static Shax.MeshData CreateCube() => Shax.Engine.CreateCube();
    }

    public static class Primitives
    {
        public static Shax.MeshData Cube() => Shax.PrimitiveGeometry.CreateCube();
    }
}
