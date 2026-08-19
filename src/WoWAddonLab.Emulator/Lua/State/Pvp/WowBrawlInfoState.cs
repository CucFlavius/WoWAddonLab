namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBrawlInfoState(
    int BrawlId,
    string Name,
    string ShortDescription,
    string LongDescription,
    bool CanQueue,
    int MinLevel,
    int MaxLevel,
    bool GroupsAllowed,
    bool CrossFactionAllowed,
    int? TimeLeftUntilNextChange,
    int BrawlType,
    IReadOnlyList<string> MapNames,
    bool IncludesAllArenas,
    int MinItemLevel,
    bool ShouldHideRewardIcon);
