using System.Collections.Concurrent;
using System.Numerics;
using WoWAddonLab.Automation;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace WoWAddonLab.Diagnostics;

internal static class ViewportCaptureGeometry
{
    public static ViewportReadRectangle Resolve(
        Vector2 canvasOrigin,
        Vector2 canvasSize,
        Vector2 framebufferScale,
        Vector2D<int> framebufferSize)
    {
        var left = Math.Clamp(
            (int)MathF.Floor(canvasOrigin.X * framebufferScale.X),
            0,
            framebufferSize.X);
        var top = Math.Clamp(
            (int)MathF.Floor(canvasOrigin.Y * framebufferScale.Y),
            0,
            framebufferSize.Y);
        var right = Math.Clamp(
            (int)MathF.Ceiling((canvasOrigin.X + canvasSize.X) * framebufferScale.X),
            left,
            framebufferSize.X);
        var bottom = Math.Clamp(
            (int)MathF.Ceiling((canvasOrigin.Y + canvasSize.Y) * framebufferScale.Y),
            top,
            framebufferSize.Y);
        return new ViewportReadRectangle(
            left,
            framebufferSize.Y - bottom,
            right - left,
            bottom - top);
    }

    public static void FlipRowsInPlace(byte[] rgba, int width, int height)
    {
        var rowBytes = checked(width * 4);
        var temporary = new byte[rowBytes];
        for (var top = 0; top < height / 2; top++)
        {
            var bottom = height - top - 1;
            var topRow = rgba.AsSpan(top * rowBytes, rowBytes);
            var bottomRow = rgba.AsSpan(bottom * rowBytes, rowBytes);
            topRow.CopyTo(temporary);
            bottomRow.CopyTo(topRow);
            temporary.CopyTo(bottomRow);
        }
    }
}
