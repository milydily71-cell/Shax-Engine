# Shax C# API design

The engine's public API and gameplay scripts are C#. The renderer backend may use platform-specific bindings internally, but gameplay scripts should not need direct OpenGL calls.

## Example intended gameplay script

```csharp
using Shax;

public sealed class SpinBehaviour : ShaxBehaviour
{
    public float DegreesPerSecond { get; set; } = 45f;

    public override void Update(float deltaTime)
    {
        Transform.Rotate(Vector3.Up, DegreesPerSecond * deltaTime);
    }
}
```

## Core API contracts
- `Entity`: stable ID, name, active state, transform and typed component access.
- `Component`: owning entity, enabled state and lifecycle.
- `Transform`: local position/rotation/scale, parent-child relationship and world matrix.
- `ShaxBehaviour`: overridable lifecycle methods and per-frame update.
- `Scene`: entity creation/destruction, lookup and safe deferred mutation during iteration.
- `MeshRenderer`: mesh and material references.
- `Camera`: projection parameters and view matrix.
- `DirectionalLight`: direction, color and intensity.
- `Time`: delta time, elapsed time and frame index.
- `Input`: normalized touch and key/gamepad abstraction.

## Script safety and loading
Use a separate project build step to compile user scripts and show compiler diagnostics. Keep the engine API versioned. Do not load arbitrary assemblies from untrusted projects silently. Define permissions and trusted-project behavior before enabling plugins or native interop.

## First testable slice
Create a scene, add a cube entity, attach a mesh renderer and a C# behavior, advance the behavior with a fixed/variable timestep policy, then render the resulting transform through the GLES backend.
