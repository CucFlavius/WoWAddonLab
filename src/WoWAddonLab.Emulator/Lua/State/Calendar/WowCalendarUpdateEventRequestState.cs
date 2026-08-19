using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarUpdateEventRequestState(
    ulong EventId,
    ulong ClubId,
    string Title,
    string Description,
    byte EventType,
    int TextureId,
    WowCalendarEventDateState Date,
    WowCalendarEventTimeState Time,
    WowCalendarEventFlags EventFlags,
    int MaximumSize);
