# Shax API, mobile editor, and renderer integration

## Public C# entry points

The core assembly targets .NET Standard 2.1 for portability. Use namespace Shax for Entity, Transform, Scene, ShaxBehaviour, ScriptDocument, and Engine. A compatibility entry point is also available in namespace ShaxEngine as ShaxEngine.Engine and ShaxEngine.Primitives.

## Runtime packaging

src/Shax.Android/Shax.Android.csproj targets net10.0-android and project-references Shax.Core. This is the intended .NET 10 APK path. The legacy Gradle/Java prototype remains in android/ until the .NET app is built and device-tested; these are separate build paths today.

## Editor roadmap

1. Stabilize the .NET Android host and renderer.
2. Add scene hierarchy and selection, transform inspector, gizmos, and viewport navigation.
3. Add C# documents, diagnostics, project files, save/load, and syntax-aware editing.
4. Add Roslyn compilation only after dependency size, offline restore, assembly-loading restrictions, and Android AOT/dynamic-code behavior are tested. Do not run arbitrary scripts with unrestricted host permissions.
5. Share the scene graph with the renderer, then add GLB/glTF assets, materials, lighting, shadows, and GPU diagnostics.

## OpenGL ES capability policy

OpenGL ES 3.2 is the preferred feature tier, not a safe assumption on every Android GPU. Query the actual GL version and extensions and gate advanced paths; retain a GLES 3.0 baseline for devices that do not expose 3.2. The current .NET viewport is an initial GLES 3.0 API prototype and does not yet implement an explicit EGL 3.2 context request.
