using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarEventInviteResponseRequestState(
    ulong EventId,
    ulong InviteId,
    byte Response);
