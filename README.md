# Shax Engine

**A mobile-first 3D game engine and editor built around OpenGL ES and a C# gameplay API.**

Shax is being developed toward a native Android editor with a live 3D viewport, scene hierarchy, inspector, asset browser and Play mode. C# is the intended public engine API and gameplay scripting language.

## Current foundation

- Architecture and staged implementation roadmap
- Initial `Shax.Core` C# project
- Minimal `Entity`, `Transform` and `ShaxBehaviour` lifecycle contracts

These are starting points, not a finished engine. The Android app, GLES renderer, visible grid/sky/geometry, editor UI and C# script compiler are still planned and must be implemented and tested.

## Target first viewport

- OpenGL ES 3.0 rendering on Android
- Perspective camera and touch orbit/pan/zoom
- Ground grid and XYZ axes
- Zenith-to-horizon sky gradient
- Cube and UV sphere geometry with depth testing
- Basic directional light and material
- Lifecycle-safe render surface

## Repository guide

- [Architecture](docs/ARCHITECTURE.md)
- [Roadmap](docs/ROADMAP.md)
- [C# API design](docs/CSHARP_API_DESIGN.md)
- [Shax.Core](src/Shax.Core/README.md)

## Direction

The engine should stay mobile-first: respect GPU and memory budgets, recover from OpenGL context loss, keep GL work on the render thread, and make projects portable. The renderer and editor should share a scene model, while editor-only gizmos and overlays remain separate from game builds.

## License and contributions

License and build/CI policy still need to be selected and documented before a public release.
