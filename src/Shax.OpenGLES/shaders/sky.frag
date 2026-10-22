#version 300 es
precision highp float;
in vec2 vUv;
out vec4 fragColor;
uniform vec3 uZenithColor;
uniform vec3 uHorizonColor;
uniform float uHorizonPower;
void main() {
    float height = clamp(vUv.y, 0.0, 1.0);
    float blend = pow(height, max(uHorizonPower, 0.05));
    vec3 sky = mix(uHorizonColor, uZenithColor, blend);
    fragColor = vec4(sky, 1.0);
}
