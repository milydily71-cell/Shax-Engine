package com.shax.engine;

import android.opengl.GLES30;
import android.opengl.GLSurfaceView;
import android.opengl.Matrix;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.nio.FloatBuffer;
import javax.microedition.khronos.egl.EGLConfig;
import javax.microedition.khronos.opengles.GL10;

final class ShaxRenderer implements GLSurfaceView.Renderer {
    private int skyProgram, solidProgram, gridProgram;
    private FloatBuffer fullscreen, cube, grid;
    private int gridCount;
    private final float[] projection = new float[16], view = new float[16], vp = new float[16], mvp = new float[16], model = new float[16];
    private float yaw = 0.65f, pitch = 0.45f, distance = 7.0f;
    private int width = 1, height = 1;
    private long startNs;
    private String glVersion = "OpenGL ES";

    void orbit(float dx, float dy) { yaw += dx; pitch = Math.max(-1.25f, Math.min(1.25f, pitch + dy)); }
    void zoom(float factor) { if (factor > 0) distance = Math.max(2.5f, Math.min(18f, distance / factor)); }

    @Override public void onSurfaceCreated(GL10 unused, EGLConfig config) {
        glVersion = GLES30.glGetString(GLES30.GL_VERSION);
        skyProgram = program(SKY_VS, SKY_FS);
        solidProgram = program(SOLID_VS, SOLID_FS);
        gridProgram = program(SOLID_VS, SOLID_FS);
        fullscreen = floats(new float[]{-1,-1, 3,-1, -1,3});
        cube = floats(cubeVertices());
        grid = floats(gridVertices());
        gridCount = gridVertices().length / 6;
        GLES30.glEnable(GLES30.GL_DEPTH_TEST);
        GLES30.glDepthFunc(GLES30.GL_LEQUAL);
        GLES30.glClearColor(0.035f, 0.055f, 0.09f, 1f);
        startNs = System.nanoTime();
    }

    @Override public void onSurfaceChanged(GL10 unused, int w, int h) {
        width = Math.max(1, w); height = Math.max(1, h);
        GLES30.glViewport(0, 0, width, height);
        Matrix.perspectiveM(projection, 0, 55f, (float)width / height, 0.1f, 100f);
    }

    @Override public void onDrawFrame(GL10 unused) {
        GLES30.glClear(GLES30.GL_COLOR_BUFFER_BIT | GLES30.GL_DEPTH_BUFFER_BIT);
        // Zenith-to-horizon gradient; draw first without depth writes.
        GLES30.glDisable(GLES30.GL_DEPTH_TEST);
        GLES30.glUseProgram(skyProgram);
        GLES30.glBindBuffer(GLES30.GL_ARRAY_BUFFER, 0);
        GLES30.glEnableVertexAttribArray(0);
        GLES30.glVertexAttribPointer(0, 2, GLES30.GL_FLOAT, false, 0, fullscreen);
        uniform3(skyProgram, "uZenithColor", 0.035f, 0.16f, 0.34f);
        uniform3(skyProgram, "uHorizonColor", 0.42f, 0.57f, 0.69f);
        uniform1(skyProgram, "uHorizonPower", 0.72f);
        GLES30.glDrawArrays(GLES30.GL_TRIANGLES, 0, 3);
        GLES30.glDisableVertexAttribArray(0);
        GLES30.glEnable(GLES30.GL_DEPTH_TEST);

        float cx = (float)(Math.sin(yaw) * Math.cos(pitch) * distance);
        float cy = (float)(Math.sin(pitch) * distance + 1.1f);
        float cz = (float)(Math.cos(yaw) * Math.cos(pitch) * distance);
        Matrix.setLookAtM(view, 0, cx, cy, cz, 0, 0.7f, 0, 0, 1, 0);
        Matrix.multiplyMM(vp, 0, projection, 0, view, 0);

        // Grid lies on XZ plane at y = -0.02. Depth test prevents it showing through the cube.
        GLES30.glUseProgram(gridProgram);
        Matrix.setIdentityM(model, 0);
        Matrix.multiplyMM(mvp, 0, vp, 0, model, 0);
        uniformMatrix(gridProgram, "uMVP", mvp);
        GLES30.glLineWidth(1f);
        GLES30.glEnable(GLES30.GL_BLEND);
        GLES30.glBlendFunc(GLES30.GL_SRC_ALPHA, GLES30.GL_ONE_MINUS_SRC_ALPHA);
        GLES30.glEnableVertexAttribArray(0);
        grid.position(0);
        GLES30.glVertexAttribPointer(0, 3, GLES30.GL_FLOAT, false, 6 * 4, grid);
        GLES30.glEnableVertexAttribArray(1);
        grid.position(3);
        GLES30.glVertexAttribPointer(1, 3, GLES30.GL_FLOAT, false, 6 * 4, grid);
        grid.position(0);
        GLES30.glDrawArrays(GLES30.GL_LINES, 0, gridCount);
        GLES30.glDisableVertexAttribArray(0);
        GLES30.glDisableVertexAttribArray(1);
        GLES30.glDisable(GLES30.GL_BLEND);

        // Animated cube, basic face lighting baked into vertex colors.
        float seconds = (System.nanoTime() - startNs) / 1_000_000_000f;
        Matrix.setIdentityM(model, 0);
        Matrix.translateM(model, 0, 0, 0.48f, 0);
        Matrix.rotateM(model, 0, seconds * 22f, 0.3f, 1f, 0.15f);
        Matrix.multiplyMM(mvp, 0, vp, 0, model, 0);
        GLES30.glUseProgram(solidProgram);
        uniformMatrix(solidProgram, "uMVP", mvp);
        GLES30.glEnableVertexAttribArray(0);
        cube.position(0);
        GLES30.glVertexAttribPointer(0, 3, GLES30.GL_FLOAT, false, 6 * 4, cube);
        GLES30.glEnableVertexAttribArray(1);
        cube.position(3);
        GLES30.glVertexAttribPointer(1, 3, GLES30.GL_FLOAT, false, 6 * 4, cube);
        cube.position(0);
        GLES30.glDrawArrays(GLES30.GL_TRIANGLES, 0, cubeVertices().length / 6);
        GLES30.glDisableVertexAttribArray(0);
        GLES30.glDisableVertexAttribArray(1);
    }

