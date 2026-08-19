namespace WoWAddonLab.Emulator.Lua;

public sealed record WowArtifactArtInfoState(
    string? TextureKit,
    string TitleName,
    WowArtifactColorState TitleColor,
    WowArtifactColorState BarConnectedColor,
    WowArtifactColorState BarDisconnectedColor,
    int UiModelSceneId,
    int SpellVisualKitId);
