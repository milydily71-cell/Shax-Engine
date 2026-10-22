# Shax Engine roadmap

A checked box means the source implementation exists. Build/device verification is tracked separately and must not be inferred from source alone.

## Milestone 0 — reproducible foundation
- [x] C# core multi-targets .NET Standard 2.1 and .NET 10
- [x] .NET 10 Android host references Shax.Core
- [x] C++20 native renderer source and CMake build for Android ABIs
- [x] P/Invoke bridge and ABI-specific `libshax.so` packaging rules
- [x] Pinned .NET/Java/NDK/CMake/Android SDK workflow and APK checksum metadata
- [x] Core tests for transform hierarchy, scene JSON, behaviour ticks and script sandbox
- [ ] Successful CI build, artifact inspection and emulator smoke test on current commit

## Milestone 1 — mobile viewport
- [x] C++ native renderer with zenith sky gradient and colored grid axes
- [x] Perspective cube rendering driven by the C# scene transform
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
- [x] Live cube model matrix sourced from the C# entity world transform
- [ ] Verify core tests and behaviour-to-renderer bridge in CI/device runs
- [x] Pass the active orbit camera and PBR roughness/metallic settings into the native renderer; add explicit disposable resource-cache API
- [ ] Bind scene Camera/Light components and per-entity Material/MeshRenderer instances to draw submission
- [ ] Add scene format migrations, asset references and crash-safe transactional saves

## Milestone 3 — script IDE
- [x] Editable C# source buffer and structured diagnostic model
- [x] Basic keyword/comment coloring and delimiter diagnostics
- [x] App-private script save/load
- [x] Restricted allow-listed C# behaviour interpreter designed for Android AOT (no IL/JIT/assembly load)
- [x] Sandbox rejects file/network/reflection/process tokens and unknown statements
- [x] Source-size and instruction-count limits, line/column diagnostics, stop-on-runtime-error behavior
- [x] Core tests cover supported rotation behaviour and blocked file APIs
- [ ] Verify user-edited behaviour changes the rendered cube in the APK/emulator run
- [ ] Expand safe syntax only with tests; general Roslyn/full-language support remains out of scope until a compatible runtime is proven
- [x] Multi-file C# project tree backed by app-private script files
- [ ] Multi-file project tree backed by real files

## Milestone 4 — integrated mobile editor
- [x] Scene hierarchy prototype and selected-entity transform inspector
- [x] Full transform undo/redo snapshots, atomic scene/script/material saves, and autosave on edits/pause
- [x] Asset-browser and console panels (initial shell)
- [x] GLB 2.0 container validation, safe app-private staging/copy, and mesh/material/texture/node/animation metadata reporting
- [ ] Decode glTF accessors, upload imported meshes/textures to GPU, generate thumbnails and maintain a persistent asset metadata cache
- [x] Position, rotation and scale inspector controls
- [ ] GPU-backed selection/picking and interactive translate/rotate/scale gizmos
- [x] Play/stop controls, transform command history, and autosave on edits/pause
- [ ] Multi-scene workflow and crash-recovery restore UI

## Milestone 5 — rendering and production
- [x] Native C++ shared library and GLES 3.0-compatible baseline
- [x] Parse and validate the active GLES version; keep ES 3.2/3.0 selection explicit
- [ ] Add actual GLES 3.2-only rendering features with a tested ES 3.0 fallback
- [x] Metallic-roughness Cook-Torrance PBR cube shading, procedural environment-cubemap IBL and live roughness/metallic editor controls
- [x] Bounded 12-step animated volumetric-cloud/atmosphere approximation in the sky shader
- [ ] Shadow maps, HDR environment import, convolved IBL and physically based atmosphere/clouds
- [x] GLB 2.0 container/JSON validation and asset staging API
- [ ] Full glTF accessor/mesh/material/texture GPU import and skeletal animation
- [x] Periodic CPU render-submission timing and approximate geometry-memory diagnostics
- [ ] GPU timer-query timing, total GPU resource accounting and frame-budget overlays
- [ ] Automated emulator launch, crash checks, signed release APK and checksum verification
- [ ] Physical-device matrix: GLES 3.2, GLES 3.0, pause/resume and context-loss recovery

## Definition of done
Source presence is not the same as successful verification. CI must pass relevant build/tests; physical-device claims require a real device test. Never label a source-only milestone as a verified release.
