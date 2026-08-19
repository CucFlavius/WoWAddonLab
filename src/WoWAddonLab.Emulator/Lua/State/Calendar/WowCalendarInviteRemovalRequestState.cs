using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarInviteRemovalRequestState(
    int InviteIndex,
    ulong InviteId,
    string Guid);
