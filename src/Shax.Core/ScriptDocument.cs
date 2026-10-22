namespace Shax;

public enum ScriptDiagnosticSeverity { Info, Warning, Error }

public sealed class ScriptDiagnostic
{
    public ScriptDiagnosticSeverity Severity { get; init; }
    public string Message { get; init; } = string.Empty;
    public int Line { get; init; }
    public int Column { get; init; }
}

public sealed class ScriptDocument
{
    public string Path { get; set; } = "Scripts/PlayerScript.cs";
    public string Source { get; set; } = DefaultSource;
    public IReadOnlyList<ScriptDiagnostic> Diagnostics { get; set; } = Array.Empty<ScriptDiagnostic>();

    public static string DefaultSource => @"using Shax;

// Edit and save this behaviour source. Runtime compilation is not enabled yet.
public sealed class PlayerScript : ShaxBehaviour
{
    public override void Update(float deltaTime)
    {
        Transform.Rotate(System.Numerics.Vector3.UnitY, 45f * deltaTime);
    }
}";
}
