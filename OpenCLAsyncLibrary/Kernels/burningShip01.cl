#pragma OPENCL EXTENSION cl_khr_fp64 : enable

__kernel void burningShip01(
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

    double x0 = (px - width / 2.0) / (width / 2.0) / zoom + offsetX;
    double y0 = (py - height / 2.0) / (height / 2.0) / zoom + offsetY;
    double x = 0.0;
    double y = 0.0;
    int iter = 0;

    while (x * x + y * y <= 4.0 && iter < maxIter)
    {
        double xtemp = x * x - y * y + x0;
        y = fabs(2.0 * x * y) + y0;
        x = fabs(xtemp);
        iter++;
    }

    float t = (float)iter / maxIter;
    int index = (py * width + px) * 4;
    outputPixels[index + 0] = (uchar)(baseR * t);
    outputPixels[index + 1] = (uchar)(baseG * t);
    outputPixels[index + 2] = (uchar)(baseB * t);
    outputPixels[index + 3] = 255;
}
