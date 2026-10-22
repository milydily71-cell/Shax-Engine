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
The surface requests an ES 3-capable EGL config, tries an OpenGL ES 3.1 context first, and falls back to ES 3.0 if context creation fails. The renderer currently uses only ES 3.0 APIs and GLSL ES 3.00, so both context versions use the same rendering path. The actual driver version is available through `GL_VERSION`.

## Verification status
Source has not yet been built or run on a device in this change. CI is configured to compile the APK and upload it as an artifact. A successful CI build proves compilation only; touch interaction, visual output, lifecycle recovery, and GPU compatibility still need device/emulator testing.
