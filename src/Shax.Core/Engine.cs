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
        public override Entity CreateEntity(string name = "Entity")
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
    public class GameObject : Shax.GameObject
    {
        public GameObject(string name = "GameObject") : base(name) { }
    }
    public class BoxCollider : Shax.BoxCollider { }
    public class SphereCollider : Shax.SphereCollider { }
    public class Rigidbody : Shax.Rigidbody { }
    public class CharacterController : Shax.CharacterController { }
    public class PlayerController : Shax.PlayerController { }
    public class CameraComponent : Shax.CameraComponent { }
    public class PhysicsWorld : Shax.PhysicsWorld { }
    public class SceneManager : Shax.SceneManager { }
    public class FpsCounter : Shax.FpsCounter { }

    public static class Game
    {
        public static float DeltaTime => Shax.Game.DeltaTime;
        public static float Time => Shax.Game.Time;
        public static float TimeScale { get => Shax.Game.TimeScale; set => Shax.Game.TimeScale = value; }
        public static long FrameCount => Shax.Game.FrameCount;
        public static bool IsPlaying => Shax.Game.IsPlaying;
        public static void Pause() => Shax.Game.Pause();
        public static void Resume() => Shax.Game.Resume();
        public static void RequestQuit() => Shax.Game.RequestQuit();
    }
    public static class Time
    {
        public static float DeltaTime => Shax.Time.DeltaTime;
        public static float TimeSinceStartup => Shax.Time.TimeSinceStartup;
        public static float TimeScale { get => Shax.Time.TimeScale; set => Shax.Time.TimeScale = value; }
    }
    public static class Input
    {
        public static System.Numerics.Vector2 MoveAxis => Shax.Input.MoveAxis;
        public static System.Numerics.Vector2 LookDelta => Shax.Input.LookDelta;
        public static bool JumpPressed => Shax.Input.JumpPressed;
        public static void SetMoveAxis(float x, float y) => Shax.Input.SetMoveAxis(x, y);
        public static void SetLookDelta(float x, float y) => Shax.Input.SetLookDelta(x, y);
        public static void PressJump() => Shax.Input.PressJump();
    }
    public static class GltfAssetDecoder
    {
        public static Shax.GltfAsset Decode(byte[] bytes) => Shax.GltfAssetDecoder.Decode(bytes);
        public static Shax.GltfAsset DecodeFile(string path) => Shax.GltfAssetDecoder.DecodeFile(path);
        public static System.Numerics.Matrix4x4[] BuildWorldMatrices(IReadOnlyList<Shax.GltfNode> nodes) => Shax.GltfAssetDecoder.BuildWorldMatrices(nodes);
    }
}
