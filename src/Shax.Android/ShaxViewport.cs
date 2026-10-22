using Android.Content;
using Android.Opengl;
using Android.Views;
using Javax.Microedition.Khronos.Egl;
using EGLConfig = Javax.Microedition.Khronos.Egl.EGLConfig;
using EGLContext = Javax.Microedition.Khronos.Egl.EGLContext;
using EGLDisplay = Javax.Microedition.Khronos.Egl.EGLDisplay;
using EGL10 = Javax.Microedition.Khronos.Egl.EGL10;
using IEGL10 = Javax.Microedition.Khronos.Egl.IEGL10;
using GLMatrix = Android.Opengl.Matrix;
using Javax.Microedition.Khronos.Opengles;

namespace Shax.Android;

/// <summary>Perspective mobile viewport with grid, cube, and GLES capability reporting.</summary>
public sealed class ShaxViewport : GLSurfaceView
{
    public ShaxViewport(Context context) : base(context)
    {
        SetEGLContextClientVersion(3);
        SetEGLContextFactory(new Es32ContextFactory());
        SetRenderer(new ViewportRenderer());
        RenderMode = Rendermode.Continuously;
        PreserveEGLContextOnPause = true;
    }

    private sealed class Es32ContextFactory : Java.Lang.Object, GLSurfaceView.IEGLContextFactory
    {
        public EGLContext? CreateContext(IEGL10? egl, EGLDisplay? display, EGLConfig? config)
        {
            if (egl == null || display == null || config == null)
                throw new InvalidOperationException("EGL is not ready.");
            EGLContext? Try(int minor)
            {
                int[] attributes = minor == 0
                    ? new[] { 0x3098, 3, 0x3038 }
                    : new[] { 0x3098, 3, 0x30FB, minor, 0x3038 };
                return egl.EglCreateContext(display, config, IEGL10.EglNoContext, attributes);
            }
            var context = Try(2);
            if (context != null && !context.Equals(IEGL10.EglNoContext)) return context;
            context = Try(1);
            if (context != null && !context.Equals(IEGL10.EglNoContext)) return context;
            context = Try(0);
            if (context == null || context.Equals(IEGL10.EglNoContext))
                throw new InvalidOperationException("Shax requires OpenGL ES 3.0 or newer.");
            return context;
        }

        public void DestroyContext(IEGL10? egl, EGLDisplay? display, EGLContext? context)
        {
            if (egl != null && display != null && context != null && !egl.EglDestroyContext(display, context))
                global::Android.Util.Log.Error("ShaxEngine", "Failed to destroy EGL context.");
        }
    }

    private sealed class ViewportRenderer : Java.Lang.Object, GLSurfaceView.IRenderer
    {
        private int _program;
        private int _width = 1, _height = 1;
        private long _start;
        private readonly float[] _projection = new float[16];
        private readonly float[] _view = new float[16];
        private readonly float[] _vp = new float[16];
        private readonly float[] _model = new float[16];
        private readonly float[] _mvp = new float[16];
        private readonly float[] _cube = BuildCube();
        private readonly float[] _grid = BuildGrid();
        private const string Vertex = "#version 300 es\nlayout(location=0) in vec3 aPosition; uniform mat4 uMvp; void main(){ gl_Position=uMvp*vec4(aPosition,1.0); }";
        private const string Fragment = "#version 300 es\nprecision mediump float; out vec4 color; uniform vec3 uColor; void main(){ color=vec4(uColor,1.0); }";

        public void OnSurfaceCreated(IGL10? gl, EGLConfig? config)
        {
            var version = GLES30.GlGetString(GLES30.GlVersion) ?? "OpenGL ES";
            global::Android.Util.Log.Info("ShaxEngine", "Viewport driver: " + version);
            _program = Link(Vertex, Fragment);
            GLES30.GlEnable(GLES30.GlDepthTest);
            GLES30.GlDepthFunc(GLES30.GlLequal);
            GLES30.GlClearColor(0.035f, 0.055f, 0.09f, 1f);
            _start = Java.Lang.JavaSystem.CurrentTimeMillis();
        }

        public void OnSurfaceChanged(IGL10? gl, int width, int height)
        {
            _width = Math.Max(1, width); _height = Math.Max(1, height);
            GLES30.GlViewport(0, 0, _width, _height);
            GLMatrix.PerspectiveM(_projection, 0, 55f, (float)_width / _height, 0.1f, 100f);
        }

