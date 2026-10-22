# Shax Engine roadmap

## Milestone 0 — foundation
- [x] Initial engine-neutral C# API and architecture documents
- [x] Multi-target core for .NET Standard 2.1 and .NET 10
- [x] Scene lifecycle tick and script document/diagnostic contracts
- [x] .NET 10 Android host referencing Shax.Core
- [ ] Reproducible Android/.NET build and verified CI artifact

## Milestone 1 — mobile 3D viewport
- [x] .NET Android editor shell with Scene, Script IDE buffer, and Play tick
- [x] Perspective camera, procedural grid, and rotating cube source
- [x] Java and .NET Android sources request ES 3.2 first, then fall back to 3.1 and 3.0
- [ ] Zenith sky gradient, colored axes, orbit/pinch controls in the .NET host
- [ ] Build and launch on physical Android devices with both ES 3.2 and ES 3.0 paths
- [ ] Context-loss and pause/resume verification

## Milestone 2 — engine/runtime
- [x] Entity/component and Transform starting contracts
- [x] Scene lifecycle callbacks and primitive cube data
- [ ] Transform hierarchy and world matrices
- [ ] Camera, lights, material, mesh renderer, and resource ownership
- [ ] Scene serialization and stable IDs
- [ ] Verify a user C# behaviour changes the rendered cube

## Milestone 3 — script IDE
- [x] Editable C# source buffer and diagnostic data model
- [ ] Syntax highlighting, project tree, file save/load, diagnostics panel
- [ ] Compiler/runtime integration with Android/AOT constraints tested
- [ ] Sandboxed script permissions and robust error reporting

## Milestone 4 — integrated mobile editor
- [ ] Scene hierarchy and selection
- [ ] Transform inspector and gizmos
- [ ] Asset browser and import pipeline
- [ ] Console, play/stop, undo/redo, and autosave

## Milestone 5 — rendering and production
- [ ] GLES 3.2 advanced feature paths gated by capability detection
- [ ] PBR materials, image-based lighting, shadows, GLB/glTF, animation
- [ ] GPU/CPU frame-time and memory diagnostics
- [ ] Automated smoke tests, crash-safe saves, and release APK verification

## Definition of done
A checkbox is complete only when source exists and relevant builds/tests succeed; device-specific rendering is verified only after testing on that hardware. Source-only prototypes are not verified releases.
