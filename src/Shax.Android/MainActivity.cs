using global::Android.Content;
using Android.App;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using System.Numerics;
using System.Text.RegularExpressions;
using Shax;

namespace Shax.Android;

[Activity(Label = "Shax Engine", MainLauncher = true, Exported = true,
    ConfigurationChanges = global::Android.Content.PM.ConfigChanges.Orientation |
                           global::Android.Content.PM.ConfigChanges.ScreenSize |
                           global::Android.Content.PM.ConfigChanges.KeyboardHidden)]
public sealed class MainActivity : Activity
{
    private readonly Scene _scene = Engine.CreateScene("Main Scene");
    private Entity? _cubeEntity;
    private LinearLayout? _root;
    private FrameLayout? _workspace;
    private ShaxViewport? _viewport;
    private EditText? _scriptEditor;
    private TextView? _status;
    private TextView? _diagnostics;
    private LinearLayout? _hierarchyPanel;
    private LinearLayout? _scriptPanel;
    private TextView? _assetPanel;
    private TextView? _consolePanel;
    private EditText? _positionX, _positionY, _positionZ;
    private readonly Stack<Vector3> _undo = new();
    private readonly Stack<Vector3> _redo = new();
    private bool _highlighting;\n    private RuntimeScriptBehaviour? _runtimeScript;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);
        _cubeEntity = _scene.CreateEntity("Cube");
        _cubeEntity.Transform.LocalPosition = new Vector3(0f, 0.5f, 0f);
        _cubeEntity.AddComponent(new PreviewSpinBehaviour());
        BuildEditorShell();
        LoadScript();
        LoadScene();
    }

    private void BuildEditorShell()
    {
        _root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        _root.SetBackgroundColor(Android.Graphics.Color.Rgb(18, 22, 30));
        var toolbarScroll = new HorizontalScrollView(this) { HorizontalScrollBarEnabled = true };
        var toolbar = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        toolbar.SetPadding(5, 3, 5, 3);
        toolbar.AddView(Label("SHAX", 15, true), new LinearLayout.LayoutParams(-2, -1));
        AddAction(toolbar, "Scene", () => ShowPanel("scene"));
        AddAction(toolbar, "Script IDE", () => ShowPanel("script"));
        AddAction(toolbar, "Assets", () => ShowPanel("assets"));
        AddAction(toolbar, "Console", () => ShowPanel("console"));
        AddAction(toolbar, "Save", SaveScript);
        AddAction(toolbar, "Save Scene", SaveScene);
        AddAction(toolbar, "Undo", UndoTransform);
        AddAction(toolbar, "Redo", RedoTransform);
        AddAction(toolbar, "Play", CompileAndPlay);
        toolbarScroll.AddView(toolbar);
        _root.AddView(toolbarScroll, new LinearLayout.LayoutParams(-1, -2));

        _workspace = new FrameLayout(this);
        _root.AddView(_workspace, new LinearLayout.LayoutParams(-1, 0, 1));
        _viewport = new ShaxViewport(this, _scene, _cubeEntity!);
        _workspace.AddView(_viewport, new FrameLayout.LayoutParams(-1, -1));

        var scriptPanel = new LinearLayout(this) { Orientation = Orientation.Vertical };
        _scriptPanel = scriptPanel;
        scriptPanel.SetBackgroundColor(Android.Graphics.Color.Rgb(25, 31, 42));
        _scriptEditor = new EditText(this)
        {
            Text = ScriptDocument.DefaultSource,
            Gravity = GravityFlags.Top | GravityFlags.Left,
            Typeface = Android.Graphics.Typeface.Monospace,
            TextSize = 13,
            SingleLine = false
        };
        _scriptEditor.SetTextColor(Android.Graphics.Color.Rgb(225, 233, 245));
        _scriptEditor.SetBackgroundColor(Android.Graphics.Color.Rgb(25, 31, 42));
        _scriptEditor.SetPadding(12, 12, 12, 12);
        scriptPanel.AddView(_scriptEditor, new LinearLayout.LayoutParams(-1, 0, 1));
        _diagnostics = Label("Diagnostics: waiting for edits", 11, false);
        _diagnostics.SetPadding(10, 6, 10, 6);
        scriptPanel.AddView(_diagnostics, new LinearLayout.LayoutParams(-1, -2));
        _scriptEditor.TextChanged += (_, _) => { ApplySyntaxHighlighting(); UpdateDiagnostics(); };
        _workspace.AddView(scriptPanel, new FrameLayout.LayoutParams(-1, -1));
        scriptPanel.Visibility = ViewStates.Gone;

        _hierarchyPanel = new LinearLayout(this) { Orientation = Orientation.Vertical };
        _hierarchyPanel.SetPadding(12, 10, 12, 10);
        _hierarchyPanel.SetBackgroundColor(Android.Graphics.Color.Rgb(24, 30, 40));
        _hierarchyPanel.AddView(Label("SCENE HIERARCHY", 14, true));
        _hierarchyPanel.AddView(Label("▾ Main Scene", 13, true));
        var cubeButton = new Button(this) { Text = "  ◈ Cube (selected)", Gravity = GravityFlags.Left };
        cubeButton.SetOnClickListener(new ClickAction(() => RefreshInspector()));
        _hierarchyPanel.AddView(cubeButton);
        _hierarchyPanel.AddView(Label("TRANSFORM INSPECTOR", 13, true));
        var positionRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        _positionX = NumberField("X"); _positionY = NumberField("Y"); _positionZ = NumberField("Z");
        positionRow.AddView(_positionX, new LinearLayout.LayoutParams(0, -2, 1));
        positionRow.AddView(_positionY, new LinearLayout.LayoutParams(0, -2, 1));
        positionRow.AddView(_positionZ, new LinearLayout.LayoutParams(0, -2, 1));
        _hierarchyPanel.AddView(positionRow);
        AddAction(_hierarchyPanel, "Apply Position", ApplyTransform);
        _workspace.AddView(_hierarchyPanel, new FrameLayout.LayoutParams(-1, 260, GravityFlags.Bottom));
        _hierarchyPanel.Visibility = ViewStates.Gone;

        _assetPanel = Label("ASSET BROWSER\n\nScripts/\n  PlayerScript.cs\n\nScenes/\n  MainScene.shax.json\n\nTextures/\n  Drop image assets here (import pipeline pending)\n\nModels/\n  GLB/glTF importer pending", 14, false);
        _assetPanel.SetPadding(16, 16, 16, 16);
        _assetPanel.SetBackgroundColor(Android.Graphics.Color.Rgb(24, 30, 40));
        _workspace.AddView(_assetPanel, new FrameLayout.LayoutParams(-1, -1));
        _assetPanel.Visibility = ViewStates.Gone;

        _consolePanel = Label("CONSOLE\nShax Engine starting…", 12, false);
        _consolePanel.SetPadding(16, 16, 16, 16);
        _consolePanel.SetBackgroundColor(Android.Graphics.Color.Rgb(15, 19, 26));
        _workspace.AddView(_consolePanel, new FrameLayout.LayoutParams(-1, -1));
        _consolePanel.Visibility = ViewStates.Gone;

        _status = Label("NET 10 • C# CORE • NATIVE GLES 3.2 / 3.0", 11, false);
        _status.SetPadding(10, 6, 10, 6);
        _root.AddView(_status, new LinearLayout.LayoutParams(-1, -2));
        SetContentView(_root);
        RefreshInspector();
    }

    private EditText NumberField(string label) => new(this)
    {
        Hint = label, InputType = Android.Text.InputTypes.ClassNumber | Android.Text.InputTypes.NumberFlagDecimal | Android.Text.InputTypes.NumberFlagSigned,
        TextSize = 14
    };

    private TextView Label(string text, float size, bool bold)
    {
        var label = new TextView(this) { Text = text, TextSize = size, Gravity = GravityFlags.CenterVertical };
        label.SetTextColor(Android.Graphics.Color.Rgb(225, 233, 245));
        if (bold) label.SetTypeface(Android.Graphics.Typeface.Default, Android.Graphics.TypefaceStyle.Bold);
        return label;
    }

    private void AddAction(LinearLayout parent, string text, Action action)
    {
        var button = new Button(this) { Text = text, TextSize = 10 };
        button.SetOnClickListener(new ClickAction(action));
        parent.AddView(button, new LinearLayout.LayoutParams(-2, -1));
    }

    private void ShowPanel(string panel)
    {
        if (_viewport != null) _viewport.Visibility = panel == "scene" ? ViewStates.Visible : ViewStates.Gone;
        if (_scriptPanel != null) _scriptPanel.Visibility = panel == "script" ? ViewStates.Visible : ViewStates.Gone;
        if (_hierarchyPanel != null) _hierarchyPanel.Visibility = panel == "scene" ? ViewStates.Visible : ViewStates.Gone;
        if (_assetPanel != null) _assetPanel.Visibility = panel == "assets" ? ViewStates.Visible : ViewStates.Gone;
        if (_consolePanel != null) _consolePanel.Visibility = panel == "console" ? ViewStates.Visible : ViewStates.Gone;
        if (panel == "scene") RefreshInspector();
        SetStatus(panel switch {
            "scene" => "Scene • hierarchy, transform inspector and live viewport",
            "script" => "Script IDE • C# syntax colors and static diagnostics; runtime compilation pending",
            "assets" => "Asset browser • project tree prototype",
            _ => "Console • native and editor messages"
        });
    }

    private void RefreshInspector()
    {
        if (_cubeEntity is null) return;
        if (_positionX != null) _positionX.Text = _cubeEntity.Transform.LocalPosition.X.ToString("0.###");
        if (_positionY != null) _positionY.Text = _cubeEntity.Transform.LocalPosition.Y.ToString("0.###");
        if (_positionZ != null) _positionZ.Text = _cubeEntity.Transform.LocalPosition.Z.ToString("0.###");
    }

    private void ApplyTransform()
    {
        if (_cubeEntity is null) return;
        if (!float.TryParse(_positionX?.Text, out var x) || !float.TryParse(_positionY?.Text, out var y) || !float.TryParse(_positionZ?.Text, out var z))
        {
            SetStatus("Transform error: enter valid numeric X, Y and Z values."); return;
        }
        _undo.Push(_cubeEntity.Transform.LocalPosition); _redo.Clear();
        _cubeEntity.Transform.LocalPosition = new Vector3(x, y, z);
        SetStatus("Cube transform applied to the live native viewport.");
    }

    private void UndoTransform()
    {
        if (_cubeEntity is null || _undo.Count == 0) { SetStatus("Nothing to undo."); return; }
        _redo.Push(_cubeEntity.Transform.LocalPosition);
        _cubeEntity.Transform.LocalPosition = _undo.Pop(); RefreshInspector();
    }

    private void RedoTransform()
    {
        if (_cubeEntity is null || _redo.Count == 0) { SetStatus("Nothing to redo."); return; }
        _undo.Push(_cubeEntity.Transform.LocalPosition);
        _cubeEntity.Transform.LocalPosition = _redo.Pop(); RefreshInspector();
    }

    private void ApplySyntaxHighlighting()
    {
        if (_highlighting || _scriptEditor?.Text is not IEditable editable) return;
        _highlighting = true;
        try
        {
            var existing = editable.GetSpans(0, editable.Length, Java.Lang.Class.FromType(typeof(ForegroundColorSpan)));
            foreach (var span in existing) editable.RemoveSpan(span);
            var source = editable.ToString() ?? string.Empty;
            var keywords = new Regex(@"\b(using|namespace|public|private|protected|class|sealed|override|virtual|void|float|int|return|new|this|var)\b");
            foreach (Match match in keywords.Matches(source))
                editable.SetSpan(new ForegroundColorSpan(Android.Graphics.Color.Rgb(112, 174, 255)), match.Index, match.Index + match.Length, SpanTypes.ExclusiveExclusive);
            var comments = new Regex(@"//[^\r\n]*");
            foreach (Match match in comments.Matches(source))
                editable.SetSpan(new ForegroundColorSpan(Android.Graphics.Color.Rgb(117, 165, 127)), match.Index, match.Index + match.Length, SpanTypes.ExclusiveExclusive);
        }
        finally { _highlighting = false; }
    }

    private void UpdateDiagnostics()
    {
        var source = _scriptEditor?.Text?.ToString() ?? string.Empty;
        var braces = source.Count(c => c == '{') - source.Count(c => c == '}');
        var parens = source.Count(c => c == '(') - source.Count(c => c == ')');
        var errors = new List<string>();
        if (braces != 0) errors.Add("unbalanced braces");
        if (parens != 0) errors.Add("unbalanced parentheses");
        if (_diagnostics != null) _diagnostics.Text = errors.Count == 0
            ? "Diagnostics: no basic delimiter errors • C# compiler not connected"
            : "Diagnostics: " + string.Join(", ", errors);
    }

    private void LoadScript()
    {
        try
        {
            using var stream = OpenFileInput("PlayerScript.cs") ?? throw new System.IO.IOException("Could not open saved script.");
            using var reader = new System.IO.StreamReader(stream);
            if (_scriptEditor != null) _scriptEditor.Text = reader.ReadToEnd();
            ApplySyntaxHighlighting(); UpdateDiagnostics();
        }
        catch (Java.IO.FileNotFoundException) { }
        catch (Exception ex) { SetStatus("Script load failed: " + ex.Message); }
    }

    private void SaveScript()
    {
        try
        {
            if (_scriptEditor == null) return;
            using var stream = OpenFileOutput("PlayerScript.cs", FileCreationMode.Private) ?? throw new System.IO.IOException("Could not open script file for writing.");
            using var writer = new System.IO.StreamWriter(stream);
            writer.Write(_scriptEditor.Text?.ToString() ?? string.Empty);
            SetStatus("Saved PlayerScript.cs to app-private storage.");
        }
        catch (Exception ex) { SetStatus("Script save failed: " + ex.Message); }
    }

    private void SaveScene()
    {
        try
        {
            using var stream = OpenFileOutput("MainScene.shax.json", FileCreationMode.Private) ?? throw new System.IO.IOException("Could not open scene file for writing.");
            using var writer = new System.IO.StreamWriter(stream);
            writer.Write(SceneSerializer.Serialize(_scene));
            SetStatus("Saved scene JSON with stable entity IDs.");
        }
        catch (Exception ex) { SetStatus("Scene save failed: " + ex.Message); }
    }

    private void LoadScene()
    {
        try
        {
            using var stream = OpenFileInput("MainScene.shax.json");
            using var reader = new System.IO.StreamReader(stream);
            var restored = SceneSerializer.Deserialize(reader.ReadToEnd());
            var savedCube = restored.Entities.FirstOrDefault(e => e.Name == "Cube");
            if (savedCube != null && _cubeEntity != null)
            {
                _cubeEntity.Transform.LocalPosition = savedCube.Transform.LocalPosition;
                _cubeEntity.Transform.LocalRotation = savedCube.Transform.LocalRotation;
                _cubeEntity.Transform.LocalScale = savedCube.Transform.LocalScale;
            }
        }
        catch (Java.IO.FileNotFoundException) { }
        catch (Exception ex) { SetStatus("Scene load failed: " + ex.Message); }
    }

    private void SetStatus(string message)
    {
        if (_status != null) _status.Text = message;
        if (_consolePanel != null) _consolePanel.Text = "CONSOLE\n" + message;
    }

    protected override void OnPause()
    {
        try { SaveScene(); SaveScript(); } catch { }
        _viewport?.OnPause();
        base.OnPause();
    }
    protected override void OnResume() { base.OnResume(); _viewport?.OnResume(); }

    private sealed class PreviewSpinBehaviour : ShaxBehaviour
    {
        public override void Update(float deltaTime) => Transform.Rotate(Vector3.UnitY, 32f * deltaTime);
    }

    private sealed class ClickAction : Java.Lang.Object, View.IOnClickListener
    {
        private readonly Action _action;
        public ClickAction(Action action) => _action = action;
        public void OnClick(View? v) => _action();
    }
}
