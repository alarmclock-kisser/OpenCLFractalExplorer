#pragma OPENCL EXTENSION cl_khr_fp64 : enable

__kernel void phoenix01(
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

    double zx = 0.0;
    double zy = 0.0;
    double zx_old = 0.0;
    double zy_old = 0.0;

    double cx = (px - width / 2.0) / (width / 2.0) / zoom + offsetX;
    double cy = (py - height / 2.0) / (height / 2.0) / zoom + offsetY;

    double p = -0.5; // Phoenix-Konstante
    int iter = 0;
    while (zx * zx + zy * zy < 4.0 && iter < maxIter)
    {
        double xtemp = zx * zx - zy * zy + cx + p * zx_old;
        zy = 2.0 * zx * zy + cy + p * zy_old;
        zx = xtemp;

        zx_old = zx;
        zy_old = zy;
        iter++;
    }

    float t = (float)iter / maxIter;
    int index = (py * width + px) * 4;
    outputPixels[index + 0] = (uchar)(baseR * t);
    outputPixels[index + 1] = (uchar)(baseG * t);
    outputPixels[index + 2] = (uchar)(baseB * t);
    outputPixels[index + 3] = 255;
}
