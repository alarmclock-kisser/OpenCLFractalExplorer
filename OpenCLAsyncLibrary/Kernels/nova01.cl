#pragma OPENCL EXTENSION cl_khr_fp64 : enable

__kernel void novaFractal(
    __global uchar* outputPixels,
    const int width,
    const int height,
    const double zoom,
    const double offsetX,
    const double offsetY,
    const int iterCoeff,
    const int baseR,
    const int baseG,
    const int baseB)
{
    int px = get_global_id(0);
    int py = get_global_id(1);
    if (px >= width || py >= height)
        return;

    int maxIter = 100 + (int)(clamp(iterCoeff, 1, 1000) * log(zoom + 1.0));

    double cx = (px - width / 2.0) / (width / 2.0) / zoom + offsetX;
    double cy = (py - height / 2.0) / (height / 2.0) / zoom + offsetY;
    double zx = cx;
    double zy = cy;

    int iter = 0;
    while (zx * zx + zy * zy < 100.0 && iter < maxIter)
    {
        double r2 = zx * zx + zy * zy;
        double r4 = r2 * r2;
        double denom = 3.0 * r4;
        if (denom == 0.0) break;

        double fx = zx * (zx * zx - 3.0 * zy * zy) - 1.0;
        double fy = zy * (3.0 * zx * zx - zy * zy);

        zx = zx - fx / denom;
        zy = zy - fy / denom;
        iter++;
    }

    float t = (float)iter / maxIter;
    int index = (py * width + px) * 4;
    outputPixels[index + 0] = (uchar)(baseR * t);
    outputPixels[index + 1] = (uchar)(baseG * t);
    outputPixels[index + 2] = (uchar)(baseB * t);
    outputPixels[index + 3] = 255;
}
