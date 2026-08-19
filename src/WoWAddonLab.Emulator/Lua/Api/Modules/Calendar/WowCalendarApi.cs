using System.Globalization;
using System.Text;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCalendarApi : LuaApiModule
{
    private const ulong MaximumExactLuaInteger = 9_007_199_254_740_991;
    private const int MinimumCalendarYear = 2004;
    private const int MinimumCalendarMonth = 11;
    private const int MaximumCalendarYear = 2030;
    private const WowCalendarEventFlags MassInviteExcludedEventFlags =
        WowCalendarEventFlags.GuildAnnouncement |
        WowCalendarEventFlags.CommunityEvent |
        WowCalendarEventFlags.GuildEvent;
    private static readonly int[] CalendarInviteStatusSortRanks =
        [9, 3, 6, 1, 7, 4, 2, 8, 5];
    private static readonly string[] EventTypes =
    [
        "CALENDAR_TYPE_RAID",
        "CALENDAR_TYPE_DUNGEON",
        "CALENDAR_TYPE_PVP",
        "CALENDAR_TYPE_MEETING",
        "CALENDAR_TYPE_OTHER"
    ];
    private static readonly lua_CFunction Callback = Dispatch;

    private readonly record struct CalendarTimeArgument(
        uint MonthDayZeroBased,
        uint MonthZeroBased,
        uint WeekdayZeroBased,
        int Year,
        int Hour,
        int Minute);

    private static readonly string[] Functions =
    [
        "AddEvent",
        "AreNamesReady",
        "CanAddEvent",
        "CanSendInvite",
        "CloseEvent",
        "ContextMenuEventCanComplain",
        "ContextMenuEventCanEdit",
        "ContextMenuEventCanRemove",
        "ContextMenuEventClipboard",
        "ContextMenuEventCopy",
        "ContextMenuEventGetCalendarType",
        "ContextMenuEventPaste",
        "ContextMenuEventRemove",
        "ContextMenuEventSignUp",
        "ContextMenuGetEventIndex",
        "ContextMenuInviteAvailable",
        "ContextMenuInviteDecline",
        "ContextMenuInviteRemove",
        "ContextMenuInviteTentative",
        "ContextMenuSelectEvent",
        "CreateCommunitySignUpEvent",
        "CreateGuildAnnouncementEvent",
        "CreateGuildSignUpEvent",
        "CreatePlayerEvent",
        "EventAvailable",
        "EventCanEdit",
        "EventClearAutoApprove",
        "EventClearLocked",
        "EventClearModerator",
        "EventDecline",
        "EventGetCalendarType",
        "EventGetClubId",
        "EventGetInvite",
        "EventGetInviteResponseTime",
        "EventGetInviteSortCriterion",
        "EventGetSelectedInvite",
        "EventGetStatusOptions",
        "EventGetTextures",
        "EventGetTypes",
        "EventGetTypesDisplayOrdered",
        "EventHasPendingInvite",
        "EventHaveSettingsChanged",
        "EventInvite",
        "EventRemoveInvite",
        "EventRemoveInviteByGuid",
        "EventSelectInvite",
        "EventSetAutoApprove",
        "EventSetClubId",
        "EventSetDate",
        "EventSetDescription",
        "EventSetInviteStatus",
        "EventSetLocked",
        "EventSetModerator",
        "EventSetTextureID",
        "EventSetTime",
        "EventSetTitle",
        "EventSetType",
        "EventSignUp",
        "EventSortInvites",
        "EventTentative",
        "GetClubCalendarEvents",
        "GetDayEvent",
        "GetDefaultGuildFilter",
        "GetEventIndex",
        "GetEventIndexInfo",
        "GetEventInfo",
        "GetGuildEventInfo",
        "GetGuildEventSelectionInfo",
        "GetFirstPendingInvite",
        "GetHolidayInfo",
        "GetMaxCreateDate",
        "GetMinDate",
        "GetMonthInfo",
        "GetNextClubId",
        "GetNumDayEvents",
        "GetNumGuildEvents",
        "GetNumInvites",
        "GetNumPendingInvites",
        "GetRaidInfo",
        "IsActionPending",
        "IsEventOpen",
        "MassInviteCommunity",
        "MassInviteGuild",
        "OpenCalendar",
        "OpenEvent",
        "RemoveEvent",
        "SetAbsMonth",
        "SetMonth",
        "SetNextClubId",
        "UpdateEvent"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Calendar");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var calendar = runtime.Calendar;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "AddEvent":
                AddEvent(runtime, calendar);
                return 0;
            case "AreNamesReady":
                return PushBoolean(
                    state,
                    calendar.IsBackendAvailable &&
                    calendar.PendingNameCount == 0);
            case "CanAddEvent":
                return PushBoolean(state, CanAddEvent(runtime, calendar));
            case "CanSendInvite":
                return PushBoolean(state, CanSendInvite(runtime, calendar));
            case "CloseEvent":
                CloseEvent(runtime, calendar);
                return 0;
            case "ContextMenuEventCanComplain":
                return ContextMenuEventPredicate(
                    state,
                    calendar,
                    "ContextMenuEventCanComplain",
                    value => value.CanComplain);
            case "ContextMenuEventCanEdit":
                return ContextMenuEventPredicate(
                    state,
                    calendar,
                    "ContextMenuEventCanEdit",
                    value => value.CanEdit);
            case "ContextMenuEventCanRemove":
                return ContextMenuEventPredicate(
                    state,
                    calendar,
                    "ContextMenuEventCanRemove",
                    value => value.CanRemove);
            case "ContextMenuEventClipboard":
                return PushBoolean(
                    state,
                    calendar.ContextMenuClipboardEventIndex is not null);
            case "ContextMenuEventCopy":
                ContextMenuEventCopy(calendar);
                return 0;
            case "ContextMenuEventGetCalendarType":
                PushContextMenuCalendarType(state, calendar);
                return 1;
            case "ContextMenuEventPaste":
                ContextMenuEventPaste(state, runtime, calendar);
                return 0;
            case "ContextMenuEventRemove":
                ContextMenuEventRemove(calendar);
                return 0;
            case "ContextMenuEventSignUp":
                ContextMenuEventSignUp(calendar);
                return 0;
            case "ContextMenuGetEventIndex":
                return PushEventIndex(state, calendar.ContextMenuEventIndex);
            case "ContextMenuInviteAvailable":
                ContextMenuInviteResponse(calendar, 1, false);
                return 0;
            case "ContextMenuInviteDecline":
                ContextMenuInviteResponse(calendar, 2, false);
                return 0;
            case "ContextMenuInviteRemove":
                ContextMenuInviteRemove(calendar);
                return 0;
            case "ContextMenuInviteTentative":
                ContextMenuInviteTentative(calendar);
                return 0;
            case "ContextMenuSelectEvent":
                ContextMenuSelectEvent(state, calendar);
                return 0;
            case "CreateCommunitySignUpEvent":
                CreateDraftEvent(
                    runtime,
                    calendar,
                    WowCalendarDraftKind.CommunitySignUp);
                return 0;
            case "CreateGuildAnnouncementEvent":
                CreateGuildDraftEvent(
                    runtime,
                    calendar,
                    WowCalendarDraftKind.GuildAnnouncement);
                return 0;
            case "CreateGuildSignUpEvent":
                CreateGuildDraftEvent(
                    runtime,
                    calendar,
                    WowCalendarDraftKind.GuildSignUp);
                return 0;
            case "CreatePlayerEvent":
                CreateDraftEvent(runtime, calendar, WowCalendarDraftKind.Player);
                runtime.TriggerEvent("CALENDAR_UPDATE_EVENT");
                return 0;
            case "EventAvailable":
                EventRespond(runtime, calendar, 1);
                return 0;
            case "EventCanEdit":
                return PushBoolean(state, EventCanEdit(calendar));
            case "EventClearAutoApprove":
                EventClearFlag(calendar, clearAutoApprove: true);
                return 0;
            case "EventClearLocked":
                EventClearFlag(calendar, clearAutoApprove: false);
                return 0;
            case "EventClearModerator":
                EventSetModerator(state, runtime, calendar, false);
                return 0;
            case "EventDecline":
                EventRespond(runtime, calendar, 2);
                return 0;
            case "EventGetCalendarType":
                SetOptionalValue(
                    state,
                    calendar.IsEventOpen
                        ? calendar.OpenEventCalendarType ?? string.Empty
                        : null);
                return 1;
            case "EventGetClubId":
                PushOptionalDatabaseId(
                    state,
                    calendar.IsEventOpen
                        ? calendar.OpenEventClubId
                        : null);
                return 1;
            case "EventGetInvite":
                return EventGetInvite(state, calendar);
            case "EventGetInviteResponseTime":
                return EventGetInviteResponseTime(state, calendar);
            case "EventGetInviteSortCriterion":
                lua_pushstring(
                    state,
                    calendar.IsEventOpen
                        ? calendar.InviteSortCriterion
                        : string.Empty);
                lua_pushboolean(
                    state,
                    calendar.IsEventOpen && calendar.InviteSortReverse ? 1 : 0);
                return 2;
            case "EventGetSelectedInvite":
                if (!calendar.IsEventOpen)
                    lua_pushnil(state);
                else
                    lua_pushinteger(state, GetSelectedInviteIndex(calendar));
                return 1;
            case "EventGetStatusOptions":
                EventGetStatusOptions(state, calendar);
                return 1;
            case "EventGetTextures":
                EventGetTextures(state, calendar);
                return 1;
            case "EventGetTypes":
                PushStringArray(state, EventTypes);
                return 1;
            case "EventGetTypesDisplayOrdered":
                PushEventTypes(state, calendar.EventTypesDisplayOrdered);
                return 1;
            case "EventHasPendingInvite":
                return PushBoolean(
                    state,
                    calendar.IsEventOpen &&
                    calendar.PendingEventInviteIds.Contains(calendar.OpenEventId));
            case "EventHaveSettingsChanged":
                return PushBoolean(
                    state,
                    calendar.IsEventOpen && calendar.IsEventDirty);
            case "EventInvite":
                EventInvite(state, runtime, calendar);
                return 0;
            case "EventRemoveInvite":
                EventRemoveInvite(state, runtime, calendar);
                return 0;
            case "EventRemoveInviteByGuid":
                EventRemoveInviteByGuid(state, runtime, calendar);
                return 0;
            case "EventSelectInvite":
                EventSelectInvite(state, calendar);
                return 0;
            case "EventSetAutoApprove":
                EventSetAutoApprove(calendar);
                return 0;
            case "EventSetClubId":
                EventSetClubId(state, calendar);
                return 0;
            case "EventSetDate":
                EventSetDate(state, calendar);
                return 0;
            case "EventSetDescription":
                EventSetDescription(state, calendar);
                return 0;
            case "EventSetInviteStatus":
                EventSetInviteStatus(state, runtime, calendar);
                return 0;
            case "EventSetLocked":
                EventSetLocked(calendar);
                return 0;
            case "EventSetModerator":
                EventSetModerator(state, runtime, calendar, true);
                return 0;
            case "EventSetTextureID":
                EventSetTextureId(state, calendar);
                return 0;
            case "EventSetTime":
                EventSetTime(state, calendar);
                return 0;
            case "EventSetTitle":
                EventSetTitle(state, calendar);
                return 0;
            case "EventSetType":
                EventSetType(state, calendar);
                return 0;
            case "EventSignUp":
                EventSignUp(runtime, calendar);
                return 0;
            case "EventSortInvites":
                EventSortInvites(state, runtime, calendar);
                return 0;
            case "EventTentative":
                EventRespond(runtime, calendar, 8);
                return 0;
            case "GetClubCalendarEvents":
                return GetClubCalendarEvents(state, runtime, calendar);
            case "GetDayEvent":
                return GetDayEvent(state, calendar);
            case "GetDefaultGuildFilter":
                PushDefaultGuildFilter(state, calendar);
                return 1;
            case "GetEventIndex":
                return PushEventIndex(state, calendar.EventIndex);
            case "GetEventIndexInfo":
                return GetEventIndexInfo(state, calendar);
            case "GetEventInfo":
                return GetEventInfo(state, runtime, calendar);
            case "GetGuildEventInfo":
                return GetGuildEventInfo(state, calendar);
            case "GetGuildEventSelectionInfo":
                return GetGuildEventSelectionInfo(state, runtime, calendar);
            case "GetFirstPendingInvite":
                return GetFirstPendingInvite(state, calendar);
            case "GetHolidayInfo":
                return GetHolidayInfo(state, calendar);
            case "GetMaxCreateDate":
                PushCalendarTime(state, GetMaximumCreateDate(runtime));
                return 1;
            case "GetMinDate":
                PushCalendarTime(state, new DateTime(2004, 11, 24, 0, 0, 0));
                return 1;
            case "GetMonthInfo":
                PushMonthInfo(state, runtime, calendar);
                return 1;
            case "GetNextClubId":
                PushOptionalDatabaseId(state, calendar.NextClubId);
                return 1;
            case "GetNumDayEvents":
                return GetNumberOfDayEvents(state, calendar);
            case "GetNumGuildEvents":
                lua_pushinteger(state, calendar.GuildEvents.Count);
                return 1;
            case "GetNumInvites":
                lua_pushinteger(
                    state,
                    calendar.IsEventOpen ? calendar.InviteCount : 0);
                return 1;
            case "GetNumPendingInvites":
                lua_pushinteger(state, calendar.PendingInviteCount);
                return 1;
            case "GetRaidInfo":
                return GetRaidInfo(state, calendar);
            case "IsActionPending":
                return PushBoolean(state, calendar.IsActionPending);
            case "IsEventOpen":
                return PushBoolean(state, calendar.IsEventOpen);
            case "MassInviteCommunity":
                MassInviteCommunity(state, calendar);
                return 0;
            case "MassInviteGuild":
                MassInviteGuild(state, calendar);
                return 0;
            case "OpenCalendar":
                calendar.IsCalendarOpen = true;
                return 0;
            case "OpenEvent":
                return PushBoolean(
                    state,
                    OpenEvent(state, runtime, calendar));
            case "RemoveEvent":
                RemoveEvent(runtime, calendar);
                return 0;
            case "SetAbsMonth":
                SetAbsoluteMonth(state, runtime, calendar);
                return 0;
            case "SetMonth":
                SetRelativeMonth(state, runtime, calendar);
                return 0;
            case "SetNextClubId":
                calendar.NextClubId = OptionalDatabaseId(
                    state,
                    1,
                    "Usage: C_Calendar.SetNextClubId([clubId])");
                return 0;
            case "UpdateEvent":
                UpdateEvent(calendar);
                return 0;
            default:
                return 0;
        }
    }

    private static void AddEvent(
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        if (calendar.IsActionPending || !calendar.IsBackendAvailable)
            return;

        if (!CanAddEvent(runtime, calendar))
        {
            calendar.ThrottledAddEventRequestCount++;
            return;
        }

        if (!calendar.IsCurrentRealmDateValidForEvent ||
            !calendar.CanCreatePlayerEvent)
        {
            return;
        }

        calendar.LastAddEventRequestTickMilliseconds =
            runtime.FrameTime.TickMilliseconds;
        calendar.AddEventRequestCount++;
        calendar.IsActionPending = true;
    }

    private static bool CanAddEvent(
        LuaRuntime runtime,
        WowCalendarState calendar) =>
        CanPerformThrottledAction(
            runtime,
            calendar,
            calendar.LastAddEventRequestTickMilliseconds,
            5_000);

    private static bool CanSendInvite(
        LuaRuntime runtime,
        WowCalendarState calendar) =>
        CanPerformThrottledAction(
            runtime,
            calendar,
            calendar.LastInviteRequestTickMilliseconds,
            2_000);

    private static bool CanPerformThrottledAction(
        LuaRuntime runtime,
        WowCalendarState calendar,
        uint previousTick,
        int minimumElapsedMilliseconds)
    {
        if (calendar.IsActionPending)
            return false;
        if (calendar.BypassActionThrottles || previousTick == 0)
            return true;
        return unchecked(
            (int)(runtime.FrameTime.TickMilliseconds - previousTick)) >=
            minimumElapsedMilliseconds;
    }

    private static int ContextMenuEventPredicate(
        lua_State state,
        WowCalendarState calendar,
        string operation,
        Func<WowCalendarContextMenuEventState, bool> selector)
    {
        var key = RequiredEventKey(
            state,
            $"Usage: local result = C_Calendar.{operation}(" +
            "offsetMonths, monthDay, eventIndex)");
        var result = calendar.ContextMenuEvents.TryGetValue(key, out var value) &&
                     selector(value);
        return PushBoolean(state, result);
    }

    private static void ContextMenuEventCopy(WowCalendarState calendar)
    {
        if (TryGetSelectedContextMenuEvent(calendar, out _, out var index))
            calendar.ContextMenuClipboardEventIndex = index;
    }

    private static void PushContextMenuCalendarType(
        lua_State state,
        WowCalendarState calendar)
    {
        if (TryGetSelectedContextMenuEvent(calendar, out var selected, out _))
            SetOptionalValue(state, selected.CalendarType);
        else
            lua_pushnil(state);
    }

    private static void ContextMenuEventPaste(
        lua_State state,
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: C_Calendar.ContextMenuEventPaste(offsetMonths, monthDay)";
        var offsetMonths = RequiredInt32(state, 1, usage);
        var monthDay = RequiredOneBasedIndex(state, 2, usage);
        if (calendar.IsActionPending ||
            calendar.ContextMenuClipboardEventIndex is not { } source)
        {
            return;
        }

        if (!CanAddEvent(runtime, calendar))
        {
            calendar.ThrottledAddEventRequestCount++;
            return;
        }

        var (month, year) = GetClampedMonth(runtime, calendar, offsetMonths);
        if (monthDay < 1 ||
            monthDay > DateTime.DaysInMonth(year, month) ||
            !calendar.IsCurrentRealmDateValidForEvent)
        {
            return;
        }

        calendar.LastContextMenuPasteRequest =
            new WowCalendarPasteRequestState(source, offsetMonths, monthDay);
        calendar.LastAddEventRequestTickMilliseconds =
            runtime.FrameTime.TickMilliseconds;
        calendar.IsActionPending = true;
    }

    private static void ContextMenuEventRemove(WowCalendarState calendar)
    {
        if (calendar.IsActionPending ||
            !TryGetSelectedContextMenuEvent(
                calendar,
                out var selected,
                out var index) ||
            !selected.CanRemove)
        {
            return;
        }

        calendar.LastContextMenuRemovedEvent = index;
        calendar.IsActionPending = true;
    }

    private static void ContextMenuEventSignUp(WowCalendarState calendar)
    {
        if (calendar.IsActionPending ||
            !TryGetSelectedContextMenuEvent(
                calendar,
                out var selected,
                out var index) ||
            !selected.CanSignUp)
        {
            return;
        }

        calendar.LastContextMenuSignedUpEvent = index;
        calendar.IsActionPending = true;
    }

    private static void ContextMenuInviteResponse(
        WowCalendarState calendar,
        byte response,
        bool usesSignUpPacket)
    {
        if (calendar.IsActionPending ||
            !TryGetSelectedContextMenuEvent(
                calendar,
                out var selected,
                out var index) ||
            !selected.CanRespondToInvite)
        {
            return;
        }

        calendar.LastContextMenuInviteResponse =
            new WowCalendarInviteResponseState(
                index,
                response,
                usesSignUpPacket);
        calendar.IsActionPending = true;
    }

    private static void ContextMenuInviteRemove(WowCalendarState calendar)
    {
        if (calendar.IsActionPending ||
            !TryGetSelectedContextMenuEvent(
                calendar,
                out var selected,
                out var index) ||
            !selected.CanRemoveInvite)
        {
            return;
        }

        calendar.LastContextMenuInviteRemovedEvent = index;
        calendar.IsActionPending = true;
    }

    private static void ContextMenuInviteTentative(WowCalendarState calendar)
    {
        if (calendar.IsActionPending ||
            !TryGetSelectedContextMenuEvent(
                calendar,
                out var selected,
                out var index) ||
            !selected.CanTentative)
        {
            return;
        }

        var usesSignUpPacket = selected.TentativeUsesSignUpPacket;
        calendar.LastContextMenuInviteResponse =
            new WowCalendarInviteResponseState(
                index,
                usesSignUpPacket ? (byte)1 : (byte)8,
                usesSignUpPacket);
        calendar.IsActionPending = true;
    }

    private static void ContextMenuSelectEvent(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: C_Calendar.ContextMenuSelectEvent(" +
            "offsetMonths, monthDay, eventIndex)";
        var key = RequiredEventKey(state, usage);
        if (calendar.ContextMenuEvents.ContainsKey(key))
        {
            calendar.ContextMenuEventIndex =
                new WowCalendarEventIndexState(
                    key.OffsetMonths,
                    key.MonthDay,
                    key.EventIndex);
        }
    }

    private static bool TryGetSelectedContextMenuEvent(
        WowCalendarState calendar,
        out WowCalendarContextMenuEventState selected,
        out WowCalendarEventIndexState index)
    {
        if (calendar.ContextMenuEventIndex is { } current &&
            calendar.ContextMenuEvents.TryGetValue(
                (current.OffsetMonths, current.MonthDay, current.EventIndex),
                out selected!))
        {
            index = current;
            return true;
        }

        selected = null!;
        index = null!;
        return false;
    }

    private static void CreateGuildDraftEvent(
        LuaRuntime runtime,
        WowCalendarState calendar,
        WowCalendarDraftKind kind)
    {
        if (!calendar.IsPlayerInGuild)
        {
            calendar.IsActionPending = false;
            return;
        }

        CreateDraftEvent(runtime, calendar, kind);
    }

    private static void CreateDraftEvent(
        LuaRuntime runtime,
        WowCalendarState calendar,
        WowCalendarDraftKind kind)
    {
        calendar.IsEventOpen = true;
        calendar.IsOpenEventLocal = true;
        calendar.OpenEventFlags = kind switch
        {
            WowCalendarDraftKind.Player => WowCalendarEventFlags.Player,
            WowCalendarDraftKind.GuildAnnouncement =>
                WowCalendarEventFlags.GuildAnnouncement,
            WowCalendarDraftKind.CommunitySignUp =>
                WowCalendarEventFlags.CommunityEvent,
            WowCalendarDraftKind.GuildSignUp =>
                WowCalendarEventFlags.GuildEvent,
            _ => WowCalendarEventFlags.None
        };
        calendar.OpenEventId = 0;
        calendar.DraftEvent = new WowCalendarDraftEventState(kind);
        calendar.OpenEventCalendarType = kind switch
        {
            WowCalendarDraftKind.Player => "PLAYER",
            WowCalendarDraftKind.GuildAnnouncement => "GUILD_ANNOUNCEMENT",
            WowCalendarDraftKind.CommunitySignUp => "COMMUNITY_EVENT",
            WowCalendarDraftKind.GuildSignUp => "GUILD_EVENT",
            _ => string.Empty
        };
        calendar.OpenEventClubId = 0;
        calendar.OpenEventDate = new WowCalendarEventDateState(-1, -1, -1);
        calendar.OpenEventTime = new WowCalendarEventTimeState(-1, -1);
        calendar.OpenEventLockoutTime =
            new WowCalendarTimeValueState(1, 1, 1, 2000, 0, 0);
        calendar.OpenEventCreatorName = null;
        calendar.OpenEventType = 0;
        calendar.OpenEventRepeatOption = 0;
        calendar.OpenEventMaximumSize = 100;
        calendar.OpenEventTextureId = 0;
        calendar.OpenEventDescription = string.Empty;
        calendar.OpenEventTitle = string.Empty;
        calendar.OpenEventUsesSignUpStatusRules =
            kind is WowCalendarDraftKind.CommunitySignUp or
                WowCalendarDraftKind.GuildSignUp;
        calendar.OpenEventInvitesDisabled =
            kind == WowCalendarDraftKind.GuildAnnouncement;
        calendar.CanEditOpenEvent = true;
        calendar.IsAutoApproveEnabled = false;
        calendar.IsEventLocked = false;
        calendar.IsEventDirty = true;
        calendar.LastEventInviteResponse = null;
        calendar.LastEventInviteResponseRequest = null;
        calendar.LastEventSignUpRequest = null;
        calendar.LastEventModeratorRequest = null;
        calendar.LastEventInviteRemovalRequest = null;
        calendar.LastEventInviteStatusRequest = null;
        calendar.LastMassInviteRequest = null;
        calendar.LastOpenEventRequest = null;
        calendar.LastUpdateEventRequest = null;
        calendar.EventInvites.Clear();
        var player = runtime.Units.Find("player");
        calendar.OpenEventCreatorName = player?.Name;
        if (player is not null && kind != WowCalendarDraftKind.GuildAnnouncement)
        {
            calendar.EventInvites.Add(
                new WowCalendarEventInviteState
                {
                    InviteId = 0,
                    Name = player.Name,
                    Level = player.Level,
                    ClassName = player.ClassName,
                    ClassFilename = player.ClassFile,
                    InviteStatus = 3,
                    ModeratorStatus = 2,
                    InviteIsMine = true,
                    Type = kind is WowCalendarDraftKind.CommunitySignUp or
                        WowCalendarDraftKind.GuildSignUp
                            ? (byte)1
                            : (byte)0,
                    ClassId = player.ClassId,
                    Guid = player.Guid
                });
        }
        calendar.InviteCount = calendar.EventInvites.Count;
        calendar.InviteSortCriterion = "status";
        calendar.InviteSortReverse = false;
        calendar.SelectedInviteIndex = calendar.EventInvites.Count > 0 ? 1 : 0;
        calendar.SelectedInviteId = calendar.EventInvites.Count > 0
            ? calendar.EventInvites[0].InviteId
            : 0;
        calendar.EventIndex = null;
    }

    private static void MassInviteCommunity(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: C_Calendar.MassInviteCommunity(" +
            "clubId, minLevel, maxLevel [, maxRankOrder])";
        var clubId = RequiredDatabaseId(state, 1, usage);
        var minimumLevel = RequiredByte(state, 2, usage);
        var maximumLevel = RequiredByte(state, 3, usage);
        var maximumRankOrderZeroBased =
            OptionalZeroBasedIndex(state, 4, usage);

        SubmitMassInvite(
            calendar,
            clubId,
            minimumLevel,
            maximumLevel,
            maximumRankOrderZeroBased);
    }

    private static void MassInviteGuild(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: C_Calendar.MassInviteGuild(" +
            "minLevel, maxLevel, maxRankOrder)";
        var minimumLevel = RequiredByte(state, 1, usage);
        var maximumLevel = RequiredByte(state, 2, usage);
        var maximumRankOrderZeroBased =
            RequiredZeroBasedIndex(state, 3, usage);

        if (!calendar.IsPlayerInGuild)
        {
            calendar.LastError = "ERR_GUILD_PLAYER_NOT_IN_GUILD";
            calendar.IsActionPending = false;
            return;
        }

        SubmitMassInvite(
            calendar,
            0,
            minimumLevel,
            maximumLevel,
            maximumRankOrderZeroBased);
    }

    private static void SubmitMassInvite(
        WowCalendarState calendar,
        ulong clubId,
        byte minimumLevel,
        byte maximumLevel,
        int? maximumRankOrderZeroBased)
    {
        if (calendar.IsActionPending ||
            !calendar.IsEventOpen ||
            !calendar.IsOpenEventLocal ||
            (calendar.OpenEventFlags & MassInviteExcludedEventFlags) != 0)
        {
            return;
        }

        calendar.LastMassInviteRequest =
            new WowCalendarMassInviteRequestState(
                clubId,
                minimumLevel,
                maximumLevel,
                maximumRankOrderZeroBased is { } rankOrder
                    ? unchecked((byte)rankOrder)
                    : (byte)0);
        calendar.MassInviteRequestCount++;
        calendar.IsActionPending = true;
    }

    private static bool OpenEvent(
        lua_State state,
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local success = C_Calendar.OpenEvent(" +
            "offsetMonths, monthDay, index)";
        var offsetMonths = RequiredInt32(state, 1, usage);
        var monthDay = RequiredOneBasedIndex(state, 2, usage);
        var eventIndex = RequiredOneBasedIndex(state, 3, usage);

        if (calendar.IsActionPending ||
            offsetMonths is < -1 or > 1 ||
            monthDay is < 1 or > 31 ||
            eventIndex < 1 ||
            !calendar.DayEvents.TryGetValue(
                (offsetMonths, monthDay, eventIndex),
                out var calendarEvent))
        {
            return false;
        }

        var (month, year) = GetClampedMonth(runtime, calendar, offsetMonths);
        if (monthDay > DateTime.DaysInMonth(year, month))
            return false;

        if ((calendarEvent.EventFlags & 0xD43) != 0)
        {
            calendar.LastOpenEventRequest =
                new WowCalendarOpenEventRequestState(calendarEvent.EventId);
            calendar.OpenEventRequestCount++;
            calendar.IsActionPending = true;
            return true;
        }

        calendar.EventIndex = new WowCalendarEventIndexState(
            offsetMonths,
            monthDay,
            eventIndex);
        runtime.TriggerEvent(
            "CALENDAR_OPEN_EVENT",
            CalendarTypeFromFlags(calendarEvent.EventFlags));
        return true;
    }

    private static void RemoveEvent(
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        calendar.LastEventInviteError = null;
        calendar.LastEventInviteRemovalRequest = null;
        if (calendar.IsActionPending ||
            !calendar.IsEventOpen ||
            calendar.IsOpenEventLocal)
        {
            return;
        }

        var inviteIndex = FindPlayerInviteIndex(runtime, calendar);
        if (inviteIndex < 0)
            return;

        var invite = calendar.EventInvites[inviteIndex];
        if (invite.ModeratorStatus == 2)
        {
            calendar.LastEventInviteError =
                "CALENDAR_ERROR_DELETE_CREATOR_FAILED";
            calendar.IsActionPending = false;
            return;
        }

        calendar.LastEventInviteRemovalRequest =
            new WowCalendarInviteRemovalRequestState(
                inviteIndex + 1,
                invite.InviteId,
                invite.Guid);
        calendar.IsActionPending = true;
    }

    private static void UpdateEvent(WowCalendarState calendar)
    {
        calendar.LastUpdateEventRequest = null;
        if (calendar.IsActionPending ||
            !calendar.IsEventOpen ||
            calendar.IsOpenEventLocal)
        {
            return;
        }

        if (!EventCanEdit(calendar))
        {
            calendar.LastError = "CALENDAR_ERROR_PERMISSIONS";
            calendar.IsActionPending = false;
            return;
        }

        if (!calendar.IsEventDirty)
            return;

        calendar.IsEventDirty = false;
        if (!calendar.IsCurrentRealmDateValidForEvent)
        {
            calendar.LastError = "CALENDAR_ERROR_EVENT_PASSED";
            calendar.IsActionPending = false;
            return;
        }

        calendar.LastUpdateEventRequest =
            new WowCalendarUpdateEventRequestState(
                calendar.OpenEventId,
                calendar.OpenEventClubId,
                calendar.OpenEventTitle,
                calendar.OpenEventDescription,
                calendar.OpenEventType == 5
                    ? (byte)1
                    : calendar.OpenEventType,
                calendar.OpenEventTextureId,
                calendar.OpenEventDate,
                calendar.OpenEventTime,
                calendar.OpenEventFlags,
                calendar.OpenEventMaximumSize);
        calendar.UpdateEventRequestCount++;
        calendar.IsActionPending = true;
    }

    private static string CalendarTypeFromFlags(uint eventFlags)
    {
        if ((eventFlags & 0x1) != 0)
            return "PLAYER";
        if ((eventFlags & 0x40) != 0)
            return "GUILD_ANNOUNCEMENT";
        if ((eventFlags & 0x800) != 0)
            return "GUILD_EVENT";
        if ((eventFlags & 0x400) != 0)
            return "COMMUNITY_EVENT";
        if ((eventFlags & 0x4) != 0)
            return "SYSTEM";
        if ((eventFlags & 0x8) != 0)
            return "HOLIDAY";
        if ((eventFlags & 0x80) != 0)
            return "RAID_LOCKOUT";
        if ((eventFlags & 0x200) != 0)
            return "RAID_RESET";
        if ((eventFlags & 0x102) != 0)
            return "PLAYER";
        return string.Empty;
    }

    private static bool EventCanEdit(WowCalendarState calendar)
    {
        if (!calendar.IsEventOpen)
            return false;
        if ((calendar.OpenEventFlags &
             WowCalendarEventFlags.GuildAnnouncement) != 0)
            return calendar.IsPlayerInGuild && calendar.CanEditGuildEvents;
        return calendar.CanEditOpenEvent;
    }

    private static void EventClearFlag(
        WowCalendarState calendar,
        bool clearAutoApprove)
    {
        if (!EventCanEdit(calendar))
            return;

        if (clearAutoApprove)
        {
            if (!calendar.IsAutoApproveEnabled)
                return;
            calendar.IsAutoApproveEnabled = false;
        }
        else
        {
            if (!calendar.IsEventLocked)
                return;
            calendar.IsEventLocked = false;
        }

        calendar.IsEventDirty = true;
    }

    private static void EventRespond(
        LuaRuntime runtime,
        WowCalendarState calendar,
        byte response)
    {
        if (calendar.IsActionPending ||
            !calendar.IsEventOpen)
        {
            return;
        }

        var playerInvite = FindPlayerInvite(runtime, calendar);
        if (calendar.OpenEventUsesSignUpStatusRules &&
            HasLoadedClubState(runtime, calendar.OpenEventClubId) &&
            playerInvite is null)
        {
            if (response == 8 && calendar.DraftEvent is null)
            {
                calendar.LastEventSignUpRequest =
                    new WowCalendarEventSignUpRequestState(
                        calendar.OpenEventId,
                        calendar.OpenEventClubId,
                        IsTentative: true);
                calendar.EventSignUpRequestCount++;
                calendar.IsActionPending = true;
            }
            return;
        }

        if (calendar.DraftEvent is not null ||
            response is not (1 or 2 or 8) ||
            playerInvite is null ||
            (calendar.OpenEventUsesSignUpStatusRules &&
             playerInvite.Type == 1))
        {
            return;
        }

        calendar.LastEventInviteResponse = response;
        calendar.LastEventInviteResponseRequest =
            new WowCalendarEventInviteResponseRequestState(
                calendar.OpenEventId,
                playerInvite.InviteId,
                response);
        calendar.EventInviteResponseRequestCount++;
        calendar.IsActionPending = true;
    }

    private static bool HasLoadedClubState(
        LuaRuntime runtime,
        ulong clubId) =>
        runtime.Clubs.SubscribedClubs.Any(club => club.ClubId == clubId) &&
        runtime.Clubs.SelfMemberInfoByClubId.ContainsKey(clubId);

    private static void EventSetModerator(
        lua_State state,
        LuaRuntime runtime,
        WowCalendarState calendar,
        bool isModerator)
    {
        var usage = isModerator
            ? "Usage: C_Calendar.EventSetModerator(inviteIndex)"
            : "Usage: C_Calendar.EventClearModerator(inviteIndex)";
        var inviteIndex = RequiredOneBasedIndex(state, 1, usage);
        if (!EventCanEdit(calendar) ||
            inviteIndex < 1 ||
            inviteIndex > calendar.EventInvites.Count ||
            (calendar.DraftEvent is null && calendar.IsActionPending))
        {
            return;
        }

        var invite = calendar.EventInvites[inviteIndex - 1];
        var moderatorStatus = isModerator ? (byte)1 : (byte)0;
        if (invite.ModeratorStatus == moderatorStatus)
            return;

        if (calendar.DraftEvent is not null)
        {
            invite.ModeratorStatus = moderatorStatus;
            runtime.TriggerEvent("CALENDAR_UPDATE_EVENT");
            return;
        }

        calendar.LastEventModeratorRequest =
            new WowCalendarModeratorRequestState(inviteIndex, isModerator);
    }

    private static int EventGetInvite(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local info = C_Calendar.EventGetInvite(eventIndex)";
        var inviteIndex = RequiredOneBasedIndex(state, 1, usage);
        if (!calendar.IsEventOpen || inviteIndex < 1 ||
            inviteIndex > calendar.EventInvites.Count)
            return 0;

        var invite = calendar.EventInvites[inviteIndex - 1];
        lua_newtable(state);
        SetOptionalString(state, "name", invite.Name);
        SetNumber(state, "level", invite.Level);
        SetOptionalString(state, "className", invite.ClassName);
        SetOptionalString(state, "classFilename", invite.ClassFilename);
        SetOptionalNumber(
            state,
            "inviteStatus",
            invite.InviteStatus is { } inviteStatus ? inviteStatus : null);
        SetOptionalString(
            state,
            "modStatus",
            invite.ModeratorStatus switch
            {
                1 => "MODERATOR",
                2 => "CREATOR",
                _ => null
            });
        SetBoolean(state, "inviteIsMine", invite.InviteIsMine);
        SetNumber(state, "type", invite.Type);
        SetString(state, "notes", invite.Notes);
        SetOptionalNumber(state, "classID", invite.ClassId);
        SetString(state, "guid", invite.Guid);
        return 1;
    }

    private static int EventGetInviteResponseTime(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local time = " +
            "C_Calendar.EventGetInviteResponseTime(eventIndex)";
        var inviteIndex = RequiredOneBasedIndex(state, 1, usage);
        if (!calendar.IsEventOpen ||
            inviteIndex < 1 ||
            inviteIndex > calendar.EventInvites.Count ||
            calendar.EventInvites[inviteIndex - 1].ResponseTime is not { } time)
        {
            return 0;
        }

        PushCalendarTime(state, time);
        return 1;
    }

    private static void EventGetStatusOptions(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local options = " +
            "C_Calendar.EventGetStatusOptions(eventIndex)";
        var inviteIndex = RequiredOneBasedIndex(state, 1, usage);
        lua_newtable(state);
        if (!calendar.IsEventOpen)
            return;

        if (!calendar.CanEditOpenEvent)
        {
            calendar.IsActionPending = false;
            return;
        }

        if (inviteIndex < 1 || inviteIndex > calendar.EventInvites.Count)
            return;

        var currentStatus =
            calendar.EventInvites[inviteIndex - 1].InviteStatus ?? 0;
        var resultIndex = 1;
        foreach (var status in new byte[] { 1, 2, 3, 4, 5, 8 })
        {
            if (status == currentStatus ||
                (calendar.OpenEventUsesSignUpStatusRules && status <= 2))
            {
                continue;
            }

            lua_pushinteger(state, resultIndex++);
            lua_newtable(state);
            SetNumber(state, "status", status);
            SetString(
                state,
                "statusString",
                status switch
                {
                    1 => "CALENDAR_STATUS_ACCEPTED",
                    2 => "CALENDAR_STATUS_DECLINED",
                    3 => "CALENDAR_STATUS_CONFIRMED",
                    4 => "CALENDAR_STATUS_OUT",
                    5 => "CALENDAR_STATUS_STANDBY",
                    8 => "CALENDAR_STATUS_TENTATIVE",
                    _ => string.Empty
                });
            lua_settable(state, -3);
        }
    }

    private static void EventGetTextures(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local textures = C_Calendar.EventGetTextures(eventType)";
        var eventType = RequiredCalendarEventType(state, 1, usage);

        lua_newtable(state);
        if (!TryGetEventTextures(calendar, eventType, out var textures))
        {
            return;
        }

        for (var index = 0; index < textures.Count; index++)
        {
            var texture = textures[index];
            lua_newtable(state);
            SetString(state, "title", texture.Title);
            SetFileAsset(state, "iconTexture", texture.IconTexture);
            SetNumber(state, "expansionLevel", texture.ExpansionLevel);
            SetOptionalNumber(state, "difficultyId", texture.DifficultyId);
            SetOptionalNumber(state, "mapId", texture.MapId);
            SetOptionalBoolean(state, "isLfr", texture.IsLfr);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static bool TryGetEventTextures(
        WowCalendarState calendar,
        byte eventType,
        out IList<WowCalendarEventTextureState> textures)
    {
        var nativeListType = eventType is 1 or 5 ? (byte)1 : eventType;
        return calendar.EventTexturesByType.TryGetValue(
            nativeListType,
            out textures!);
    }

    private static void EventInvite(
        lua_State state,
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        const string usage = "Usage: C_Calendar.EventInvite(name)";
        var name = RequiredString(state, 1, usage);
        calendar.LastEventInviteError = null;
        if (name.Length == 0 ||
            calendar.IsActionPending ||
            !calendar.IsEventOpen ||
            !calendar.CanEditOpenEvent)
        {
            return;
        }

        if (!CanSendInvite(runtime, calendar))
        {
            calendar.LastEventInviteError =
                "CALENDAR_ERROR_INVITE_THROTTLED";
            calendar.IsActionPending = false;
            return;
        }

        if (calendar.OpenEventInvitesDisabled)
        {
            calendar.LastEventInviteError =
                "CALENDAR_ERROR_INVITES_DISABLED";
            calendar.IsActionPending = false;
            return;
        }

        if (calendar.EventInvites.Count >= 100)
        {
            calendar.LastEventInviteError =
                "CALENDAR_ERROR_INVITES_EXCEEDED";
            calendar.IsActionPending = false;
            return;
        }

        var normalizedName = NormalizeCalendarInviteName(name);
        if (calendar.EventInvites.Any(
                invite => string.Equals(
                    invite.Name,
                    normalizedName,
                    StringComparison.Ordinal)))
        {
            calendar.LastEventInviteError =
                "CALENDAR_ERROR_ALREADY_INVITED_TO_EVENT_S";
            calendar.IsActionPending = false;
            return;
        }

        calendar.LastEventInviteRequestName = normalizedName;
        calendar.EventInviteRequestCount++;
        calendar.LastInviteRequestTickMilliseconds =
            runtime.FrameTime.TickMilliseconds;
        calendar.IsActionPending = true;
    }

    private static string NormalizeCalendarInviteName(string value)
    {
        if (value.Length == 0)
            return value;

        var characters = value.ToCharArray();
        characters[0] = CalendarNameUpper(characters[0]);
        for (var index = 1; index < characters.Length; index++)
            characters[index] = CalendarNameLower(characters[index]);

        var normalized = new string(characters);
        if (Encoding.UTF8.GetByteCount(normalized) <= 48)
            return normalized;

        var truncated = new StringBuilder(normalized.Length);
        var byteCount = 0;
        foreach (var rune in normalized.EnumerateRunes())
        {
            if (byteCount + rune.Utf8SequenceLength > 48)
                break;
            truncated.Append(rune.ToString());
            byteCount += rune.Utf8SequenceLength;
        }
        return truncated.ToString();
    }

    private static char CalendarNameUpper(char value)
    {
        var code = (int)value;
        if ((uint)(code - 'a') <= 25 || (uint)(code - 0x00E0) <= 30)
            return (char)(code - 32);
        return code switch
        {
            0x0153 => (char)0x0152,
            >= 0x0430 and <= 0x044F => (char)(code - 32),
            0x0451 => (char)0x0401,
            0x00FF => (char)0x0178,
            _ => value
        };
    }

    private static void EventRemoveInvite(
        lua_State state,
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: C_Calendar.EventRemoveInvite(inviteIndex)";
        var inviteIndex = RequiredOneBasedIndex(state, 1, usage);
        RemoveEventInvite(runtime, calendar, inviteIndex);
    }

    private static void EventRemoveInviteByGuid(
        lua_State state,
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: C_Calendar.EventRemoveInviteByGuid(guid)";
        var guid = RequiredString(state, 1, usage);
        calendar.LastEventInviteError = null;
        calendar.LastEventInviteRemovalRequest = null;
        if (calendar.IsActionPending || !calendar.IsEventOpen ||
            !TryParsePlayerGuidIdentity(guid, out var identity))
        {
            return;
        }

        for (var index = 0; index < calendar.EventInvites.Count; index++)
        {
            if (!TryParsePlayerGuidIdentity(
                    calendar.EventInvites[index].Guid,
                    out var inviteIdentity) ||
                inviteIdentity != identity)
            {
                continue;
            }

            RemoveEventInvite(runtime, calendar, index + 1);
            return;
        }
    }

    private static void EventSelectInvite(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: C_Calendar.EventSelectInvite(inviteIndex)";
        var inviteIndex = RequiredOneBasedIndex(state, 1, usage);
        if (!calendar.IsEventOpen || inviteIndex < 1 ||
            inviteIndex > calendar.EventInvites.Count)
            return;

        calendar.SelectedInviteId =
            calendar.EventInvites[inviteIndex - 1].InviteId;
        calendar.SelectedInviteIndex = inviteIndex;
    }

    private static void EventSetAutoApprove(WowCalendarState calendar)
    {
        if (!EventCanEdit(calendar) || calendar.IsAutoApproveEnabled)
            return;
        calendar.IsAutoApproveEnabled = true;
        calendar.IsEventDirty = true;
    }

    private static void EventSetLocked(WowCalendarState calendar)
    {
        if (!EventCanEdit(calendar) || calendar.IsEventLocked)
            return;
        calendar.IsEventLocked = true;
        calendar.IsEventDirty = true;
    }

    private static void EventSetTextureId(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: C_Calendar.EventSetTextureID(textureIndex)";
        var zeroBasedTextureIndex = RequiredZeroBasedIndex(state, 1, usage);
        if (!EventCanEdit(calendar) ||
            calendar.OpenEventType is not (0 or 1 or 5) ||
            !TryGetEventTextures(
                calendar,
                calendar.OpenEventType,
                out var textures) ||
            zeroBasedTextureIndex < 0 ||
            zeroBasedTextureIndex >= textures.Count)
        {
            return;
        }

        var eventTextureId = textures[zeroBasedTextureIndex].EventTextureId;
        if (calendar.OpenEventTextureId == eventTextureId)
            return;

        calendar.OpenEventTextureId = eventTextureId;
        calendar.IsEventDirty = true;
    }

    private static void EventSetTime(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: C_Calendar.EventSetTime(hour, minute)";
        var hour = unchecked((int)RequiredUInt32(state, 1, usage));
        var minute = unchecked((int)RequiredUInt32(state, 2, usage));
        calendar.LastError = null;
        if (!EventCanEdit(calendar))
        {
            ReportInvalidEventTime(calendar);
            return;
        }

        var current = calendar.OpenEventTime;
        if (current.Hour != hour || current.Minute != minute)
            calendar.IsEventDirty = true;

        if (hour >= 24 || minute >= 60)
        {
            ReportInvalidEventTime(calendar);
            return;
        }

        calendar.OpenEventTime = new WowCalendarEventTimeState(hour, minute);
    }

    private static void ReportInvalidEventTime(WowCalendarState calendar)
    {
        calendar.LastError = "CALENDAR_ERROR_INVALID_TIME";
        calendar.IsActionPending = false;
    }

    private static void EventSetClubId(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: C_Calendar.EventSetClubId([clubId])";
        var clubId = OptionalDatabaseId(state, 1, usage) ?? 0;
        calendar.LastError = null;
        if (!EventCanEdit(calendar))
        {
            calendar.LastError = "CALENDAR_ERROR_PERMISSIONS";
            calendar.IsActionPending = false;
            return;
        }

        if (calendar.OpenEventClubId == clubId)
            return;
        calendar.OpenEventClubId = clubId;
        calendar.IsEventDirty = true;
    }

    private static void EventSetDate(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: C_Calendar.EventSetDate(month, monthDay, year)";
        var monthZeroBased = RequiredZeroBasedIndex(state, 1, usage);
        var monthDayZeroBased = RequiredZeroBasedIndex(state, 2, usage);
        var year = RequiredUInt32(state, 3, usage);
        var yearOffset = unchecked((int)(year - 2000u));

        calendar.LastError = null;
        if (!EventCanEdit(calendar))
        {
            ReportInvalidEventDate(calendar);
            return;
        }

        var current = calendar.OpenEventDate;
        if (current.MonthZeroBased != monthZeroBased ||
            current.MonthDayZeroBased != monthDayZeroBased ||
            current.YearSince2000 != yearOffset)
        {
            calendar.IsEventDirty = true;
        }

        if (!TrySetCalendarEventDate(
                monthZeroBased,
                monthDayZeroBased,
                yearOffset,
                out var date))
        {
            ReportInvalidEventDate(calendar);
            return;
        }

        calendar.OpenEventDate = date;
    }

    private static bool TrySetCalendarEventDate(
        int monthZeroBased,
        int monthDayZeroBased,
        int yearOrOffset,
        out WowCalendarEventDateState date)
    {
        date = default!;
        if (monthZeroBased >= 12 || monthDayZeroBased >= 31)
            return false;

        var normalizedYearSince2000 = yearOrOffset < 2000
            ? yearOrOffset
            : yearOrOffset - 2000;
        if (normalizedYearSince2000 > 31)
            return false;

        date = new WowCalendarEventDateState(
            monthZeroBased,
            monthDayZeroBased,
            normalizedYearSince2000,
            ResolveWeekdayZeroBased(
                monthZeroBased,
                monthDayZeroBased,
                normalizedYearSince2000));
        return true;
    }

    private static int ResolveWeekdayZeroBased(
        int monthZeroBased,
        int monthDayZeroBased,
        int yearSince2000)
    {
        var year = yearSince2000 + 2000;
        if (year is < 1 or > 9999 ||
            monthZeroBased is < 0 or > 11 ||
            monthDayZeroBased < 0)
        {
            return -1;
        }

        try
        {
            return (int)new DateTime(year, monthZeroBased + 1, 1)
                .AddDays(monthDayZeroBased)
                .DayOfWeek;
        }
        catch (ArgumentOutOfRangeException)
        {
            return -1;
        }
    }

    private static void ReportInvalidEventDate(WowCalendarState calendar)
    {
        calendar.LastError = "CALENDAR_ERROR_INVALID_DATE";
        calendar.IsActionPending = false;
    }

    private static void EventSetDescription(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: C_Calendar.EventSetDescription(description)";
        var sourceBytes =
            LuaStringInterop.RequiredCStringBytes(state, 1, usage);
        if (!EventCanEdit(calendar))
            return;

        if (calendar.OpenEventDescriptionEquals(sourceBytes))
            return;

        calendar.SetOpenEventDescriptionUtf8(
            CopyBoundedUtf8Text(
                sourceBytes,
                maxCodepoints: 256,
                destinationCapacity: 1025,
                flags: 0));
        calendar.IsEventDirty = true;
    }

    private static byte[] CopyBoundedUtf8Text(
        ReadOnlySpan<byte> source,
        int maxCodepoints,
        int destinationCapacity,
        byte flags)
    {
        var output = new List<byte>(
            Math.Min(source.Length, destinationCapacity - 1));
        foreach (var value in source)
        {
            if (output.Count >= destinationCapacity - 1)
                break;
            if ((flags & 2) == 0 && value == (byte)'|')
                continue;
            if ((flags & 4) != 0 &&
                value is >= 1 and <= 31 &&
                value is not (9 or 10))
            {
                continue;
            }
            output.Add(value);
        }

        if ((flags & 1) != 0)
        {
            for (var index = 0; index < output.Count; index++)
            {
                var value = output[index];
                if (value is 10 or 13 ||
                    (value is (byte)'\\' or (byte)'|' &&
                     index + 1 < output.Count &&
                     output[index + 1] == (byte)'n'))
                {
                    output.RemoveRange(index, output.Count - index);
                    break;
                }
            }
        }

        var codepointCount = 0;
        for (var index = 0; index < output.Count; index++)
        {
            if ((output[index] & 0xC0) == 0x80)
                continue;

            codepointCount++;
            if (codepointCount == maxCodepoints)
            {
                if (index + 1 < output.Count)
                    output.RemoveRange(index + 1, output.Count - index - 1);
                break;
            }
        }

        return output.ToArray();
    }

    private static void EventSetTitle(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage = "Usage: C_Calendar.EventSetTitle(title)";
        var sourceBytes =
            LuaStringInterop.RequiredCStringBytes(state, 1, usage);
        if (!EventCanEdit(calendar) ||
            calendar.OpenEventTitleEquals(sourceBytes))
        {
            return;
        }

        calendar.SetOpenEventTitleUtf8(
            CopyBoundedUtf8Text(
                sourceBytes,
                maxCodepoints: 32,
                destinationCapacity: 129,
                flags: 1));
        calendar.IsEventDirty = true;
    }

    private static void EventSetType(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage = "Usage: C_Calendar.EventSetType(typeIndex)";
        var eventType = RequiredCalendarEventType(state, 1, usage);
        if (!EventCanEdit(calendar))
            return;

        var normalizedCurrentType = calendar.OpenEventType == 5
            ? (byte)1
            : calendar.OpenEventType;
        if (eventType == normalizedCurrentType)
            return;

        calendar.OpenEventType = eventType;
        calendar.IsEventDirty = true;
    }

    private static void EventSignUp(
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        if (calendar.IsActionPending ||
            !calendar.IsEventOpen ||
            !calendar.OpenEventUsesSignUpStatusRules ||
            calendar.DraftEvent is not null ||
            EventHasPlayerInvite(runtime, calendar))
        {
            return;
        }

        calendar.LastEventSignUpRequest =
            new WowCalendarEventSignUpRequestState(
                calendar.OpenEventId,
                calendar.OpenEventClubId,
                IsTentative: false);
        calendar.EventSignUpRequestCount++;
        calendar.IsActionPending = true;
    }

    private static bool EventHasPlayerInvite(
        LuaRuntime runtime,
        WowCalendarState calendar) =>
        FindPlayerInviteIndex(runtime, calendar) >= 0;

    private static WowCalendarEventInviteState? FindPlayerInvite(
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        var index = FindPlayerInviteIndex(runtime, calendar);
        return index >= 0 ? calendar.EventInvites[index] : null;
    }

    private static int FindPlayerInviteIndex(
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        if (runtime.Units.Find("player") is not { } player ||
            !TryParsePlayerGuidIdentity(player.Guid, out var playerIdentity))
        {
            return -1;
        }

        for (var index = 0; index < calendar.EventInvites.Count; index++)
        {
            var invite = calendar.EventInvites[index];
            if (TryParsePlayerGuidIdentity(invite.Guid, out var inviteIdentity) &&
                inviteIdentity == playerIdentity)
            {
                return index;
            }
        }

        return -1;
    }

    private static void EventSortInvites(
        lua_State state,
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: C_Calendar.EventSortInvites(criterion, reverse)";
        var requestedCriterion = RequiredString(state, 1, usage);
        if (lua_gettop(state) < 2)
        {
            luaL_error(state, usage);
            return;
        }
        var toggleDirection = lua_toboolean(state, 2) != 0;

        if (!calendar.IsEventOpen)
            return;

        var criterion = requestedCriterion switch
        {
            "name" => CalendarInviteSortCriterion.Name,
            "level" => CalendarInviteSortCriterion.Level,
            "class" => CalendarInviteSortCriterion.Class,
            "status" => CalendarInviteSortCriterion.Status,
            "party" => CalendarInviteSortCriterion.Party,
            "notes" => CalendarInviteSortCriterion.Notes,
            _ => CalendarInviteSortCriterion.Status
        };
        var canonicalCriterion = criterion switch
        {
            CalendarInviteSortCriterion.Name => "name",
            CalendarInviteSortCriterion.Level => "level",
            CalendarInviteSortCriterion.Class => "class",
            CalendarInviteSortCriterion.Status => "status",
            CalendarInviteSortCriterion.Party => "party",
            CalendarInviteSortCriterion.Notes => "notes",
            _ => "status"
        };

        var direction = calendar.InviteSortCriterion == canonicalCriterion &&
                        calendar.InviteSortReverse;
        if (toggleDirection)
            direction = !direction;
        calendar.InviteSortCriterion = canonicalCriterion;
        calendar.InviteSortReverse = direction;

        var sorted = calendar.EventInvites.ToList();
        sorted.Sort((left, right) =>
            CompareCalendarInvites(left, right, criterion, direction));
        calendar.EventInvites.Clear();
        foreach (var invite in sorted)
            calendar.EventInvites.Add(invite);

        calendar.SelectedInviteIndex = GetSelectedInviteIndex(calendar);
        runtime.TriggerEvent("CALENDAR_UPDATE_EVENT");
    }

    private static int CompareCalendarInvites(
        WowCalendarEventInviteState left,
        WowCalendarEventInviteState right,
        CalendarInviteSortCriterion criterion,
        bool reverse)
    {
        var comparison = criterion switch
        {
            CalendarInviteSortCriterion.Name => CompareInviteNames(left, right),
            CalendarInviteSortCriterion.Level => left.Level.CompareTo(right.Level),
            CalendarInviteSortCriterion.Class => StringComparer.OrdinalIgnoreCase.Compare(
                left.ClassName ?? string.Empty,
                right.ClassName ?? string.Empty),
            CalendarInviteSortCriterion.Status =>
                CalendarInviteStatusSortRank(left.InviteStatus)
                    .CompareTo(CalendarInviteStatusSortRank(right.InviteStatus)),
            CalendarInviteSortCriterion.Party =>
                right.IsInPlayerGroup.CompareTo(left.IsInPlayerGroup),
            CalendarInviteSortCriterion.Notes => StringComparer.Ordinal.Compare(
                left.Notes,
                right.Notes),
            _ => 0
        };
        if (reverse)
            comparison = -comparison;
        return comparison != 0 || criterion == CalendarInviteSortCriterion.Name
            ? comparison
            : CompareInviteNames(left, right);
    }

    private static int CompareInviteNames(
        WowCalendarEventInviteState left,
        WowCalendarEventInviteState right) =>
        StringComparer.OrdinalIgnoreCase.Compare(
            left.Name ?? string.Empty,
            right.Name ?? string.Empty);

    private static int CalendarInviteStatusSortRank(byte? status) =>
        status is < 9
            ? CalendarInviteStatusSortRanks[status.Value]
            : 0;

    private static void EventSetInviteStatus(
        lua_State state,
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: C_Calendar.EventSetInviteStatus(eventIndex, status)";
        var zeroBasedInviteIndex = RequiredZeroBasedIndex(state, 1, usage);
        var status = RequiredCalendarInviteStatus(state, 2, usage);
        if (!EventCanEdit(calendar) ||
            (calendar.DraftEvent is null && calendar.IsActionPending) ||
            !ConsumeInviteStatusThrottle(runtime, calendar) ||
            !CanSetInviteStatus(calendar, zeroBasedInviteIndex, status))
        {
            return;
        }

        var invite = calendar.EventInvites[zeroBasedInviteIndex];
        if (calendar.DraftEvent is not null)
        {
            invite.InviteStatus = status;
            runtime.TriggerEvent("CALENDAR_UPDATE_EVENT");
            return;
        }

        calendar.LastEventInviteStatusRequest =
            new WowCalendarInviteStatusRequestState(
                zeroBasedInviteIndex + 1,
                invite.InviteId,
                invite.Guid,
                status);
        calendar.EventInviteStatusRequestCount++;
    }

    private static bool ConsumeInviteStatusThrottle(
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        var current = calendar.InviteStatusThrottleCount;
        var maximum = calendar.InviteStatusThrottleMaximum;
        var now = runtime.FrameTime.TickMilliseconds;
        if (current >= maximum &&
            unchecked(now -
                calendar.LastInviteStatusThrottleResetTickMilliseconds) >=
            calendar.InviteStatusThrottleWindowMilliseconds)
        {
            calendar.LastInviteStatusThrottleResetTickMilliseconds = now;
            current = 0;
        }

        current++;
        calendar.InviteStatusThrottleCount = current;
        if (current <= maximum)
            return true;

        calendar.ThrottledInviteStatusRequestCount++;
        return false;
    }

    private static bool CanSetInviteStatus(
        WowCalendarState calendar,
        int zeroBasedInviteIndex,
        byte status)
    {
        if (!calendar.CanEditOpenEvent ||
            zeroBasedInviteIndex < 0 ||
            zeroBasedInviteIndex >= calendar.EventInvites.Count ||
            ((status < 1 || status > 5) && status < 8) ||
            (calendar.OpenEventUsesSignUpStatusRules && status <= 2))
        {
            return false;
        }

        return calendar.EventInvites[zeroBasedInviteIndex].InviteStatus !=
            status;
    }

    private static int GetSelectedInviteIndex(WowCalendarState calendar)
    {
        for (var index = 0; index < calendar.EventInvites.Count; index++)
        {
            if (calendar.EventInvites[index].InviteId ==
                calendar.SelectedInviteId)
            {
                return index + 1;
            }
        }
        return 0;
    }

    private static void RemoveEventInvite(
        LuaRuntime runtime,
        WowCalendarState calendar,
        int inviteIndex)
    {
        calendar.LastEventInviteError = null;
        calendar.LastEventInviteRemovalRequest = null;
        if (calendar.IsActionPending ||
            !calendar.IsEventOpen ||
            inviteIndex < 1 ||
            inviteIndex > calendar.EventInvites.Count)
        {
            return;
        }

        var invite = calendar.EventInvites[inviteIndex - 1];
        if (invite.ModeratorStatus == 2)
        {
            calendar.LastEventInviteError =
                "CALENDAR_ERROR_DELETE_CREATOR_FAILED";
            calendar.IsActionPending = false;
            return;
        }

        if (!invite.InviteIsMine && !calendar.CanEditOpenEvent)
        {
            calendar.LastEventInviteError = "CALENDAR_ERROR_PERMISSIONS";
            calendar.IsActionPending = false;
            return;
        }

        if (calendar.DraftEvent is not null)
        {
            calendar.EventInvites.RemoveAt(inviteIndex - 1);
            calendar.InviteCount = calendar.EventInvites.Count;
            calendar.SelectedInviteIndex = GetSelectedInviteIndex(calendar);
            runtime.TriggerEvent("CALENDAR_UPDATE_EVENT");
            return;
        }

        calendar.LastEventInviteRemovalRequest =
            new WowCalendarInviteRemovalRequestState(
                inviteIndex,
                invite.InviteId,
                invite.Guid);
        calendar.IsActionPending = true;
    }

    private static bool TryParsePlayerGuidIdentity(
        string value,
        out WowGuidIdentity identity)
    {
        identity = default;
        var separator = value.IndexOf('-');
        if (separator < 0 ||
            !value.AsSpan(0, separator).Equals(
                "Player",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var payload = value.AsSpan(separator + 1);
        var position = 0;
        if (!TryReadGuidDecimal(payload, ref position, out var realmId) ||
            realmId > ushort.MaxValue ||
            position >= payload.Length || payload[position] != '-')
        {
            return false;
        }

        var high = (realmId + 0x20000UL) << 42;
        position++;
        if (payload[position..].IndexOf('-') >= 0)
        {
            if (!TryReadGuidDecimal(payload, ref position, out var regionId) ||
                regionId > 3 ||
                position >= payload.Length || payload[position] != '-')
            {
                return false;
            }

            high += (regionId & 3) << 40;
            position++;
            if (regionId == 2)
            {
                if (!TryReadGuidDecimal(
                        payload,
                        ref position,
                        out var realmGroupId) ||
                    realmGroupId > 0xFFFFFF ||
                    position >= payload.Length || payload[position] != '-')
                {
                    return false;
                }

                high = (high & 0xFFFFFF000000FFFFUL) |
                       (realmGroupId << 16);
                position++;
            }
        }

        if (position + 2 < payload.Length &&
            payload[position] == '0' &&
            payload[position + 1] is 'x' or 'X')
        {
            position += 2;
        }

        if (!TryReadGuidHex(payload, ref position, out var low))
            return false;

        identity = new WowGuidIdentity(low, high);
        return high >> 58 != 0;
    }

    private static bool TryReadGuidDecimal(
        ReadOnlySpan<char> value,
        ref int position,
        out ulong result)
    {
        result = 0;
        var start = position;
        var remaining = 20;
        while (position < value.Length && remaining-- > 0 &&
               value[position] is >= '0' and <= '9')
        {
            var digit = (uint)(value[position] - '0');
            if (result > (ulong.MaxValue - digit) / 10)
                return false;
            result = result * 10 + digit;
            position++;
        }
        return position > start;
    }

    private static bool TryReadGuidHex(
        ReadOnlySpan<char> value,
        ref int position,
        out ulong result)
    {
        result = 0;
        var start = position;
        var remaining = 16;
        while (position < value.Length && remaining-- > 0)
        {
            var character = value[position];
            int digit;
            if (character is >= '0' and <= '9')
                digit = character - '0';
            else if (character is >= 'a' and <= 'f')
                digit = character - 'a' + 10;
            else if (character is >= 'A' and <= 'F')
                digit = character - 'A' + 10;
            else
                break;
            result = (result << 4) | (uint)digit;
            position++;
        }
        return position > start;
    }

    private readonly record struct WowGuidIdentity(ulong Low, ulong High);

    private enum CalendarInviteSortCriterion : byte
    {
        Name,
        Level,
        Class,
        Status,
        Party,
        Notes
    }

    private static char CalendarNameLower(char value)
    {
        var code = (int)value;
        if ((uint)(code - 'A') <= 25 || (uint)(code - 0x00C0) <= 30)
            return (char)(code + 32);
        return code switch
        {
            0x0152 => (char)0x0153,
            >= 0x0410 and <= 0x042F => (char)(code + 32),
            0x0401 => (char)0x0451,
            0x0178 => (char)0x00FF,
            _ => value
        };
    }

    private static void PushStringArray(
        lua_State state,
        IReadOnlyList<string> values)
    {
        lua_newtable(state);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushstring(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void SetOptionalValue(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }

    private static void CloseEvent(
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        var wasOpen = calendar.IsEventOpen;
        calendar.IsEventOpen = false;
        calendar.IsOpenEventLocal = false;
        calendar.OpenEventFlags = WowCalendarEventFlags.None;
        calendar.OpenEventId = 0;
        calendar.DraftEvent = null;
        calendar.OpenEventCalendarType = null;
        calendar.OpenEventClubId = 0;
        calendar.OpenEventDate = new WowCalendarEventDateState(-1, -1, -1);
        calendar.OpenEventTime = new WowCalendarEventTimeState(-1, -1);
        calendar.OpenEventLockoutTime =
            new WowCalendarTimeValueState(1, 1, 1, 2000, 0, 0);
        calendar.OpenEventCreatorName = null;
        calendar.OpenEventType = 0;
        calendar.OpenEventRepeatOption = 0;
        calendar.OpenEventMaximumSize = 100;
        calendar.OpenEventTextureId = 0;
        calendar.OpenEventDescription = string.Empty;
        calendar.OpenEventTitle = string.Empty;
        calendar.OpenEventUsesSignUpStatusRules = false;
        calendar.OpenEventInvitesDisabled = false;
        calendar.InviteSortCriterion = string.Empty;
        calendar.InviteSortReverse = false;
        calendar.SelectedInviteIndex = 0;
        calendar.SelectedInviteId = 0;
        calendar.CanEditOpenEvent = false;
        calendar.IsAutoApproveEnabled = false;
        calendar.IsEventLocked = false;
        calendar.IsEventDirty = false;
        calendar.LastEventInviteResponse = null;
        calendar.LastEventInviteResponseRequest = null;
        calendar.LastEventSignUpRequest = null;
        calendar.LastEventModeratorRequest = null;
        calendar.LastEventInviteRemovalRequest = null;
        calendar.LastEventInviteStatusRequest = null;
        calendar.LastMassInviteRequest = null;
        calendar.LastOpenEventRequest = null;
        calendar.LastUpdateEventRequest = null;
        calendar.EventInvites.Clear();
        calendar.InviteCount = 0;
        calendar.EventIndex = null;
        if (wasOpen)
            runtime.TriggerEvent("CALENDAR_CLOSE_EVENT");
    }

    private static int GetDayEvent(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local event = " +
            "C_Calendar.GetDayEvent(monthOffset, monthDay, index)";
        var key = RequiredEventKey(state, usage);
        if (!calendar.DayEvents.TryGetValue(key, out var calendarEvent))
            return 0;
        PushDayEvent(state, calendarEvent);
        return 1;
    }

    private static int GetClubCalendarEvents(
        lua_State state,
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local events = C_Calendar.GetClubCalendarEvents(" +
            "clubId, startTime, endTime)";
        var clubId = RequiredDatabaseId(state, 1, usage);
        var startTime = RequiredCalendarTime(state, 2, usage);
        var endTime = RequiredCalendarTime(state, 3, usage);

        lua_newtable(state);
        if (!TryGetCalendarDate(startTime, out var startDate) ||
            !TryGetCalendarDate(endTime, out var endDate) ||
            startDate > endDate)
        {
            return 1;
        }

        var currentTime = runtime.DateAndTime.CurrentTime.LocalDateTime;
        var baseMonth = calendar.Month is >= 1 and <= 12
            ? calendar.Month
            : currentTime.Month;
        var baseYear = calendar.Year != 0 ? calendar.Year : currentTime.Year;
        var startMinute = startTime.Hour * 60L + startTime.Minute;
        var endMinute = endTime.Hour * 60L + endTime.Minute;
        var resultIndex = 1;
        var currentDate = startDate;
        for (var visitedDayCount = 0;
             visitedDayCount < 0x5D && currentDate <= endDate;
             visitedDayCount++, currentDate = currentDate.AddDays(1))
        {
            var offsetMonths =
                (currentDate.Year - baseYear) * 12 + currentDate.Month - baseMonth;
            if (offsetMonths is < -1 or > 1)
                continue;
            foreach (var pair in calendar.DayEvents
                         .Where(pair =>
                             pair.Key.OffsetMonths == offsetMonths &&
                             pair.Key.MonthDay == currentDate.Day)
                         .OrderBy(pair => pair.Key.EventIndex))
            {
                var calendarEvent = pair.Value;
                if (calendarEvent.ClubId != clubId)
                    continue;

                var eventMinute =
                    calendarEvent.StartTime.Hour * 60L +
                    calendarEvent.StartTime.Minute;
                if ((currentDate == startDate && eventMinute < startMinute) ||
                    (currentDate == endDate && eventMinute > endMinute))
                {
                    continue;
                }

                PushDayEvent(state, calendarEvent);
                lua_rawseti(state, -2, resultIndex++);
            }
        }

        return 1;
    }

    private static int GetEventIndexInfo(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local eventIndexInfo = " +
            "C_Calendar.GetEventIndexInfo(eventID " +
            "[, monthOffset, monthDay])";
        var eventId = RequiredDatabaseId(state, 1, usage);
        var monthOffset = OptionalInt32Value(state, 2, usage);
        var monthDay = OptionalInt32Value(state, 3, usage);

        if (monthOffset is not null || monthDay is not null)
        {
            lua_pushnil(state);
            return 1;
        }

        for (var offsetMonths = -1; offsetMonths <= 1; offsetMonths++)
        {
            for (var day = 1; day <= 31; day++)
            {
                foreach (var entry in calendar.DayEvents
                             .Where(pair =>
                                 pair.Key.OffsetMonths == offsetMonths &&
                                 pair.Key.MonthDay == day)
                             .OrderBy(pair => pair.Key.EventIndex))
                {
                    if (entry.Value.EventId != eventId)
                        continue;

                    lua_newtable(state);
                    SetNumber(state, "offsetMonths", offsetMonths);
                    SetNumber(state, "monthDay", day);
                    SetNumber(state, "eventIndex", entry.Key.EventIndex);
                    return 1;
                }
            }
        }

        lua_pushnil(state);
        return 1;
    }

    private static int GetEventInfo(
        lua_State state,
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        if (!calendar.IsEventOpen)
            return 0;

        lua_newtable(state);
        SetString(state, "title", calendar.OpenEventTitle);
        SetString(state, "description", calendar.OpenEventDescription);
        SetOptionalString(
            state,
            "creator",
            calendar.OpenEventCreatorName);
        SetNumber(
            state,
            "eventType",
            calendar.OpenEventType == 5 ? 1 : calendar.OpenEventType);
        SetNumber(state, "repeatOption", calendar.OpenEventRepeatOption);
        SetNumber(state, "maxSize", calendar.OpenEventMaximumSize);
        SetOptionalNumber(
            state,
            "textureIndex",
            GetOpenEventTextureIndex(calendar));

        var date = calendar.OpenEventDate;
        var time = calendar.OpenEventTime;
        PushCalendarTime(
            state,
            new WowCalendarTimeValueState(
                date.MonthDayZeroBased + 1,
                date.MonthZeroBased + 1,
                date.WeekdayZeroBased + 1,
                date.YearSince2000 + 2000,
                time.Hour,
                time.Minute));
        lua_setfield(state, -2, "time");
        PushCalendarTime(state, calendar.OpenEventLockoutTime);
        lua_setfield(state, -2, "lockoutTime");

        SetBoolean(state, "isLocked", calendar.IsEventLocked);
        SetBoolean(state, "isAutoApprove", calendar.IsAutoApproveEnabled);
        SetBoolean(
            state,
            "hasPendingInvite",
            calendar.PendingEventInviteIds.Contains(calendar.OpenEventId));

        var playerInvite = FindPlayerInvite(runtime, calendar);
        if (playerInvite is not null)
        {
            SetOptionalNumber(
                state,
                "inviteStatus",
                playerInvite.InviteStatus ?? 0);
            SetOptionalNumber(state, "inviteType", playerInvite.Type);
        }
        else if (calendar.OpenEventUsesSignUpStatusRules)
        {
            SetOptionalNumber(state, "inviteStatus", 7);
            SetOptionalNumber(state, "inviteType", 1);
        }
        else
        {
            SetOptionalNumber(state, "inviteStatus", null);
            SetOptionalNumber(state, "inviteType", null);
        }

        SetString(
            state,
            "calendarType",
            calendar.OpenEventCalendarType ?? string.Empty);
        SetOptionalString(
            state,
            "communityName",
            GetOpenEventCommunityName(runtime, calendar));
        return 1;
    }

    private static int? GetOpenEventTextureIndex(WowCalendarState calendar)
    {
        byte? textureListType = calendar.OpenEventType switch
        {
            0 => (byte?)0,
            1 or 5 => (byte?)1,
            _ => null
        };
        if (textureListType is null ||
            !calendar.EventTexturesByType.TryGetValue(
                textureListType.Value,
                out var textures))
        {
            return null;
        }

        for (var index = 0; index < textures.Count; index++)
        {
            if (textures[index].EventTextureId == calendar.OpenEventTextureId)
                return index + 1;
        }
        return null;
    }

    private static int GetGuildEventInfo(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local info = C_Calendar.GetGuildEventInfo(index)";
        var eventIndex = RequiredOneBasedIndex(state, 1, usage);
        if (eventIndex < 1 || eventIndex > calendar.GuildEvents.Count)
            return 0;

        var guildEvent = calendar.GuildEvents[eventIndex - 1];
        var time = guildEvent.Time;
        lua_newtable(state);
        SetDatabaseId(state, "eventID", guildEvent.EventId);
        SetNumber(state, "year", time.Year);
        SetNumber(state, "month", time.Month);
        SetNumber(state, "monthDay", time.MonthDay);
        SetNumber(state, "weekday", time.Weekday);
        SetNumber(state, "hour", time.Hour);
        SetNumber(state, "minute", time.Minute);
        SetNumber(
            state,
            "eventType",
            guildEvent.EventType == 5 ? 1 : guildEvent.EventType);
        SetString(state, "title", guildEvent.Title);
        SetString(state, "calendarType", guildEvent.CalendarType);
        SetFileAsset(state, "texture", guildEvent.TextureFileAsset);
        SetNumber(state, "inviteStatus", guildEvent.InviteStatus);
        SetDatabaseId(state, "clubID", guildEvent.ClubId);
        return 1;
    }

    private static int GetGuildEventSelectionInfo(
        lua_State state,
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local info = " +
            "C_Calendar.GetGuildEventSelectionInfo(index)";
        var guildEventIndex = RequiredOneBasedIndex(state, 1, usage);
        if (guildEventIndex < 1 || guildEventIndex > calendar.GuildEvents.Count)
            return 0;

        var guildEvent = calendar.GuildEvents[guildEventIndex - 1];
        var time = guildEvent.Time;
        if (time.Month is < 1 or > 12 || time.MonthDay is < 1 or > 31)
            return 0;

        var currentTime = runtime.DateAndTime.CurrentTime.LocalDateTime;
        var baseMonth = calendar.Month is >= 1 and <= 12
            ? calendar.Month
            : currentTime.Month;
        var baseYear = calendar.Year != 0 ? calendar.Year : currentTime.Year;
        var offsetMonths =
            (time.Year - baseYear) * 12 + time.Month - baseMonth;
        if (offsetMonths is < -1 or > 1)
            return 0;

        foreach (var pair in calendar.DayEvents
                     .Where(pair =>
                         pair.Key.OffsetMonths == offsetMonths &&
                         pair.Key.MonthDay == time.MonthDay)
                     .OrderBy(pair => pair.Key.EventIndex))
        {
            var dayEvent = pair.Value;
            var matches = guildEvent.EventId != 0 &&
                          guildEvent.EventId == dayEvent.EventId;
            if (guildEvent.MapId != 0)
            {
                matches = guildEvent.MapId == dayEvent.MapId;
                if (matches && (guildEvent.EventFlags & 0x80) != 0)
                {
                    matches = guildEvent.DifficultyId ==
                              unchecked((short)dayEvent.Difficulty);
                }
            }

            if (!matches)
                continue;

            lua_newtable(state);
            SetNumber(state, "offsetMonths", offsetMonths);
            SetNumber(state, "monthDay", time.MonthDay);
            SetNumber(state, "eventIndex", pair.Key.EventIndex);
            return 1;
        }

        return 0;
    }

    private static string? GetOpenEventCommunityName(
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        if (runtime.Clubs.ClubInfoById.TryGetValue(
                calendar.OpenEventClubId,
                out var club))
        {
            return club.Name;
        }

        return runtime.Clubs.SubscribedClubs
            .FirstOrDefault(entry =>
                entry.ClubId == calendar.OpenEventClubId)
            ?.Name;
    }

    private static int GetFirstPendingInvite(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local firstPendingInvite = " +
            "C_Calendar.GetFirstPendingInvite(offsetMonths, monthDay)";
        var offsetMonths = RequiredInt32(state, 1, usage);
        var monthDay = RequiredOneBasedIndex(state, 2, usage);
        if (calendar.FirstPendingInviteByDay.TryGetValue(
                (offsetMonths, monthDay),
                out var inviteIndex) &&
            inviteIndex > 0)
        {
            lua_pushinteger(state, inviteIndex);
        }
        else
        {
            lua_pushnil(state);
        }
        return 1;
    }

    private static int GetHolidayInfo(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local event = " +
            "C_Calendar.GetHolidayInfo(monthOffset, monthDay, index)";
        var key = RequiredEventKey(state, usage);
        if (!calendar.Holidays.TryGetValue(key, out var holiday))
            return 0;
        lua_newtable(state);
        SetOptionalString(state, "name", holiday.Name);
        SetOptionalString(state, "description", holiday.Description);
        SetNumber(state, "texture", holiday.TextureFileId);
        SetOptionalCalendarTime(state, "startTime", holiday.StartTime);
        SetOptionalCalendarTime(state, "endTime", holiday.EndTime);
        return 1;
    }

    private static int GetNumberOfDayEvents(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local numDayEvents = " +
            "C_Calendar.GetNumDayEvents(offsetMonths, monthDay)";
        var offsetMonths = RequiredInt32(state, 1, usage);
        var monthDay = RequiredOneBasedIndex(state, 2, usage);
        var count = calendar.DayEvents.Keys.Count(
            key => key.OffsetMonths == offsetMonths &&
                   key.MonthDay == monthDay);
        lua_pushinteger(state, count);
        return 1;
    }

    private static int GetRaidInfo(
        lua_State state,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local info = " +
            "C_Calendar.GetRaidInfo(offsetMonths, monthDay, eventIndex)";
        var key = RequiredEventKey(state, usage);
        if (!calendar.Raids.TryGetValue(key, out var raid))
            return 0;
        lua_newtable(state);
        SetString(state, "name", raid.Name);
        SetString(state, "calendarType", raid.CalendarType);
        SetNumber(state, "raidID", raid.RaidId);
        PushCalendarTime(state, raid.Time);
        lua_setfield(state, -2, "time");
        SetNumber(state, "difficulty", raid.Difficulty);
        SetOptionalString(state, "difficultyName", raid.DifficultyName);
        return 1;
    }

    private static void PushMonthInfo(
        lua_State state,
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        const string usage =
            "Usage: local monthInfo = C_Calendar.GetMonthInfo([offsetMonths])";
        var offsetMonths = OptionalInt32(state, 1, 0, usage);
        var (month, year) = GetClampedMonth(
            runtime,
            calendar,
            offsetMonths);
        var firstDay = new DateTime(year, month, 1);
        lua_newtable(state);
        SetNumber(state, "month", month);
        SetNumber(state, "year", year);
        SetNumber(state, "numDays", DateTime.DaysInMonth(year, month));
        SetNumber(state, "firstWeekday", (int)firstDay.DayOfWeek + 1);
    }

    private static void SetAbsoluteMonth(
        lua_State state,
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        const string usage = "Usage: C_Calendar.SetAbsMonth(month, year)";
        var month = RequiredOneBasedIndex(state, 1, usage);
        var year = RequiredInt32(state, 2, usage);
        var currentYear = runtime.DateAndTime.CurrentTime.LocalDateTime.Year;
        var minimumYear = Math.Max(MinimumCalendarYear, currentYear - 5);
        var maximumYear = Math.Min(MaximumCalendarYear, currentYear + 5);
        calendar.Month = ((month - 1) % 12) + 1;
        calendar.Year = Math.Clamp(year, minimumYear, maximumYear);
    }

    private static void SetRelativeMonth(
        lua_State state,
        LuaRuntime runtime,
        WowCalendarState calendar)
    {
        const string usage = "Usage: C_Calendar.SetMonth(offsetMonths)";
        var offsetMonths = RequiredInt32(state, 1, usage);
        var (month, year) = GetClampedMonth(
            runtime,
            calendar,
            offsetMonths);
        calendar.Month = month;
        calendar.Year = year;
    }

    private static (int Month, int Year) GetClampedMonth(
        LuaRuntime runtime,
        WowCalendarState calendar,
        int offsetMonths)
    {
        var current = runtime.DateAndTime.CurrentTime.LocalDateTime;
        var baseMonth = calendar.Month is >= 1 and <= 12
            ? calendar.Month
            : current.Month;
        var baseYear = calendar.Year != 0 ? calendar.Year : current.Year;
        var totalMonths =
            ((long)baseYear * 12) + baseMonth - 1 + offsetMonths;
        var minimum = ((long)MinimumCalendarYear * 12) +
                      MinimumCalendarMonth - 1;
        var maximum = ((long)MaximumCalendarYear * 12) + 11;
        totalMonths = Math.Clamp(totalMonths, minimum, maximum);
        return ((int)(totalMonths % 12) + 1, (int)(totalMonths / 12));
    }

    private static DateTime GetMaximumCreateDate(LuaRuntime runtime)
    {
        var current = runtime.DateAndTime.CurrentTime.LocalDateTime;
        var firstOfCurrentMonth = new DateTime(current.Year, current.Month, 1);
        return firstOfCurrentMonth.AddMonths(13).AddMinutes(-1);
    }

    private static (int OffsetMonths, int MonthDay, int EventIndex)
        RequiredEventKey(lua_State state, string usage)
    {
        return (
            RequiredInt32(state, 1, usage),
            RequiredOneBasedIndex(state, 2, usage),
            RequiredOneBasedIndex(state, 3, usage));
    }

    private static int PushEventIndex(
        lua_State state,
        WowCalendarEventIndexState? eventIndex)
    {
        if (eventIndex is null)
            return 0;
        lua_newtable(state);
        SetNumber(state, "offsetMonths", eventIndex.OffsetMonths);
        SetNumber(state, "monthDay", eventIndex.MonthDay);
        SetNumber(state, "eventIndex", eventIndex.EventIndex);
        return 1;
    }

    private static void PushEventTypes(
        lua_State state,
        IList<WowCalendarEventTypeDisplayState> eventTypes)
    {
        lua_newtable(state);
        for (var index = 0; index < eventTypes.Count; index++)
        {
            var eventType = eventTypes[index];
            lua_newtable(state);
            SetString(state, "displayString", eventType.DisplayString);
            SetNumber(state, "eventType", eventType.EventType);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushDefaultGuildFilter(
        lua_State state,
        WowCalendarState calendar)
    {
        lua_newtable(state);
        SetNumber(state, "minLevel", calendar.GuildFilterMaximumLevel);
        SetNumber(state, "maxLevel", calendar.GuildFilterMaximumLevel);
        SetNumber(state, "rank", calendar.GuildFilterRank);
    }

    private static void PushDayEvent(
        lua_State state,
        WowCalendarDayEventState calendarEvent)
    {
        lua_newtable(state);
        SetDatabaseId(state, "eventID", calendarEvent.EventId);
        SetString(state, "title", calendarEvent.Title);
        SetBoolean(state, "isCustomTitle", calendarEvent.IsCustomTitle);
        PushCalendarTime(state, calendarEvent.StartTime);
        lua_setfield(state, -2, "startTime");
        PushCalendarTime(state, calendarEvent.EndTime);
        lua_setfield(state, -2, "endTime");
        SetOptionalString(state, "calendarType", calendarEvent.CalendarType);
        SetOptionalString(state, "sequenceType", calendarEvent.SequenceType);
        SetNumber(state, "eventType", calendarEvent.EventType);
        SetOptionalNumber(state, "iconTexture", calendarEvent.IconTexture);
        SetOptionalString(
            state,
            "modStatus",
            calendarEvent.ModeratorStatus);
        SetNumber(state, "inviteStatus", calendarEvent.InviteStatus);
        SetString(state, "invitedBy", calendarEvent.InvitedBy);
        SetNumber(state, "difficulty", calendarEvent.Difficulty);
        SetNumber(state, "inviteType", calendarEvent.InviteType);
        SetNumber(state, "sequenceIndex", calendarEvent.SequenceIndex);
        SetNumber(
            state,
            "numSequenceDays",
            calendarEvent.NumberOfSequenceDays);
        SetOptionalString(
            state,
            "difficultyName",
            calendarEvent.DifficultyName);
        SetBoolean(
            state,
            "dontDisplayBanner",
            calendarEvent.DoNotDisplayBanner);
        SetBoolean(
            state,
            "dontDisplayEnd",
            calendarEvent.DoNotDisplayEnd);
        SetDatabaseId(state, "clubID", calendarEvent.ClubId);
        SetBoolean(state, "isLocked", calendarEvent.IsLocked);
    }

    private static void PushCalendarTime(lua_State state, DateTime value)
    {
        lua_newtable(state);
        SetNumber(state, "monthDay", value.Day);
        SetNumber(state, "month", value.Month);
        SetNumber(state, "weekday", (int)value.DayOfWeek + 1);
        SetNumber(state, "year", value.Year);
        SetNumber(state, "hour", value.Hour);
        SetNumber(state, "minute", value.Minute);
    }

    private static void PushCalendarTime(
        lua_State state,
        WowCalendarTimeValueState value)
    {
        lua_newtable(state);
        SetNumber(state, "monthDay", value.MonthDay);
        SetNumber(state, "month", value.Month);
        SetNumber(state, "weekday", value.Weekday);
        SetNumber(state, "year", value.Year);
        SetNumber(state, "hour", value.Hour);
        SetNumber(state, "minute", value.Minute);
    }

    private static void SetOptionalCalendarTime(
        lua_State state,
        string field,
        DateTime? value)
    {
        if (value is { } time)
            PushCalendarTime(state, time);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static void PushOptionalDatabaseId(
        lua_State state,
        ulong? value)
    {
        if (value is { } databaseId)
            PushDatabaseId(state, databaseId);
        else
            lua_pushnil(state);
    }

    private static void SetDatabaseId(
        lua_State state,
        string field,
        ulong value)
    {
        PushDatabaseId(state, value);
        lua_setfield(state, -2, field);
    }

    private static void PushDatabaseId(lua_State state, ulong value)
    {
        if (value > MaximumExactLuaInteger)
        {
            lua_pushstring(
                state,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"0x{value:X16}"));
        }
        else
        {
            lua_pushnumber(state, value);
        }
    }

    private static ulong? OptionalDatabaseId(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) ||
            lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return null;
        }

        if (lua_type(state, index) == LUA_TSTRING)
        {
            var text = lua_tostring(state, index);
            if (text is not null &&
                text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (ulong.TryParse(
                        text.AsSpan(2),
                        NumberStyles.AllowHexSpecifier,
                        CultureInfo.InvariantCulture,
                        out var value) &&
                    value > MaximumExactLuaInteger)
                {
                    return value;
                }
                luaL_error(state, usage);
                return null;
            }
        }

        if (lua_isnumber(state, index) != 0)
        {
            var number = lua_tonumber(state, index);
            if (double.IsFinite(number) &&
                number is >= 0 and <= MaximumExactLuaInteger)
            {
                return (ulong)number;
            }
        }

        luaL_error(state, usage);
        return null;
    }

    private static ulong RequiredDatabaseId(
        lua_State state,
        int index,
        string usage)
    {
        var value = OptionalDatabaseId(state, index, usage);
        if (value is not null)
            return value.Value;
        luaL_error(state, usage);
        return 0;
    }

    private static CalendarTimeArgument RequiredCalendarTime(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_type(state, index) != LUA_TTABLE)
        {
            luaL_error(state, usage);
            return default;
        }

        return new CalendarTimeArgument(
            RequiredCalendarTimeIndexField(state, index, "monthDay", usage),
            RequiredCalendarTimeIndexField(state, index, "month", usage),
            RequiredCalendarTimeIndexField(state, index, "weekday", usage),
            RequiredCalendarTimeInt32Field(state, index, "year", usage),
            RequiredCalendarTimeInt32Field(state, index, "hour", usage),
            RequiredCalendarTimeInt32Field(state, index, "minute", usage));
    }

    private static uint RequiredCalendarTimeIndexField(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        var number = RequiredUInt32Number(state, -1, usage) - 1.0;
        lua_pop(state, 1);
        var signedValue = number is < int.MinValue or > int.MaxValue
            ? int.MinValue
            : (int)number;
        return unchecked((uint)signedValue);
    }

    private static int RequiredCalendarTimeInt32Field(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        var value = RequiredInt32(state, -1, usage);
        lua_pop(state, 1);
        return value;
    }

    private static bool TryGetCalendarDate(
        CalendarTimeArgument value,
        out DateTime date)
    {
        if (value.Year is < 1 or > 9999 ||
            value.MonthZeroBased > 11 ||
            value.MonthDayZeroBased > 30)
        {
            date = default;
            return false;
        }

        var month = (int)value.MonthZeroBased + 1;
        var monthDay = (int)value.MonthDayZeroBased + 1;
        if (monthDay > DateTime.DaysInMonth(value.Year, month))
        {
            date = default;
            return false;
        }

        date = new DateTime(value.Year, month, monthDay);
        return true;
    }

    private static int OptionalInt32(
        lua_State state,
        int index,
        int defaultValue,
        string usage)
    {
        if (index > lua_gettop(state) ||
            lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return defaultValue;
        }
        return RequiredInt32(state, index, usage);
    }

    private static int? OptionalInt32Value(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) ||
            lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return null;
        }
        return RequiredInt32(state, index, usage);
    }

    private static int? OptionalZeroBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) ||
            lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return null;
        }
        return RequiredZeroBasedIndex(state, index, usage);
    }

    private static int RequiredOneBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        return unchecked(RequiredZeroBasedIndex(state, index, usage) + 1);
    }

    private static int RequiredZeroBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        var number = RequiredUInt32Number(state, index, usage) - 1.0;
        return number > int.MaxValue ? int.MinValue : (int)number;
    }

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage)
    {
        return (uint)RequiredUInt32Number(state, index, usage);
    }

    private static double RequiredUInt32Number(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number < 0 || number > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return number;
    }

    private static byte RequiredByte(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < 0 or > byte.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (byte)(int)number;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number is < int.MinValue or > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (int)number;
    }

    private static byte RequiredCalendarEventType(
        lua_State state,
        int index,
        string usage)
    {
        var eventType = unchecked((byte)RequiredInt32(state, index, usage));
        if (eventType > 5)
        {
            luaL_error(state, usage);
            return 0;
        }
        return eventType;
    }

    private static byte RequiredCalendarInviteStatus(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var status = unchecked((byte)(int)lua_tonumber(state, index));
        if (status > 8)
        {
            luaL_error(state, usage);
            return 0;
        }
        return status;
    }

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static void SetBoolean(lua_State state, string field, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, field);
    }

    private static void SetNumber(lua_State state, string field, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalNumber(
        lua_State state,
        string field,
        int? value)
    {
        if (value is { } number)
            lua_pushinteger(state, number);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static void SetFileAsset(
        lua_State state,
        string field,
        int value)
    {
        if (value == 0)
            lua_pushnil(state);
        else
            lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalBoolean(
        lua_State state,
        string field,
        bool? value)
    {
        if (value is { } boolean)
            lua_pushboolean(state, boolean ? 1 : 0);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static void SetString(lua_State state, string field, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        if (value is not null)
            lua_pushstring(state, value);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }
}
