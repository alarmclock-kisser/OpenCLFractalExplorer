#pragma OPENCL EXTENSION cl_khr_fp64 : enable

// DoubleDouble-Struktur
typedef struct {
    double hi;
    double lo;
} dd;

// DoubleDouble + (Addition)
dd dd_add(dd a, dd b) {
    double s = a.hi + b.hi;
    double v = s - a.hi;
    double t = ((b.hi - v) + (a.hi - (s - v))) + a.lo + b.lo;
    return (dd){ s + t, t - (s + t - s) };
}

// DoubleDouble - (Subtraktion)
dd dd_sub(dd a, dd b) {
    return dd_add(a, (dd){ -b.hi, -b.lo });
}

// DoubleDouble * (Multiplikation)
dd dd_mul(dd a, dd b) {
    double p = a.hi * b.hi;
    double a1 = __builtin_fma(a.hi, b.hi, -p); // Restfehler
    double err = a1 + (a.hi * b.lo + a.lo * b.hi);
    return (dd){ p, err };
}

// double / dd
dd dd_div(double a, dd b) {
    double q = a / b.hi;
    dd qb = dd_mul((dd){ q, 0.0 }, b);
    dd r = dd_sub((dd){ a, 0.0 }, qb);
    double corr = (r.hi + r.lo) / b.hi;
    return dd_add((dd){ q, 0.0 }, (dd){ corr, 0.0 });
}

// Hauptkernel
__kernel void mandelbrotEdgeAware01(
    __global uchar* outputPixels,
    int width,
    int height,
    double zoomHi,
    double zoomLo,
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

    // Zoom als DoubleDouble
    dd zoom = { zoomHi, zoomLo };

    // Berechne normierte Koordinaten
    dd dx = dd_div(2.0 * ((double)px - width / 2.0), zoom);
    dd dy = dd_div(2.0 * ((double)py - height / 2.0), zoom);

    double x0 = dx.hi / width + offsetX;
    double y0 = dy.hi / height + offsetY;

    double x = 0.0;
    double y = 0.0;
    int iter = 0;

    iterCoeff = max(1, min(iterCoeff, 1000));
    int maxIter = 100 + (int)(iterCoeff * log(zoom.hi + 1.0));

    while (x * x + y * y <= 4.0 && iter < maxIter) {
        double xtemp = x * x - y * y + x0;
        y = 2.0 * x * y + y0;
        x = xtemp;
        iter++;
    }

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
