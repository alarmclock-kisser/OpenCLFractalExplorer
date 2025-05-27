// Hilfsfunktion: 3D -> 2D-Projektion
float2 project(
    float3 v,
    float cx, float sx,
    float cy, float sy,
    float cz, float sz,
    float3 cam,
    float scale,
    int width,
    int height
) {
    float3 rx = (float3)(v.x, cx*v.y - sx*v.z, sx*v.y + cx*v.z);
    float3 ry = (float3)(cy*rx.x + sy*rx.z, rx.y, -sy*rx.x + cy*rx.z);
    float3 rz = (float3)(cz*ry.x - sz*ry.y, sz*ry.x + cz*ry.y, ry.z);
    float3 p = rz - cam;
    float fov = 2.0f;
    float px = p.x / (p.z / fov);
    float py = p.y / (p.z / fov);
    return (float2)(width/2 + px*scale, height/2 - py*scale);
}

// Gleichmäßige Verteilung der Ecken auf einer Kugel (Fibonacci-Lattice)
float3 getCorner(int i, int n_corners) {
    float phi = acos(1.0f - 2.0f * (i + 0.5f) / n_corners);
    float theta = 3.1415926f * (1.0f + sqrt(5.0f)) * (i + 0.5f);
    float x = sin(phi) * cos(theta);
    float y = sin(phi) * sin(theta);
    float z = cos(phi);
    return (float3)(x, y, z);
}

__kernel void bodyRenderer01(
    __global uchar* pixels,
    int width,
    int height,
    int n_corners,
    float rotX,
    float rotY,
    float rotZ
) {
    float cx = cos(rotX), sx = sin(rotX);
    float cy = cos(rotY), sy = sin(rotY);
    float cz = cos(rotZ), sz = sin(rotZ);

    int x = get_global_id(0);
    int y = get_global_id(1);
    if (x >= width || y >= height) return;

    uchar4 color = (uchar4)(255,255,255,255); // weißer Hintergrund

    float scale = min(width, height) * 0.35f;
    float3 cam = (float3)(0,0,-4);

    // Ecken berechnen
    // (maximal 64 Ecken, für größere n_corners bitte erhöhen)
    float3 corners[64];
    int max_c = min(n_corners, 64);
    for (int i = 0; i < max_c; i++) {
        corners[i] = getCorner(i, n_corners);
    }

    // Kanten: Jede Ecke mit jeder anderen verbinden (geschlossener Körper)
    float minDist = 2.0f;
    for (int i = 0; i < max_c; i++) {
        for (int j = i+1; j < max_c; j++) {
            float3 v0 = corners[i];
            float3 v1 = corners[j];

            float2 p0 = project(v0, cx, sx, cy, sy, cz, sz, cam, scale, width, height);
            float2 p1 = project(v1, cx, sx, cy, sy, cz, sz, cam, scale, width, height);

            float2 pa = (float2)(x, y) - p0;
            float2 ba = p1 - p0;
            float ba_dot = dot(ba, ba);
            if (ba_dot == 0.0f) continue;
            float h = clamp(dot(pa, ba) / ba_dot, 0.0f, 1.0f);
            float2 s = p0 + h*ba;
            float dist = distance((float2)(x, y), s);
            if (dist < minDist) minDist = dist;
        }
    }
    if (minDist < 1.2f) {
        color = (uchar4)(0,0,0,255); // schwarze Linie
    }

    int idx = (y * width + x) * 4;
    pixels[idx + 0] = color.x;
    pixels[idx + 1] = color.y;
    pixels[idx + 2] = color.z;
    pixels[idx + 3] = color.w;
}
