namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRuneforgePowerInfo(
    int RuneforgePowerId,
    int State,
    string? Name,
    int DescriptionSpellId,
    string Description,
    string? Source,
    int IconFileId,
    string? SpecName,
    bool MatchesSpec,
    bool MatchesCovenant,
    int? CovenantId,
    IReadOnlyList<string> Slots);
