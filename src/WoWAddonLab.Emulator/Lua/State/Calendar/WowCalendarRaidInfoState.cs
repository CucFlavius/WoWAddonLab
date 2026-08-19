using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarRaidInfoState(
    string Name,
    string CalendarType,
    int RaidId,
    DateTime Time,
    int Difficulty,
    string? DifficultyName);
