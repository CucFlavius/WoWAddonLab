namespace WoWAddonLab.Emulator.Lua;

public sealed record WowArtifactAppearanceInfoState(
    int ArtifactAppearanceSetId,
    int ArtifactAppearanceId,
    string AppearanceName,
    int DisplayIndex,
    bool Unlocked,
    string? FailureDescription,
    int UiCameraId,
    int? AltHandCameraId,
    double SwatchColorRed,
    double SwatchColorGreen,
    double SwatchColorBlue,
    double ModelOpacity,
    double ModelSaturation,
    bool Obtainable);
