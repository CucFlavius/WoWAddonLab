namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPrivateAuraAppliedSoundInfoState(
    string UnitToken,
    int SpellId,
    string? SoundFileName = null,
    long? SoundFileId = null,
    string? OutputChannel = null);
