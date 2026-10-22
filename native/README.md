# Native Shax renderer

This directory builds `libshax.so`, a C++20 shared library loaded by the .NET 10 Android app using P/Invoke.

## Build

The CI workflow uses Android NDK 27.2.12479018, CMake 3.22.1 and API level 24. It builds:

- `arm64-v8a/libshax.so`
- `x86_64/libshax.so`

The workflow copies each output into `src/Shax.Android/native-libs/<ABI>/` before publishing the APK. Those libraries are packaged with the managed C# assemblies and the .NET Android runtime; C# is not merged into the C++ shared object.

## Native API

- `shax_init()`: compiles native sky/scene shaders, creates the interleaved geometry VBO and logs the active GLES driver.
- `shax_resize(width, height)`: updates the GL viewport.
- `shax_draw(viewProjection, cubeModel, timeSeconds)`: draws the sky gradient, grid/colored axes and cube.
- `shax_shutdown()`: releases GPU resources when called with a current valid GL context.

All entry points are C ABI exports. The C# bridge calls them from the GLSurfaceView render thread only.

## Compatibility

The app requests ES 3.2 first, then ES 3.0 directly. The current renderer deliberately uses GLSL ES 3.00 and GLES 3.0-compatible calls on both paths. ES 3.2-specific effects are not claimed as implemented yet. Resource handles are rebuilt in the surface-created callback to handle EGL context recreation.
