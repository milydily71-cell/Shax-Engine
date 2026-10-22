using System.Numerics;

namespace Shax;

/// <summary>Third-/first-person character movement with camera-relative input, gravity, jump and collision callbacks.</summary>
public class PlayerController : ShaxBehaviour, ICollisionListener
{
    private CharacterController? _controller;
    private CameraComponent? _camera;
    private float _verticalSpeed;
    private bool _jumpQueued;
    public float MoveSpeed { get; set; } = 5f;
    public float JumpHeight { get; set; } = 1.25f;
    public float Gravity { get; set; } = -9.81f;
    public float AirControl { get; set; } = 0.65f;
    public Vector2 MoveInput { get; private set; }
    public CameraComponent? Camera { get; set; }
    public bool IsGrounded => _controller?.IsGrounded ?? false;
    public Vector3 Velocity => new(MoveInput.X * MoveSpeed, _verticalSpeed, MoveInput.Y * MoveSpeed);
    public event Action<CollisionInfo>? CollisionEntered;

    public override void Awake()
    {
        _controller = Entity!.GetComponent<CharacterController>() ?? Entity.AddComponent(new CharacterController());
        if (Entity.GetComponent<Collider>() is null)
            Entity.AddComponent(new BoxCollider { Size = new Vector3(0.7f, 1.8f, 0.7f), Center = new Vector3(0, 0.9f, 0) });
        _camera = Camera ?? Entity.Components.OfType<CameraComponent>().FirstOrDefault();
    }

    public void SetMoveInput(float horizontal, float forward)
    {
        MoveInput = Vector2.Clamp(new Vector2(horizontal, forward), new Vector2(-1), new Vector2(1));
        if (MoveInput.LengthSquared() > 1f) MoveInput = Vector2.Normalize(MoveInput);
    }

    public void Jump() => _jumpQueued = true;

    public override void Update(float deltaTime)
    {
        if (_controller is null || Entity is null || deltaTime <= 0) return;
        var axis = MoveInput == Vector2.Zero ? Input.MoveAxis : MoveInput;
        if (Input.LookDelta != Vector2.Zero && _camera is not null)
        {
            _camera.YawDegrees += Input.LookDelta.X;
            _camera.PitchDegrees = Math.Clamp(_camera.PitchDegrees - Input.LookDelta.Y, -89f, 89f);
            Input.ClearLookDelta();
        }
        var forward = _camera?.Forward ?? Vector3.UnitZ;
        forward.Y = 0;
        if (forward.LengthSquared() < 0.0001f) forward = Vector3.UnitZ;
        forward = Vector3.Normalize(forward);
        var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        var input = right * axis.X + forward * axis.Y;
        if (input.LengthSquared() > 1f) input = Vector3.Normalize(input);
        var control = _controller.IsGrounded ? 1f : Math.Clamp(AirControl, 0f, 1f);
        if (_controller.IsGrounded && _verticalSpeed < 0) _verticalSpeed = -0.5f;
        if ((_jumpQueued || Input.ConsumeJumpPressed()) && _controller.IsGrounded)
            _verticalSpeed = MathF.Sqrt(MathF.Max(0.1f, JumpHeight) * -2f * MathF.Min(-0.001f, Gravity));
        _jumpQueued = false;
        _verticalSpeed += Gravity * deltaTime;
        _controller.Velocity = input * MoveSpeed * control + new Vector3(0, _verticalSpeed, 0);
        _controller.Move(_controller.Velocity * deltaTime);
        if (_controller.IsGrounded && _verticalSpeed < 0) _verticalSpeed = 0;
    }

    public void OnCollisionEnter(CollisionInfo collision)
    {
        if (collision.Normal.Y > 0.4f) { _controller!.IsGrounded = true; if (_verticalSpeed < 0) _verticalSpeed = 0; }
        CollisionEntered?.Invoke(collision);
    }
    public void OnCollisionStay(CollisionInfo collision)
    {
        if (collision.Normal.Y > 0.4f) { _controller!.IsGrounded = true; if (_verticalSpeed < 0) _verticalSpeed = 0; }
    }
    public void OnCollisionExit(CollisionInfo collision) { }
}
