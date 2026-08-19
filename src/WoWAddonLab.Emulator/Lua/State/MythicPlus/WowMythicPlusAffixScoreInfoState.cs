namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMythicPlusAffixScoreInfoState(
    IReadOnlyList<WowMythicPlusAffixScoreState> AffixScores,
    int BestOverallScore);
