namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCommentatorTrackedSpellsState(
    IReadOnlyList<int>? SpellIds,
    WowTrackedSpellsResult Result = WowTrackedSpellsResult.Success);
