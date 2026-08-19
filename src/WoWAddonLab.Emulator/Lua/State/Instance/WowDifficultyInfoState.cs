namespace WoWAddonLab.Emulator.Lua;

public sealed record WowDifficultyInfoState(
    int Id,
    string Name,
    string InstanceType,
    bool IsHeroic,
    bool IsChallengeMode,
    bool DisplayHeroic,
    bool DisplayMythic,
    int? ToggleDifficultyId,
    bool IsLookingForRaid,
    int? MinimumPlayers,
    int? MaximumPlayers,
    bool IsUserSelectable,
    bool IsLegacy = false);
