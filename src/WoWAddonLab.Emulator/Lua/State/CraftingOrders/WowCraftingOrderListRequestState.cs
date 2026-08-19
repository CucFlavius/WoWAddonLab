namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOrderListRequestState(
    WowCraftingOrderSortState PrimarySort,
    WowCraftingOrderSortState SecondarySort,
    uint Offset,
    bool HasCallback);
