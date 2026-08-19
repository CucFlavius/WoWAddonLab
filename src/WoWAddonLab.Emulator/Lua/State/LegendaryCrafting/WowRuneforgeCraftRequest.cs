namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRuneforgeCraftRequest(
    WowItemLocation BaseItem,
    int PowerId,
    IReadOnlyList<int> Modifiers);
