namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGatheringOperationInfo(
    int SpellId,
    int MaxDifficulty,
    int BaseSkill,
    int BonusSkill,
    IReadOnlyList<WowCraftingOperationBonusStatInfo> BonusStats);
