using System.Runtime.InteropServices;
using Android.Content;
using Android.Opengl;
using Android.Views;
using Javax.Microedition.Khronos.Egl;
using EGLConfig = Javax.Microedition.Khronos.Egl.EGLConfig;
using EGLContext = Javax.Microedition.Khronos.Egl.EGLContext;
using EGLDisplay = Javax.Microedition.Khronos.Egl.EGLDisplay;
using IEGL10 = Javax.Microedition.Khronos.Egl.IEGL10;
using GLMatrix = Android.Opengl.Matrix;
using Javax.Microedition.Khronos.Opengles;
using Shax;

namespace Shax.Android;

/// <summary>Native C++ GLES viewport. Requests ES 3.2 and falls back directly to ES 3.0.</summary>
public sealed class ShaxViewport : GLSurfaceView
{
    private readonly ViewportRenderer _renderer;
    private readonly global::Android.Views.ScaleGestureDetector _scaleDetector;
    private float _previousX, _previousY;
    private bool _wasScaling;

    public ShaxViewport(Context context, Scene scene, Entity cubeEntity) : base(context)
    {
        _renderer = new ViewportRenderer(scene, cubeEntity);
        _scaleDetector = new global::Android.Views.ScaleGestureDetector(context, new ScaleGestureListener { OnScaleAction = factor => QueueEvent(() => _renderer.Zoom(factor)) });
        SetEGLContextClientVersion(3);
        SetEGLConfigChooser(8, 8, 8, 8, 24, 8);
        SetEGLContextFactory(new Es32OrEs30ContextFactory());
        SetRenderer(_renderer);
        RenderMode = Rendermode.Continuously;
        PreserveEGLContextOnPause = false;
        KeepScreenOn = true;
    }

    public override bool OnTouchEvent(MotionEvent? e)
    {
        if (e is null) return false;
        _scaleDetector.OnTouchEvent(e);
        float x = e.GetX(), y = e.GetY();
        if (e.ActionMasked == MotionEventActions.Move && e.PointerCount == 1 && !_scaleDetector.IsInProgress)
        {
            var dx = x - _previousX; var dy = y - _previousY;
            if (!_wasScaling) QueueEvent(() => _renderer.Orbit(dx, dy));
        }
        _wasScaling = _scaleDetector.IsInProgress;
        _previousX = x; _previousY = y;
        return true;
    }

    private sealed class Es32OrEs30ContextFactory : Java.Lang.Object, GLSurfaceView.IEGLContextFactory
    {
        public EGLContext? CreateContext(IEGL10? egl, EGLDisplay? display, EGLConfig? config)
        {
            if (egl is null || display is null || config is null)
                throw new InvalidOperationException("EGL is not ready.");

            EGLContext? TryCreate(int minor)
            {
                var attributes = minor == 0
                    ? new[] { 0x3098, 3, 0x3038 }
                    : new[] { 0x3098, 3, 0x30FB, minor, 0x3038 };
                return egl.EglCreateContext(display, config, IEGL10.EglNoContext, attributes);
            }

            var context = TryCreate(2);
            if (context is not null && !context.Equals(IEGL10.EglNoContext)) return context;
            // Intentional direct fallback: ES 3.1 is not a supported target for this project.
            context = TryCreate(0);
            if (context is null || context.Equals(IEGL10.EglNoContext))
                throw new InvalidOperationException("Shax requires OpenGL ES 3.2 or OpenGL ES 3.0.");
            return context;
        }

        public void DestroyContext(IEGL10? egl, EGLDisplay? display, EGLContext? context)
        {
            if (egl is not null && display is not null && context is not null && !egl.EglDestroyContext(display, context))
                global::Android.Util.Log.Error("ShaxEngine", "Failed to destroy EGL context.");
        }
    }

    private sealed class ViewportRenderer : Java.Lang.Object, GLSurfaceView.IRenderer
    {
        [DllImport("shax", EntryPoint = "shax_init")]
        private static extern int NativeInit();
        [DllImport("shax", EntryPoint = "shax_resize")]
        private static extern void NativeResize(int width, int height);
        [DllImport("shax", EntryPoint = "shax_draw")]
        private static extern void NativeDraw([In] float[] viewProjection, [In] float[] cubeModel, float timeSeconds);
        [DllImport("shax", EntryPoint = "shax_shutdown")]
        private static extern void NativeShutdown();

