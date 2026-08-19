namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingItemSlotModification(
    int DataSlotIndex,
    WowCraftingReagentInfo Reagent);
