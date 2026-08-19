namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOrderBucketInfoState(
    int ItemId,
    int SpellId,
    int SkillLineAbilityId,
    ulong TipAmountAverage,
    ulong TipAmountMaximum,
    int NumberAvailable);
