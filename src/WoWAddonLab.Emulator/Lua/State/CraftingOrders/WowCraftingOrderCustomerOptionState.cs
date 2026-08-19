namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOrderCustomerOptionState(
    int SkillLineAbilityId,
    int ProfessionId,
    int SkillUpSkillLineId,
    int SpellId,
    int ItemId,
    string ItemName,
    int PrimaryCategoryId,
    int ItemLevelMinimum,
    int? ItemLevelMaximum = null,
    bool CanUse = false,
    bool BindOnPickup = false,
    IReadOnlyList<int>? QualityItemLevelBonuses = null,
    IReadOnlyList<int>? CraftingQualityIds = null,
    int? Quality = null,
    int? Slots = null,
    int? Level = null,
    int? Skill = null,
    int? SecondaryCategoryId = null,
    int? TertiaryCategoryId = null,
    int? ExpansionId = null);
