namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTraitConfigInfoState(
    int Id,
    uint Type,
    string Name,
    IReadOnlyList<int> TreeIds,
    bool UsesSharedActionBars);