    private static FloatBuffer floats(float[] data) {
        FloatBuffer b = ByteBuffer.allocateDirect(data.length * 4).order(ByteOrder.nativeOrder()).asFloatBuffer();
        b.put(data).position(0); return b;
    }
    private static int program(String vs, String fs) {
        int v = shader(GLES30.GL_VERTEX_SHADER, vs), f = shader(GLES30.GL_FRAGMENT_SHADER, fs);
        int p = GLES30.glCreateProgram(); GLES30.glAttachShader(p, v); GLES30.glAttachShader(p, f); GLES30.glLinkProgram(p);
        int[] ok = new int[1]; GLES30.glGetProgramiv(p, GLES30.GL_LINK_STATUS, ok, 0);
        if (ok[0] == 0) throw new RuntimeException("Program link failed: " + GLES30.glGetProgramInfoLog(p));
        GLES30.glDeleteShader(v); GLES30.glDeleteShader(f); return p;
    }
    private static int shader(int type, String source) {
        int s = GLES30.glCreateShader(type); GLES30.glShaderSource(s, source); GLES30.glCompileShader(s);
        int[] ok = new int[1]; GLES30.glGetShaderiv(s, GLES30.GL_COMPILE_STATUS, ok, 0);
        if (ok[0] == 0) throw new RuntimeException("Shader compile failed: " + GLES30.glGetShaderInfoLog(s));
        return s;
    }
    private static void uniformMatrix(int p, String name, float[] m) { int l=GLES30.glGetUniformLocation(p,name); if(l>=0) GLES30.glUniformMatrix4fv(l,1,false,m,0); }
    private static void uniform3(int p,String n,float x,float y,float z) { int l=GLES30.glGetUniformLocation(p,n); if(l>=0) GLES30.glUniform3f(l,x,y,z); }
    private static void uniform1(int p,String n,float x) { int l=GLES30.glGetUniformLocation(p,n); if(l>=0) GLES30.glUniform1f(l,x); }

