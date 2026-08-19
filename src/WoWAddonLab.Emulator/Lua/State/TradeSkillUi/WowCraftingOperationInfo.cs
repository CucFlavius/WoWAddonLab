namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOperationInfo(
    int RecipeId,
    int BaseDifficulty,
    int BonusDifficulty,
    int BaseSkill,
    int BonusSkill,
    bool IsQualityCraft,
    float Quality,
    int CraftingQuality,
    int CraftingQualityId,
    int CraftingDataId,
    int LowerSkillThreshold,
    int UpperSkillThreshold,
    int GuaranteedCraftingQualityId,
    IReadOnlyList<WowCraftingOperationBonusStatInfo> BonusStats,
    int ConcentrationCurrencyId,
    int ConcentrationCost,
    int IngenuityRefund);
