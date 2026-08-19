using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarMassInviteRequestState(
    ulong ClubId,
    byte MinimumLevel,
    byte MaximumLevel,
    byte MaximumRankOrderZeroBased);
