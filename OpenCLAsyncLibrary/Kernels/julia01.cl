#pragma OPENCL EXTENSION cl_khr_fp64 : enable

__kernel void juliaAutoPrecise01(
    __global const uchar* inputPixels,
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

    int clampedIterCoeff = max(1, min(iterCoeff, 1000));
    int maxIter = 100 + (int)(clampedIterCoeff * log(zoom + 1.0));

    // Koordinate auf komplexer Ebene berechnen
    double x = (px - width / 2.0) / (width / 2.0) / zoom + offsetX;
    double y = (py - height / 2.0) / (height / 2.0) / zoom + offsetY;

    // Klassisches Julia-c (kann auch als Parameter übergeben werden)
    const double cx = -0.8;
    const double cy = 0.156;

    int iter = 0;
    while (x * x + y * y <= 4.0 && iter < maxIter)
    {
        double xtemp = x * x - y * y + cx;
        y = 2.0 * x * y + cy;
        x = xtemp;
        iter++;
    }

    // Farbwert berechnen
    float t = (float)iter / maxIter;
    uchar r = (uchar)(baseR * t);
    uchar g = (uchar)(baseG * t);
    uchar b = (uchar)(baseB * t);

    int index = (py * width + px) * 4;
    outputPixels[index + 0] = r;
    outputPixels[index + 1] = g;
    outputPixels[index + 2] = b;
    outputPixels[index + 3] = 255; // Alpha
}
