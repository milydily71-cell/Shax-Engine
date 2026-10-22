using System.Collections.Generic;

namespace Shax;

/// <summary>
/// Built-in, executable examples for the mobile Script IDE. Every sample intentionally uses
/// only statements accepted by CSharpSubsetRuntime; examples do not run arbitrary C# or load IL.
/// </summary>
public static class ScriptSamples
{
    public static IReadOnlyDictionary<string, string> All { get; } =
        new Dictionary<string, string>
        {
            ["Rotate cube around Y"] = @"using Shax;
using System.Numerics;

public sealed class PlayerScript : ShaxBehaviour
{
    public override void Update(float deltaTime)
    {
        Transform.Rotate(Vector3.UnitY, 45f * deltaTime);
    }
}",
            ["Rotate cube around X"] = @"using Shax;
using System.Numerics;

public sealed class PlayerScript : ShaxBehaviour
{
    public override void Update(float deltaTime)
    {
        Transform.Rotate(Vector3.UnitX, 30f * deltaTime);
    }
}",
            ["Move player forward"] = @"using Shax;
using System.Numerics;

public sealed class PlayerScript : ShaxBehaviour
{
    public override void Update(float deltaTime)
    {
        Transform.Translate(Vector3.UnitZ, 3f * deltaTime);
    }
}",
            ["Set cube position"] = @"using Shax;
using System.Numerics;

public sealed class PlayerScript : ShaxBehaviour
{
    public override void Update(float deltaTime)
    {
        Transform.LocalPosition = new Vector3(0f, 0.5f, 0f);
    }
}",
            ["Set cube scale"] = @"using Shax;
using System.Numerics;

public sealed class PlayerScript : ShaxBehaviour
{
    public override void Update(float deltaTime)
    {
        Transform.LocalScale = new Vector3(1.5f, 1.5f, 1.5f);
    }
}",
            ["Rotate and scale"] = @"using Shax;
using System.Numerics;

public sealed class PlayerScript : ShaxBehaviour
{
    public override void Update(float deltaTime)
    {
        Transform.Rotate(Vector3.UnitY, 60f * deltaTime);
        Transform.LocalScale = new Vector3(1f, 1f, 1f);
    }
}"
        };
}
