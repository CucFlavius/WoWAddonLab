namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMajorFactionDataState(
    string? Name,
    string? Description,
    IReadOnlyList<WowRenownHighlightState> Highlights,
    int FactionId,
    int ExpansionId,
    int BountySetId,
    bool IsUnlocked,
    bool UseJourneyUnlockToast,
    string? UnlockDescription,
    int UiPriority,
    int RenownLevel,
    int MaxLevel,
    int RenownReputationEarned,
    int RenownLevelThreshold,
    string? TextureKit,
    int CelebrationSoundKit,
    int RenownFanfareSoundKitId,
    WowMajorFactionColorState? FactionFontColor,
    int? RenownTrackLevelEffectId,
    int? PlayerCompanionId);
