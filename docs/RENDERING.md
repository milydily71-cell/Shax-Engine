# Shax mobile rendering path

## Current native renderer source

The Android renderer is C++20 in native/shax.cpp, loaded as libshax.so. It uses the OpenGL ES 3.0 shader language/API baseline so the same source can run when the .NET host requests ES 3.2 and directly falls back to ES 3.0.

Implemented in source:
- Zenith-to-horizon sky gradient, sun glow, and a bounded 12-step animated volumetric-cloud approximation.
- Procedurally generated six-face environment cubemap with mipmaps.
- Metallic-roughness Cook-Torrance PBR using GGX distribution, Schlick Fresnel, Smith geometry, direct light, diffuse environment sampling, and roughness-dependent mip sampling for specular IBL.
- Face normals generated for the viewport cube; C# world matrix, orbit-camera position, and material roughness/metallic values are passed to the native renderer.
- Editor material sliders for roughness and metallic.
- CPU render-submission timing and approximate geometry-memory logs every 300 frames.

## Important limits

This is a real-time rasterized PBR renderer, not hardware ray tracing. OpenGL ES 3.2 does not provide a standard hardware ray-tracing pipeline. A future ray-tracing renderer would require a separate Vulkan backend and explicit device/driver extension checks; the current Android viewport must not claim ray-traced reflections, GI, or shadows.

The current environment map is procedural and low-resolution, not a user-provided HDR panorama. Specular IBL uses cubemap mip levels as a lightweight roughness approximation; offline GGX convolution and split-sum BRDF integration are not implemented yet. The volumetric effect is a low-cost fixed-step cloud/atmosphere approximation, not a physically complete atmosphere or cloud renderer. Shadows, cascaded shadow maps, glTF mesh decoding/GPU upload, skeletal animation, GPU timer queries, and real-device performance validation remain outstanding.

## Validation

CI compilation and emulator smoke tests are the first validation layer. They do not replace physical-device tests on an actual GLES 3.2 device and a GLES 3.0-only device. See docs/ROADMAP.md for the separate source and hardware-verification checklists.
