namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOperationBonusStatInfo(
    string BonusStatName,
    int BonusStatValue,
    string RatingDescription,
    float RatingPercent,
    float BonusRatingPercent);
