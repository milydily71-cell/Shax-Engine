# Shax Engine

Shax is a mobile-first 3D engine/editor prototype built around a C# engine API, a .NET 10 Android host, and a native C++ OpenGL ES renderer.

## Architecture

- `src/Shax.Core`: engine-neutral C# API, scene/entity lifecycle, transform hierarchy/world matrices, camera/light/material/mesh-renderer contracts, stable entity IDs and JSON scene serialization.
- `src/Shax.Android`: .NET 10 Android editor and live viewport. It calls the native renderer through P/Invoke and includes Scene, multi-file Script IDE, GLB container import/metadata, Console, position/rotation/scale inspector, PBR material controls, atomic scene/script/material persistence, autosave and full-transform undo/redo.
- `native/`: C++20 renderer exported as `libshax.so`, built with Android NDK/CMake for arm64-v8a and x86_64.
- `tests/Shax.Core.Tests`: automated tests for transform hierarchy, scene serialization and behaviour ticks.

## Graphics policy

The Android host requests **OpenGL ES 3.2 first, then falls directly back to ES 3.0**. There is no ES 3.1 fallback target. The current native renderer uses GLES 3.0-compatible APIs so the same baseline path works on both; ES 3.2-only features are a future capability-gated milestone.

The C++ viewport draws a zenith sky gradient with bounded volumetric-cloud raymarching, grid and colored axes, plus a C#-transform-driven cube shaded by Cook-Torrance metallic-roughness PBR and a procedural environment-cubemap IBL. Material roughness/metallic and orbit-camera position are passed from the .NET host. One-finger drag orbits the camera and pinch gestures zoom. See [rendering notes](docs/RENDERING.md) for the exact scope and limitations.

## Reproducible build and artifact

The workflow `.github/workflows/dotnet-android-build.yml` pins .NET SDK 10.0.100, Java 17, Android platform/build tools 35, NDK 27.2.12479018 and CMake 3.22.1. It builds native libraries for arm64-v8a and x86_64, runs C# core tests, publishes the APK, checks that both `libshax.so` files are inside it, emits a SHA-256 checksum, uploads the artifact and attempts an Android emulator smoke test.

Open [GitHub Actions](https://github.com/milydily71-cell/Shax-Engine/actions) and download `shax-engine-net10-native-apk` only from a successful run. The APK is not considered verified until the packaging and emulator smoke-test jobs pass.

## Local build

Install .NET SDK 10.0.100, Java 17, Android SDK platform/build tools 35, NDK 27.2.12479018 and CMake 3.22.1. See [Android build instructions](src/Shax.Android/README.md).

## Known limitations

The Script IDE provides a source buffer, basic syntax coloring, delimiter diagnostics and Play-time interpretation of a deliberately restricted C# behaviour subset. The subset currently supports rotation and local position/scale assignments; it rejects filesystem/network/reflection/process APIs and unsupported statements. It is **not** a general C# compiler and does not load generated assemblies or IL, so expanding it requires additional sandbox and regression tests.

Automated CI can verify an emulator run but cannot certify physical hardware. ES 3.2/3.0 device-specific rendering, pause/resume and EGL context-loss behavior still require hands-on device tests. True shadow maps, HDR environment import/convolved IBL, full glTF mesh/texture GPU upload, skeletal animation, multi-scene workflows, GPU timer-query profiling, and physical-device verification remain planned. The current renderer is rasterized PBR; GLES 3.2 has no standard hardware ray-tracing pipeline, so ray tracing would require a separate Vulkan backend and device-extension checks.
