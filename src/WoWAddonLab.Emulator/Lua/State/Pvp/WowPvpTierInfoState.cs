namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPvpTierInfoState(
    string Name,
    int DescendRating,
    int AscendRating,
    int DescendTier,
    int AscendTier,
    int PvpTierEnum,
    int TierIconId);
