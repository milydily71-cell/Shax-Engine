# Shax .NET 10 Android host

This SDK-style net10.0-android app references Shax.Core directly, so the C# API is packaged into the Android app rather than remaining disconnected source.

## Build

Install the .NET 10 SDK and Android workload, then run from the repository root:

    dotnet workload install android
    dotnet restore src/Shax.Android/Shax.Android.csproj
    dotnet publish src/Shax.Android/Shax.Android.csproj -c Release -f net10.0-android -p:AndroidPackageFormat=apk

The publish output reports the resulting artifact location.

## Current state

The app shell has Scene, Script IDE, and Play controls, and the Android project references the engine-neutral C# core. The script editor is a source buffer only: dynamic compilation, diagnostics integration, project save/load, and safe assembly loading are not implemented yet.

The .NET viewport explicitly attempts an OpenGL ES 3.2 EGL context, then 3.1 and 3.0. The current shader and drawing code uses GLES 3.0-compatible APIs, and advanced ES 3.2 rendering features are not implemented yet. Build and device verification remain pending.
