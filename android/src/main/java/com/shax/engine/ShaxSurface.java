package com.shax.engine;

import android.content.Context;
import android.opengl.GLSurfaceView;
import android.view.MotionEvent;
import android.view.ScaleGestureDetector;
import javax.microedition.khronos.egl.EGL10;
import javax.microedition.khronos.egl.EGLConfig;
import javax.microedition.khronos.egl.EGLContext;
import javax.microedition.khronos.egl.EGLDisplay;

final class ShaxSurface extends GLSurfaceView {
    private final ShaxRenderer renderer;
    private final ScaleGestureDetector scaleDetector;
    private float previousX, previousY;

    ShaxSurface(Context context) {
        super(context);
        setEGLContextClientVersion(3);
        setEGLContextFactory(new EGLContextFactory() {
            private final int EGL_CONTEXT_MAJOR_VERSION_KHR = 0x3098;
            private final int EGL_CONTEXT_MINOR_VERSION_KHR = 0x30FB;
            private EGLContext tryContext(EGL10 egl, EGLDisplay display, EGLConfig config, int minor) {
                int[] attrs = minor == 0
                    ? new int[]{EGL_CONTEXT_MAJOR_VERSION_KHR, 3, EGL10.EGL_NONE}
                    : new int[]{EGL_CONTEXT_MAJOR_VERSION_KHR, 3, EGL_CONTEXT_MINOR_VERSION_KHR, minor, EGL10.EGL_NONE};
                return egl.eglCreateContext(display, config, EGL10.EGL_NO_CONTEXT, attrs);
            }
            @Override public EGLContext createContext(EGL10 egl, EGLDisplay display, EGLConfig config) {
                EGLContext context = tryContext(egl, display, config, 2);
                if (context != null && context != EGL10.EGL_NO_CONTEXT) return context;
                context = tryContext(egl, display, config, 1);
                if (context != null && context != EGL10.EGL_NO_CONTEXT) return context;
                context = tryContext(egl, display, config, 0);
                if (context == null || context == EGL10.EGL_NO_CONTEXT) {
                    throw new RuntimeException("Shax requires OpenGL ES 3.0 or newer.");
                }
                return context;
            }
            @Override public void destroyContext(EGL10 egl, EGLDisplay display, EGLContext context) {
                if (!egl.eglDestroyContext(display, context)) {
                    android.util.Log.e("ShaxSurface", "Failed to destroy EGL context: " + egl.eglGetError());
                }
            }
        });
        renderer = new ShaxRenderer();
        setRenderer(renderer);
        setRenderMode(GLSurfaceView.RENDERMODE_CONTINUOUSLY);
        scaleDetector = new ScaleGestureDetector(context, new ScaleGestureDetector.SimpleOnScaleGestureListener() {
            @Override public boolean onScale(ScaleGestureDetector detector) {
                renderer.zoom(detector.getScaleFactor());
                return true;
            }
        });
    }

    @Override public boolean onTouchEvent(MotionEvent event) {
        scaleDetector.onTouchEvent(event);
        float x = event.getX(), y = event.getY();
        if (event.getActionMasked() == MotionEvent.ACTION_MOVE && event.getPointerCount() == 1) {
            renderer.orbit((x - previousX) * 0.008f, (y - previousY) * 0.008f);
        }
        previousX = x; previousY = y;
        return true;
    }
}
