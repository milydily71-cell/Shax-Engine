# Shax Engine

Shax is a mobile-first 3D engine/editor prototype built around a C# engine API, a .NET 10 Android host, and a native C++ OpenGL ES renderer.

## Architecture

- `src/Shax.Core`: engine-neutral C# API, scene/entity lifecycle, transform hierarchy/world matrices, camera/light/material/mesh-renderer contracts, stable entity IDs and JSON scene serialization.
- `src/Shax.Android`: .NET 10 Android editor and live viewport. It calls the native renderer through P/Invoke and includes Scene, Script IDE, Assets, Console, transform inspector, basic diagnostics, scene/script persistence and position undo/redo.
- `native/`: C++20 renderer exported as `libshax.so`, built with Android NDK/CMake for arm64-v8a and x86_64.
- `tests/Shax.Core.Tests`: automated tests for transform hierarchy, scene serialization and behaviour ticks.

## Graphics policy

The Android host requests **OpenGL ES 3.2 first, then falls directly back to ES 3.0**. There is no ES 3.1 fallback target. The current native renderer uses GLES 3.0-compatible APIs so the same baseline path works on both; ES 3.2-only features are a future capability-gated milestone.

The C++ viewport draws a zenith sky gradient, grid, colored X/Y/Z axes and a cube driven by the C# scene transform. One-finger drag orbits the camera and pinch gestures zoom.

## Reproducible build and artifact

The workflow `.github/workflows/dotnet-android-build.yml` pins .NET SDK 10.0.100, Java 17, Android platform/build tools 35, NDK 27.2.12479018 and CMake 3.22.1. It builds native libraries for arm64-v8a and x86_64, runs C# core tests, publishes the APK, checks that both `libshax.so` files are inside it, emits a SHA-256 checksum, uploads the artifact and attempts an Android emulator smoke test.

Open [GitHub Actions](https://github.com/milydily71-cell/Shax-Engine/actions) and download `shax-engine-net10-native-apk` only from a successful run. The APK is not considered verified until the packaging and emulator smoke-test jobs pass.

## Local build

Install .NET SDK 10.0.100, Java 17, Android SDK platform/build tools 35, NDK 27.2.12479018 and CMake 3.22.1. See [Android build instructions](src/Shax.Android/README.md).

## Known limitations

The Script IDE currently provides a source buffer, basic syntax coloring and delimiter diagnostics; it does **not** compile and execute arbitrary edited C# code. The visible rotating cube uses a compiled-in C# behaviour. Dynamic scripting must account for .NET Android AOT limitations and needs a deliberately constrained, tested execution model.

Automated CI can verify an emulator run but cannot certify physical hardware. ES 3.2/3.0 device-specific rendering, pause/resume and EGL context-loss behavior still require hands-on device tests. PBR, image-based lighting, shadows, glTF/GLB import, animation, full asset pipeline, robust sandboxing and GPU profiling remain planned.
