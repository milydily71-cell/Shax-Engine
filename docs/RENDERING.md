# Shax mobile rendering path

## Current native renderer source

The Android renderer is C++20 in native/shax.cpp, loaded as libshax.so. It uses the OpenGL ES 3.0 shader language/API baseline so the same source can run when the .NET host requests ES 3.2 and directly falls back to ES 3.0.

Implemented in source:
- World-locked sky rendering: the native sky reconstructs world-space view rays from the inverse view-projection matrix, so the sun and cloud field stay fixed to the world while the camera orbits.
- Zenith-to-horizon atmosphere gradient, sun disc/halo, and a bounded 12-step animated cloud-density integration sampled in world-direction space.
- Procedurally generated six-face environment cubemap with mipmaps.
- Metallic-roughness Cook-Torrance PBR using GGX distribution, Schlick Fresnel, Smith geometry, direct light, diffuse environment sampling, and roughness-dependent mip sampling for specular IBL.
- Face normals generated for the viewport cube; C# world matrix, camera position and inverse view-projection are passed to native rendering. Cube normals use an inverse-transpose normal matrix to remain correct under non-uniform scale.
- Script IDE includes executable sample templates for Y/X rotation, position, scale and combined rotate/scale; templates run through the restricted AOT-safe interpreter and are covered by core tests.
- Editor material sliders for roughness and metallic.
- CPU render-submission timing and approximate geometry-memory logs every 300 frames.

## Important limits

This is a real-time rasterized PBR renderer, not hardware ray tracing. OpenGL ES 3.2 does not provide a standard hardware ray-tracing pipeline. A future ray-tracing renderer would require a separate Vulkan backend and explicit device/driver extension checks; the current Android viewport must not claim ray-traced reflections, GI, or shadows.

The current environment map is procedural and low-resolution, not a user-provided HDR panorama. Specular IBL uses cubemap mip levels as a lightweight roughness approximation; offline GGX convolution and split-sum BRDF integration are not implemented yet. The cloud effect is a low-cost fixed-step density integration with procedural noise and world-space directional sampling, not a physically complete participating-media atmosphere or cloud renderer. Shadows, cascaded shadow maps, glTF mesh decoding/GPU upload, skeletal animation, GPU timer queries, and real-device performance validation remain outstanding.

## Validation

CI compilation and emulator smoke tests are the first validation layer. They do not replace physical-device tests on an actual GLES 3.2 device and a GLES 3.0-only device. See docs/ROADMAP.md for the separate source and hardware-verification checklists.
