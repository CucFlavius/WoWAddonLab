namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRuneforgeModifierInfoRule(
    WowItemLocation BaseItem,
    int? PowerId,
    int AddedModifierIndex,
    IReadOnlyList<int> Modifiers,
    string Name,
    IReadOnlyList<string> Description);
