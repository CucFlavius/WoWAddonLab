namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingReagentSlotSchematic(
    IReadOnlyList<WowCraftingReagentInfo> Reagents,
    uint ReagentType,
    IReadOnlyList<WowCraftingReagentQuantity> VariableQuantities,
    int QuantityRequired,
    WowCraftingReagentSlotInfo? SlotInfo,
    uint DataSlotType,
    int DataSlotIndex,
    int SlotIndex,
    int? OrderSource,
    bool Required,
    bool HiddenInCraftingForm);
