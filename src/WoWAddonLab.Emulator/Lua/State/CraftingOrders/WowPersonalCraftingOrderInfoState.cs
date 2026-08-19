namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPersonalCraftingOrderInfoState(
    uint Profession,
    int NumberOfPersonalOrders,
    string? ProfessionName);
