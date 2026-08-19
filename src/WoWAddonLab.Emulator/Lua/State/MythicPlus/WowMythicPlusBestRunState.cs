namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMythicPlusBestRunState(
    int DurationSec,
    int Level,
    WowMythicPlusDateState CompletionDate,
    IReadOnlyList<int> AffixIds,
    IReadOnlyList<WowMythicPlusMemberState> Members,
    int DungeonScore);
