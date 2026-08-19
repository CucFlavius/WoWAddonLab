using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarEventIndexState(
    int OffsetMonths,
    int MonthDay,
    int EventIndex);
