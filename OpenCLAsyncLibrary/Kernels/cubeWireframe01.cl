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

__kernel void cubeWireframe01(
    __global uchar* pixels,
    int width,
    int height,
    float rotX,
    float rotY,
    float rotZ
) {
    const float3 cube[8] = {
        (float3)(-1, -1, -1),
        (float3)( 1, -1, -1),
        (float3)( 1,  1, -1),
        (float3)(-1,  1, -1),
        (float3)(-1, -1,  1),
        (float3)( 1, -1,  1),
        (float3)( 1,  1,  1),
        (float3)(-1,  1,  1)
    };
    const int edges[12][2] = {
        {0,1},{1,2},{2,3},{3,0},
        {4,5},{5,6},{6,7},{7,4},
        {0,4},{1,5},{2,6},{3,7}
    };

    float cx = cos(rotX), sx = sin(rotX);
    float cy = cos(rotY), sy = sin(rotY);
    float cz = cos(rotZ), sz = sin(rotZ);

    int x = get_global_id(0);
    int y = get_global_id(1);
    if (x >= width || y >= height) return;

    // Standard: weißer Hintergrund, volle Deckkraft
    uchar4 color = (uchar4)(255,255,255,255);

    float scale = min(width, height) * 0.35f;
    float3 cam = (float3)(0,0,-4);

    float minDist = 2.0f;
    for (int i = 0; i < 12; i++) {
        float2 p0 = project(cube[edges[i][0]], cx, sx, cy, sy, cz, sz, cam, scale, width, height);
        float2 p1 = project(cube[edges[i][1]], cx, sx, cy, sy, cz, sz, cam, scale, width, height);
        float2 pa = (float2)(x, y) - p0;
        float2 ba = p1 - p0;
        float ba_dot = dot(ba, ba);
        if (ba_dot == 0.0f) continue;
        float h = clamp(dot(pa, ba) / ba_dot, 0.0f, 1.0f);
        float2 s = p0 + h*ba;
        float dist = distance((float2)(x, y), s);
        if (dist < minDist) minDist = dist;
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
