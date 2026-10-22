using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;

namespace Shax;

/// <summary>
/// Interprets a deliberately tiny, allow-listed C# behaviour subset. It does not load assemblies,
/// use reflection, compile IL, access files/network, or invoke arbitrary methods.
/// </summary>
public static class CSharpSubsetRuntime
{
    private static readonly Regex BehaviourType = new(@"\bclass\s+\w+\s*:\s*(?:Shax\.)?ShaxBehaviour\b", RegexOptions.Compiled);
    private static readonly Regex UpdateMethod = new(@"\bUpdate\s*\(\s*float\s+deltaTime\s*\)\s*\{(?<body>[^{}]*)\}", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly Regex Rotate = new(@"^Transform\.Rotate\s*\(\s*(?:(?:System\.Numerics)\.)?Vector3\.Unit(?<axis>[XYZ])\s*,\s*(?<speed>[+-]?(?:\d+(?:\.\d*)?|\.\d+))f?\s*\*\s*deltaTime\s*\)$", RegexOptions.Compiled);
    private static readonly Regex VectorAssignment = new(@"^Transform\.(?<property>LocalPosition|LocalScale)\s*=\s*new\s+(?:(?:System\.Numerics)\.)?Vector3\s*\(\s*(?<x>[+-]?(?:\d+(?:\.\d*)?|\.\d+))f?\s*,\s*(?<y>[+-]?(?:\d+(?:\.\d*)?|\.\d+))f?\s*,\s*(?<z>[+-]?(?:\d+(?:\.\d*)?|\.\d+))f?\s*\)$", RegexOptions.Compiled);
    private static readonly Regex Comments = new(@"//[^\r\n]*|/\*.*?\*/", RegexOptions.Compiled | RegexOptions.Singleline);
    private static readonly string[] ForbiddenTokens =
    {
        "System.IO", "System.Net", "Reflection", "DllImport", "Process", "unsafe",
        "Assembly", "File.", "Directory.", "Socket", "HttpClient", "Activator",
        "Type.GetType", "Environment.", "dynamic", "extern", "Thread", "Task.Run"
    };

    public static ScriptCompilationResult Compile(string source)
    {
        var diagnostics = new List<ScriptDiagnostic>();
        if (string.IsNullOrWhiteSpace(source))
            return Failure("Script source is empty.", 1, 1);

        var clean = Comments.Replace(source, string.Empty);
        foreach (var token in ForbiddenTokens)
        {
            var index = clean.IndexOf(token, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var (line, column) = Location(clean, index);
                diagnostics.Add(new ScriptDiagnostic("SHAX0001", $"Sandbox blocked forbidden API/token '{token}'.", line, column, ScriptDiagnosticSeverity.Error));
                return new ScriptCompilationResult(null, diagnostics);
            }
        }

        if (!BehaviourType.IsMatch(clean))
            return Failure("Expected a class deriving from ShaxBehaviour.", 1, 1);
        var update = UpdateMethod.Match(clean);
        if (!update.Success)
            return Failure("Expected Update(float deltaTime) with a single-level method body.", 1, 1);

        var instructions = new List<ScriptInstruction>();
        var body = update.Groups["body"].Value;
        var cursor = update.Groups["body"].Index;
        foreach (var raw in body.Split(';'))
        {
            var statement = raw.Trim();
            if (statement.Length == 0) continue;
            var offset = clean.IndexOf(statement, cursor, StringComparison.Ordinal);
            if (offset >= 0) cursor = offset + statement.Length;
            var (line, column) = Location(clean, Math.Max(0, offset));
            var rotate = Rotate.Match(statement);
            if (rotate.Success)
            {
                var speed = Parse(rotate.Groups["speed"].Value);
                var axis = rotate.Groups["axis"].Value switch { "X" => Vector3.UnitX, "Y" => Vector3.UnitY, _ => Vector3.UnitZ };
                instructions.Add(new ScriptInstruction(ScriptInstructionKind.Rotate, axis, speed));
                continue;
            }
            var assignment = VectorAssignment.Match(statement);
            if (assignment.Success)
            {
                var value = new Vector3(Parse(assignment.Groups["x"].Value), Parse(assignment.Groups["y"].Value), Parse(assignment.Groups["z"].Value));
                var kind = assignment.Groups["property"].Value == "LocalPosition" ? ScriptInstructionKind.SetPosition : ScriptInstructionKind.SetScale;
                instructions.Add(new ScriptInstruction(kind, value, 0));
                continue;
            }
            diagnostics.Add(new ScriptDiagnostic("SHAX0002", "Statement is outside the safe Shax subset. Allowed: Transform.Rotate(UnitX/Y/Z, speed * deltaTime), LocalPosition and LocalScale Vector3 assignments.", line, column, ScriptDiagnosticSeverity.Error));
            return new ScriptCompilationResult(null, diagnostics);
        }

        diagnostics.Add(new ScriptDiagnostic("SHAX0000", $"Safe behaviour compiled to {instructions.Count} allow-listed instruction(s); no IL/JIT assembly is loaded.", 1, 1, ScriptDiagnosticSeverity.Info));
        return new ScriptCompilationResult(new ScriptProgram(instructions), diagnostics);
    }

    private static float Parse(string value) => float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    private static ScriptCompilationResult Failure(string message, int line, int column) =>
        new(null, new[] { new ScriptDiagnostic("SHAX0001", message, line, column, ScriptDiagnosticSeverity.Error) });

    private static (int line, int column) Location(string source, int index)
    {
        var line = 1; var column = 1;
        for (var i = 0; i < Math.Min(index, source.Length); i++)
        {
            if (source[i] == '\n') { line++; column = 1; }
            else column++;
        }
        return (line, column);
    }
}

public sealed class ScriptCompilationResult
{
    public ScriptProgram? Program { get; }
    public IReadOnlyList<ScriptDiagnostic> Diagnostics { get; }
    public bool Success => Program is not null && Diagnostics.All(d => d.Severity != ScriptDiagnosticSeverity.Error);
    public ScriptCompilationResult(ScriptProgram? program, IReadOnlyList<ScriptDiagnostic> diagnostics)
    {
        Program = program; Diagnostics = diagnostics;
    }
}

public sealed class ScriptProgram
{
    private readonly IReadOnlyList<ScriptInstruction> _instructions;
    internal ScriptProgram(IReadOnlyList<ScriptInstruction> instructions) => _instructions = instructions;

    public void Execute(ShaxBehaviour behaviour, float deltaTime)
    {
        if (behaviour is null) throw new ArgumentNullException(nameof(behaviour));
        if (deltaTime < 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        foreach (var instruction in _instructions)
        {
            switch (instruction.Kind)
            {
                case ScriptInstructionKind.Rotate: behaviour.Transform.Rotate(instruction.Vector, instruction.Scalar * deltaTime); break;
                case ScriptInstructionKind.SetPosition: behaviour.Transform.LocalPosition = instruction.Vector; break;
                case ScriptInstructionKind.SetScale: behaviour.Transform.LocalScale = instruction.Vector; break;
                default: throw new InvalidOperationException("Unknown safe script instruction.");
            }
        }
    }
}

internal enum ScriptInstructionKind { Rotate, SetPosition, SetScale }
internal sealed class ScriptInstruction
{
    public ScriptInstructionKind Kind { get; }
    public Vector3 Vector { get; }
    public float Scalar { get; }
    public ScriptInstruction(ScriptInstructionKind kind, Vector3 vector, float scalar)
    {
        Kind = kind; Vector = vector; Scalar = scalar;
    }
}
