﻿#pragma OPENCL EXTENSION cl_khr_fp64 : enable

__kernel void mandelbrotEdgesOnly01(
    __global const uchar* inputPixels, 
    __global uchar* outputPixels,
    int width,
    int height,
    double zoom,
    double offsetX,
    double offsetY,
    int iterCoeff,
    int baseR,
    int baseG,
    int baseB,
    int edgeThickness)
{
    int px = get_global_id(0);
    int py = get_global_id(1);

    if (px >= width || py >= height) return;

    // Index berechnen (RGBA = 4 Kanäle)
    int idx = (py * width + px) * 4;

    // Randbedingung prüfen (oben, unten, links, rechts)
    bool isEdge = (px < edgeThickness || px >= width - edgeThickness ||
                   py < edgeThickness || py >= height - edgeThickness);

    if (!isEdge)
    {
        // Innenbereich → direkt kopieren
        outputPixels[idx + 0] = inputPixels[idx + 0];
        outputPixels[idx + 1] = inputPixels[idx + 1];
        outputPixels[idx + 2] = inputPixels[idx + 2];
        outputPixels[idx + 3] = inputPixels[idx + 3];
        return;
    }

    // Iterationsanzahl (logarithmisch abhängig von Zoom)
    iterCoeff = max(1, min(iterCoeff, 1000));
    int maxIter = 100 + (int)(iterCoeff * log(zoom + 1.0));

    // Normierte Koordinaten (Zentrum = 0,0)
    double x0 = ((double)px - width / 2.0) / (width / 2.0) / zoom + offsetX;
    double y0 = ((double)py - height / 2.0) / (height / 2.0) / zoom + offsetY;

    // Mandelbrot-Iteration
    double x = 0.0;
    double y = 0.0;
    int iter = 0;

    while (x*x + y*y <= 4.0 && iter < maxIter)
    {
        double xtemp = x*x - y*y + x0;
        y = 2.0 * x * y + y0;
        x = xtemp;
        iter++;
    }

    if (iter == maxIter)
    {
        outputPixels[idx + 0] = baseR;
        outputPixels[idx + 1] = baseG;
        outputPixels[idx + 2] = baseB;
    }
    else
    {
        float t = (float)iter / (float)maxIter;
        float r = sin(t * 3.14159f) * 255.0f;
        float g = sin(t * 6.28318f + 1.0472f) * 255.0f;
        float b = sin(t * 9.42477f + 2.0944f) * 255.0f;

        outputPixels[idx + 0] = clamp((int)(baseR + r * (1.0f - t)), 0, 255);
        outputPixels[idx + 1] = clamp((int)(baseG + g * (1.0f - t)), 0, 255);
        outputPixels[idx + 2] = clamp((int)(baseB + b * (1.0f - t)), 0, 255);
    }

    outputPixels[idx + 3] = 255;
}
