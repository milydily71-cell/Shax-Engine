# Shax Android editor prototype

## Build a debug APK

Requirements: JDK 17, Android SDK platform 35, Android build tools 35, and Gradle 8.9+.

From the repository root:

```sh
gradle --no-daemon :android:assembleDebug
```

Expected output: `android/build/outputs/apk/debug/android-debug.apk` (the exact filename can vary with Gradle configuration).

## Prototype controls
- Drag with one finger to orbit the scene camera.
- Pinch to zoom.
- The scene contains a zenith-to-horizon sky gradient, a colored world grid/axes, and a rotating cube.

## Graphics compatibility
The initial renderer uses OpenGL ES 3.0 APIs and GLSL ES 3.00, which are also valid on OpenGL ES 3.1-capable devices. Android's standard GLSurfaceView context request selects an ES 3.x context but does not explicitly negotiate the ES 3.1 minor version. The renderer must check `GL_VERSION` at runtime; this first slice intentionally needs no ES 3.1-only feature, so ES 3.0 is the compatibility baseline.

## Verification status
Source has not yet been built or run on a device in this change. CI is configured to compile the APK and upload it as an artifact. A successful CI build proves compilation only; touch interaction, visual output, lifecycle recovery, and GPU compatibility still need device/emulator testing.
