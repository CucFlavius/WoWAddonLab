namespace WoWAddonLab.Emulator.Lua;

public sealed class WowClubFinderTabardInfoState
{
    public WowClubFinderColorState BackgroundColor { get; init; } =
        new(0, 0, 0);

    public WowClubFinderColorState BorderColor { get; init; } =
        new(0, 0, 0);

    public WowClubFinderColorState EmblemColor { get; init; } =
        new(0, 0, 0);

    public int EmblemFileId { get; init; }
    public int EmblemStyle { get; init; }
}
