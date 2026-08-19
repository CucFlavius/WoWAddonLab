namespace WoWAddonLab.Emulator.Lua;

public sealed record WowScrappingRequest(
    IReadOnlyList<WowItemLocation> Items);
