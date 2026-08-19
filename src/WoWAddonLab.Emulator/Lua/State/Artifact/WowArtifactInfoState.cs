namespace WoWAddonLab.Emulator.Lua;

public sealed record WowArtifactInfoState(
    int ItemId,
    int? AltItemId,
    string Name,
    int? IconFileDataId,
    double TotalXp,
    int PointsSpent,
    int Quality,
    int ArtifactAppearanceId,
    int AppearanceModId,
    int? ItemAppearanceId,
    int? AltItemAppearanceId,
    bool AltOnTop,
    byte Tier);
