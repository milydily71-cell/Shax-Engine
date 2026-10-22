using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using Shax;

namespace Shax.Android;

[Activity(Label = "Shax Engine", MainLauncher = true, Exported = true,
    ConfigurationChanges = Android.Content.PM.ConfigChanges.Orientation |
                           Android.Content.PM.ConfigChanges.ScreenSize |
                           Android.Content.PM.ConfigChanges.KeyboardHidden)]
public sealed class MainActivity : Activity
{
    private LinearLayout? _root;
    private FrameLayout? _workspace;
    private ShaxViewport? _viewport;
    private EditText? _scriptEditor;
    private TextView? _status;
    private readonly Scene _scene = Engine.CreateScene("Main Scene");

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        Window?.SetFlags(WindowManagerFlags.KeepScreenOn, WindowManagerFlags.KeepScreenOn);
        _scene.CreateEntity("Cube");
        BuildEditorShell();
    }

    private void BuildEditorShell()
    {
        _root = new LinearLayout(this) { Orientation = Orientation.Vertical };
        _root.SetBackgroundColor(Android.Graphics.Color.Rgb(18, 22, 30));
        var toolbar = new LinearLayout(this) { Orientation = Orientation.Horizontal };
        toolbar.SetPadding(8, 4, 8, 4);
        toolbar.AddView(Label("SHAX ENGINE", 15, true), new LinearLayout.LayoutParams(0, -1, 1));
        AddAction(toolbar, "Scene", () => ShowWorkspace(false));
        AddAction(toolbar, "Script IDE", () => ShowWorkspace(true));
        AddAction(toolbar, "Play", () =>
        {
            _scene.Tick(1f / 60f);
            if (_status != null) _status.Text = "C# Scene.Tick executed";
        });
        _root.AddView(toolbar, new LinearLayout.LayoutParams(-1, -2));

        _workspace = new FrameLayout(this);
        _root.AddView(_workspace, new LinearLayout.LayoutParams(-1, 0, 1));
        _viewport = new ShaxViewport(this);
        _workspace.AddView(_viewport, new FrameLayout.LayoutParams(-1, -1));

        _scriptEditor = new EditText(this)
        {
            Text = ScriptDocument.DefaultSource,
            Gravity = GravityFlags.Top | GravityFlags.Left,
            Typeface = Android.Graphics.Typeface.Monospace,
            TextSize = 13
        };
        _scriptEditor.SetTextColor(Android.Graphics.Color.Rgb(225, 233, 245));
        _scriptEditor.SetBackgroundColor(Android.Graphics.Color.Rgb(25, 31, 42));
        _scriptEditor.Visibility = ViewStates.Gone;
        _workspace.AddView(_scriptEditor, new FrameLayout.LayoutParams(-1, -1));

        _status = Label("NET 10 • C# CORE LINKED • GLES VIEWPORT", 11, false);
        _status.SetPadding(10, 6, 10, 6);
        _root.AddView(_status, new LinearLayout.LayoutParams(-1, -2));
        SetContentView(_root);
    }

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

    private void ShowWorkspace(bool script)
    {
        if (_viewport != null) _viewport.Visibility = script ? ViewStates.Gone : ViewStates.Visible;
        if (_scriptEditor != null) _scriptEditor.Visibility = script ? ViewStates.Visible : ViewStates.Gone;
        if (_status != null) _status.Text = script
            ? "SCRIPT IDE • EDITABLE C# BUFFER • COMPILER NOT YET CONNECTED"
            : "SCENE • LIVE VIEWPORT • C# CORE REFERENCED";
    }

    protected override void OnPause() { _viewport?.OnPause(); base.OnPause(); }
    protected override void OnResume() { base.OnResume(); _viewport?.OnResume(); }

    private sealed class ClickAction : Java.Lang.Object, View.IOnClickListener
    {
        private readonly Action _action;
        public ClickAction(Action action) => _action = action;
        public void OnClick(View? v) => _action();
    }
}
