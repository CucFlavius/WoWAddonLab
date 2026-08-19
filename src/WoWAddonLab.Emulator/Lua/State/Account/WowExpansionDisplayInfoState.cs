namespace WoWAddonLab.Emulator.Lua;

public sealed record WowExpansionDisplayInfoState(
    uint Logo,
    int Banner,
    IReadOnlyList<WowExpansionDisplayInfoFeatureState> Features,
    uint HighResBackgroundId,
    uint LowResBackgroundId,
    string TextureKit,
    int? GlueAmbianceSoundKit = null,
    int? GlueMusicSoundKit = null,
    int? GlueCreditsSoundKit = null);
