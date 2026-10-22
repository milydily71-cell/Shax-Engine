# Shax.Core

Initial C# API foundation for Shax Engine. This slice defines an entity, transform data and a gameplay behaviour lifecycle contract. It is not yet a complete scene runtime and is not yet connected to an OpenGL ES renderer.

Build with a .NET SDK that supports .NET Standard 2.1:

```sh
dotnet build src/Shax.Core/Shax.Core.csproj
```

Next: add a scene registry, lifecycle scheduler, camera/light/mesh components, serialization and tests.
