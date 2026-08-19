namespace WoWAddonLab.Emulator.Lua;

public sealed record WowModifiedInstanceInfoState(
    int? LfrItemLevel,
    int? NormalItemLevel,
    int? HeroicItemLevel,
    int? MythicItemLevel,
    string? UiTextureKit,
    string? Description);
