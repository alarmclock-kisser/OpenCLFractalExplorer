__kernel void Mandelbrot05(
    __global uchar* pixels,
    int width,
    int height,
    float zoom,
    float offsetX,
    float offsetY,
    int maxIter,
    int hueR,
    int hueG,
    int hueB)
{
    int px = get_global_id(0);
    int py = get_global_id(1);

    if (px >= width || py >= height)
        return;

    float x0 = (px - width / 2.0f) / (width / 2.0f) / zoom + offsetX;
    float y0 = (py - height / 2.0f) / (height / 2.0f) / zoom + offsetY;

    float x = 0.0f;
    float y = 0.0f;
    int iter = 0;

    while (x * x + y * y <= 4.0f && iter < maxIter)
    {
        float xtemp = x * x - y * y + x0;
        y = 2.0f * x * y + y0;
        x = xtemp;
        iter++;
    }

    int pixelIndex = (py * width + px) * 4;

    if (iter == maxIter) {
        // Schwarz für Punkte innerhalb der Menge
        pixels[pixelIndex + 0] = 0;
        pixels[pixelIndex + 1] = 0;
        pixels[pixelIndex + 2] = 0;
        pixels[pixelIndex + 3] = 255;
    } else {
        // Farbverlauf basierend auf Iteration und Grundfarbe
        float t = (float)iter / (float)maxIter;
        uchar r = (uchar)(fmin(1.0f, t) * hueR);
        uchar g = (uchar)(fmin(1.0f, t) * hueG);
        uchar b = (uchar)(fmin(1.0f, t) * hueB);

        pixels[pixelIndex + 0] = r;
        pixels[pixelIndex + 1] = g;
        pixels[pixelIndex + 2] = b;
        pixels[pixelIndex + 3] = 255;
    }
}
