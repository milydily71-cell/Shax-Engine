package com.shax.engine;

import android.content.Context;
import android.opengl.GLSurfaceView;
import android.view.MotionEvent;
import android.view.ScaleGestureDetector;

final class ShaxSurface extends GLSurfaceView {
    private final ShaxRenderer renderer;
    private final ScaleGestureDetector scaleDetector;
    private float previousX, previousY;

    ShaxSurface(Context context) {
        super(context);
        // Request ES 3.x. The renderer reports the actual context version and uses ES 3.0 APIs only.
        setEGLContextClientVersion(3);
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
