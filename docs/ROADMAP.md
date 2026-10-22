# Shax Engine roadmap

A checked box means the source implementation exists; build/device verification is tracked separately and must not be inferred from source alone.

## Milestone 0 — reproducible foundation
- [x] C# core multi-targets .NET Standard 2.1 and .NET 10
- [x] .NET 10 Android host references Shax.Core
- [x] C++20 native renderer source and CMake build for Android ABIs
- [x] P/Invoke bridge and ABI-specific `libshax.so` packaging rules
- [x] Pinned .NET/Java/NDK/CMake/Android SDK workflow and APK checksum metadata
- [x] Core tests for transform hierarchy, scene JSON and behaviour ticks
- [ ] Successful CI build, artifact inspection and emulator smoke test on current commit

## Milestone 1 — mobile viewport
- [x] C++ native renderer with zenith sky gradient and colored grid axes
- [x] Perspective cube rendering driven by C# scene transform
- [x] GLES 3.2 requested first, direct fallback to GLES 3.0 only
- [x] One-finger orbit and pinch zoom in the .NET Android host
- [x] Native GPU resources recreated from surface-created callback
- [ ] Verify GLES 3.2 path on a physical device
- [ ] Verify GLES 3.0 path on a physical device
- [ ] Verify pause/resume and forced EGL context loss on physical hardware
- [ ] Add capability-gated GLES 3.2-only render features and GPU diagnostics

## Milestone 2 — engine/runtime
- [x] Entity/component and behaviour lifecycle API
- [x] Parent/child transform hierarchy, cycle checks and world matrices
- [x] Camera, light, material and mesh-renderer API contracts
- [x] Explicit ownership flags for renderer resources
- [x] Stable entity IDs and versioned JSON scene serialization
- [x] Live cube model matrix sourced from C# transform; compiled C# preview behaviour rotates it
- [ ] Verify core tests and behaviour-renderer bridge in CI/device runs
- [ ] Add transform-aware camera/light/material rendering, resource cache and disposal integration
- [ ] Add scene format migrations, asset references and crash-safe transactional saves

## Milestone 3 — script IDE
- [x] Editable C# source buffer and script document/diagnostic data model
- [x] Basic keyword/comment coloring and delimiter diagnostics
- [x] App-private script save/load
- [ ] Project tree backed by real files and multi-file editing
- [ ] Roslyn diagnostics/compiler strategy compatible with Android AOT
- [ ] Safe execution model for user-authored scripts (do not load arbitrary JIT assemblies)
- [ ] Permission-limited script API, quotas, exception isolation and robust error reporting
- [ ] Prove a user-edited script changes the rendered cube under the chosen execution model

## Milestone 4 — integrated mobile editor
- [x] Scene hierarchy prototype and selected-entity transform inspector
- [x] Position undo/redo and scene/script save on pause
- [x] Asset-browser and console panels (initial shell)
- [ ] Real asset import pipeline and thumbnail/metadata cache
- [ ] Selection/picking, transform gizmos, rotation/scale inspector
- [ ] Play/stop state, full command history, autosave recovery and multi-scene workflow

## Milestone 5 — rendering and production
- [x] Native C++ shared library and GLES 3.0-compatible baseline
- [ ] Capability-gated GLES 3.2 feature path
- [ ] PBR, image-based lighting, shadows and material editor
- [ ] GLB/glTF import, mesh/material assets, skeletal animation
- [ ] GPU/CPU frame-time and memory diagnostics
- [ ] Automated emulator launch, crash checks, signed release APK and checksum verification
- [ ] Physical-device matrix: GLES 3.2, GLES 3.0, pause/resume and context-loss recovery

## Definition of done
Source presence is not the same as successful verification. CI must pass relevant build/tests; physical-device claims require a real device test. Never label a source-only milestone as a verified release.
