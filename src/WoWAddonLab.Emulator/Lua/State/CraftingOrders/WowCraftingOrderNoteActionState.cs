namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOrderNoteActionState(
    ulong OrderId,
    string CrafterNote,
    byte Profession);
