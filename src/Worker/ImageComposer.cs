using SkiaSharp;
using Shared;

namespace Worker;

public static class ImageComposer
{
    public static byte[] Compose(byte[] source, ImageTaskMessage task)
    {
        using var bitmap = SKBitmap.Decode(source);
        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));

        var canvas = surface.Canvas;
        canvas.DrawBitmap(bitmap, 0, 0);

        var bandTop = bitmap.Height - 150;

        using var band = new SKPaint { Color = new SKColor(0, 0, 0, 140) };
        canvas.DrawRect(0, bandTop, bitmap.Width, 150, band);

        using var typeface = SKTypeface.Default;
        using var titleFont = new SKFont(typeface, 44);
        using var bodyFont = new SKFont(typeface, 28);
        using var ink = new SKPaint { Color = SKColors.White, IsAntialias = true };

        var temp = task.Temperature.HasValue
            ? $"{task.Temperature.Value:0.#} °C"
            : "no reading";

        canvas.DrawText(task.StationName, 30, bandTop + 50, SKTextAlign.Left, titleFont, ink);
        canvas.DrawText(temp, 30, bandTop + 90, SKTextAlign.Left, bodyFont, ink);
        canvas.DrawText(task.Description ?? "", 30, bandTop + 125, SKTextAlign.Left, bodyFont, ink);

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }
}