    private static float[] cubeVertices() {
        float[] out = new float[36 * 6]; int k=0;
        float[][] faces = {
            {-0.5f,-0.5f,0.5f, 0.5f,-0.5f,0.5f, 0.5f,0.5f,0.5f, -0.5f,-0.5f,0.5f, 0.5f,0.5f,0.5f, -0.5f,0.5f,0.5f},
            {0.5f,-0.5f,-0.5f, -0.5f,-0.5f,-0.5f, -0.5f,0.5f,-0.5f, 0.5f,-0.5f,-0.5f, -0.5f,0.5f,-0.5f, 0.5f,0.5f,-0.5f},
            {-0.5f,-0.5f,-0.5f, -0.5f,-0.5f,0.5f, -0.5f,0.5f,0.5f, -0.5f,-0.5f,-0.5f, -0.5f,0.5f,0.5f, -0.5f,0.5f,-0.5f},
            {0.5f,-0.5f,0.5f, 0.5f,-0.5f,-0.5f, 0.5f,0.5f,-0.5f, 0.5f,-0.5f,0.5f, 0.5f,0.5f,-0.5f, 0.5f,0.5f,0.5f},
            {-0.5f,0.5f,0.5f, 0.5f,0.5f,0.5f, 0.5f,0.5f,-0.5f, -0.5f,0.5f,0.5f, 0.5f,0.5f,-0.5f, -0.5f,0.5f,-0.5f},
            {-0.5f,-0.5f,-0.5f, 0.5f,-0.5f,-0.5f, 0.5f,-0.5f,0.5f, -0.5f,-0.5f,-0.5f, 0.5f,-0.5f,0.5f, -0.5f,-0.5f,0.5f}
        };
        float[][] colors={{0.28f,0.7f,1f},{0.15f,0.35f,0.65f},{0.2f,0.55f,0.9f},{0.25f,0.48f,0.82f},{0.45f,0.78f,1f},{0.12f,0.28f,0.5f}};
        for(int f=0;f<6;f++) for(int i=0;i<18;i+=3) { out[k++]=faces[f][i]; out[k++]=faces[f][i+1]; out[k++]=faces[f][i+2]; out[k++]=colors[f][0]; out[k++]=colors[f][1]; out[k++]=colors[f][2]; }
        return out;
    }
    private static float[] gridVertices() {
        java.util.ArrayList<Float> a = new java.util.ArrayList<>();
        for(int i=-20;i<=20;i++) {
            float c = (i==0)?0.95f:0.24f, alpha=(i==0)?1f:0.42f;
            float r=(i==0)?0.95f:0.32f, g=(i==0)?0.24f:0.39f, b=(i==0)?0.25f:0.5f;
            addLine(a, i,-0.02f,-20, i,-0.02f,20, c,g,b);
            float xr=(i==0)?0.25f:0.28f, xg=(i==0)?0.85f:0.39f, xb=(i==0)?0.42f:0.5f;
            addLine(a, -20,-0.02f,i, 20,-0.02f,i, xr,xg,xb);
        }
        float[] out=new float[a.size()]; for(int i=0;i<out.length;i++) out[i]=a.get(i); return out;
    }
    private static void addLine(java.util.ArrayList<Float> a,float x,float y,float z,float X,float Y,float Z,float r,float g,float b) {
        a.add(x);a.add(y);a.add(z);a.add(r);a.add(g);a.add(b); a.add(X);a.add(Y);a.add(Z);a.add(r);a.add(g);a.add(b);
    }

    private static final String SKY_VS="#version 300 es\nlayout(location=0) in vec2 aPosition; out vec2 vUv; void main(){vUv=aPosition*0.5+0.5;gl_Position=vec4(aPosition,1.0,1.0);}";
    private static final String SKY_FS="#version 300 es\nprecision mediump float; in vec2 vUv; out vec4 fragColor; uniform vec3 uZenithColor; uniform vec3 uHorizonColor; uniform float uHorizonPower; void main(){float t=pow(clamp(vUv.y,0.0,1.0),max(uHorizonPower,0.05));fragColor=vec4(mix(uHorizonColor,uZenithColor,t),1.0);}";
    private static final String SOLID_VS="#version 300 es\nlayout(location=0) in vec3 aPosition; layout(location=1) in vec3 aColor; uniform mat4 uMVP; out vec3 vColor; void main(){vColor=aColor;gl_Position=uMVP*vec4(aPosition,1.0);}";
    private static final String SOLID_FS="#version 300 es\nprecision mediump float; in vec3 vColor; out vec4 fragColor; void main(){fragColor=vec4(vColor,1.0);}";
}
