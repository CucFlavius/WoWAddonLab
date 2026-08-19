namespace WoWAddonLab.Emulator.Lua;

public sealed record WowChallengeMapState(
    int Id,
    string Name,
    int TimeLimitSeconds,
    int? TextureFileId = null,
    int BackgroundTextureFileId = 0,
    int MapId = 0);
