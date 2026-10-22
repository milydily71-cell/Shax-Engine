using System.Numerics;

namespace Shax;

public interface IEntityComponent
{
    void OnAttached(Entity entity);
}

public interface ICollisionListener
{
    void OnCollisionEnter(CollisionInfo collision);
    void OnCollisionStay(CollisionInfo collision);
    void OnCollisionExit(CollisionInfo collision);
}

public abstract class Collider : IEntityComponent
{
    public Entity? Entity { get; private set; }
    public Vector3 Center { get; set; }
    public bool IsTrigger { get; set; }
    public abstract Vector3 HalfExtents { get; }
    public void OnAttached(Entity entity) => Entity = entity ?? throw new ArgumentNullException(nameof(entity));
    internal Bounds3 Bounds
    {
        get
        {
            var p = Entity?.Transform.WorldPosition ?? Vector3.Zero;
            var s = Entity?.Transform.LocalScale ?? Vector3.One;
            var ext = HalfExtents * Vector3.Abs(s);
            return new Bounds3(p + Center, ext);
        }
    }
}

public sealed class BoxCollider : Collider
{
    public Vector3 Size { get; set; } = Vector3.One;
    public override Vector3 HalfExtents => Vector3.Max(Vector3.Abs(Size) * 0.5f, new Vector3(0.001f));
}

public sealed class SphereCollider : Collider
{
    public float Radius { get; set; } = 0.5f;
    public override Vector3 HalfExtents
    {
        get
        {
            var r = MathF.Max(0.001f, MathF.Abs(Radius));
            return new Vector3(r);
        }
    }
}

public sealed class Rigidbody : IEntityComponent
{
    public Entity? Entity { get; private set; }
    public Vector3 Velocity { get; set; }
    public float Mass { get; set; } = 1f;
    public float LinearDamping { get; set; } = 0.02f;
    public bool UseGravity { get; set; } = true;
    public bool IsKinematic { get; set; }
    public bool FreezePositionX { get; set; }
    public bool FreezePositionY { get; set; }
    public bool FreezePositionZ { get; set; }
    public void AddForce(Vector3 force) => Velocity += force / MathF.Max(0.0001f, Mass);
    public void OnAttached(Entity entity) => Entity = entity ?? throw new ArgumentNullException(nameof(entity));
}

public sealed class CharacterController : IEntityComponent
{
    public Entity? Entity { get; private set; }
    public float Radius { get; set; } = 0.35f;
    public float Height { get; set; } = 1.8f;
    public float SkinWidth { get; set; } = 0.03f;
    public float StepOffset { get; set; } = 0.25f;
    public bool IsGrounded { get; internal set; }
    public Vector3 Velocity { get; set; }
    public Vector3 LastMove { get; private set; }
    internal PhysicsWorld? World { get; set; }
    public void OnAttached(Entity entity) => Entity = entity ?? throw new ArgumentNullException(nameof(entity));
    public CollisionInfo? Move(Vector3 displacement, PhysicsWorld? physics = null)
    {
        if (Entity is null) throw new InvalidOperationException("CharacterController must be attached to an entity.");
        LastMove = displacement;
        var world = physics ?? World;
        if (world is null) { Entity.Transform.LocalPosition += displacement; return null; }
        return world.MoveCharacter(this, displacement);
    }
}

public readonly record struct CollisionInfo(Entity Self, Entity Other, Vector3 Normal, float Penetration, bool IsTrigger);

internal readonly record struct Bounds3(Vector3 Center, Vector3 Extents)
{
    public Vector3 Min => Center - Extents;
    public Vector3 Max => Center + Extents;
    public bool Intersects(Bounds3 other) =>
        Min.X <= other.Max.X && Max.X >= other.Min.X &&
        Min.Y <= other.Max.Y && Max.Y >= other.Min.Y &&
        Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;
    public Vector3 Overlap(Bounds3 other) => Vector3.Min(Max, other.Max) - Vector3.Max(Min, other.Min);
}

