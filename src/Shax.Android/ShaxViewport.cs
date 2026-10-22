using Android.Content;
using Android.Opengl;
using Android.Views;
using Javax.Microedition.Khronos.Egl;
using Javax.Microedition.Khronos.Opengles;

namespace Shax.Android;

/// <summary>Initial viewport baseline. The driver version is queried for future ES 3.2 feature gating.</summary>
public sealed class ShaxViewport : GLSurfaceView
{
    public ShaxViewport(Context context) : base(context)
    {
        SetEGLContextClientVersion(3);
        SetRenderer(new ViewportRenderer());
        RenderMode = Rendermode.Continuously;
        PreserveEGLContextOnPause = true;
    }

    private sealed class ViewportRenderer : Java.Lang.Object, IRenderer
    {
        private int _program;
        private int _width = 1, _height = 1;
        private long _start;
        private const string Vertex = "#version 300 es\nlayout(location=0) in vec3 aPosition; uniform mat4 uMvp; void main(){ gl_Position=uMvp*vec4(aPosition,1.0); }";
        private const string Fragment = "#version 300 es\nprecision mediump float; out vec4 color; uniform vec3 uColor; void main(){ color=vec4(uColor,1.0); }";

        public void OnSurfaceCreated(IGL10? gl, EGLConfig? config)
        {
            var version = GLES30.GlGetString(GLES30.GlVersion) ?? "OpenGL ES";
            Android.Util.Log.Info("ShaxEngine", "Viewport driver: " + version);
            _program = Link(Vertex, Fragment);
            GLES30.GlEnable(GLES30.GlDepthTest);
            GLES30.GlClearColor(0.035f, 0.055f, 0.09f, 1f);
            _start = Java.Lang.JavaSystem.CurrentTimeMillis();
        }

        public void OnSurfaceChanged(IGL10? gl, int width, int height)
        {
            _width = Math.Max(1, width); _height = Math.Max(1, height);
            GLES30.GlViewport(0, 0, _width, _height);
        }

        public void OnDrawFrame(IGL10? gl)
        {
            GLES30.GlClear(GLES30.GlColorBufferBit | GLES30.GlDepthBufferBit);
            GLES30.GlUseProgram(_program);
            var mvpLocation = GLES30.GlGetUniformLocation(_program, "uMvp");
            var mvp = new float[16];
            Matrix.SetIdentityM(mvp, 0);
            Matrix.TranslateM(mvp, 0, 0f, 0f, -3f);
            Matrix.RotateM(mvp, 0, (Java.Lang.JavaSystem.CurrentTimeMillis() - _start) * 0.025f, 0.4f, 1f, 0.2f);
            Matrix.PerspectiveM(mvp, 0, 55f, (float)_width / _height, 0.1f, 100f);
            GLES30.GlUniformMatrix4fv(mvpLocation, 1, false, mvp, 0);
            GLES30.GlUniform3f(GLES30.GlGetUniformLocation(_program, "uColor"), 0.2f, 0.72f, 0.95f);
            float[] points = {
                -0.6f,-0.6f,0f, 0.6f,-0.6f,0f, 0f,0.6f,0f,
                -0.6f,-0.6f,0f, 0f,0.6f,0f, -0.6f,0.6f,0f,
                0.6f,-0.6f,0f, 0.6f,0.6f,0f, 0f,0.6f,0f,
                -0.6f,0.6f,0f, 0f,0.6f,0f, 0.6f,0.6f,0f
            };
            using var vertices = Java.Nio.ByteBuffer.AllocateDirect(points.Length * 4)
                .Order(Java.Nio.ByteOrder.NativeOrder()).AsFloatBuffer();
            vertices.Put(points);
            vertices.Position(0);
            GLES30.GlEnableVertexAttribArray(0);
            GLES30.GlVertexAttribPointer(0, 3, GLES30.GlFloat, false, 0, vertices);
            GLES30.GlDrawArrays(GLES30.GlTriangles, 0, points.Length / 3);
            GLES30.GlDisableVertexAttribArray(0);
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
