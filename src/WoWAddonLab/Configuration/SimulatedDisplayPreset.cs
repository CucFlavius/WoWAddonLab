namespace WoWAddonLab.Configuration;

internal sealed record SimulatedDisplayPreset(string Group, int Width, int Height)
{
    public string Label => $"{Width} x {Height}";

    public static readonly IReadOnlyList<SimulatedDisplayPreset> All =
    [
        new("16:9", 1280, 720),
        new("16:9", 1600, 900),
        new("16:9", 1920, 1080),
        new("16:9", 2560, 1440),
        new("16:9", 3840, 2160),
        new("16:10", 1920, 1200),
        new("16:10", 2560, 1600),
        new("Ultrawide", 2560, 1080),
        new("Ultrawide", 3440, 1440),
        new("Ultrawide", 5120, 1440)
    ];
}
