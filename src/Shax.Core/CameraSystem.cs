using System.Numerics;

namespace Shax;

public enum CameraMode { Orbit, FirstPerson, ThirdPerson }

public sealed class CameraComponent : IEntityComponent
{
    public Entity? Entity { get; private set; }
    public CameraMode Mode { get; set; } = CameraMode.Orbit;
    public Entity? Target { get; set; }
    public float FieldOfViewDegrees { get; set; } = 60f;
    public float NearClip { get; set; } = 0.05f;
    public float FarClip { get; set; } = 1000f;
    public float YawDegrees { get; set; }
    public float PitchDegrees { get; set; } = 12f;
    public float FollowDistance { get; set; } = 4f;
    public Vector3 FollowOffset { get; set; } = new(0, 1.5f, 0);
    public Vector3 OrbitTarget { get; set; }
    public void OnAttached(Entity entity) => Entity = entity ?? throw new ArgumentNullException(nameof(entity));

    public Vector3 WorldPosition
    {
        get
        {
            var target = Target?.Transform.WorldPosition ?? OrbitTarget;
            var yaw = MathUtil.DegreesToRadians(YawDegrees);
            var pitch = MathUtil.DegreesToRadians(Math.Clamp(PitchDegrees, -89f, 89f));
            var forward = Vector3.Normalize(new Vector3(MathF.Sin(yaw) * MathF.Cos(pitch), MathF.Sin(pitch), -MathF.Cos(yaw) * MathF.Cos(pitch)));
            return Mode switch
            {
                CameraMode.FirstPerson => target + FollowOffset,
                CameraMode.ThirdPerson => target + FollowOffset - forward * MathF.Max(0.1f, FollowDistance),
                _ => OrbitTarget - forward * MathF.Max(0.1f, FollowDistance)
            };
        }
    }

    public Vector3 Forward
    {
        get
        {
            var yaw = MathUtil.DegreesToRadians(YawDegrees);
            var pitch = MathUtil.DegreesToRadians(PitchDegrees);
            return Vector3.Normalize(new Vector3(MathF.Sin(yaw) * MathF.Cos(pitch), MathF.Sin(pitch), -MathF.Cos(yaw) * MathF.Cos(pitch)));
        }
    }

    public Matrix4x4 GetViewMatrix()
    {
        var target = Mode == CameraMode.Orbit ? OrbitTarget : (Target?.Transform.WorldPosition ?? Vector3.Zero) + FollowOffset;
        return Matrix4x4.CreateLookAt(WorldPosition, target, Vector3.UnitY);
    }

    public Matrix4x4 GetProjectionMatrix(float aspectRatio)
    {
        if (aspectRatio <= 0 || float.IsNaN(aspectRatio) || float.IsInfinity(aspectRatio)) throw new ArgumentOutOfRangeException(nameof(aspectRatio));
        return Matrix4x4.CreatePerspectiveFieldOfView(MathUtil.DegreesToRadians(Math.Clamp(FieldOfViewDegrees, 10, 120)),
            aspectRatio, MathF.Max(0.001f, NearClip), MathF.Max(NearClip + 0.001f, FarClip));
    }
}

public sealed class FpsCounter
{
    private readonly float[] _samples = new float[120];
    private int _next, _count;
    public float CurrentFps { get; private set; }
    public float AverageFps { get; private set; }
    public float FrameMilliseconds { get; private set; }
    public void AddFrame(float deltaTime)
    {
        if (deltaTime <= 0 || !float.IsFinite(deltaTime)) return;
        _samples[_next] = deltaTime;
        _next = (_next + 1) % _samples.Length;
        _count = Math.Min(_count + 1, _samples.Length);
        CurrentFps = 1f / deltaTime;
        FrameMilliseconds = deltaTime * 1000f;
        var sum = 0f;
        for (var i = 0; i < _count; i++) sum += _samples[i];
        AverageFps = _count / MathF.Max(sum, 0.000001f);
    }
}
