using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarEventSignUpRequestState(
    ulong EventId,
    ulong ClubId,
    bool IsTentative);
