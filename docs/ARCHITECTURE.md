# Shax Engine — Mobile-first architecture

## Product goal
A native Android 3D game editor and runtime, with OpenGL ES 3.0 rendering and C# as the public engine API and gameplay scripting language.

## Runtime layers
1. **Android host** — Activity lifecycle, touch input, storage permissions, app packaging and surface lifecycle.
2. **Graphics backend** — OpenGL ES 3.0 context, shader/program management, GPU buffers, textures, depth testing, frame timing and device-loss recovery.
3. **Renderer** — camera matrices, mesh/material submission, directional lighting, sky, grid and debug geometry.
4. **Scene core (C#)** — entities, components, transforms, hierarchy, scene serialization and lifecycle.
5. **Scripting (C#)** — user scripts deriving from `ShaxBehaviour`; compile/load scripts in a controlled project environment. Never execute untrusted assemblies without an explicit trust model.
6. **Editor UI** — hierarchy, scene viewport, inspector, asset browser, console and play/stop controls.
7. **Project/build pipeline** — portable project format, assets, script assemblies and Android export.

## Mobile rendering rules
- Start with OpenGL ES 3.0; check the actual device capabilities at runtime.
- Use a resizable EGL/GL surface and recreate GPU resources after context loss.
- Keep GL calls on the GL/render thread. Queue scene changes from the UI thread.
- Use indexed meshes, interleaved vertex data, frustum culling, texture compression and explicit resource disposal.
- Use a perspective camera and a separate orthographic editor grid. Draw the grid with a distance fade; avoid an infinite dense grid.
- The zenith sky starts as a gradient sky dome/skybox with configurable horizon and zenith colors. Add HDR/environment maps later.
- Mobile editor and game runtime share the same scene and renderer; editor gizmos and overlays are editor-only passes.

## Proposed solution layout
- `src/Shax.Core`: entities, components, transforms, scene and serialization contracts.
- `src/Shax.Graphics`: renderer abstractions, camera, mesh/material and debug draw.
- `src/Shax.OpenGLES`: GLES 3 backend and shaders.
- `src/Shax.Scripting`: C# script lifecycle and compilation/loading boundary.
- `src/Shax.Editor.Android`: Android editor shell, viewport and touch navigation.
- `samples/BasicScene`: grid, sky, cube, sphere and directional light.
- `tests`: math, scene, serialization and lifecycle tests.

## Important implementation boundary
The repository currently begins with a short README. This document is the target architecture, not a claim that these systems have already been implemented or tested. Add each layer incrementally and verify it on a real Android device and emulator.
