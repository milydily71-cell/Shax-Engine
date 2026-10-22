# Shax Engine

Shax is a mobile-first 3D engine/editor prototype with a C# public API, .NET 10 Android host, and OpenGL ES rendering.

## Current source foundations

- Engine-neutral C# core multi-targeting .NET Standard 2.1 and .NET 10
- Entity/component and Transform contracts, Scene lifecycle tick, and primitive mesh data
- Both using Shax; and a compatibility entry point under using ShaxEngine;
- .NET 10 Android application project that references Shax.Core
- Mobile shell with Scene, Script IDE source buffer, Play tick, and perspective cube/grid viewport
- Java/Gradle prototype prefers an ES 3.2 EGL context, then falls back to ES 3.1 and ES 3.0
- .NET Android host now requests ES 3.2 first, with ES 3.1 and ES 3.0 fallback

These are source implementations, not yet a verified release. CI and physical-device builds must pass before the APK can be called working. Advanced ES 3.2-only rendering paths, a sky gradient in the .NET viewport, and editor-grade scripting remain in progress.

## Build the .NET Android app

Install .NET 10 SDK, Java 17, and Android SDK, then run:

    dotnet workload install android
    dotnet restore src/Shax.Android/Shax.Android.csproj
    dotnet publish src/Shax.Android/Shax.Android.csproj -c Release -f net10.0-android -p:AndroidPackageFormat=apk

The CI workflow is .github/workflows/dotnet-android-build.yml. Check its run and artifact before installing.

## Project guide

- Architecture: docs/ARCHITECTURE.md
- Roadmap: docs/ROADMAP.md
- API and editor integration: docs/SHAX_API_AND_EDITOR.md
- Core API: src/Shax.Core/README.md
- .NET Android host: src/Shax.Android/README.md

## Next editor/runtime milestones

Add sky gradient and axis colors, orbit/pinch controls to the .NET viewport, selection and transform inspector, asset browser, script diagnostics, project save/load, and a secure C# compilation pipeline.
