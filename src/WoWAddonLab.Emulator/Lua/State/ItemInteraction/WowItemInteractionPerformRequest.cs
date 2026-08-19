namespace WoWAddonLab.Emulator.Lua;

public sealed record WowItemInteractionPerformRequest(
    WowItemLocation Item,
    int SlotIndex);