/// <summary>Deterministic fixed-step AABB collision and gravity simulation for mobile prototypes.</summary>
public sealed class PhysicsWorld
{
    private readonly HashSet<(Guid A, Guid B)> _activePairs = new();
    public Vector3 Gravity { get; set; } = new(0, -9.81f, 0);
    public int SolverIterations { get; set; } = 4;
    public int CollisionCount { get; private set; }
    public float FixedDeltaTime { get; set; } = 1f / 60f;
    private float _accumulator;
    private Scene? _scene;

    internal void Bind(Scene scene) => _scene = scene;

    public void Step(Scene scene, float deltaTime)
    {
        if (scene is null) throw new ArgumentNullException(nameof(scene));
        Bind(scene);
        foreach (var controller in scene.Entities.SelectMany(e => e.Components.OfType<CharacterController>())) controller.World = this;
        if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) throw new ArgumentOutOfRangeException(nameof(deltaTime));
        _accumulator += MathF.Min(deltaTime, 0.25f);
        var step = Math.Clamp(FixedDeltaTime, 1f / 240f, 1f / 15f);
        var iterations = 0;
        while (_accumulator >= step && iterations++ < 8)
        {
            Simulate(scene, step);
            _accumulator -= step;
        }
    }

    public CollisionInfo? MoveCharacter(CharacterController controller, Vector3 displacement)
    {
        var entity = controller.Entity ?? throw new InvalidOperationException("Unattached character controller.");
        var collider = entity.Components.OfType<Collider>().FirstOrDefault() ??
            entity.AddComponent(new BoxCollider { Size = new Vector3(controller.Radius * 2, controller.Height, controller.Radius * 2), Center = new Vector3(0, controller.Height * 0.5f, 0) });
        var start = entity.Transform.LocalPosition;
        entity.Transform.LocalPosition = start + new Vector3(displacement.X, 0, displacement.Z);
        var hit = FindCollision(entity, collider);
        if (hit is not null) entity.Transform.LocalPosition = start;
        else start = entity.Transform.LocalPosition;
        entity.Transform.LocalPosition = start + new Vector3(0, displacement.Y, 0);
        var verticalHit = FindCollision(entity, collider);
        if (verticalHit is not null)
        {
            entity.Transform.LocalPosition = start;
            controller.IsGrounded = displacement.Y <= 0 && verticalHit.Value.Normal.Y > 0;
        }
        else controller.IsGrounded = false;
        return verticalHit ?? hit;
    }

    private void Simulate(Scene scene, float dt)
    {
        var bodies = scene.Entities.Where(e => e.Active)
            .SelectMany(e => e.Components.OfType<Rigidbody>().Select(body => (Entity: e, Body: body))).ToArray();
        var previous = bodies.ToDictionary(x => x.Entity.Id, x => x.Entity.Transform.LocalPosition);
        foreach (var item in bodies)
        {
            var body = item.Body;
            if (body.IsKinematic) continue;
            if (body.UseGravity) body.Velocity += Gravity * dt;
            body.Velocity *= 1f / (1f + MathF.Max(0, body.LinearDamping) * dt);
            var delta = body.Velocity * dt;
            if (body.FreezePositionX) delta.X = 0;
            if (body.FreezePositionY) delta.Y = 0;
            if (body.FreezePositionZ) delta.Z = 0;
            item.Entity.Transform.LocalPosition += delta;
        }

        var colliders = scene.Entities.Where(e => e.Active).SelectMany(e => e.Components.OfType<Collider>())
            .Where(c => c.Entity is not null).ToArray();
        CollisionCount = 0;
        var nextPairs = new HashSet<(Guid A, Guid B)>();
        for (var i = 0; i < colliders.Length; i++)
        for (var j = i + 1; j < colliders.Length; j++)
        {
            var a = colliders[i]; var b = colliders[j];
            if (a.Entity == b.Entity || !a.Bounds.Intersects(b.Bounds)) continue;
            var ea = a.Entity!; var eb = b.Entity!;
            var overlap = a.Bounds.Overlap(b.Bounds);
            var normal = overlap.X <= overlap.Y && overlap.X <= overlap.Z
                ? new Vector3(ea.Transform.WorldPosition.X < eb.Transform.WorldPosition.X ? -1 : 1, 0, 0)
                : overlap.Y <= overlap.Z
                    ? new Vector3(0, ea.Transform.WorldPosition.Y < eb.Transform.WorldPosition.Y ? -1 : 1, 0)
                    : new Vector3(0, 0, ea.Transform.WorldPosition.Z < eb.Transform.WorldPosition.Z ? -1 : 1);
            var info = new CollisionInfo(ea, eb, normal, MathF.Max(0, MathF.Min(overlap.X, MathF.Min(overlap.Y, overlap.Z))), a.IsTrigger || b.IsTrigger);
            CollisionCount++;
            var pair = ea.Id.CompareTo(eb.Id) < 0 ? (ea.Id, eb.Id) : (eb.Id, ea.Id);
            nextPairs.Add(pair);
            Notify(ea, eb, info, _activePairs.Contains(pair));
            Notify(eb, ea, info with { Self = eb, Other = ea, Normal = -normal }, _activePairs.Contains(pair));
            if (info.IsTrigger) continue;
            ResolveBody(ea, normal, previous);
            ResolveBody(eb, -normal, previous);
        }
        _activePairs.Clear();
        foreach (var pair in nextPairs) _activePairs.Add(pair);
    }

    private static void ResolveBody(Entity entity, Vector3 normal, Dictionary<Guid, Vector3> previous)
    {
        var body = entity.GetComponent<Rigidbody>();
        if (body is null || body.IsKinematic) return;
        if (normal.X != 0) body.Velocity = new Vector3(0, body.Velocity.Y, body.Velocity.Z);
        if (normal.Y != 0) body.Velocity = new Vector3(body.Velocity.X, 0, body.Velocity.Z);
        if (normal.Z != 0) body.Velocity = new Vector3(body.Velocity.X, body.Velocity.Y, 0);
        if (previous.TryGetValue(entity.Id, out var pos) && normal.Y != 0 && normal.Y < 0)
            entity.Transform.LocalPosition = new Vector3(entity.Transform.LocalPosition.X, MathF.Max(entity.Transform.LocalPosition.Y, pos.Y), entity.Transform.LocalPosition.Z);
    }

    private CollisionInfo? FindCollision(Entity entity, Collider self)
    {
        if (_scene is null) return null;
        foreach (var other in _scene.Entities.Where(e => e.Active && !ReferenceEquals(e, entity))
                     .SelectMany(e => e.Components.OfType<Collider>()))
        {
            if (other.IsTrigger || !self.Bounds.Intersects(other.Bounds)) continue;
            var overlap = self.Bounds.Overlap(other.Bounds);
            var delta = self.Bounds.Center - other.Bounds.Center;
            var normal = overlap.X <= overlap.Y && overlap.X <= overlap.Z
                ? new Vector3(delta.X < 0 ? -1 : 1, 0, 0)
                : overlap.Y <= overlap.Z
                    ? new Vector3(0, delta.Y < 0 ? -1 : 1, 0)
                    : new Vector3(0, 0, delta.Z < 0 ? -1 : 1);
            return new CollisionInfo(entity, other.Entity!, normal,
                MathF.Max(0, MathF.Min(overlap.X, MathF.Min(overlap.Y, overlap.Z))), false);
        }
        return null;
    }

    private static void Notify(Entity self, Entity other, CollisionInfo info, bool wasActive)
    {
        foreach (var listener in self.Components.OfType<ICollisionListener>())
        {
            if (wasActive) listener.OnCollisionStay(info);
            else listener.OnCollisionEnter(info);
        }
    }
}