        public void OnDrawFrame(IGL10? gl)
        {
            GLES30.GlClear(GLES30.GlColorBufferBit | GLES30.GlDepthBufferBit);
            GLMatrix.SetLookAtM(_view, 0, 3.6f, 2.8f, 4.8f, 0f, 0.5f, 0f, 0f, 1f, 0f);
            GLMatrix.MultiplyMM(_vp, 0, _projection, 0, _view, 0);
            GLES30.GlUseProgram(_program);

            GLMatrix.SetIdentityM(_model, 0);
            GLMatrix.MultiplyMM(_mvp, 0, _vp, 0, _model, 0);
            Draw(_grid, GLES30.GlLines, _grid.Length / 3, 0.23f, 0.32f, 0.42f);

            GLMatrix.SetIdentityM(_model, 0);
            GLMatrix.TranslateM(_model, 0, 0f, 0.52f, 0f);
            GLMatrix.RotateM(_model, 0, (Java.Lang.JavaSystem.CurrentTimeMillis() - _start) * 0.025f, 0.25f, 1f, 0.15f);
            GLMatrix.MultiplyMM(_mvp, 0, _vp, 0, _model, 0);
            Draw(_cube, GLES30.GlTriangles, _cube.Length / 3, 0.2f, 0.72f, 0.95f);
        }

        private void Draw(float[] points, int mode, int count, float r, float g, float b)
        {
            GLES30.GlUniformMatrix4fv(GLES30.GlGetUniformLocation(_program, "uMvp"), 1, false, _mvp, 0);
            GLES30.GlUniform3f(GLES30.GlGetUniformLocation(_program, "uColor"), r, g, b);
            using var vertices = Java.Nio.ByteBuffer.AllocateDirect(points.Length * 4)
                .Order(Java.Nio.ByteOrder.NativeOrder()).AsFloatBuffer();
            vertices.Put(points);
            vertices.Position(0);
            GLES30.GlEnableVertexAttribArray(0);
            GLES30.GlVertexAttribPointer(0, 3, GLES30.GlFloat, false, 0, vertices);
            GLES30.GlDrawArrays(mode, 0, count);
            GLES30.GlDisableVertexAttribArray(0);
        }

        private static float[] BuildGrid()
        {
            var values = new List<float>();
            for (int i = -10; i <= 10; i++)
            {
                values.Add(i); values.Add(0f); values.Add(-10f);
                values.Add(i); values.Add(0f); values.Add(10f);
                values.Add(-10f); values.Add(0f); values.Add(i);
                values.Add(10f); values.Add(0f); values.Add(i);
            }
            return values.ToArray();
        }

        private static float[] BuildCube()
        {
            var values = new List<float>();
            void Face(float[] a, float[] b, float[] c, float[] d)
            {
                values.AddRange(a); values.AddRange(b); values.AddRange(c);
                values.AddRange(a); values.AddRange(c); values.AddRange(d);
            }
            float[] p000={-0.5f,-0.5f,-0.5f}, p100={0.5f,-0.5f,-0.5f};
            float[] p110={0.5f,0.5f,-0.5f}, p010={-0.5f,0.5f,-0.5f};
            float[] p001={-0.5f,-0.5f,0.5f}, p101={0.5f,-0.5f,0.5f};
            float[] p111={0.5f,0.5f,0.5f}, p011={-0.5f,0.5f,0.5f};
            Face(p000,p100,p110,p010); Face(p101,p001,p011,p111);
            Face(p001,p000,p010,p011); Face(p100,p101,p111,p110);
            Face(p010,p110,p111,p011); Face(p001,p101,p100,p000);
            return values.ToArray();
        }

        private static int Link(string vertex, string fragment)
        {
            var vs = Compile(GLES30.GlVertexShader, vertex);
            var fs = Compile(GLES30.GlFragmentShader, fragment);
            var program = GLES30.GlCreateProgram();
            GLES30.GlAttachShader(program, vs); GLES30.GlAttachShader(program, fs); GLES30.GlLinkProgram(program);
            var status = new int[1]; GLES30.GlGetProgramiv(program, GLES30.GlLinkStatus, status, 0);
            if (status[0] == 0) throw new InvalidOperationException("Shader link failed: " + GLES30.GlGetProgramInfoLog(program));
            GLES30.GlDeleteShader(vs); GLES30.GlDeleteShader(fs);
            return program;
        }

        private static int Compile(int type, string source)
        {
            var shader = GLES30.GlCreateShader(type);
            GLES30.GlShaderSource(shader, source); GLES30.GlCompileShader(shader);
            var status = new int[1]; GLES30.GlGetShaderiv(shader, GLES30.GlCompileStatus, status, 0);
            if (status[0] == 0) throw new InvalidOperationException("Shader compile failed: " + GLES30.GlGetShaderInfoLog(shader));
            return shader;
        }
    }
}
