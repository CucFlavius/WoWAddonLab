using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarEventTypeDisplayState(
    string DisplayString,
    byte EventType);
