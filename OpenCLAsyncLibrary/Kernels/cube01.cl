// Helper function to rotate a 3D point
float3 rotate(float3 point, float3 angles) {
    float cx = cos(angles.x);
    float sx = sin(angles.x);
    float cy = cos(angles.y);
    float sy = sin(angles.y);
    float cz = cos(angles.z);
    float sz = sin(angles.z);
    
    // Rotation matrices combined
    float3 result;
    result.x = cy*cz * point.x + (sx*sy*cz - cx*sz) * point.y + (cx*sy*cz + sx*sz) * point.z;
    result.y = cy*sz * point.x + (sx*sy*sz + cx*cz) * point.y + (cx*sy*sz - sx*cz) * point.z;
    result.z = -sy * point.x + sx*cy * point.y + cx*cy * point.z;
    
    return result;
}

// Project 3D to 2D with perspective
float2 project(float3 point, float zoom) {
    float z = point.z + 3.0f; // Distance from camera
    return (float2)(point.x / z * zoom, point.y / z * zoom);
}

// Draw line between two points
void drawLine(__global uchar* pixels, int width, int height, 
              float2 p1, float2 p2, uchar r, uchar g, uchar b) {
    float dist = distance(p1, p2);
    int steps = (int)(dist * 2.0f);
    
    for (int i = 0; i <= steps; i++) {
        float t = (float)i / steps;
        float2 p = mix(p1, p2, t);
        int px = (int)(p.x + width/2);
        int py = (int)(p.y + height/2);
        
        if (px >= 0 && px < width && py >= 0 && py < height) {
            int idx = (py * width + px) * 3;
            pixels[idx] = r;
            pixels[idx+1] = g;
            pixels[idx+2] = b;
        }
    }
}

__kernel void cube01(
    __global uchar* outputPixels,
    int width,
    int height,
    float zoom,
    float rotationX,
    float rotationY,
    float rotationZ,
    uchar backR,
    uchar backG,
    uchar backB)
{
    int x = get_global_id(0);
    int y = get_global_id(1);
    
    // Clear background
    int idx = (y * width + x) * 3;
    outputPixels[idx] = backR;
    outputPixels[idx+1] = backG;
    outputPixels[idx+2] = backB;
    
    barrier(CLK_GLOBAL_MEM_FENCE);
    
    // Only thread 0 draws the cube (for simplicity)
    if (x == 0 && y == 0) {
        // Normalize rotation angles (-1 to 1 -> -2π to 2π)
        float3 rot;
        rot.x = fmod(rotationX, 1.0f) * 2.0f * M_PI;
        rot.y = fmod(rotationY, 1.0f) * 2.0f * M_PI;
        rot.z = fmod(rotationZ, 1.0f) * 2.0f * M_PI;
        
        // Cube vertices (1 unit size, centered at origin)
        float3 vertices[8] = {
            {-0.5f, -0.5f, -0.5f},
            { 0.5f, -0.5f, -0.5f},
            { 0.5f,  0.5f, -0.5f},
            {-0.5f,  0.5f, -0.5f},
            {-0.5f, -0.5f,  0.5f},
            { 0.5f, -0.5f,  0.5f},
            { 0.5f,  0.5f,  0.5f},
            {-0.5f,  0.5f,  0.5f}
        };
        
        // Rotate all vertices
        float3 rotated[8];
        for (int i = 0; i < 8; i++) {
            rotated[i] = rotate(vertices[i], rot);
        }
        
        // Cube edges (vertex indices)
        int2 edges[12] = {
            {0,1}, {1,2}, {2,3}, {3,0}, // Bottom
            {4,5}, {5,6}, {6,7}, {7,4}, // Top
            {0,4}, {1,5}, {2,6}, {3,7}  // Sides
        };
        
        // Draw all edges
        for (int i = 0; i < 12; i++) {
            float2 p1 = project(rotated[edges[i].x], zoom);
            float2 p2 = project(rotated[edges[i].y], zoom);
            drawLine(outputPixels, width, height, p1, p2, 255, 255, 255); // White edges
        }
    }
}