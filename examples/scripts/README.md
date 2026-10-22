# Shax mobile C# script examples

These examples are also available from the Android Script IDE's **Examples** button.

The current mobile runtime deliberately interprets a small allow-listed C# subset for AOT-safe execution. Supported statements in `Update(float deltaTime)` are:

- `Transform.Rotate(Vector3.UnitX/UnitY/UnitZ, speed * deltaTime)`
- `Transform.LocalPosition = new Vector3(x, y, z)`
- `Transform.LocalScale = new Vector3(x, y, z)`

Use finite numeric literals. Unsupported C# is rejected with diagnostics; this is not yet a full Roslyn compiler and does not load arbitrary assemblies. Press **Play** in the mobile IDE to validate and run a script.
