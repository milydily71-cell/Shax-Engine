using System.Numerics;

namespace Shax;

/// <summary>Local transform with cycle-safe parent hierarchy and world-matrix evaluation.</summary>
public class Transform
{
    private readonly List<Transform> _children = new();
    private Transform? _parent;
    public Vector3 LocalPosition { get; set; } = Vector3.Zero;
    public Quaternion LocalRotation { get; set; } = Quaternion.Identity;
    public Vector3 LocalScale { get; set; } = Vector3.One;
    public Transform? Parent => _parent;
    public IReadOnlyList<Transform> Children => _children;
    public Matrix4x4 LocalMatrix => Matrix4x4.CreateScale(LocalScale) *
        Matrix4x4.CreateFromQuaternion(Quaternion.Normalize(LocalRotation)) *
        Matrix4x4.CreateTranslation(LocalPosition);
    public Matrix4x4 WorldMatrix => _parent is null ? LocalMatrix : LocalMatrix * _parent.WorldMatrix;
    public Vector3 WorldPosition => Vector3.Transform(Vector3.Zero, WorldMatrix);

    public void SetParent(Transform? parent)
    {
        if (ReferenceEquals(parent, this)) throw new InvalidOperationException("A transform cannot parent itself.");
        for (var cursor = parent; cursor is not null; cursor = cursor.Parent)
            if (ReferenceEquals(cursor, this)) throw new InvalidOperationException("Transform parenting cannot create a cycle.");
        if (ReferenceEquals(_parent, parent)) return;
        _parent?._children.Remove(this);
        _parent = parent;
        if (parent is not null && !parent._children.Contains(this)) parent._children.Add(this);
    }

    public void Translate(Vector3 direction, float distance)
    {
        if (direction.LengthSquared() < 1e-8f || !float.IsFinite(distance)) return;
        LocalPosition += Vector3.Normalize(direction) * distance;
    }

    public void Rotate(Vector3 axis, float degrees)
    {
        if (axis.LengthSquared() < 1e-8f) return;
        var delta = Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), degrees * (MathF.PI / 180f));
        LocalRotation = Quaternion.Normalize(delta * LocalRotation);
    }
}
