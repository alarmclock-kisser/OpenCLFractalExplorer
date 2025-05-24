#pragma OPENCL EXTENSION cl_khr_fp64 : enable

inline int mandelbrot(double real, double imag, int maxIter)
{
    double zReal = 0.0;
    double zImag = 0.0;
    int iter = 0;

    while (zReal * zReal + zImag * zImag <= 4.0 && iter < maxIter)
    {
        double temp = zReal * zReal - zImag * zImag + real;
        zImag = 2.0 * zReal * zImag + imag;
        zReal = temp;
        iter++;
    }
    return iter;
}

__kernel void mandelbrotDD01(
    const double zoomHi,
    const double zoomLo,
    __global const uchar* input,
    __global uchar* output,
    const int width,
    const int height,
    const double offsetX,
    const double offsetY,
    const int maxIter,
    const int baseR,
    const int baseG,
    const int baseB)
{
    int x = get_global_id(0);
    int y = get_global_id(1);
    int idx = y * width + x;

    if (x >= width || y >= height)
        return;

    double zoom = zoomHi + zoomLo;
    double scale = 1.0 / (zoom * (width / 4.0)); // Korrigierte Skalierung

    double real = ((double)x - (double)width / 2.0) * scale + offsetX;
    double imag = ((double)y - (double)height / 2.0) * scale + offsetY;

    int iter = mandelbrot(real, imag, maxIter);

    // Einfarbige Ausgabe mit Int-RGB
    if (iter == maxIter)
    {
        // Innerhalb der Menge: Basis-Farbe
        output[idx] = (baseR << 16) | (baseG << 8) | baseB;
    }
    else
    {
        // Außerhalb: Farbverlauf basierend auf Iterationen
        float t = (float)iter / (float)maxIter;
        int r = (int)(baseR * t);
        int g = (int)(baseG * t);
        int b = (int)(baseB * t);
        output[idx] = (r << 16) | (g << 8) | b;
    }
}