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
    private Button? _importGlbButton;
    private const int RequestImportGlb = 4107;
    private TextView? _consolePanel;
    private LinearLayout? _materialPanel;
    private TextView? _roughnessValue, _metallicValue;
    private SeekBar? _roughnessSeek, _metallicSeek;
    private EditText? _positionX, _positionY, _positionZ;
    private EditText? _rotationX, _rotationY, _rotationZ;
    private EditText? _scaleX, _scaleY, _scaleZ;
    private readonly Stack<Vector3> _undo = new();
    private readonly Stack<Vector3> _redo = new();
    private bool _highlighting;
    private RuntimeScriptBehaviour? _runtimeScript;
    private bool _forceGles30;
    private string _currentScriptName = "PlayerScript.cs";
    private Button? _scriptProjectButton;
    private readonly PbrMaterialSettings _materialSettings = new();

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);
        _forceGles30 = Intent?.GetBooleanExtra("shax.forceGles30", false) ?? false;
        _cubeEntity = _scene.CreateEntity("Cube");
        _cubeEntity.Transform.LocalPosition = new Vector3(0f, 0.5f, 0f);
        try { var loaded = PbrMaterialStore.Load(Path.Combine(FilesDir!.AbsolutePath, "PbrMaterial.shax.json")); CopyMaterial(loaded); } catch (Exception ex) { global::Android.Util.Log.Warn("ShaxEngine", "Using default material settings: " + ex.Message); }
        BuildEditorShell();
        _viewport?.SetMaterial(_materialSettings.Roughness, _materialSettings.Metallic);
        LoadScript();
        LoadScene();
        CompileAndPlay();
    }

    private void BuildEditorShell()
    {
        _root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        _root.SetBackgroundColor(global::Android.Graphics.Color.Rgb(18, 22, 30));
        var toolbarScroll = new HorizontalScrollView(this) { HorizontalScrollBarEnabled = true };
        var toolbar = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        toolbar.SetPadding(5, 3, 5, 3);
        toolbar.AddView(Label("SHAX", 15, true), new LinearLayout.LayoutParams(-2, -1));
        AddAction(toolbar, "Scene", () => ShowPanel("scene"));
        AddAction(toolbar, "Script IDE", () => ShowPanel("script"));
        AddAction(toolbar, "Assets", () => ShowPanel("assets"));
        AddAction(toolbar, "Material", () => ShowPanel("material"));
        AddAction(toolbar, "Console", () => ShowPanel("console"));
        AddAction(toolbar, "Save", SaveScript);
        AddAction(toolbar, "Save Scene", SaveScene);
        AddAction(toolbar, "Save Material", SaveMaterialSettings);
        AddAction(toolbar, "Undo", UndoTransform);
        AddAction(toolbar, "Redo", RedoTransform);
        AddAction(toolbar, "Play", CompileAndPlay);
        AddAction(toolbar, "Stop", StopScript);
        toolbarScroll.AddView(toolbar);
        _root.AddView(toolbarScroll, new LinearLayout.LayoutParams(-1, -2));

        _workspace = new FrameLayout(this);
        _root.AddView(_workspace, new LinearLayout.LayoutParams(-1, 0, 1));
        _viewport = new ShaxViewport(this, _scene, _cubeEntity!, _forceGles30);
        _workspace.AddView(_viewport, new FrameLayout.LayoutParams(-1, -1));

        _scriptPanel = new LinearLayout(this) { Orientation = Orientation.Vertical };
        _scriptPanel.SetBackgroundColor(global::Android.Graphics.Color.Rgb(25, 31, 42));
        _scriptEditor = new EditText(this)
        {
            Text = ScriptDocument.DefaultSource,
            Gravity = GravityFlags.Top | GravityFlags.Left,
            Typeface = global::Android.Graphics.Typeface.Monospace,
            TextSize = 13,
        };
        _scriptEditor.SetTextColor(global::Android.Graphics.Color.Rgb(225, 233, 245));
        _scriptEditor.SetBackgroundColor(global::Android.Graphics.Color.Rgb(25, 31, 42));
        _scriptEditor.SetPadding(12, 12, 12, 12);
        var scriptProjectRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        _scriptProjectButton = new Button(this) { Text = "Project: PlayerScript.cs", TextSize = 10 };
        _scriptProjectButton.SetOnClickListener(new ClickAction(ShowScriptPicker));
        scriptProjectRow.AddView(_scriptProjectButton, new LinearLayout.LayoutParams(0, -2, 1));
        var newScriptButton = new Button(this) { Text = "+ New", TextSize = 10 };
        newScriptButton.SetOnClickListener(new ClickAction(CreateScriptDialog));
        scriptProjectRow.AddView(newScriptButton, new LinearLayout.LayoutParams(-2, -2));
        _scriptPanel.AddView(scriptProjectRow, new LinearLayout.LayoutParams(-1, -2));
        _scriptPanel.AddView(_scriptEditor, new LinearLayout.LayoutParams(-1, 0, 1));
        _diagnostics = Label("Diagnostics: waiting for edits", 11, false);
        _diagnostics.SetPadding(10, 6, 10, 6);
        _scriptPanel.AddView(_diagnostics, new LinearLayout.LayoutParams(-1, -2));
        _scriptEditor.TextChanged += (_, _) => { ApplySyntaxHighlighting(); UpdateDiagnostics(); };
        _workspace.AddView(_scriptPanel, new FrameLayout.LayoutParams(-1, -1));
        _scriptPanel.Visibility = ViewStates.Gone;

        _hierarchyPanel = new LinearLayout(this) { Orientation = Orientation.Vertical };
        _hierarchyPanel.SetPadding(12, 10, 12, 10);
        _hierarchyPanel.SetBackgroundColor(global::Android.Graphics.Color.Rgb(24, 30, 40));
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
        _hierarchyPanel.AddView(Label("ROTATION (DEGREES)", 11, true));
        var rotationRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        _rotationX = NumberField("X°"); _rotationY = NumberField("Y°"); _rotationZ = NumberField("Z°");
        rotationRow.AddView(_rotationX, new LinearLayout.LayoutParams(0, -2, 1));
        rotationRow.AddView(_rotationY, new LinearLayout.LayoutParams(0, -2, 1));
        rotationRow.AddView(_rotationZ, new LinearLayout.LayoutParams(0, -2, 1));
        _hierarchyPanel.AddView(rotationRow);
        _hierarchyPanel.AddView(Label("LOCAL SCALE", 11, true));
        var scaleRow = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        _scaleX = NumberField("X"); _scaleY = NumberField("Y"); _scaleZ = NumberField("Z");
        scaleRow.AddView(_scaleX, new LinearLayout.LayoutParams(0, -2, 1));
        scaleRow.AddView(_scaleY, new LinearLayout.LayoutParams(0, -2, 1));
        scaleRow.AddView(_scaleZ, new LinearLayout.LayoutParams(0, -2, 1));
        _hierarchyPanel.AddView(scaleRow);
        AddAction(_hierarchyPanel, "Apply Transform", ApplyTransform);
        _workspace.AddView(_hierarchyPanel, new FrameLayout.LayoutParams(-1, 420, GravityFlags.Bottom));
        _hierarchyPanel.Visibility = ViewStates.Gone;

        _assetPanel = Label("ASSET BROWSER\n\nScripts/\n  PlayerScript.cs\n\nScenes/\n  MainScene.shax.json\n\nTextures/\n  Image import pending\n\nModels/\n  GLB/glTF importer pending", 14, false);
        _assetPanel.SetPadding(16, 16, 16, 16);
        _assetPanel.SetBackgroundColor(global::Android.Graphics.Color.Rgb(24, 30, 40));
        _workspace.AddView(_assetPanel, new FrameLayout.LayoutParams(-1, -1));
        _importGlbButton = new Button(this) { Text = "Import GLB model…" };
        _importGlbButton.SetOnClickListener(new ClickAction(PickGlbAsset));
        _workspace.AddView(_importGlbButton, new FrameLayout.LayoutParams(-2, -2, GravityFlags.Bottom | GravityFlags.CenterHorizontal));
        _assetPanel.Visibility = ViewStates.Gone;
        _importGlbButton.Visibility = ViewStates.Gone;

        _consolePanel = Label("CONSOLE\nShax Engine starting…", 12, false);
        _consolePanel.SetPadding(16, 16, 16, 16);
        _consolePanel.SetBackgroundColor(global::Android.Graphics.Color.Rgb(15, 19, 26));
        _workspace.AddView(_consolePanel, new FrameLayout.LayoutParams(-1, -1));
        _consolePanel.Visibility = ViewStates.Gone;

        _materialPanel = new LinearLayout(this) { Orientation = Orientation.Vertical };
        _materialPanel.SetPadding(18, 16, 18, 16);
        _materialPanel.SetBackgroundColor(global::Android.Graphics.Color.Rgb(24, 30, 40));
        _materialPanel.AddView(Label("PBR MATERIAL INSPECTOR", 15, true));
        _materialPanel.AddView(Label("Metallic-roughness Cook-Torrance BRDF with procedural environment-cubemap IBL", 12, false));
        _roughnessValue = Label("Roughness: 0.28", 13, false);
        _materialPanel.AddView(_roughnessValue);
        _roughnessSeek = new SeekBar(this) { Max = 96, Progress = Math.Clamp((int)MathF.Round(_materialSettings.Roughness * 96f), 4, 96) };
        _roughnessSeek.ProgressChanged += (_, e) => { if (e.FromUser) { var value = Math.Clamp(e.Progress / 96f, 0.04f, 1f); _materialSettings.Roughness = value; if (_roughnessValue != null) _roughnessValue.Text = $"Roughness: {value:0.00}"; _viewport?.SetMaterial(value, _materialSettings.Metallic); } };
        _materialPanel.AddView(_roughnessSeek);
        _metallicValue = Label("Metallic: 0.08", 13, false);
        _materialPanel.AddView(_metallicValue);
        _metallicSeek = new SeekBar(this) { Max = 100, Progress = Math.Clamp((int)MathF.Round(_materialSettings.Metallic * 100f), 0, 100) };
        _metallicSeek.ProgressChanged += (_, e) => { if (e.FromUser) { var value = e.Progress / 100f; _materialSettings.Metallic = value; if (_metallicValue != null) _metallicValue.Text = $"Metallic: {value:0.00}"; _viewport?.SetMaterial(_materialSettings.Roughness, value); } };
        _materialPanel.AddView(_metallicSeek);
        _workspace.AddView(_materialPanel, new FrameLayout.LayoutParams(-1, -2, GravityFlags.Top));
        _materialPanel.Visibility = ViewStates.Gone;

        _status = Label("NET 10 • C# CORE • NATIVE GLES 3.2 / 3.0", 11, false);
        _status.SetPadding(10, 6, 10, 6);
        _root.AddView(_status, new LinearLayout.LayoutParams(-1, -2));
        SetContentView(_root);
        RefreshInspector();
    }

    private EditText NumberField(string label) => new(this)
    {
        Hint = label,
        InputType = global::Android.Text.InputTypes.ClassNumber | global::Android.Text.InputTypes.NumberFlagDecimal | global::Android.Text.InputTypes.NumberFlagSigned,
        TextSize = 14
    };

    private TextView Label(string text, float size, bool bold)
    {
        var label = new TextView(this) { Text = text, TextSize = size, Gravity = GravityFlags.CenterVertical };
        label.SetTextColor(global::Android.Graphics.Color.Rgb(225, 233, 245));
        if (bold) label.SetTypeface(global::Android.Graphics.Typeface.Default, global::Android.Graphics.TypefaceStyle.Bold);
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
        if (_importGlbButton != null) _importGlbButton.Visibility = panel == "assets" ? ViewStates.Visible : ViewStates.Gone;
        if (_consolePanel != null) _consolePanel.Visibility = panel == "console" ? ViewStates.Visible : ViewStates.Gone;
        if (_materialPanel != null) _materialPanel.Visibility = panel == "material" ? ViewStates.Visible : ViewStates.Gone;
        if (panel == "scene") RefreshInspector();
        SetStatus(panel switch {
            "scene" => "Scene • hierarchy, transform inspector and live viewport",
            "script" => "Script IDE • restricted AOT-safe C# subset and diagnostics",
            "assets" => "Asset browser • project tree and GLB importer APIs",
            "material" => "Material • live PBR roughness and metallic controls",
            _ => "Console • native and editor messages"
        });
    }


    private void PickGlbAsset()
    {
        var intent = new Intent(Intent.ActionOpenDocument);
        intent.AddCategory(Intent.CategoryOpenable);
        intent.SetType("model/gltf-binary");
        StartActivityForResult(Intent.CreateChooser(intent, "Import GLB model"), RequestImportGlb);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        if (requestCode != RequestImportGlb || resultCode != Result.Ok || data?.Data is null) return;
        var staging = Path.Combine(CacheDir!.AbsolutePath, "shax-import-" + Guid.NewGuid().ToString("N") + ".glb");
        try
        {
            using (var input = ContentResolver!.OpenInputStream(data.Data))
            using (var output = File.Create(staging))
            {
                if (input is null) throw new InvalidOperationException("Android could not open the selected document.");
                input.CopyTo(output);
            }
            var imported = GlbAssetImporter.Import(staging, Path.Combine(FilesDir!.AbsolutePath, "Assets"));
            if (_assetPanel != null)
                _assetPanel.Text = $"ASSET BROWSER\n\nImported: {Path.GetFileName(imported.AssetPath)}\nSize: {imported.FileSize:N0} bytes\\nMeshes: {imported.MeshCount}\nMaterials: {imported.MaterialCount}\nTextures: {imported.TextureCount}\nImages: {imported.ImageCount}\nNodes: {imported.NodeCount}\nAnimations: {imported.AnimationCount}\n\nGLB 2.0 container validated and copied into app-private Assets/. Mesh decoding/GPU upload is not yet connected.";
            SetStatus($"GLB metadata validated: {imported.MeshCount} mesh records, {imported.MaterialCount} materials. Geometry upload is not implemented yet.");
        }
        catch (Exception ex)
        {
            SetStatus("GLB import failed: " + ex.Message);
            global::Android.Util.Log.Error("ShaxEngine", "GLB import failed: " + ex);
        }
        finally { if (File.Exists(staging)) File.Delete(staging); }
    }

    private void RefreshInspector()
    {
        if (_cubeEntity is null) return;
        if (_positionX != null) _positionX.Text = _cubeEntity.Transform.LocalPosition.X.ToString("0.###");
        if (_positionY != null) _positionY.Text = _cubeEntity.Transform.LocalPosition.Y.ToString("0.###");
        if (_positionZ != null) _positionZ.Text = _cubeEntity.Transform.LocalPosition.Z.ToString("0.###");
        var euler = _cubeEntity.Transform.LocalRotation;
        if (_rotationX != null) _rotationX.Text = (MathF.Atan2(2f * (euler.W * euler.X + euler.Y * euler.Z), 1f - 2f * (euler.X * euler.X + euler.Y * euler.Y)) * 180f / MathF.PI).ToString("0.###");
        if (_rotationY != null) _rotationY.Text = (MathF.Asin(Math.Clamp(2f * (euler.W * euler.Y - euler.Z * euler.X), -1f, 1f)) * 180f / MathF.PI).ToString("0.###");
        if (_rotationZ != null) _rotationZ.Text = (MathF.Atan2(2f * (euler.W * euler.Z + euler.X * euler.Y), 1f - 2f * (euler.Y * euler.Y + euler.Z * euler.Z)) * 180f / MathF.PI).ToString("0.###");
        if (_scaleX != null) _scaleX.Text = _cubeEntity.Transform.LocalScale.X.ToString("0.###");
        if (_scaleY != null) _scaleY.Text = _cubeEntity.Transform.LocalScale.Y.ToString("0.###");
        if (_scaleZ != null) _scaleZ.Text = _cubeEntity.Transform.LocalScale.Z.ToString("0.###");
    }

    private void ApplyTransform()
    {
        if (_cubeEntity is null) return;
        if (!float.TryParse(_positionX?.Text, out var x) || !float.TryParse(_positionY?.Text, out var y) || !float.TryParse(_positionZ?.Text, out var z)
            || !float.TryParse(_rotationX?.Text, out var rx) || !float.TryParse(_rotationY?.Text, out var ry) || !float.TryParse(_rotationZ?.Text, out var rz)
            || !float.TryParse(_scaleX?.Text, out var sx) || !float.TryParse(_scaleY?.Text, out var sy) || !float.TryParse(_scaleZ?.Text, out var sz))
        {
            SetStatus("Transform error: enter valid numeric position, rotation and scale values."); return;
        }
        _undo.Push(_cubeEntity.Transform.LocalPosition); _redo.Clear();
        _cubeEntity.Transform.LocalPosition = new Vector3(x, y, z);
        const float radians = MathF.PI / 180f;
        _cubeEntity.Transform.LocalRotation = Quaternion.Normalize(Quaternion.CreateFromYawPitchRoll(ry * radians, rx * radians, rz * radians));
        _cubeEntity.Transform.LocalScale = new Vector3(sx, sy, sz);
        SetStatus("Position, rotation and scale applied to the live native viewport.");
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

    private void StopScript()
    {
        if (_runtimeScript != null) _runtimeScript.Program = null;
        SetStatus("Script stopped.");
    }

    private void CompileAndPlay()
    {
        if (_cubeEntity is null || _scriptEditor is null) return;
        var result = CSharpSubsetRuntime.Compile(_scriptEditor.Text?.ToString() ?? string.Empty);
        if (_diagnostics != null)
            _diagnostics.Text = string.Join("\n", result.Diagnostics.Select(d => $"{d.Severity} L{d.Line}:{d.Column} — {d.Message}"));
        if (!result.Success || result.Program is null)
        {
            ShowPanel("script");
            SetStatus("Script rejected by the safe subset; see diagnostics.");
            return;
        }
        if (_runtimeScript is null)
        {
            _runtimeScript = new RuntimeScriptBehaviour(result.Program, SetStatus);
            _cubeEntity.AddComponent(_runtimeScript);
        }
        else _runtimeScript.Program = result.Program;
        ShowPanel("scene");
        SetStatus("AOT-safe Shax C# subset compiled and running.");
        global::Android.Util.Log.Info("ShaxEngine", "AOT-safe Shax C# subset compiled and running.");
    }

    private void ApplySyntaxHighlighting()
    {
        if (_highlighting || _scriptEditor is null) return;
        var editable = _scriptEditor.EditableText;
        _highlighting = true;
        try
        {
            var existing = editable.GetSpans(0, editable.Length(), Java.Lang.Class.FromType(typeof(ForegroundColorSpan)));
            foreach (var span in existing) editable.RemoveSpan(span);
            var source = editable.ToString() ?? string.Empty;
            var keywords = new Regex(@"\b(using|namespace|public|private|protected|class|sealed|override|virtual|void|float|int|return|new|this|var)\b");
            foreach (Match match in keywords.Matches(source))
                editable.SetSpan(new ForegroundColorSpan(global::Android.Graphics.Color.Rgb(112, 174, 255)), match.Index, match.Index + match.Length, SpanTypes.ExclusiveExclusive);
            var comments = new Regex(@"//[^\r\n]*");
            foreach (Match match in comments.Matches(source))
                editable.SetSpan(new ForegroundColorSpan(global::Android.Graphics.Color.Rgb(117, 165, 127)), match.Index, match.Index + match.Length, SpanTypes.ExclusiveExclusive);
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
            ? "Diagnostics: no basic delimiter errors • press Play to compile safe subset"
            : "Diagnostics: " + string.Join(", ", errors);
    }

    private string ScriptDirectory => Path.Combine(FilesDir!.AbsolutePath, "Scripts");

    private void LoadScript() => LoadScriptFile(_currentScriptName);

    private void LoadScriptFile(string name)
    {
        try
        {
            Directory.CreateDirectory(ScriptDirectory);
            var path = Path.Combine(ScriptDirectory, name);
            if (!File.Exists(path)) WritePrivateFileAtomically(Path.Combine("Scripts", name), ScriptDocument.DefaultSource);
            if (_scriptEditor != null) _scriptEditor.Text = File.ReadAllText(path);
            if (_scriptProjectButton != null) _scriptProjectButton.Text = "Project: " + name;
            ApplySyntaxHighlighting(); UpdateDiagnostics();
            SetStatus("Opened project script: " + name);
        }
        catch (Exception ex) { SetStatus("Script load failed: " + ex.Message); }
    }

    private void ShowScriptPicker()
    {
        try
        {
            Directory.CreateDirectory(ScriptDirectory);
            var files = Directory.GetFiles(ScriptDirectory, "*.cs").Select(Path.GetFileName).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
            if (files.Length == 0) files = new[] { "PlayerScript.cs" };
            new AlertDialog.Builder(this).SetTitle("Project scripts")
                .SetItems(files, (_, e) => { _currentScriptName = files[e.Which]; LoadScriptFile(_currentScriptName); })
                .SetNeutralButton("New Script", (_, _) => CreateScriptDialog())
                .SetNegativeButton("Cancel", (_, _) => { }).Show();
        }
        catch (Exception ex) { SetStatus("Project tree failed: " + ex.Message); }
    }

    private void CreateScriptDialog()
    {
        var input = new EditText(this) { Hint = "Example.cs", SingleLine = true };
        new AlertDialog.Builder(this).SetTitle("Create C# script").SetView(input)
            .SetNegativeButton("Cancel", (_, _) => { })
            .SetPositiveButton("Create", (_, _) =>
            {
                var name = input.Text?.Trim() ?? string.Empty;
                if (!name.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) name += ".cs";
                if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name) || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                { SetStatus("Invalid script filename."); return; }
                var path = Path.Combine(ScriptDirectory, name);
                if (File.Exists(path)) { SetStatus("Script already exists: " + name); return; }
                WritePrivateFileAtomically(Path.Combine("Scripts", name), ScriptDocument.DefaultSource);
                _currentScriptName = name; LoadScriptFile(name);
            }).Show();
    }


    private void CopyMaterial(PbrMaterialSettings source)
    {
        _materialSettings.Name = source.Name;
        _materialSettings.BaseColorR = source.BaseColorR; _materialSettings.BaseColorG = source.BaseColorG;
        _materialSettings.BaseColorB = source.BaseColorB; _materialSettings.BaseColorA = source.BaseColorA;
        _materialSettings.Metallic = source.Metallic; _materialSettings.Roughness = source.Roughness;
        _materialSettings.AmbientOcclusion = source.AmbientOcclusion; _materialSettings.NormalScale = source.NormalScale;
    }

    private void SaveMaterialSettings()
    {
        try
        {
            PbrMaterialStore.SaveAtomic(Path.Combine(FilesDir!.AbsolutePath, "PbrMaterial.shax.json"), _materialSettings);
            SetStatus("PBR material saved atomically to app-private storage.");
        }
        catch (Exception ex) { SetStatus("Material save failed: " + ex.Message); }
    }

    private void WritePrivateFileAtomically(string name, string content)
    {
        var directory = FilesDir!.AbsolutePath;
        Directory.CreateDirectory(directory);
        var destination = Path.Combine(directory, name);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, content, new System.Text.UTF8Encoding(false));
            File.Move(temporary, destination, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private void SaveScript()
    {
        try
        {
            if (_scriptEditor == null) return;
            WritePrivateFileAtomically(Path.Combine("Scripts", _currentScriptName), _scriptEditor.Text?.ToString() ?? string.Empty);
            SetStatus("Saved " + _currentScriptName + " atomically to app-private project storage.");
        }
        catch (Exception ex) { SetStatus("Script save failed: " + ex.Message); }
    }

    private void SaveScene()
    {
        try
        {
            WritePrivateFileAtomically("MainScene.shax.json", SceneSerializer.Serialize(_scene));
            SetStatus("Saved scene JSON atomically with stable entity IDs.");
        }
        catch (Exception ex) { SetStatus("Scene save failed: " + ex.Message); }
    }

    private void LoadScene()
    {
        try
        {
            using var stream = OpenFileInput("MainScene.shax.json") ?? throw new System.IO.IOException("Could not open scene file.");
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
        try { SaveScene(); SaveScript(); SaveMaterialSettings(); } catch { }
        _viewport?.OnPause();
        base.OnPause();
    }
    protected override void OnResume() { base.OnResume(); _viewport?.OnResume(); }

    private sealed class RuntimeScriptBehaviour : ShaxBehaviour
    {
        private readonly Action<string> _onError;
        public ScriptProgram? Program { get; set; }
        public RuntimeScriptBehaviour(ScriptProgram program, Action<string> onError)
        {
            Program = program; _onError = onError;
        }
        public override void Update(float deltaTime)
        {
            try { Program?.Execute(this, deltaTime); }
            catch (Exception ex)
            {
                Program = null;
                _onError("Script runtime stopped safely: " + ex.Message);
            }
        }
    }

    private sealed class ClickAction : Java.Lang.Object, View.IOnClickListener
    {
        private readonly Action _action;
        public ClickAction(Action action) => _action = action;
        public void OnClick(View? v) => _action();
    }
}
