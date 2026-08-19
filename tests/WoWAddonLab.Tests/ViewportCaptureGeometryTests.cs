using System.Numerics;
using WoWAddonLab.Diagnostics;
using Silk.NET.Maths;

namespace WoWAddonLab.Tests;

public sealed class ViewportCaptureGeometryTests
{
    [Fact]
    public void ResolvesLogicalTopLeftCoordinatesIntoClampedOpenGlPixels()
    {
        var rectangle = ViewportCaptureGeometry.Resolve(
            new Vector2(10.25f, 20.5f),
            new Vector2(100.5f, 50.25f),
            new Vector2(2, 1.5f),
            new Vector2D<int>(400, 300));

        Assert.Equal(new ViewportReadRectangle(20, 193, 202, 77), rectangle);
    }

    [Fact]
    public void FlipsOpenGlRowsToTopDownPngOrder()
    {
        var pixels = new byte[]
        {
            1, 1, 1, 1,
            2, 2, 2, 2,
            3, 3, 3, 3
        };

        ViewportCaptureGeometry.FlipRowsInPlace(pixels, 1, 3);

        Assert.Equal(
            new byte[]
            {
                3, 3, 3, 3,
                2, 2, 2, 2,
                1, 1, 1, 1
            },
            pixels);
    }
}
