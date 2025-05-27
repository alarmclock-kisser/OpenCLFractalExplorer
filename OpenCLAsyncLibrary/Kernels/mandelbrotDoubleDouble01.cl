#pragma OPENCL EXTENSION cl_khr_fp64 : enable

typedef double2 dd;

inline dd dd_add(dd a, dd b) {
    double s = a.x + b.x;
    double v = s - a.x;
    double t = ((b.x - v) + (a.x - (s - v))) + a.y + b.y;
    return (dd)(s, t);
}

inline dd dd_sub(dd a, dd b) {
    b.x = -b.x; b.y = -b.y;
    return dd_add(a, b);
}

inline dd dd_mul(dd a, dd b) {
    double p = a.x * b.x;
    double e = fma(a.x, b.x, -p) + a.x * b.y + a.y * b.x;
    return (dd)(p, e);
}

inline dd dd_div(dd a, dd b) {
    double q = a.x / b.x;
    dd qb = dd_mul((dd)(q, 0.0), b);
    dd r = dd_sub(a, qb);
    double q2 = (r.x + r.y) / b.x;
    return (dd)(q + q2, 0.0);
}

__kernel void mandelbrotDoubleDouble01(
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

    iterCoeff = max(1, min(iterCoeff, 1000));
    double zoomApprox = zoomHi + zoomLo;
    int maxIter = 100 + (int)(iterCoeff * log(zoomApprox + 1.0));

    dd zoom = (dd)(zoomHi, zoomLo);

    double normX = ((double)px - width / 2.0) / (width / 2.0);
    double normY = ((double)py - height / 2.0) / (height / 2.0);

    dd x0 = dd_add(dd_div((dd)(normX, 0.0), zoom), (dd)(offsetX, 0.0));
    dd y0 = dd_add(dd_div((dd)(normY, 0.0), zoom), (dd)(offsetY, 0.0));

    dd x = (dd)(0.0, 0.0);
    dd y = (dd)(0.0, 0.0);

    int iter = 0;
    while (iter < maxIter) {
        dd x2 = dd_mul(x, x);
        dd y2 = dd_mul(y, y);
        dd xy = dd_mul(x, y);

        dd sum = dd_add(x2, y2);
        if (sum.x > 4.0) break;

        dd xtemp = dd_add(dd_sub(x2, y2), x0);
        y = dd_add(dd_add(xy, xy), y0);
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
