using System.Numerics;

namespace WoWAddonLab.Tests;

public sealed class CanvasPanClampTests
{
    private static readonly Vector2 Origin = new(100, 50);
    private static readonly Vector2 Available = new(1200, 800);

    private static float VisibleFraction(Vector2 pan, Vector2 size, int axis)
    {
        var clamped = LabHost.ClampCanvasPan(pan, Origin, Available, size);
        var canvasStart = CanvasOrigin(clamped, size, axis);
        var canvasEnd = canvasStart + Component(size, axis);
        var viewStart = Component(Origin, axis);
        var viewEnd = viewStart + Component(Available, axis);
        var overlap = MathF.Min(canvasEnd, viewEnd) - MathF.Max(canvasStart, viewStart);
        return overlap / MathF.Min(Component(size, axis), Component(Available, axis));
    }

    private static float Component(Vector2 value, int axis) => axis == 0 ? value.X : value.Y;

    private static float CanvasOrigin(Vector2 clampedPan, Vector2 size, int axis)
    {
        var centre = Vector2.Max(Vector2.Zero, (Available - size) / 2);
        return Component(Origin, axis) + Component(centre, axis) + Component(clampedPan, axis);
    }

    [Theory]
    [InlineData(600, 400)]
    [InlineData(1200, 800)]
    [InlineData(4000, 2600)]
    [InlineData(12000, 8000)]
    public void ExtremePanKeepsAtLeastATenthVisible(float width, float height)
    {
        var size = new Vector2(width, height);
        foreach (var pan in new[]
                 {
                     new Vector2(-100000, -100000),
                     new Vector2(100000, 100000),
                     new Vector2(-100000, 100000),
                     new Vector2(100000, -100000)
                 })
        {
            Assert.True(VisibleFraction(pan, size, 0) >= 0.099f);
            Assert.True(VisibleFraction(pan, size, 1) >= 0.099f);
        }
    }

    [Theory]
    [InlineData(600, 400)]
    [InlineData(4000, 2600)]
    public void ExtremePanPushesExactlyToTheBoundary(float width, float height)
    {
        var size = new Vector2(width, height);
        Assert.Equal(0.1f, VisibleFraction(new Vector2(100000, 0), size, 0), 3);
        Assert.Equal(0.1f, VisibleFraction(new Vector2(-100000, 0), size, 0), 3);
    }

    [Fact]
    public void PanWithinBoundsIsUnchanged()
    {
        var size = new Vector2(4000, 2600);
        var pan = new Vector2(-320, 180);
        Assert.Equal(pan, LabHost.ClampCanvasPan(pan, Origin, Available, size));
    }

    [Theory]
    [InlineData(260, 150)]
    [InlineData(417, 235)]
    [InlineData(600, 400)]
    [InlineData(1100, 620)]
    [InlineData(4000, 2600)]
    public void PanWithinBoundsRoundTripsExactly(float width, float height)
    {
        var size = new Vector2(width, height);
        foreach (var pan in new[]
                 {
                     new Vector2(210.8f, -22.5f),
                     new Vector2(0.1f, 0.1f),
                     new Vector2(-63.25f, 41.75f)
                 })
        {
            var clamped = LabHost.ClampCanvasPan(pan, Origin, Available, size);
            Assert.True(
                clamped == pan,
                $"canvas {width}x{height} pan {pan} came back as {clamped}");
        }
    }

    [Fact]
    public void SmallCanvasMayLeaveTheViewportAlmostEntirely()
    {
        var size = new Vector2(600, 400);
        var clamped = LabHost.ClampCanvasPan(new Vector2(100000, 0), Origin, Available, size);
        var centre = Vector2.Max(Vector2.Zero, (Available - size) / 2);
        var canvasLeft = Origin.X + centre.X + clamped.X;
        Assert.Equal(Origin.X + Available.X - 60, canvasLeft, 3);
    }
}
