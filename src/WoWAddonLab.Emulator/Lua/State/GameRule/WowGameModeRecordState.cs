namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGameModeRecordState(int Id)
{
    public string? GlueScreenName { get; init; }
    public string? PromoGlobalString { get; init; }
    public WowGameModeDisplayInfoState? DisplayInfo { get; init; }
}
