namespace WoWAddonLab.Automation;

public sealed record ViewportCapture(
    byte[] PngBytes,
    int Width,
    int Height);
