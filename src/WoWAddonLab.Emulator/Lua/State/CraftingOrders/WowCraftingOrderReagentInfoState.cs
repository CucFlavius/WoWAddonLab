namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOrderReagentInfoState(
    WowCraftingReagentInfo Reagent,
    int DataSlotIndex,
    int Quantity);
