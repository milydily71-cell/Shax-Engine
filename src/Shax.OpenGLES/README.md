# Shax.OpenGLES

This folder begins the OpenGL ES 3.0 shader foundation. GLSL ES 3.00 is required; compile shaders only after a GLES 3 context is current. The Android host must handle context recreation and recreate all GPU objects afterward.

Initial shaders:
- `shaders/sky.vert` + `shaders/sky.frag`: full-screen zenith/horizon gradient.
- `shaders/grid.vert` + `shaders/grid.frag`: world-space ground grid with distance fade.

These shaders are source assets only until a C# GLES backend compiles them, supplies uniforms, and draws the required geometry/full-screen triangle.
