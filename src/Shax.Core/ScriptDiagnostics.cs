namespace Shax;

public enum ScriptDiagnosticSeverity { Info, Warning, Error }

/// <summary>Compiler/editor diagnostic independent of a specific compiler implementation.</summary>
public sealed class ScriptDiagnostic
{
    public string Code { get; }
    public string Message { get; }
    public int Line { get; }
    public int Column { get; }
    public ScriptDiagnosticSeverity Severity { get; }

    public ScriptDiagnostic(string code, string message, int line = 0, int column = 0,
        ScriptDiagnosticSeverity severity = ScriptDiagnosticSeverity.Error)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Line = Math.Max(0, line);
        Column = Math.Max(0, column);
        Severity = severity;
    }
}

/// <summary>An editable C# source document. Compilation is supplied by the runtime host.</summary>
public sealed class ScriptDocument
{
    public string Name { get; set; }
    public string Source { get; private set; }
    public bool IsDirty { get; private set; }

    public ScriptDocument(string name = "NewScript.cs", string source = DefaultSource)
    {
        Name = name;
        Source = source;
    }

    public void ReplaceSource(string source)
    {
        Source = source ?? throw new ArgumentNullException(nameof(source));
        IsDirty = true;
    }

    public void MarkSaved() => IsDirty = false;

    public const string DefaultSource = "using Shax;\n\npublic sealed class RotateCube : ShaxBehaviour\n{\n    public override void Update(float deltaTime)\n    {\n        Transform.Rotate(System.Numerics.Vector3.UnitY, 45f * deltaTime);\n    }\n}\n";
}
