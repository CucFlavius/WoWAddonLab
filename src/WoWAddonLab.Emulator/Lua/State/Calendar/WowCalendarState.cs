using System.Text;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCalendarState
{
    private byte[] _openEventDescriptionUtf8 = [];
    private byte[] _openEventTitleUtf8 = [];

    public bool IsBackendAvailable { get; set; } = true;
    public int PendingNameCount { get; set; }
    public bool BypassActionThrottles { get; set; }
    public bool IsCurrentRealmDateValidForEvent { get; set; } = true;
    public bool CanCreatePlayerEvent { get; set; } = true;
    public bool IsPlayerInGuild { get; set; } = true;
    public bool CanEditGuildEvents { get; set; } = true;
    public bool CanEditOpenEvent { get; set; }
    public bool OpenEventInvitesDisabled { get; set; }
    public uint LastAddEventRequestTickMilliseconds { get; set; }
    public uint LastInviteRequestTickMilliseconds { get; set; }
    public uint InviteStatusThrottleMaximum { get; set; } = 4;
    public uint InviteStatusThrottleWindowMilliseconds { get; set; } = 1_000;
    public uint InviteStatusThrottleCount { get; set; }
    public uint LastInviteStatusThrottleResetTickMilliseconds { get; set; }
    public int AddEventRequestCount { get; set; }
    public int ThrottledAddEventRequestCount { get; set; }
    public int ThrottledInviteStatusRequestCount { get; set; }
    public int PendingInviteCount { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public ulong? NextClubId { get; set; }
    public int GuildFilterMaximumLevel { get; set; } = 90;
    public int GuildFilterRank { get; set; }
    public bool IsActionPending { get; set; }
    public bool IsCalendarOpen { get; set; }
    public bool IsEventOpen { get; set; }
    public bool IsOpenEventLocal { get; set; }
    public WowCalendarEventFlags OpenEventFlags { get; set; }
    public ulong OpenEventId { get; set; }
    public WowCalendarDraftEventState? DraftEvent { get; set; }
    public string? OpenEventCalendarType { get; set; }
    public ulong OpenEventClubId { get; set; }
    public WowCalendarEventDateState OpenEventDate { get; set; } =
        new(-1, -1, -1);
    public WowCalendarEventTimeState OpenEventTime { get; set; } =
        new(-1, -1);
    public WowCalendarTimeValueState OpenEventLockoutTime { get; set; } =
        new(1, 1, 1, 2000, 0, 0);
    public string? OpenEventCreatorName { get; set; }
    public byte OpenEventType { get; set; }
    public byte OpenEventRepeatOption { get; set; }
    public int OpenEventMaximumSize { get; set; } = 100;
    public int OpenEventTextureId { get; set; }
    public string OpenEventDescription
    {
        get => Encoding.UTF8.GetString(_openEventDescriptionUtf8);
        set => _openEventDescriptionUtf8 =
            Encoding.UTF8.GetBytes(value ?? string.Empty);
    }
    public IReadOnlyList<byte> OpenEventDescriptionUtf8 =>
        _openEventDescriptionUtf8;
    public string OpenEventTitle
    {
        get => Encoding.UTF8.GetString(_openEventTitleUtf8);
        set => _openEventTitleUtf8 =
            Encoding.UTF8.GetBytes(value ?? string.Empty);
    }
    public IReadOnlyList<byte> OpenEventTitleUtf8 => _openEventTitleUtf8;
    public bool OpenEventUsesSignUpStatusRules { get; set; }
    public string InviteSortCriterion { get; set; } = string.Empty;
    public bool InviteSortReverse { get; set; }
    public int SelectedInviteIndex { get; set; }
    public ulong SelectedInviteId { get; set; }
    public bool IsAutoApproveEnabled { get; set; }
    public bool IsEventLocked { get; set; }
    public bool IsEventDirty { get; set; }
    public byte? LastEventInviteResponse { get; set; }
    public WowCalendarEventInviteResponseRequestState?
        LastEventInviteResponseRequest { get; set; }
    public int EventInviteResponseRequestCount { get; set; }
    public string? LastEventInviteRequestName { get; set; }
    public string? LastError { get; set; }
    public string? LastEventInviteError
    {
        get => LastError;
        set => LastError = value;
    }
    public int EventInviteRequestCount { get; set; }
    public WowCalendarMassInviteRequestState? LastMassInviteRequest { get; set; }
    public int MassInviteRequestCount { get; set; }
    public WowCalendarOpenEventRequestState? LastOpenEventRequest { get; set; }
    public int OpenEventRequestCount { get; set; }
    public WowCalendarUpdateEventRequestState? LastUpdateEventRequest
        { get; set; }
    public int UpdateEventRequestCount { get; set; }
    public WowCalendarModeratorRequestState? LastEventModeratorRequest { get; set; }
    public WowCalendarInviteRemovalRequestState? LastEventInviteRemovalRequest
        { get; set; }
    public WowCalendarInviteStatusRequestState? LastEventInviteStatusRequest
        { get; set; }
    public int EventInviteStatusRequestCount { get; set; }
    public WowCalendarEventSignUpRequestState? LastEventSignUpRequest
        { get; set; }
    public int EventSignUpRequestCount { get; set; }
    public int InviteCount { get; set; }
    public IList<WowCalendarEventInviteState> EventInvites { get; } =
        new List<WowCalendarEventInviteState>();
    public WowCalendarEventIndexState? EventIndex { get; set; }
    public WowCalendarEventIndexState? ContextMenuEventIndex { get; set; }
    public WowCalendarEventIndexState? ContextMenuClipboardEventIndex { get; set; }
    public WowCalendarPasteRequestState? LastContextMenuPasteRequest { get; set; }
    public WowCalendarEventIndexState? LastContextMenuRemovedEvent { get; set; }
    public WowCalendarEventIndexState? LastContextMenuSignedUpEvent { get; set; }
    public WowCalendarInviteResponseState? LastContextMenuInviteResponse { get; set; }
    public WowCalendarEventIndexState? LastContextMenuInviteRemovedEvent { get; set; }
    public IDictionary<(int OffsetMonths, int MonthDay, int EventIndex),
        WowCalendarContextMenuEventState> ContextMenuEvents { get; } =
        new Dictionary<(int OffsetMonths, int MonthDay, int EventIndex),
            WowCalendarContextMenuEventState>();
    public IList<WowCalendarEventTypeDisplayState> EventTypesDisplayOrdered { get; } =
        new List<WowCalendarEventTypeDisplayState>();
    public IDictionary<byte, IList<WowCalendarEventTextureState>> EventTexturesByType
        { get; } = new Dictionary<byte, IList<WowCalendarEventTextureState>>();
    public IList<WowCalendarGuildEventInfoState> GuildEvents { get; } =
        new List<WowCalendarGuildEventInfoState>();
    public ISet<ulong> PendingEventInviteIds { get; } = new HashSet<ulong>();
    public IDictionary<(int OffsetMonths, int MonthDay, int EventIndex),
        WowCalendarDayEventState> DayEvents { get; } =
        new Dictionary<(int OffsetMonths, int MonthDay, int EventIndex),
            WowCalendarDayEventState>();
    public IDictionary<(int OffsetMonths, int MonthDay, int EventIndex),
        WowCalendarHolidayInfoState> Holidays { get; } =
        new Dictionary<(int OffsetMonths, int MonthDay, int EventIndex),
            WowCalendarHolidayInfoState>();
    public IDictionary<(int OffsetMonths, int MonthDay, int EventIndex),
        WowCalendarRaidInfoState> Raids { get; } =
        new Dictionary<(int OffsetMonths, int MonthDay, int EventIndex),
            WowCalendarRaidInfoState>();
    public IDictionary<(int OffsetMonths, int MonthDay), int>
        FirstPendingInviteByDay { get; } =
        new Dictionary<(int OffsetMonths, int MonthDay), int>();

    internal bool OpenEventDescriptionEquals(ReadOnlySpan<byte> value) =>
        value.SequenceEqual(_openEventDescriptionUtf8);

    internal void SetOpenEventDescriptionUtf8(byte[] value) =>
        _openEventDescriptionUtf8 = value;

    internal bool OpenEventTitleEquals(ReadOnlySpan<byte> value) =>
        value.SequenceEqual(_openEventTitleUtf8);

    internal void SetOpenEventTitleUtf8(byte[] value) =>
        _openEventTitleUtf8 = value;
}
