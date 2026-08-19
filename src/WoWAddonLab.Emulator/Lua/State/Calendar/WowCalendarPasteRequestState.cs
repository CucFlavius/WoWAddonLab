using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarPasteRequestState(
    WowCalendarEventIndexState Source,
    int OffsetMonths,
    int MonthDay);
