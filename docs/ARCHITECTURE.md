# Shax Engine — mobile-first architecture

## Target architecture

Shax is a .NET 10 Android application with an engine-neutral C# API and a native C++ OpenGL ES renderer. The Android package contains managed C# assemblies, the .NET Android runtime libraries and ABI-specific native `libshax.so`.

## Implemented source layers

1. **Android host** — .NET Android Activity lifecycle, touch gestures, private app storage and editor panels.
2. **Native graphics backend** — C++20 library built with Android NDK/CMake. P/Invoke functions are called only while the GLSurfaceView GL context is current.
3. **Baseline renderer** — zenith sky gradient, colored grid axes and a cube driven by the C# entity world matrix.
4. **Scene core** — entities with stable GUIDs, parent/child transforms, world matrices, behaviour lifecycle and versioned JSON serialization.
5. **Script subset** — an AOT-safe allow-listed interpreter supports a small subset of C# behaviour statements. It does not compile IL, load assemblies or expose arbitrary file/network/reflection APIs.
6. **Editor shell** — scene hierarchy and position inspector, script buffer with basic highlighting/diagnostics, asset-browser placeholder, console, save/load, undo/redo and play/stop controls.
7. **Build pipeline** — pinned SDK/NDK/CMake/.NET versions, core tests, native ABI builds, APK packaging checks, SHA-256 metadata and an Android emulator launch/resume smoke test.

## Graphics compatibility policy

- Request an OpenGL ES 3.2 context first.
- If that fails, request ES 3.0 directly. Do not request ES 3.1 as a fallback.
- Current shaders and renderer calls stay on the ES 3.0-compatible baseline. ES 3.2-only features must be capability-gated before use.
- Keep GL calls on the GLSurfaceView render thread. Recreate programs and buffers in `onSurfaceCreated` because EGL may discard the context during pause/resume.
- The C# host sends view-projection and entity world matrices to `libshax.so`; the native renderer owns GPU programs and buffers.
- The current grid/sky/cube are a prototype. PBR, IBL, shadows, GLB/glTF, animation, picking, gizmos and GPU profiling are not yet implemented.

## Repository layout

- `src/Shax.Core`: entity/component API, transform hierarchy, scene lifecycle, serialization and script subset.
- `src/Shax.Android`: .NET 10 Android editor and native bridge.
- `native`: C++ renderer and CMake build.
- `tests/Shax.Core.Tests`: core hierarchy, serialization, behaviour and sandbox tests.
- `android`: legacy Java/Gradle viewport prototype kept for comparison.
- `.github/workflows/dotnet-android-build.yml`: native + .NET APK build and emulator smoke test.

## Verification boundary

Source presence is not proof of a working release. The current CI must pass core tests, native packaging, APK assembly inspection and emulator smoke test. Physical-device GLES 3.2/3.0 compatibility, visual quality and forced context-loss recovery still require hands-on testing.
