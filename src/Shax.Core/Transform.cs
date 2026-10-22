namespace Shax;

/// <summary>Engine-neutral transform data; matrix and quaternion math will be added in the next slice.</summary>
public sealed class Transform
{
    public System.Numerics.Vector3 LocalPosition { get; set; } = System.Numerics.Vector3.Zero;
    public System.Numerics.Quaternion LocalRotation { get; set; } = System.Numerics.Quaternion.Identity;
    public System.Numerics.Vector3 LocalScale { get; set; } = System.Numerics.Vector3.One;

    public void Rotate(System.Numerics.Vector3 axis, float degrees)
    {
        if (axis.LengthSquared() < 1e-8f) return;
        var delta = System.Numerics.Quaternion.CreateFromAxisAngle(
            System.Numerics.Vector3.Normalize(axis),
            degrees * (MathF.PI / 180f));
        LocalRotation = System.Numerics.Quaternion.Normalize(delta * LocalRotation);
    }
}
