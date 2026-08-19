using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarInviteStatusRequestState(
    int InviteIndex,
    ulong InviteId,
    string Guid,
    byte Status);
