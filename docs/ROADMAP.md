# Shax Engine roadmap

## Milestone 0 — repository foundation
- [x] Define mobile-first architecture and public C# direction
- [x] Define incremental rendering and editor milestones
- [ ] Add a reproducible Android/.NET build setup
- [ ] Add CI build and unit tests

## Milestone 1 — first visible 3D scene
- [ ] Android app launches into a GLES 3.0 surface
- [ ] Clear color, viewport resize and lifecycle/context-loss handling
- [ ] Perspective camera and touch orbit/pan/zoom
- [ ] Ground grid and XYZ axes
- [ ] Zenith-to-horizon sky gradient
- [ ] Indexed cube and UV sphere with depth testing
- [ ] Directional light and basic lit material
- Acceptance: scene stays responsive through pause/resume and orientation changes on a physical Android device.

## Milestone 2 — C# engine API
- [ ] Entity/component model and stable entity IDs
- [ ] Transform hierarchy and local/world matrices
- [ ] Scene lifecycle: Awake, OnEnable, Start, Update, LateUpdate, OnDisable, OnDestroy
- [ ] Public primitives: cube, sphere, plane and capsule
- [ ] Materials, camera, light and mesh renderer components
- [ ] JSON scene serialization with schema versioning
- Acceptance: a C# script creates and moves a cube in a sample scene.

## Milestone 3 — playable runtime
- [ ] Script build/load pipeline with diagnostics
- [ ] Touch, keyboard/gamepad abstraction
- [ ] Resource/asset database and GLB/glTF import
- [ ] Audio, collision/physics integration and animation
- [ ] Package a standalone Android game from a project

## Milestone 4 — mobile editor
- [ ] Hierarchy and selection
- [ ] Scene viewport with editor-only grid and gizmos
- [ ] Inspector for transforms and component properties
- [ ] Asset browser, import status and console
- [ ] Play/stop and runtime/editor state separation
- [ ] Undo/redo and autosave

## Milestone 5 — performance and production
- [ ] GPU/CPU frame-time overlay and memory diagnostics
- [ ] Frustum culling, batching and instancing
- [ ] ETC2 texture support and mesh LODs
- [ ] Crash-safe project saves and automated smoke tests

## Definition of done
A checkbox is complete only when code exists, the relevant build/test succeeds, and the target Android behavior is verified. A plan or source-only prototype is not a tested release.
