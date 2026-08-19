using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarModeratorRequestState(
    int InviteIndex,
    bool IsModerator);
