namespace WoWAddonLab.Emulator.Lua;

public sealed record WowWhoInfoState(
    string FullName,
    string FullGuildName,
    int Level,
    string Race,
    string Class,
    string Area,
    string? Filename,
    int Gender,
    int? TimerunningSeasonId);
