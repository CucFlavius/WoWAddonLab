using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCalendarDayEventState(
    ulong EventId,
    string Title,
    bool IsCustomTitle,
    DateTime StartTime,
    DateTime EndTime,
    string? CalendarType,
    string? SequenceType,
    byte EventType,
    int? IconTexture,
    string? ModeratorStatus,
    byte InviteStatus,
    string InvitedBy,
    int Difficulty,
    byte InviteType,
    int SequenceIndex,
    int NumberOfSequenceDays,
    string? DifficultyName,
    bool DoNotDisplayBanner,
    bool DoNotDisplayEnd,
    ulong ClubId,
    bool IsLocked,
    int MapId = 0,
    uint EventFlags = 0);
