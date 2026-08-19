namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOrderActionState(
    ulong OrderId,
    byte Profession);
