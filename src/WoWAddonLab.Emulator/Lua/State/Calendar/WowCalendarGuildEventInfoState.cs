using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarGuildEventInfoState(
    ulong EventId,
    WowCalendarTimeValueState Time,
    byte EventType,
    string Title,
    string CalendarType,
    int TextureFileAsset,
    byte InviteStatus,
    ulong ClubId,
    short DifficultyId = 0,
    int MapId = 0,
    uint EventFlags = 0);
