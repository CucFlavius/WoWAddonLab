namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOrderReagentState(
    WowCraftingOrderReagentInfoState ReagentInfo,
    int SlotIndex,
    byte Source,
    bool IsBasicReagent);
