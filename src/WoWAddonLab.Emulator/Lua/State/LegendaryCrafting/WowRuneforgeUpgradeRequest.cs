namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRuneforgeUpgradeRequest(
    WowItemLocation RuneforgeLegendary,
    WowItemLocation UpgradeItem);
