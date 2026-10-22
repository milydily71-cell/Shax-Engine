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

The viewport currently requests an OpenGL ES 3 context and uses GLES 3.0 calls. It logs the actual driver version, but does not yet explicitly request an ES 3.2 EGL context. Build and device verification remain pending.
