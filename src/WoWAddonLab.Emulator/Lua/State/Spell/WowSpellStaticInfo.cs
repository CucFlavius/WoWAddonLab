namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSpellStaticInfo(
    int Id,
    string Name,
    int IconId = 0,
    int OriginalIconId = 0,
    int CastTimeMilliseconds = 0,
    float MinRange = 0,
    float MaxRange = 0);
