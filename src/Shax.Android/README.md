# Shax .NET 10 Android host

This app targets .NET 10 Android and references Shax.Core directly. It loads the C++ renderer from `libshax.so` through P/Invoke; the shared library must be packaged for each target ABI.

## Reproducible CI build

The workflow pins .NET SDK 10.0.100, Java 17, Android platform/build tools 35, NDK 27.2.12479018, and CMake 3.22.1. It builds native libraries for arm64-v8a and x86_64, runs core tests, publishes the APK, verifies both native libraries are present, emits SHA-256 metadata, and attempts an Android emulator launch.

Download the `shax-engine-net10-native-apk` artifact from a successful GitHub Actions run. The checksum is provided as `SHA256SUMS.txt`.

## Local build

Install .NET SDK 10.0.100, Java 17, Android SDK platform/build tools 35, NDK 27.2.12479018 and CMake 3.22.1.

    cmake -S native -B native/build/arm64-v8a -DCMAKE_TOOLCHAIN_FILE="$ANDROID_NDK_HOME/build/cmake/android.toolchain.cmake" -DANDROID_ABI=arm64-v8a -DANDROID_PLATFORM=android-24 -DCMAKE_BUILD_TYPE=Release
    cmake --build native/build/arm64-v8a
    mkdir -p src/Shax.Android/native-libs/arm64-v8a
    cp native/build/arm64-v8a/libshax.so src/Shax.Android/native-libs/arm64-v8a/
    dotnet restore tests/Shax.Core.Tests/Shax.Core.Tests.csproj
    dotnet test tests/Shax.Core.Tests/Shax.Core.Tests.csproj -c Release
    dotnet restore src/Shax.Android/Shax.Android.csproj
    dotnet publish src/Shax.Android/Shax.Android.csproj -c Release -f net10.0-android -p:AndroidPackageFormat=apk

For x86_64, repeat the CMake build with `-DANDROID_ABI=x86_64` and place the library under `native-libs/x86_64`.

## Current state and honest limits

The C++ renderer draws a zenith sky gradient, grid, colored axes and a rotating cube. The C# viewport requests GLES 3.2 first and falls back directly to GLES 3.0; there is no GLES 3.1 fallback. Orbit and pinch zoom are implemented in the .NET host. The C# scene entity transform drives the rendered cube.

The editor includes a hierarchy/transform panel, a source editor with basic keyword/comment coloring, delimiter diagnostics, script save/load, scene JSON save/load, asset-browser placeholder, console, and position undo/redo. The Play action runs `CSharpSubsetRuntime`, an AOT-safe interpreter for a deliberately small C#-like behaviour subset: `Transform.Rotate(Vector3.UnitX/UnitY/UnitZ, speed * deltaTime)` and `LocalPosition`/`LocalScale` assignments. It does not compile arbitrary C# or load generated assemblies. File/network/reflection/process APIs and unrecognized statements are rejected; unsupported code gets diagnostics, and runtime exceptions stop that behaviour. This is a constrained interpreter, not full Roslyn compilation. Add new syntax only with sandbox and regression tests.

CI verifies source tests, APK packaging and an emulator launch if its smoke-test job passes. It cannot claim physical-device verification; GLES 3.2 and GLES 3.0 devices, pause/resume and context-loss behavior still need hands-on testing.

## Device graphics-path test

Normal launch requests ES 3.2 first. To explicitly exercise the ES 3.0 path on a compatible test device, launch the app with the boolean intent extra:

    adb shell am start -a android.intent.action.MAIN -c android.intent.category.LAUNCHER --ez shax.forceGles30 true

Inspect Logcat for the `ShaxNative` line showing `OpenGL ES 3.0`. The app intentionally does not preserve the EGL context on pause, so background/resume recreates the context and renderer resources. This is a diagnostic path; a physical-device test is still required before claiming hardware verification.
