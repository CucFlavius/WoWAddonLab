namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingReagentSlotInfo(
    int McrSlotId,
    int RequiredSkillRank,
    string? SlotText);
