namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSpellDeadlyDebuffInfo(
    int Priority,
    string WarningText,
    int? CriticalTimeRemainingMilliseconds = null,
    int? CriticalStacks = null,
    int? SoundKitId = null);
