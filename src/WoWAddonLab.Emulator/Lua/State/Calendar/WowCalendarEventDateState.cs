using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarEventDateState(
    int MonthZeroBased,
    int MonthDayZeroBased,
    int YearSince2000,
    int WeekdayZeroBased = -1);
