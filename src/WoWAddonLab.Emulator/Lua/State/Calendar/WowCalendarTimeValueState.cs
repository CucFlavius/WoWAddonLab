using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarTimeValueState(
    int MonthDay,
    int Month,
    int Weekday,
    int Year,
    int Hour,
    int Minute);