        private readonly Entity _cubeEntity;
        private readonly Scene _scene;
        private readonly float[] _projection = new float[16];
        private readonly float[] _view = new float[16];
        private readonly float[] _viewProjection = new float[16];
        private readonly float[] _cubeModel = new float[16];
        private int _width = 1, _height = 1;
        private float _yaw = 0.65f, _pitch = 0.35f, _distance = 7.5f;
        private long _startMilliseconds;

        public ViewportRenderer(Scene scene, Entity cubeEntity) { _scene = scene; _cubeEntity = cubeEntity; }

        public void OnSurfaceCreated(IGL10? gl, EGLConfig? config)
        {
            try
            {
                if (NativeInit() == 0) throw new InvalidOperationException("Native GLES renderer initialization failed.");
                global::Android.Util.Log.Info("ShaxEngine", "Native libshax.so renderer initialized.");
                _startMilliseconds = Java.Lang.JavaSystem.CurrentTimeMillis();
                NativeResize(_width, _height);
            }
            catch (DllNotFoundException ex)
            {
                global::Android.Util.Log.Error("ShaxEngine", "libshax.so is missing from the APK: " + ex);
                throw;
            }
        }

        public void OnSurfaceChanged(IGL10? gl, int width, int height)
        {
            _width = Math.Max(1, width); _height = Math.Max(1, height);
            NativeResize(_width, _height);
            GLMatrix.PerspectiveM(_projection, 0, 55f, (float)_width / _height, 0.1f, 1000f);
        }

        public void OnDrawFrame(IGL10? gl)
        {
            _scene.Tick(1f / 60f);
            var targetX = _cubeEntity.Transform.WorldPosition.X;
            var targetY = _cubeEntity.Transform.WorldPosition.Y;
            var targetZ = _cubeEntity.Transform.WorldPosition.Z;
            var cp = MathF.Cos(_pitch);
            var eyeX = targetX + _distance * cp * MathF.Sin(_yaw);
            var eyeY = targetY + _distance * MathF.Sin(_pitch);
            var eyeZ = targetZ + _distance * cp * MathF.Cos(_yaw);
            GLMatrix.SetLookAtM(_view, 0, eyeX, eyeY, eyeZ, targetX, targetY, targetZ, 0f, 1f, 0f);
            GLMatrix.MultiplyMM(_viewProjection, 0, _projection, 0, _view, 0);
            var world = _cubeEntity.Transform.WorldMatrix;
            _cubeModel[0]=world.M11; _cubeModel[1]=world.M12; _cubeModel[2]=world.M13; _cubeModel[3]=world.M14;
            _cubeModel[4]=world.M21; _cubeModel[5]=world.M22; _cubeModel[6]=world.M23; _cubeModel[7]=world.M24;
            _cubeModel[8]=world.M31; _cubeModel[9]=world.M32; _cubeModel[10]=world.M33; _cubeModel[11]=world.M34;
            _cubeModel[12]=world.M41; _cubeModel[13]=world.M42; _cubeModel[14]=world.M43; _cubeModel[15]=world.M44;
            var time = (Java.Lang.JavaSystem.CurrentTimeMillis() - _startMilliseconds) / 1000f;
            NativeDraw(_viewProjection, _cubeModel, time);
        }

        public void Orbit(float dx, float dy)
        {
            _yaw -= dx * 0.008f;
            _pitch = Math.Clamp(_pitch + dy * 0.008f, -1.35f, 1.35f);
        }

        public void Zoom(float factor)
        {
            if (factor <= 0 || float.IsNaN(factor)) return;
            _distance = Math.Clamp(_distance / factor, 2.0f, 35.0f);
        }
    }

    private sealed class ScaleGestureListener : global::Android.Views.ScaleGestureDetector.SimpleOnScaleGestureListener
    {
        public Action<float>? OnScaleAction { get; init; }
        public override bool OnScale(global::Android.Views.ScaleGestureDetector detector)
        {
            OnScaleAction?.Invoke(detector.ScaleFactor);
            return true;
        }
    }
}
