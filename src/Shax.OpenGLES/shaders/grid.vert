#version 300 es
precision highp float;
layout(location = 0) in vec3 aPosition;
uniform mat4 uViewProjection;
out vec3 vWorldPosition;
void main() {
    vWorldPosition = aPosition;
    gl_Position = uViewProjection * vec4(aPosition, 1.0);
}
