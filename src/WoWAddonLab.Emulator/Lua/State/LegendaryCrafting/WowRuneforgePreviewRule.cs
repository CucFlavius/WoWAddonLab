namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRuneforgePreviewRule(
    WowItemLocation BaseItem,
    int? PowerId,
    IReadOnlyList<int> Modifiers,
    WowRuneforgeItemPreviewInfo Info);
