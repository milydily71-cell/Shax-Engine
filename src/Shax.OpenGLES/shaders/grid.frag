#version 300 es
precision highp float;
in vec3 vWorldPosition;
out vec4 fragColor;
uniform vec3 uGridColor;
uniform vec3 uXAxisColor;
uniform vec3 uZAxisColor;
uniform vec3 uCameraPosition;
uniform float uGridSpacing;
uniform float uGridThickness;
uniform float uFadeDistance;

float lineMask(vec2 p, float spacing, float thickness) {
    vec2 cell = abs(fract(p / spacing - 0.5) - 0.5) * spacing;
    float d = min(cell.x, cell.y);
    return 1.0 - smoothstep(thickness, thickness * 1.8, d);
}

void main() {
    float spacing = max(uGridSpacing, 0.001);
    float minor = lineMask(vWorldPosition.xz, spacing, max(uGridThickness, 0.001));
    float major = lineMask(vWorldPosition.xz, spacing * 10.0, max(uGridThickness * 1.6, 0.001));
    float distanceToCamera = distance(vWorldPosition, uCameraPosition);
    float fade = 1.0 - smoothstep(uFadeDistance * 0.55, max(uFadeDistance, 0.56), distanceToCamera);
    vec3 color = uGridColor;
    color = mix(color, uGridColor * 1.45, major * 0.65);
    float axisX = 1.0 - smoothstep(0.015, 0.04, abs(vWorldPosition.z));
    float axisZ = 1.0 - smoothstep(0.015, 0.04, abs(vWorldPosition.x));
    color = mix(color, uXAxisColor, axisX);
    color = mix(color, uZAxisColor, axisZ);
    float alpha = clamp(max(minor * 0.45, major * 0.8) * fade, 0.0, 0.85);
    if (alpha < 0.01) discard;
    fragColor = vec4(color, alpha);
}
