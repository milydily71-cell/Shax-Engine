package com.shax.engine;

import android.app.Activity;
import android.os.Bundle;
import android.view.MotionEvent;
import android.view.Gravity;
import android.widget.FrameLayout;
import android.widget.TextView;

public final class MainActivity extends Activity {
    private ShaxSurface surface;

    @Override public void onCreate(Bundle state) {
        super.onCreate(state);
        FrameLayout root = new FrameLayout(this);
        surface = new ShaxSurface(this);
        root.addView(surface, new FrameLayout.LayoutParams(-1, -1));
        TextView title = new TextView(this);
        title.setText("SHAX ENGINE  •  SCENE VIEW");
        title.setTextColor(0xffd9e8ff);
        title.setTextSize(12);
        title.setLetterSpacing(0.08f);
        title.setPadding(18, 18, 18, 18);
        FrameLayout.LayoutParams lp = new FrameLayout.LayoutParams(-2, -2, Gravity.TOP | Gravity.START);
        root.addView(title, lp);
        TextView hint = new TextView(this);
        hint.setText("Drag to orbit  •  Pinch to zoom");
        hint.setTextColor(0xffa9bfdc);
        hint.setTextSize(11);
        hint.setPadding(18, 8, 18, 14);
        FrameLayout.LayoutParams hp = new FrameLayout.LayoutParams(-2, -2, Gravity.BOTTOM | Gravity.START);
        root.addView(hint, hp);
        setContentView(root);
    }

    @Override protected void onResume() { super.onResume(); if (surface != null) surface.onResume(); }
    @Override protected void onPause() { if (surface != null) surface.onPause(); super.onPause(); }
}
