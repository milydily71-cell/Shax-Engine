using System.Numerics;

namespace Shax;

/// <summary>Frame/game state API shared by compiled gameplay behaviours and the editor runtime.</summary>
public static class Game
{
    public static float DeltaTime { get; private set; }
    public static float Time { get; private set; }
    public static float TimeScale { get; set; } = 1f;
    public static long FrameCount { get; private set; }
    public static bool IsPlaying { get; private set; } = true;
    public static event Action? QuitRequested;

    internal static void AdvanceTime(float deltaTime)
    {
        if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0)
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        var scale = float.IsNaN(TimeScale) || float.IsInfinity(TimeScale) ? 1f : Math.Clamp(TimeScale, 0f, 8f);
        DeltaTime = deltaTime * scale;
        Time += DeltaTime;
        FrameCount++;
    }
    public static void Pause() => IsPlaying = false;
    public static void Resume() => IsPlaying = true;
    public static void RequestQuit() => QuitRequested?.Invoke();
    public static void ResetClock() { DeltaTime = 0; Time = 0; FrameCount = 0; TimeScale = 1f; }
}

/// <summary>Simple host-fed mobile input state; Android touch controls can set axes and jump.</summary>
public static class Time
{
    public static float DeltaTime => Game.DeltaTime;
    public static float TimeSinceStartup => Game.Time;
    public static float TimeScale { get => Game.TimeScale; set => Game.TimeScale = value; }
}

public static class Input
{
    private static bool _jumpPressed;
    public static Vector2 MoveAxis { get; private set; }
    public static Vector2 LookDelta { get; private set; }
    public static bool JumpPressed => _jumpPressed;
    public static void SetMoveAxis(float x, float y) => MoveAxis = Vector2.Clamp(new Vector2(x, y), new Vector2(-1), new Vector2(1));
    public static void SetLookDelta(float x, float y) => LookDelta = new Vector2(x, y);
    public static void PressJump() => _jumpPressed = true;
    public static bool ConsumeJumpPressed() { var pressed = _jumpPressed; _jumpPressed = false; return pressed; }
    public static void ClearLookDelta() => LookDelta = Vector2.Zero;
    public static void Reset() { MoveAxis = Vector2.Zero; LookDelta = Vector2.Zero; _jumpPressed = false; }
}

/// <summary>Unity-style familiar alias for scene entities in gameplay scripts.</summary>
public class GameObject : Entity
{
    public GameObject(string name = "GameObject") : base(name) { }
}
