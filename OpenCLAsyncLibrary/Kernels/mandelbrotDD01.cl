﻿#pragma OPENCL EXTENSION cl_khr_fp64 : enable

typedef struct {
    double hi;
    double lo;
} dd;

// DoubleDouble: Addition
dd dd_add(dd a, dd b) {
    double s = a.hi + b.hi;
    double v = s - a.hi;
    double t = ((b.hi - v) + (a.hi - (s - v))) + a.lo + b.lo;
    return (dd){ s + t, t - (s + t - s) };
}

// DoubleDouble: Subtraktion
dd dd_sub(dd a, dd b) {
    return dd_add(a, (dd){ -b.hi, -b.lo });
}

// DoubleDouble: Multiplikation
dd dd_mul(dd a, dd b) {
    double p = a.hi * b.hi;
    double a1 = __builtin_fma(a.hi, b.hi, -p); // Restfehler
    double err = a1 + (a.hi * b.lo + a.lo * b.hi);
    return (dd){ p, err };
}

// double / double -> dd = 1 / d
dd dd_reciprocal(double d) {
    double q = 1.0 / d;
    double p = q * d;
    double r = 1.0 - p;
    return (dd){ q, r / d };
}

// double * dd
dd dd_mul_d(double a, dd b) {
    return dd_mul((dd){ a, 0.0 }, b);
}

// Hauptkernel
__kernel void mandelbrotDD01(
    __global uchar* outputPixels,
    int width,
    int height,
    double zoom,            // Nur 1x double!
    double offsetX,
    double offsetY,
    int iterCoeff,
    int baseR,
    int baseG,
    int baseB)
{
    int px = get_global_id(0);
    int py = get_global_id(1);
    if (px >= width || py >= height) return;

    // Schrittweite: 1 / zoom → als DoubleDouble
    dd invZoom = dd_reciprocal(zoom);

    // Koordinaten in -1.0 ... +1.0, dann skaliert
    double nx = ((double)px - width / 2.0) / (width / 2.0);
    double ny = ((double)py - height / 2.0) / (height / 2.0);

    // Genaue Position: offset + (norm * invZoom)
    dd dx = dd_mul_d(nx, invZoom);
    dd dy = dd_mul_d(ny, invZoom);

    double x0 = dx.hi + offsetX;
    double y0 = dy.hi + offsetY;

    // Mandelbrot-Iterationen
    double x = 0.0;
    double y = 0.0;
    int iter = 0;

    iterCoeff = max(1, min(iterCoeff, 1000));
    int maxIter = 100 + (int)(iterCoeff * log(zoom + 1.0));

    while (x * x + y * y <= 4.0 && iter < maxIter) {
        double xtemp = x * x - y * y + x0;
        y = 2.0 * x * y + y0;
        x = xtemp;
        iter++;
    }

    // Farben setzen
    int idx = (py * width + px) * 4;
    if (iter == maxIter) {
        outputPixels[idx + 0] = baseR;
        outputPixels[idx + 1] = baseG;
        outputPixels[idx + 2] = baseB;
    } else {
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
