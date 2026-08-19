using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class CalendarCompletionContractTests
{
    [Fact]
    public void ExposesRecoveredCreationAndReadinessCallbacks()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "function:function:function:function:true:true:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(C_Calendar.AddEvent)," +
                "type(C_Calendar.AreNamesReady)," +
                "type(C_Calendar.CanAddEvent)," +
                "type(C_Calendar.CanSendInvite)," +
                "tostring(C_Calendar.AreNamesReady())," +
                "tostring(C_Calendar.CanAddEvent())," +
                "tostring(C_Calendar.CanSendInvite())},':')"));

        session.Lua.Calendar.PendingNameCount = 1;
        Assert.Equal(
            "false",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.AreNamesReady('ignored'))"));

        session.Lua.Calendar.PendingNameCount = 0;
        session.Lua.Calendar.IsBackendAvailable = false;
        Assert.Equal(
            "false",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.AreNamesReady())"));
    }

    [Fact]
    public void AddEventUsesNativePendingAndFiveSecondThrottleGates()
    {
        using var session = new EmulatorSession();
        session.Tick(0.001);

        Assert.Equal(
            "0\tfalse",
            session.Lua.Evaluate(
                "return select('#',C_Calendar.AddEvent('ignored'))," +
                "tostring(C_Calendar.CanAddEvent())"));
        Assert.True(session.Lua.Calendar.IsActionPending);
        Assert.Equal(1, session.Lua.Calendar.AddEventRequestCount);
        Assert.Equal(1U, session.Lua.Calendar.LastAddEventRequestTickMilliseconds);

        session.Lua.Calendar.IsActionPending = false;
        Assert.Equal(
            "false",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.CanAddEvent())"));

        session.Lua.Evaluate("C_Calendar.AddEvent(); return ''");
        Assert.Equal(1, session.Lua.Calendar.AddEventRequestCount);
        Assert.Equal(1, session.Lua.Calendar.ThrottledAddEventRequestCount);

        TickMany(session, 19, 0.25);
        session.Tick(0.249);
        Assert.Equal(
            "false",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.CanAddEvent())"));
        session.Tick(0.002);
        Assert.Equal(
            "true",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.CanAddEvent())"));
    }

    [Fact]
    public void InviteThrottleAndPendingGateMatchNativeOrdering()
    {
        using var session = new EmulatorSession();
        session.Tick(1);
        var calendar = session.Lua.Calendar;
        calendar.LastInviteRequestTickMilliseconds =
            session.Lua.FrameTime.TickMilliseconds;

        Assert.Equal(
            "false",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.CanSendInvite())"));
        TickMany(session, 7, 0.25);
        session.Tick(0.249);
        Assert.Equal(
            "false",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.CanSendInvite())"));
        session.Tick(0.002);
        Assert.Equal(
            "true",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.CanSendInvite())"));

        calendar.BypassActionThrottles = true;
        calendar.LastAddEventRequestTickMilliseconds =
            session.Lua.FrameTime.TickMilliseconds;
        calendar.LastInviteRequestTickMilliseconds =
            session.Lua.FrameTime.TickMilliseconds;
        Assert.Equal(
            "true\ttrue",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.CanAddEvent())," +
                "tostring(C_Calendar.CanSendInvite())"));

        calendar.IsActionPending = true;
        Assert.Equal(
            "false\tfalse",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.CanAddEvent())," +
                "tostring(C_Calendar.CanSendInvite())"));
    }

    [Fact]
    public void AddEventDoesNotInventARequestWhenNativeBackendGatesFail()
    {
        using var session = new EmulatorSession();
        session.Tick(0.001);
        var calendar = session.Lua.Calendar;

        calendar.IsBackendAvailable = false;
        session.Lua.Evaluate("C_Calendar.AddEvent(); return ''");
        Assert.Equal(0, calendar.AddEventRequestCount);
        Assert.False(calendar.IsActionPending);

        calendar.IsBackendAvailable = true;
        calendar.IsCurrentRealmDateValidForEvent = false;
        session.Lua.Evaluate("C_Calendar.AddEvent(); return ''");
        Assert.Equal(0, calendar.AddEventRequestCount);
        Assert.False(calendar.IsActionPending);

        calendar.IsCurrentRealmDateValidForEvent = true;
        calendar.CanCreatePlayerEvent = false;
        session.Lua.Evaluate("C_Calendar.AddEvent(); return ''");
        Assert.Equal(0, calendar.AddEventRequestCount);
        Assert.False(calendar.IsActionPending);
    }

    [Fact]
    public void ContextMenuPredicatesUseRequiredEventKeysAndRepresentedOutcomes()
    {
        using var session = new EmulatorSession();
        session.Lua.Calendar.ContextMenuEvents[(0, 15, 2)] =
            new WowCalendarContextMenuEventState(
                CanComplain: true,
                CanEdit: false,
                CanRemove: true,
                CalendarType: "PLAYER",
                CanSignUp: true);

        Assert.Equal(
            "true:false:true:false:true:true",
            session.Lua.Evaluate(
                "local complain=C_Calendar.ContextMenuEventCanComplain(0,15,2);" +
                "local edit=C_Calendar.ContextMenuEventCanEdit(0,15,2);" +
                "local remove=C_Calendar.ContextMenuEventCanRemove(0,15,2);" +
                "local missing=C_Calendar.ContextMenuEventCanEdit(0,15,3);" +
                "local badDay=pcall(C_Calendar.ContextMenuEventCanEdit,0,0,1);" +
                "local badIndex=pcall(C_Calendar.ContextMenuEventCanRemove,0,1,0);" +
                "return table.concat({tostring(complain),tostring(edit)," +
                "tostring(remove),tostring(missing),tostring(badDay)," +
                "tostring(badIndex)},':')"));
    }

    [Fact]
    public void ContextMenuClipboardAndCalendarTypeUseSelectedEvent()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        calendar.ContextMenuEventIndex = new WowCalendarEventIndexState(1, 20, 3);
        calendar.ContextMenuEvents[(1, 20, 3)] =
            new WowCalendarContextMenuEventState(
                CanComplain: false,
                CanEdit: true,
                CanRemove: true,
                CalendarType: "GUILD_EVENT",
                CanSignUp: false);

        Assert.Equal(
            "false:GUILD_EVENT:0:true",
            session.Lua.Evaluate(
                "local before=C_Calendar.ContextMenuEventClipboard();" +
                "local kind=C_Calendar.ContextMenuEventGetCalendarType();" +
                "local copied=select('#',C_Calendar.ContextMenuEventCopy());" +
                "local after=C_Calendar.ContextMenuEventClipboard('ignored');" +
                "return table.concat({tostring(before),kind,copied," +
                "tostring(after)},':')"));
        Assert.Equal(
            calendar.ContextMenuEventIndex,
            calendar.ContextMenuClipboardEventIndex);

        calendar.ContextMenuEventIndex = null;
        Assert.Equal(
            "nil",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.ContextMenuEventGetCalendarType())"));
    }

    [Fact]
    public void ContextMenuCommandsPreserveNativePendingAndPermissionGates()
    {
        using var session = new EmulatorSession();
        session.Tick(0.001);
        var calendar = session.Lua.Calendar;
        var selectedIndex = new WowCalendarEventIndexState(0, 10, 1);
        calendar.ContextMenuEventIndex = selectedIndex;
        calendar.ContextMenuClipboardEventIndex = selectedIndex;
        calendar.ContextMenuEvents[(0, 10, 1)] =
            new WowCalendarContextMenuEventState(
                CanComplain: false,
                CanEdit: true,
                CanRemove: true,
                CalendarType: "PLAYER",
                CanSignUp: true);

        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "return select('#',C_Calendar.ContextMenuEventPaste(0,12))"));
        Assert.True(calendar.IsActionPending);
        Assert.Equal(
            new WowCalendarPasteRequestState(selectedIndex, 0, 12),
            calendar.LastContextMenuPasteRequest);
        Assert.Equal(1U, calendar.LastAddEventRequestTickMilliseconds);

        calendar.IsActionPending = false;
        session.Lua.Evaluate(
            "C_Calendar.ContextMenuEventRemove('ignored'); return ''");
        Assert.True(calendar.IsActionPending);
        Assert.Equal(selectedIndex, calendar.LastContextMenuRemovedEvent);

        calendar.IsActionPending = false;
        session.Lua.Evaluate(
            "C_Calendar.ContextMenuEventSignUp('ignored'); return ''");
        Assert.True(calendar.IsActionPending);
        Assert.Equal(selectedIndex, calendar.LastContextMenuSignedUpEvent);

        calendar.IsActionPending = false;
        calendar.ContextMenuEvents[(0, 10, 1)] =
            new WowCalendarContextMenuEventState(
                CanComplain: false,
                CanEdit: false,
                CanRemove: false,
                CalendarType: null,
                CanSignUp: false);
        calendar.LastContextMenuRemovedEvent = null;
        calendar.LastContextMenuSignedUpEvent = null;
        session.Lua.Evaluate(
            "C_Calendar.ContextMenuEventRemove();" +
            "C_Calendar.ContextMenuEventSignUp(); return ''");
        Assert.False(calendar.IsActionPending);
        Assert.Null(calendar.LastContextMenuRemovedEvent);
        Assert.Null(calendar.LastContextMenuSignedUpEvent);
        Assert.Equal(
            "false",
            session.Lua.Evaluate(
                "return tostring(pcall(C_Calendar.ContextMenuEventPaste,0))"));
    }

    [Fact]
    public void ContextMenuSelectionRequiresAValidLoadedEventKey()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        calendar.ContextMenuEvents[(2, 18, 4)] =
            new WowCalendarContextMenuEventState(
                CanComplain: false,
                CanEdit: false,
                CanRemove: false,
                CalendarType: "PLAYER",
                CanSignUp: false);

        Assert.Equal(
            "0:2:18:4",
            session.Lua.Evaluate(
                "local count=select('#'," +
                "C_Calendar.ContextMenuSelectEvent(2,18,4));" +
                "local selected=C_Calendar.ContextMenuGetEventIndex();" +
                "return table.concat({count,selected.offsetMonths," +
                "selected.monthDay,selected.eventIndex},':')"));

        session.Lua.Evaluate(
            "C_Calendar.ContextMenuSelectEvent(2,18,5); return ''");
        Assert.Equal(
            new WowCalendarEventIndexState(2, 18, 4),
            calendar.ContextMenuEventIndex);
        Assert.Equal(
            "true:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_Calendar.ContextMenuSelectEvent,2,0,1))," +
                "tostring(pcall(C_Calendar.ContextMenuSelectEvent,2,1,0))},':')"));
    }

    [Fact]
    public void ContextMenuInviteActionsUseRecoveredResponseCodesAndPendingGate()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        var selectedIndex = new WowCalendarEventIndexState(0, 8, 2);
        calendar.ContextMenuEventIndex = selectedIndex;
        calendar.ContextMenuEvents[(0, 8, 2)] =
            new WowCalendarContextMenuEventState(
                CanComplain: false,
                CanEdit: false,
                CanRemove: false,
                CalendarType: "PLAYER",
                CanSignUp: false,
                CanRespondToInvite: true,
                CanRemoveInvite: true,
                CanTentative: true,
                TentativeUsesSignUpPacket: false);

        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "return select('#'," +
                "C_Calendar.ContextMenuInviteAvailable('ignored'))"));
        Assert.Equal(
            new WowCalendarInviteResponseState(selectedIndex, 1, false),
            calendar.LastContextMenuInviteResponse);
        Assert.True(calendar.IsActionPending);

        calendar.IsActionPending = false;
        session.Lua.Evaluate(
            "C_Calendar.ContextMenuInviteDecline(); return ''");
        Assert.Equal(
            new WowCalendarInviteResponseState(selectedIndex, 2, false),
            calendar.LastContextMenuInviteResponse);

        calendar.IsActionPending = false;
        session.Lua.Evaluate(
            "C_Calendar.ContextMenuInviteTentative(); return ''");
        Assert.Equal(
            new WowCalendarInviteResponseState(selectedIndex, 8, false),
            calendar.LastContextMenuInviteResponse);

        calendar.IsActionPending = false;
        calendar.ContextMenuEvents[(0, 8, 2)] =
            calendar.ContextMenuEvents[(0, 8, 2)] with
            {
                TentativeUsesSignUpPacket = true
            };
        session.Lua.Evaluate(
            "C_Calendar.ContextMenuInviteTentative(); return ''");
        Assert.Equal(
            new WowCalendarInviteResponseState(selectedIndex, 1, true),
            calendar.LastContextMenuInviteResponse);

        calendar.IsActionPending = false;
        session.Lua.Evaluate(
            "C_Calendar.ContextMenuInviteRemove(); return ''");
        Assert.Equal(selectedIndex, calendar.LastContextMenuInviteRemovedEvent);
        Assert.True(calendar.IsActionPending);

        calendar.LastContextMenuInviteResponse = null;
        calendar.LastContextMenuInviteRemovedEvent = null;
        session.Lua.Evaluate(
            "C_Calendar.ContextMenuInviteAvailable();" +
            "C_Calendar.ContextMenuInviteRemove(); return ''");
        Assert.Null(calendar.LastContextMenuInviteResponse);
        Assert.Null(calendar.LastContextMenuInviteRemovedEvent);
    }

    [Fact]
    public void DraftCreationUsesRecoveredKindsAndOnlyPlayerSignalsUpdate()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate(
            "calendarUpdates=0; local frame=CreateFrame('Frame');" +
            "frame:RegisterEvent('CALENDAR_UPDATE_EVENT');" +
            "frame:SetScript('OnEvent',function() calendarUpdates=" +
            "calendarUpdates+1 end); return ''");

        calendar.EventIndex = new WowCalendarEventIndexState(1, 2, 3);
        Assert.Equal(
            "function:function:function:function:0",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(C_Calendar.CreateCommunitySignUpEvent)," +
                "type(C_Calendar.CreateGuildAnnouncementEvent)," +
                "type(C_Calendar.CreateGuildSignUpEvent)," +
                "type(C_Calendar.CreatePlayerEvent)," +
                "select('#',C_Calendar.CreateCommunitySignUpEvent(" +
                "'ignored'))},':')"));
        Assert.Equal(
            new WowCalendarDraftEventState(
                WowCalendarDraftKind.CommunitySignUp),
            calendar.DraftEvent);
        Assert.True(calendar.IsEventOpen);
        Assert.Null(calendar.EventIndex);

        session.Lua.Evaluate(
            "C_Calendar.CreateGuildAnnouncementEvent(); return ''");
        Assert.Equal(
            new WowCalendarDraftEventState(
                WowCalendarDraftKind.GuildAnnouncement),
            calendar.DraftEvent);

        session.Lua.Evaluate(
            "C_Calendar.CreateGuildSignUpEvent(); return ''");
        Assert.Equal(
            new WowCalendarDraftEventState(WowCalendarDraftKind.GuildSignUp),
            calendar.DraftEvent);
        Assert.Equal("0", session.Lua.Evaluate("return calendarUpdates"));

        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        Assert.Equal(
            new WowCalendarDraftEventState(WowCalendarDraftKind.Player),
            calendar.DraftEvent);
        Assert.Equal("1", session.Lua.Evaluate("return calendarUpdates"));
    }

    [Fact]
    public void GuildDraftFailurePreservesOpenDraftAndClearsPendingState()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        var playerDraft = calendar.DraftEvent;
        calendar.IsPlayerInGuild = false;

        calendar.IsActionPending = true;
        session.Lua.Evaluate(
            "C_Calendar.CreateGuildAnnouncementEvent('ignored'); return ''");
        Assert.Equal(playerDraft, calendar.DraftEvent);
        Assert.True(calendar.IsEventOpen);
        Assert.False(calendar.IsActionPending);

        calendar.IsActionPending = true;
        session.Lua.Evaluate(
            "C_Calendar.CreateGuildSignUpEvent('ignored'); return ''");
        Assert.Equal(playerDraft, calendar.DraftEvent);
        Assert.False(calendar.IsActionPending);

        session.Lua.Evaluate("C_Calendar.CloseEvent(); return ''");
        Assert.Null(calendar.DraftEvent);
        Assert.False(calendar.IsEventOpen);
    }

    [Fact]
    public void EventEditPermissionAndFlagClearsFollowTheNativeBranch()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        Assert.Equal(
            "function:function:function:function:function:false",
            session.Lua.Evaluate(
                "return table.concat({type(C_Calendar.EventCanEdit)," +
                "type(C_Calendar.EventClearAutoApprove)," +
                "type(C_Calendar.EventClearLocked)," +
                "type(C_Calendar.EventSetAutoApprove)," +
                "type(C_Calendar.EventSetLocked)," +
                "tostring(C_Calendar.EventCanEdit('ignored'))},':')"));

        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        calendar.IsAutoApproveEnabled = true;
        calendar.IsEventLocked = true;
        calendar.IsEventDirty = false;
        Assert.Equal(
            "true:0:0",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(C_Calendar.EventCanEdit())," +
                "select('#',C_Calendar.EventClearAutoApprove('ignored'))," +
                "select('#',C_Calendar.EventClearLocked('ignored'))},':')"));
        Assert.False(calendar.IsAutoApproveEnabled);
        Assert.False(calendar.IsEventLocked);
        Assert.True(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "return select('#'," +
                "C_Calendar.EventSetAutoApprove('ignored'))"));
        Assert.True(calendar.IsAutoApproveEnabled);
        Assert.True(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate("C_Calendar.EventSetAutoApprove(); return ''");
        Assert.True(calendar.IsAutoApproveEnabled);
        Assert.False(calendar.IsEventDirty);

        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "return select('#',C_Calendar.EventSetLocked('ignored'))"));
        Assert.True(calendar.IsEventLocked);
        Assert.True(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate("C_Calendar.EventSetLocked(); return ''");
        Assert.True(calendar.IsEventLocked);
        Assert.False(calendar.IsEventDirty);

        calendar.OpenEventFlags = WowCalendarEventFlags.GuildAnnouncement;
        calendar.CanEditGuildEvents = false;
        calendar.IsAutoApproveEnabled = false;
        calendar.IsEventLocked = false;
        calendar.IsEventDirty = false;
        Assert.Equal(
            "false",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.EventCanEdit())"));
        session.Lua.Evaluate(
            "C_Calendar.EventSetAutoApprove();" +
            "C_Calendar.EventSetLocked(); return ''");
        Assert.False(calendar.IsAutoApproveEnabled);
        Assert.False(calendar.IsEventLocked);
        Assert.False(calendar.IsEventDirty);
    }

    [Fact]
    public void EventSetClubIdUsesOptionalDatabaseIdAndPermissionErrors()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate(
            "C_Calendar.CreateCommunitySignUpEvent(); return ''");
        calendar.IsEventDirty = false;

        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "return select('#',C_Calendar.EventSetClubId(" +
                "'0xFEDCBA9876543210','ignored'))"));
        Assert.Equal(0xFEDCBA9876543210UL, calendar.OpenEventClubId);
        Assert.True(calendar.IsEventDirty);
        Assert.Null(calendar.LastError);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetClubId('0xFEDCBA9876543210');" +
            "return ''");
        Assert.False(calendar.IsEventDirty);

        session.Lua.Evaluate("C_Calendar.EventSetClubId(nil); return ''");
        Assert.Equal(0UL, calendar.OpenEventClubId);
        Assert.True(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        calendar.CanEditOpenEvent = false;
        calendar.IsActionPending = true;
        session.Lua.Evaluate("C_Calendar.EventSetClubId(17); return ''");
        Assert.Equal("CALENDAR_ERROR_PERMISSIONS", calendar.LastError);
        Assert.False(calendar.IsActionPending);
        Assert.Equal(0UL, calendar.OpenEventClubId);
        Assert.False(calendar.IsEventDirty);

        Assert.Equal(
            "false:false",
            session.Lua.Evaluate(
                "return tostring(pcall(" +
                "C_Calendar.EventSetClubId,false))..':'.." +
                "tostring(pcall(C_Calendar.EventSetClubId,{}))"));
    }

    [Fact]
    public void EventSetDatePreservesNativeEncodingAndDirtyBeforeValidation()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        Assert.Equal(
            new WowCalendarEventDateState(-1, -1, -1),
            calendar.OpenEventDate);

        calendar.IsEventDirty = false;
        Assert.Equal(
            "function:0",
            session.Lua.Evaluate(
                "return type(C_Calendar.EventSetDate)..':'.." +
                "select('#',C_Calendar.EventSetDate(" +
                "'8.9','15.9','2026.9','ignored'))"));
        Assert.Equal(
            new WowCalendarEventDateState(7, 14, 26, 6),
            calendar.OpenEventDate);
        Assert.True(calendar.IsEventDirty);
        Assert.Null(calendar.LastError);

        calendar.IsEventDirty = false;
        calendar.IsActionPending = true;
        session.Lua.Evaluate(
            "C_Calendar.EventSetDate(8,15,2026); return ''");
        Assert.False(calendar.IsEventDirty);
        Assert.True(calendar.IsActionPending);

        calendar.IsEventDirty = false;
        calendar.IsActionPending = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetDate(8,15,4000); return ''");
        Assert.Equal(
            new WowCalendarEventDateState(7, 14, 0, 2),
            calendar.OpenEventDate);
        Assert.True(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetDate(8,15,4000); return ''");
        Assert.True(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        Assert.Equal(
            "true:0",
            session.Lua.Evaluate(
                "local ok=pcall(C_Calendar.EventSetDate,0,0,1999);" +
                "return tostring(ok)..':'..select('#'," +
                "C_Calendar.EventSetDate(0.5,0.5,1999))"));
        Assert.Equal(
            new WowCalendarEventDateState(0, 0, -1, 5),
            calendar.OpenEventDate);
        Assert.True(calendar.IsEventDirty);
        Assert.Null(calendar.LastError);

        var dateBeforeInvalid = calendar.OpenEventDate;
        calendar.IsEventDirty = false;
        calendar.IsActionPending = true;
        session.Lua.Evaluate(
            "C_Calendar.EventSetDate(13,1,2026); return ''");
        Assert.Equal(dateBeforeInvalid, calendar.OpenEventDate);
        Assert.True(calendar.IsEventDirty);
        Assert.Equal("CALENDAR_ERROR_INVALID_DATE", calendar.LastError);
        Assert.False(calendar.IsActionPending);

        calendar.IsEventDirty = false;
        calendar.IsActionPending = true;
        session.Lua.Evaluate(
            "C_Calendar.EventSetDate(1,1,2032); return ''");
        Assert.Equal(dateBeforeInvalid, calendar.OpenEventDate);
        Assert.True(calendar.IsEventDirty);
        Assert.Equal("CALENDAR_ERROR_INVALID_DATE", calendar.LastError);
        Assert.False(calendar.IsActionPending);

        calendar.CanEditOpenEvent = false;
        calendar.IsEventDirty = false;
        calendar.IsActionPending = true;
        session.Lua.Evaluate(
            "C_Calendar.EventSetDate(1,1,2026); return ''");
        Assert.Equal(dateBeforeInvalid, calendar.OpenEventDate);
        Assert.False(calendar.IsEventDirty);
        Assert.Equal("CALENDAR_ERROR_INVALID_DATE", calendar.LastError);
        Assert.False(calendar.IsActionPending);

        Assert.Equal(
            "false:false:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_Calendar.EventSetDate,1,1))," +
                "tostring(pcall(C_Calendar.EventSetDate,-1,1,2026))," +
                "tostring(pcall(C_Calendar.EventSetDate,1,1,-1))," +
                "tostring(pcall(C_Calendar.EventSetDate,1,1,false))},':')"));
    }

    [Fact]
    public void EventSetDescriptionPreservesNativeUtf8CopyAndSilentPermissionGate()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        Assert.Empty(calendar.OpenEventDescriptionUtf8);

        calendar.IsEventDirty = false;
        calendar.IsActionPending = true;
        calendar.LastError = "sentinel";
        Assert.Equal(
            "function:0",
            session.Lua.Evaluate(
                "return type(C_Calendar.EventSetDescription)..':'.." +
                "select('#',C_Calendar.EventSetDescription(" +
                "'Alpha|Beta\\nGamma','ignored'))"));
        Assert.Equal("AlphaBeta\nGamma", calendar.OpenEventDescription);
        Assert.True(calendar.IsEventDirty);
        Assert.True(calendar.IsActionPending);
        Assert.Equal("sentinel", calendar.LastError);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetDescription('Alpha|Beta\\nGamma');return ''");
        Assert.True(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetDescription('AlphaBeta\\nGamma');return ''");
        Assert.False(calendar.IsEventDirty);

        session.Lua.Evaluate("C_Calendar.EventSetDescription(123);return ''");
        Assert.Equal("123", calendar.OpenEventDescription);

        calendar.OpenEventDescription = "Prefix";
        calendar.IsEventDirty = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetDescription(" +
            "'Prefix'..string.char(0)..'Ignored');return ''");
        Assert.Equal("Prefix", calendar.OpenEventDescription);
        Assert.False(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetDescription(string.rep('a',300));return ''");
        Assert.Equal(256, calendar.OpenEventDescriptionUtf8.Count);
        Assert.Equal(new string('a', 256), calendar.OpenEventDescription);
        Assert.True(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetDescription(string.rep('a',300));return ''");
        Assert.True(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetDescription(string.rep('a',255).." +
            "string.char(240,159,153,130));" +
            "return ''");
        Assert.Equal(256, calendar.OpenEventDescriptionUtf8.Count);
        Assert.Equal(0xF0, calendar.OpenEventDescriptionUtf8[^1]);
        Assert.EndsWith("�", calendar.OpenEventDescription);

        var descriptionBeforeDenial = calendar.OpenEventDescriptionUtf8.ToArray();
        calendar.CanEditOpenEvent = false;
        calendar.IsEventDirty = false;
        calendar.IsActionPending = true;
        calendar.LastError = "preserved";
        session.Lua.Evaluate(
            "C_Calendar.EventSetDescription('denied');return ''");
        Assert.Equal(descriptionBeforeDenial, calendar.OpenEventDescriptionUtf8);
        Assert.False(calendar.IsEventDirty);
        Assert.True(calendar.IsActionPending);
        Assert.Equal("preserved", calendar.LastError);

        session.Lua.Evaluate("C_Calendar.CloseEvent();return ''");
        Assert.Empty(calendar.OpenEventDescriptionUtf8);
        calendar.OpenEventDescription = "server";
        calendar.IsActionPending = true;
        calendar.LastError = "still-preserved";
        session.Lua.Evaluate(
            "C_Calendar.EventSetDescription('missing');return ''");
        Assert.Equal("server", calendar.OpenEventDescription);
        Assert.True(calendar.IsActionPending);
        Assert.Equal("still-preserved", calendar.LastError);

        Assert.Equal(
            "false:false:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_Calendar.EventSetDescription))," +
                "tostring(pcall(C_Calendar.EventSetDescription,nil))," +
                "tostring(pcall(C_Calendar.EventSetDescription,false))," +
                "tostring(pcall(C_Calendar.EventSetDescription,{}))},':')"));
    }

    [Fact]
    public void EventSetInviteStatusPreservesNativeValidationThrottleAndDraftSplit()
    {
        using var session = new EmulatorSession();
        session.Tick(0.001);
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate(
            "C_Calendar.CreatePlayerEvent(); statusUpdates=0;" +
            "local listener=CreateFrame('Frame');" +
            "listener:RegisterEvent('CALENDAR_UPDATE_EVENT');" +
            "listener:SetScript('OnEvent',function() " +
            "statusUpdates=statusUpdates+1 end);return ''");
        calendar.InviteStatusThrottleMaximum = 100;

        Assert.Equal(
            "function:0",
            session.Lua.Evaluate(
                "return type(C_Calendar.EventSetInviteStatus)..':'.." +
                "select('#',C_Calendar.EventSetInviteStatus(" +
                "1,4,'ignored'))"));
        Assert.Equal((byte)4, calendar.EventInvites[0].InviteStatus);
        Assert.Equal("1", session.Lua.Evaluate("return statusUpdates"));
        Assert.Equal(1U, calendar.InviteStatusThrottleCount);

        session.Lua.Evaluate(
            "C_Calendar.EventSetInviteStatus(1,4);return ''");
        Assert.Equal("1", session.Lua.Evaluate("return statusUpdates"));
        Assert.Equal(2U, calendar.InviteStatusThrottleCount);

        Assert.Equal(
            "true:true",
            session.Lua.Evaluate(
                "return tostring(pcall(" +
                "C_Calendar.EventSetInviteStatus,1,256))..':'.." +
                "tostring(pcall(" +
                "C_Calendar.EventSetInviteStatus,1,0/0))"));
        Assert.Equal((byte)4, calendar.EventInvites[0].InviteStatus);

        session.Lua.Evaluate(
            "C_Calendar.EventSetInviteStatus(1,264);return ''");
        Assert.Equal((byte)8, calendar.EventInvites[0].InviteStatus);
        Assert.Equal("2", session.Lua.Evaluate("return statusUpdates"));

        calendar.InviteStatusThrottleCount = 0;
        calendar.IsActionPending = true;
        session.Lua.Evaluate(
            "C_Calendar.EventSetInviteStatus(1,5);return ''");
        Assert.Equal((byte)5, calendar.EventInvites[0].InviteStatus);
        Assert.True(calendar.IsActionPending);
        Assert.Equal("3", session.Lua.Evaluate("return statusUpdates"));

        calendar.OpenEventUsesSignUpStatusRules = true;
        session.Lua.Evaluate(
            "C_Calendar.EventSetInviteStatus(1,1);return ''");
        Assert.Equal((byte)5, calendar.EventInvites[0].InviteStatus);
        calendar.OpenEventUsesSignUpStatusRules = false;

        var throttleBeforePermissionDenial =
            calendar.InviteStatusThrottleCount;
        calendar.CanEditOpenEvent = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetInviteStatus(1,4);return ''");
        Assert.Equal(
            throttleBeforePermissionDenial,
            calendar.InviteStatusThrottleCount);
        Assert.Equal((byte)5, calendar.EventInvites[0].InviteStatus);

        calendar.CanEditOpenEvent = true;
        calendar.IsActionPending = false;
        calendar.InviteStatusThrottleCount = 0;
        Assert.Equal(
            "true",
            session.Lua.Evaluate(
                "return tostring(pcall(" +
                "C_Calendar.EventSetInviteStatus,0,4))"));
        Assert.Equal(1U, calendar.InviteStatusThrottleCount);
        Assert.Equal((byte)5, calendar.EventInvites[0].InviteStatus);

        calendar.DraftEvent = null;
        calendar.IsActionPending = true;
        calendar.InviteStatusThrottleCount = 0;
        session.Lua.Evaluate(
            "C_Calendar.EventSetInviteStatus(1,4);return ''");
        Assert.Equal(0U, calendar.InviteStatusThrottleCount);
        Assert.Null(calendar.LastEventInviteStatusRequest);

        calendar.IsActionPending = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetInviteStatus(1,4);return ''");
        Assert.Equal((byte)5, calendar.EventInvites[0].InviteStatus);
        Assert.Equal(
            new WowCalendarInviteStatusRequestState(
                1,
                0,
                "Player-0-00000001",
                4),
            calendar.LastEventInviteStatusRequest);
        Assert.Equal(1, calendar.EventInviteStatusRequestCount);

        calendar.LastEventInviteStatusRequest = null;
        calendar.InviteStatusThrottleMaximum = 4;
        calendar.InviteStatusThrottleCount = 4;
        calendar.LastInviteStatusThrottleResetTickMilliseconds =
            session.Lua.FrameTime.TickMilliseconds;
        session.Lua.Evaluate(
            "C_Calendar.EventSetInviteStatus(1,4);return ''");
        Assert.Equal(5U, calendar.InviteStatusThrottleCount);
        Assert.Equal(1, calendar.ThrottledInviteStatusRequestCount);
        Assert.Null(calendar.LastEventInviteStatusRequest);

        TickMany(session, 4, 0.25);
        session.Lua.Evaluate(
            "C_Calendar.EventSetInviteStatus(1,4);return ''");
        Assert.Equal(1U, calendar.InviteStatusThrottleCount);
        Assert.NotNull(calendar.LastEventInviteStatusRequest);
        Assert.Equal(2, calendar.EventInviteStatusRequestCount);

        Assert.Equal(
            "false:false:false:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_Calendar.EventSetInviteStatus))," +
                "tostring(pcall(C_Calendar.EventSetInviteStatus,1))," +
                "tostring(pcall(C_Calendar.EventSetInviteStatus,1,9))," +
                "tostring(pcall(C_Calendar.EventSetInviteStatus,1,-1))," +
                "tostring(pcall(" +
                "C_Calendar.EventSetInviteStatus,1,false))},':')"));
    }

    [Fact]
    public void EventAvailableAndDeclineUsePlayerInviteIdentityAndPendingGate()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        calendar.IsEventOpen = true;
        calendar.OpenEventId = 42;
        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                InviteId = 77,
                Guid = "Player-0000-1"
            });

        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "return select('#',C_Calendar.EventAvailable('ignored'))"));
        Assert.Equal((byte)1, calendar.LastEventInviteResponse);
        Assert.Equal(
            new WowCalendarEventInviteResponseRequestState(42, 77, 1),
            calendar.LastEventInviteResponseRequest);
        Assert.True(calendar.IsActionPending);

        calendar.IsActionPending = false;
        session.Lua.Evaluate("C_Calendar.EventDecline(); return ''");
        Assert.Equal((byte)2, calendar.LastEventInviteResponse);
        Assert.Equal(
            new WowCalendarEventInviteResponseRequestState(42, 77, 2),
            calendar.LastEventInviteResponseRequest);
        Assert.True(calendar.IsActionPending);

        calendar.LastEventInviteResponse = null;
        session.Lua.Evaluate(
            "C_Calendar.EventAvailable(); C_Calendar.EventDecline(); return ''");
        Assert.Null(calendar.LastEventInviteResponse);

        calendar.IsActionPending = false;
        calendar.DraftEvent =
            new WowCalendarDraftEventState(WowCalendarDraftKind.Player);
        session.Lua.Evaluate("C_Calendar.EventAvailable(); return ''");
        Assert.Null(calendar.LastEventInviteResponse);
    }

    [Fact]
    public void EventClearModeratorSeparatesLocalMutationFromServerRequest()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        Assert.Single(calendar.EventInvites);
        Assert.Equal(2, calendar.EventInvites[0].ModeratorStatus);
        Assert.Equal(1, calendar.InviteCount);
        session.Lua.Evaluate(
            "moderatorUpdates=0; local frame=CreateFrame('Frame');" +
            "frame:RegisterEvent('CALENDAR_UPDATE_EVENT');" +
            "frame:SetScript('OnEvent',function() moderatorUpdates=" +
            "moderatorUpdates+1 end); return ''");

        Assert.Equal(
            "0:true",
            session.Lua.Evaluate(
                "local count=select('#'," +
                "C_Calendar.EventClearModerator(1));" +
                "return count..':'..tostring(pcall(" +
                "C_Calendar.EventClearModerator,0))"));
        Assert.Equal(0, calendar.EventInvites[0].ModeratorStatus);
        Assert.Equal("1", session.Lua.Evaluate("return moderatorUpdates"));

        Assert.Equal(
            "function:0:false",
            session.Lua.Evaluate(
                "return type(C_Calendar.EventSetModerator)..':'.." +
                "select('#',C_Calendar.EventSetModerator(1,'ignored'))" +
                "..':'..tostring(pcall(" +
                "C_Calendar.EventSetModerator))"));
        Assert.Equal(1, calendar.EventInvites[0].ModeratorStatus);
        Assert.Equal("2", session.Lua.Evaluate("return moderatorUpdates"));

        session.Lua.Evaluate(
            "C_Calendar.EventSetModerator(1); return ''");
        Assert.Equal("2", session.Lua.Evaluate("return moderatorUpdates"));

        calendar.DraftEvent = null;
        calendar.LastEventModeratorRequest = null;
        session.Lua.Evaluate(
            "C_Calendar.EventClearModerator(1); return ''");
        Assert.Equal(
            new WowCalendarModeratorRequestState(1, false),
            calendar.LastEventModeratorRequest);
        Assert.Equal(1, calendar.EventInvites[0].ModeratorStatus);
        Assert.Equal("2", session.Lua.Evaluate("return moderatorUpdates"));

        calendar.EventInvites[0].ModeratorStatus = 0;
        calendar.LastEventModeratorRequest = null;
        session.Lua.Evaluate(
            "C_Calendar.EventSetModerator(1); return ''");
        Assert.Equal(
            new WowCalendarModeratorRequestState(1, true),
            calendar.LastEventModeratorRequest);
        Assert.Equal(0, calendar.EventInvites[0].ModeratorStatus);

        calendar.IsActionPending = true;
        calendar.LastEventModeratorRequest = null;
        session.Lua.Evaluate(
            "C_Calendar.EventClearModerator(1); return ''");
        Assert.Null(calendar.LastEventModeratorRequest);
    }

    [Fact]
    public void EventIdentityQueriesPreserveOptionalAndDraftKindResults()
    {
        using var session = new EmulatorSession();
        Assert.Equal(
            "1:nil:1:nil:2::false:1:nil",
            session.Lua.Evaluate(
                "local calendarCount=select('#'," +
                "C_Calendar.EventGetCalendarType('ignored'));" +
                "local calendarType=C_Calendar.EventGetCalendarType();" +
                "local clubCount=select('#',C_Calendar.EventGetClubId());" +
                "local clubId=C_Calendar.EventGetClubId();" +
                "local sortCount=select('#'," +
                "C_Calendar.EventGetInviteSortCriterion());" +
                "local criterion,reverse=" +
                "C_Calendar.EventGetInviteSortCriterion();" +
                "local selectedCount=select('#'," +
                "C_Calendar.EventGetSelectedInvite());" +
                "local selected=C_Calendar.EventGetSelectedInvite();" +
                "return table.concat({calendarCount,tostring(calendarType)," +
                "clubCount,tostring(clubId),sortCount,criterion," +
                "tostring(reverse),selectedCount,tostring(selected)},':')"));

        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        Assert.Equal(
            "PLAYER:0:status:false:1",
            session.Lua.Evaluate(
                "local criterion,reverse=" +
                "C_Calendar.EventGetInviteSortCriterion();" +
                "return table.concat({C_Calendar.EventGetCalendarType()," +
                "C_Calendar.EventGetClubId(),criterion,tostring(reverse)," +
                "C_Calendar.EventGetSelectedInvite()},':')"));

        session.Lua.Evaluate(
            "C_Calendar.CreateGuildAnnouncementEvent(); return ''");
        Assert.Equal(
            "GUILD_ANNOUNCEMENT:0",
            session.Lua.Evaluate(
                "return C_Calendar.EventGetCalendarType()..':'.." +
                "C_Calendar.EventGetSelectedInvite()"));
        session.Lua.Evaluate(
            "C_Calendar.CreateCommunitySignUpEvent(); return ''");
        Assert.Equal(
            "COMMUNITY_EVENT",
            session.Lua.Evaluate(
                "return C_Calendar.EventGetCalendarType()"));
        session.Lua.Evaluate(
            "C_Calendar.CreateGuildSignUpEvent(); return ''");
        Assert.Equal(
            "GUILD_EVENT",
            session.Lua.Evaluate(
                "return C_Calendar.EventGetCalendarType()"));
    }

    [Fact]
    public void EventInviteProjectionAndResponseTimeUseExactResultShape()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        Assert.Equal(
            "Player:1:Warrior:WARRIOR:3:CREATOR:true:0::1:" +
            "Player-0-00000001:0:true",
            session.Lua.Evaluate(
                "local info=C_Calendar.EventGetInvite(1);" +
                "return table.concat({info.name,info.level,info.className," +
                "info.classFilename,info.inviteStatus,info.modStatus," +
                "tostring(info.inviteIsMine),info.type,info.notes," +
                "info.classID,info.guid," +
                "select('#',C_Calendar.EventGetInvite(2))," +
                "tostring(pcall(C_Calendar.EventGetInvite,0))},':')"));
        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "return select('#'," +
                "C_Calendar.EventGetInviteResponseTime(1))"));

        session.Lua.Calendar.EventInvites[0].ResponseTime =
            new DateTime(2026, 8, 1, 14, 35, 0);
        Assert.Equal(
            "1:8:1:2026:14:35:7",
            session.Lua.Evaluate(
                "local count=select('#'," +
                "C_Calendar.EventGetInviteResponseTime(1));" +
                "local time=C_Calendar.EventGetInviteResponseTime(1);" +
                "return table.concat({count,time.month,time.monthDay," +
                "time.year,time.hour,time.minute,time.weekday},':')"));
    }

    [Fact]
    public void EventStatusOptionsFollowNativeAllowedStatusesAndPermissionGate()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        calendar.EventInvites[0].InviteStatus = 1;
        Assert.Equal(
            "5:2:CALENDAR_STATUS_DECLINED:3:4:5:8",
            session.Lua.Evaluate(
                "local options=C_Calendar.EventGetStatusOptions(1);" +
                "return table.concat({#options,options[1].status," +
                "options[1].statusString,options[2].status," +
                "options[3].status,options[4].status," +
                "options[5].status},':')"));

        calendar.OpenEventUsesSignUpStatusRules = true;
        calendar.EventInvites[0].InviteStatus = 3;
        Assert.Equal(
            "3:4:5:8",
            session.Lua.Evaluate(
                "local options=C_Calendar.EventGetStatusOptions(1);" +
                "return table.concat({#options,options[1].status," +
                "options[2].status,options[3].status},':')"));

        calendar.CanEditOpenEvent = false;
        calendar.IsActionPending = true;
        Assert.Equal(
            "0:false:true",
            session.Lua.Evaluate(
                "local options=C_Calendar.EventGetStatusOptions(1);" +
                "return #options..':'..tostring(" +
                "C_Calendar.IsActionPending())..':'..tostring(pcall(" +
                "C_Calendar.EventGetStatusOptions,0))"));
    }

    [Fact]
    public void EventTypesAndTexturesPreserveNativeShapesAndByteEnumParsing()
    {
        using var session = new EmulatorSession();
        session.Lua.Calendar.EventTexturesByType[0] =
            new List<WowCalendarEventTextureState>
            {
                new(
                    "Raid",
                    12345,
                    10,
                    16,
                    2444,
                    true,
                    9001),
                new(
                    "No Icon",
                    0,
                    0,
                    null,
                    null,
                    null,
                    9002)
            };

        Assert.Equal(
            "1:5:CALENDAR_TYPE_RAID:CALENDAR_TYPE_DUNGEON:" +
            "CALENDAR_TYPE_PVP:CALENDAR_TYPE_MEETING:" +
            "CALENDAR_TYPE_OTHER:2:Raid:12345:10:16:2444:true:" +
            "No Icon:nil:0:nil:nil:nil",
            session.Lua.Evaluate(
                "local typeCount=select('#',C_Calendar.EventGetTypes(" +
                "'ignored'));" +
                "local types=C_Calendar.EventGetTypes();" +
                "local textures=C_Calendar.EventGetTextures(0);" +
                "return table.concat({typeCount,#types,types[1],types[2]," +
                "types[3],types[4],types[5],#textures," +
                "textures[1].title,tostring(textures[1].iconTexture)," +
                "textures[1].expansionLevel,textures[1].difficultyId," +
                "textures[1].mapId,tostring(textures[1].isLfr)," +
                "textures[2].title,tostring(textures[2].iconTexture)," +
                "textures[2].expansionLevel," +
                "tostring(textures[2].difficultyId)," +
                "tostring(textures[2].mapId)," +
                "tostring(textures[2].isLfr)},':')"));

        Assert.Equal(
            "2:2:0:true:false:false",
            session.Lua.Evaluate(
                "return table.concat({#C_Calendar.EventGetTextures('256')," +
                "#C_Calendar.EventGetTextures(256.9)," +
                "#C_Calendar.EventGetTextures(5)," +
                "tostring(pcall(C_Calendar.EventGetTextures,261))," +
                "tostring(pcall(C_Calendar.EventGetTextures,6))," +
                "tostring(pcall(C_Calendar.EventGetTextures,-1))},':')"));
    }

    [Fact]
    public void EventSetTextureIdSelectsNativeDb2IdsFromTypeSpecificLists()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        calendar.EventTexturesByType[0] =
            new List<WowCalendarEventTextureState>
            {
                new("Raid A", 101, 0, null, null, null, 7001),
                new("Raid B", 102, 0, null, null, null, 7002)
            };
        calendar.EventTexturesByType[1] =
            new List<WowCalendarEventTextureState>
            {
                new("Dungeon A", 201, 0, null, null, null, 8001),
                new("Dungeon B", 202, 0, null, null, null, 8002)
            };

        Assert.Equal(
            "function:0",
            session.Lua.Evaluate(
                "return type(C_Calendar.EventSetTextureID)..':'.." +
                "select('#',C_Calendar.EventSetTextureID(1,'ignored'))"));
        Assert.Equal(0, calendar.OpenEventTextureId);

        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        calendar.IsEventDirty = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetTextureID(2,'ignored'); return ''");
        Assert.Equal(7002, calendar.OpenEventTextureId);
        Assert.True(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate("C_Calendar.EventSetTextureID(2); return ''");
        Assert.False(calendar.IsEventDirty);
        session.Lua.Evaluate("C_Calendar.EventSetTextureID(3); return ''");
        Assert.Equal(7002, calendar.OpenEventTextureId);
        Assert.False(calendar.IsEventDirty);

        calendar.OpenEventType = 1;
        session.Lua.Evaluate("C_Calendar.EventSetTextureID(1); return ''");
        Assert.Equal(8001, calendar.OpenEventTextureId);
        Assert.True(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        calendar.OpenEventType = 5;
        session.Lua.Evaluate("C_Calendar.EventSetTextureID(2); return ''");
        Assert.Equal(8002, calendar.OpenEventTextureId);
        Assert.True(calendar.IsEventDirty);
        Assert.Equal(
            "2:2",
            session.Lua.Evaluate(
                "return #C_Calendar.EventGetTextures(1)..':'.." +
                "#C_Calendar.EventGetTextures(5)"));

        calendar.IsEventDirty = false;
        calendar.OpenEventType = 2;
        session.Lua.Evaluate("C_Calendar.EventSetTextureID(1); return ''");
        Assert.Equal(8002, calendar.OpenEventTextureId);
        Assert.False(calendar.IsEventDirty);

        calendar.OpenEventType = 0;
        calendar.CanEditOpenEvent = false;
        session.Lua.Evaluate("C_Calendar.EventSetTextureID(1); return ''");
        Assert.Equal(8002, calendar.OpenEventTextureId);
        Assert.False(calendar.IsEventDirty);

        Assert.Equal(
            "false:false:false:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_Calendar.EventSetTextureID))," +
                "tostring(pcall(C_Calendar.EventSetTextureID,-1))," +
                "tostring(pcall(C_Calendar.EventSetTextureID,4294967296))," +
                "tostring(pcall(C_Calendar.EventSetTextureID,0))},':')"));
    }

    [Fact]
    public void EventSetTimePreservesUInt32WrapAndDirtyBeforeValidation()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        calendar.IsActionPending = true;
        Assert.Equal(
            "function:0",
            session.Lua.Evaluate(
                "return type(C_Calendar.EventSetTime)..':'.." +
                "select('#',C_Calendar.EventSetTime(12,34,'ignored'))"));
        Assert.Equal(
            "CALENDAR_ERROR_INVALID_TIME",
            calendar.LastError);
        Assert.False(calendar.IsActionPending);
        Assert.Equal(new WowCalendarEventTimeState(-1, -1), calendar.OpenEventTime);

        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        calendar.IsEventDirty = false;
        calendar.IsActionPending = true;
        session.Lua.Evaluate(
            "C_Calendar.EventSetTime('12.9',34.9,'ignored'); return ''");
        Assert.Equal(new WowCalendarEventTimeState(12, 34), calendar.OpenEventTime);
        Assert.True(calendar.IsEventDirty);
        Assert.True(calendar.IsActionPending);
        Assert.Null(calendar.LastError);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate("C_Calendar.EventSetTime(12,34); return ''");
        Assert.False(calendar.IsEventDirty);

        calendar.IsActionPending = true;
        session.Lua.Evaluate("C_Calendar.EventSetTime(24,34); return ''");
        Assert.Equal(new WowCalendarEventTimeState(12, 34), calendar.OpenEventTime);
        Assert.True(calendar.IsEventDirty);
        Assert.False(calendar.IsActionPending);
        Assert.Equal(
            "CALENDAR_ERROR_INVALID_TIME",
            calendar.LastError);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetTime(4294967295,2147483648); return ''");
        Assert.Equal(
            new WowCalendarEventTimeState(-1, int.MinValue),
            calendar.OpenEventTime);
        Assert.True(calendar.IsEventDirty);
        Assert.Null(calendar.LastError);

        calendar.IsEventDirty = false;
        calendar.OpenEventFlags = WowCalendarEventFlags.GuildAnnouncement;
        calendar.CanEditGuildEvents = false;
        calendar.IsActionPending = true;
        session.Lua.Evaluate("C_Calendar.EventSetTime(1,2); return ''");
        Assert.Equal(
            new WowCalendarEventTimeState(-1, int.MinValue),
            calendar.OpenEventTime);
        Assert.False(calendar.IsEventDirty);
        Assert.False(calendar.IsActionPending);
        Assert.Equal(
            "CALENDAR_ERROR_INVALID_TIME",
            calendar.LastError);

        Assert.Equal(
            "false:false:false:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_Calendar.EventSetTime))," +
                "tostring(pcall(C_Calendar.EventSetTime,1))," +
                "tostring(pcall(C_Calendar.EventSetTime,-1,0))," +
                "tostring(pcall(C_Calendar.EventSetTime,0,-1))," +
                "tostring(pcall(C_Calendar.EventSetTime,4294967296,0))},':')"));
    }

    [Fact]
    public void EventSetTitlePreservesNativeFilteringAndCStringComparison()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        Assert.Empty(calendar.OpenEventTitleUtf8);

        calendar.IsEventDirty = false;
        calendar.IsActionPending = true;
        calendar.LastError = "sentinel";
        Assert.Equal(
            "function:0",
            session.Lua.Evaluate(
                "return type(C_Calendar.EventSetTitle)..':'.." +
                "select('#',C_Calendar.EventSetTitle(" +
                "'Alpha|Beta','ignored'))"));
        Assert.Equal("AlphaBeta", calendar.OpenEventTitle);
        Assert.True(calendar.IsEventDirty);
        Assert.True(calendar.IsActionPending);
        Assert.Equal("sentinel", calendar.LastError);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetTitle('Alpha|Beta');return ''");
        Assert.True(calendar.IsEventDirty);

        session.Lua.Evaluate(
            "C_Calendar.EventSetTitle(" +
            "'Line'..string.char(10)..'Tail');return ''");
        Assert.Equal("Line", calendar.OpenEventTitle);
        session.Lua.Evaluate(
            "C_Calendar.EventSetTitle(" +
            "'Slash'..string.char(92)..'nTail');return ''");
        Assert.Equal("Slash", calendar.OpenEventTitle);
        session.Lua.Evaluate(
            "C_Calendar.EventSetTitle('Pipe|nTail');return ''");
        Assert.Equal("PipenTail", calendar.OpenEventTitle);

        session.Lua.Evaluate("C_Calendar.EventSetTitle(123);return ''");
        Assert.Equal("123", calendar.OpenEventTitle);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetTitle(string.rep('a',40));return ''");
        Assert.Equal(32, calendar.OpenEventTitleUtf8.Count);
        Assert.Equal(new string('a', 32), calendar.OpenEventTitle);
        Assert.True(calendar.IsEventDirty);

        session.Lua.Evaluate(
            "C_Calendar.EventSetTitle(string.rep('a',31).." +
            "string.char(240,159,153,130));return ''");
        Assert.Equal(32, calendar.OpenEventTitleUtf8.Count);
        Assert.Equal(0xF0, calendar.OpenEventTitleUtf8[^1]);

        calendar.OpenEventTitle = "Visible";
        calendar.IsEventDirty = false;
        session.Lua.Evaluate(
            "C_Calendar.EventSetTitle(" +
            "'Visible'..string.char(0)..'Ignored');return ''");
        Assert.Equal("Visible", calendar.OpenEventTitle);
        Assert.False(calendar.IsEventDirty);

        var titleBeforeDenial = calendar.OpenEventTitleUtf8.ToArray();
        calendar.CanEditOpenEvent = false;
        calendar.IsEventDirty = false;
        calendar.IsActionPending = true;
        calendar.LastError = "preserved";
        session.Lua.Evaluate("C_Calendar.EventSetTitle('denied');return ''");
        Assert.Equal(titleBeforeDenial, calendar.OpenEventTitleUtf8);
        Assert.False(calendar.IsEventDirty);
        Assert.True(calendar.IsActionPending);
        Assert.Equal("preserved", calendar.LastError);

        session.Lua.Evaluate("C_Calendar.CloseEvent();return ''");
        Assert.Empty(calendar.OpenEventTitleUtf8);
        calendar.OpenEventTitle = "server";
        session.Lua.Evaluate("C_Calendar.EventSetTitle('missing');return ''");
        Assert.Equal("server", calendar.OpenEventTitle);

        Assert.Equal(
            "false:false:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_Calendar.EventSetTitle))," +
                "tostring(pcall(C_Calendar.EventSetTitle,nil))," +
                "tostring(pcall(C_Calendar.EventSetTitle,false))," +
                "tostring(pcall(C_Calendar.EventSetTitle,{}))},':')"));
    }

    [Fact]
    public void EventSetTypePreservesLowByteParsingAndTypeFiveComparisonAlias()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        calendar.IsActionPending = true;
        calendar.LastError = "sentinel";
        Assert.Equal(
            "function:0",
            session.Lua.Evaluate(
                "return type(C_Calendar.EventSetType)..':'.." +
                "select('#',C_Calendar.EventSetType('258.9','ignored'))"));
        Assert.Equal((byte)0, calendar.OpenEventType);
        Assert.True(calendar.IsActionPending);
        Assert.Equal("sentinel", calendar.LastError);

        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        calendar.IsEventDirty = false;
        calendar.IsActionPending = true;
        calendar.LastError = "preserved";
        session.Lua.Evaluate(
            "C_Calendar.EventSetType('258.9','ignored');return ''");
        Assert.Equal((byte)2, calendar.OpenEventType);
        Assert.True(calendar.IsEventDirty);
        Assert.True(calendar.IsActionPending);
        Assert.Equal("preserved", calendar.LastError);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate("C_Calendar.EventSetType(2);return ''");
        Assert.False(calendar.IsEventDirty);

        session.Lua.Evaluate("C_Calendar.EventSetType(261);return ''");
        Assert.Equal((byte)5, calendar.OpenEventType);
        Assert.True(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate("C_Calendar.EventSetType(1);return ''");
        Assert.Equal((byte)5, calendar.OpenEventType);
        Assert.False(calendar.IsEventDirty);

        session.Lua.Evaluate("C_Calendar.EventSetType(5);return ''");
        Assert.Equal((byte)5, calendar.OpenEventType);
        Assert.True(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        session.Lua.Evaluate("C_Calendar.EventSetType(256);return ''");
        Assert.Equal((byte)0, calendar.OpenEventType);
        Assert.True(calendar.IsEventDirty);

        calendar.IsEventDirty = false;
        calendar.OpenEventFlags = WowCalendarEventFlags.GuildAnnouncement;
        calendar.CanEditGuildEvents = false;
        session.Lua.Evaluate("C_Calendar.EventSetType(3);return ''");
        Assert.Equal((byte)0, calendar.OpenEventType);
        Assert.False(calendar.IsEventDirty);

        Assert.Equal(
            "false:false:false:false:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_Calendar.EventSetType))," +
                "tostring(pcall(C_Calendar.EventSetType,6))," +
                "tostring(pcall(C_Calendar.EventSetType,262))," +
                "tostring(pcall(C_Calendar.EventSetType,-1))," +
                "tostring(pcall(C_Calendar.EventSetType,false))," +
                "tostring(pcall(C_Calendar.EventSetType,2147483648))},':')"));
    }

    [Fact]
    public void EventSignUpMatchesOpenLoadedEventAndExactPlayerGuidRules()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        calendar.LastError = "sentinel";
        Assert.Equal(
            "function:0",
            session.Lua.Evaluate(
                "return type(C_Calendar.EventSignUp)..':'.." +
                "select('#',C_Calendar.EventSignUp('ignored'))"));
        Assert.Null(calendar.LastEventSignUpRequest);
        Assert.False(calendar.IsActionPending);
        Assert.Equal("sentinel", calendar.LastError);

        calendar.IsEventOpen = true;
        calendar.OpenEventId = 42;
        calendar.OpenEventClubId = 7;
        session.Lua.Evaluate("C_Calendar.EventSignUp();return ''");
        Assert.Null(calendar.LastEventSignUpRequest);

        calendar.OpenEventUsesSignUpStatusRules = true;
        calendar.DraftEvent =
            new WowCalendarDraftEventState(WowCalendarDraftKind.CommunitySignUp);
        session.Lua.Evaluate("C_Calendar.EventSignUp();return ''");
        Assert.Null(calendar.LastEventSignUpRequest);

        calendar.DraftEvent = null;
        calendar.EventInvites.Add(
            new WowCalendarEventInviteState { Guid = "Player-0000-1" });
        session.Lua.Evaluate("C_Calendar.EventSignUp();return ''");
        Assert.Null(calendar.LastEventSignUpRequest);

        calendar.EventInvites[0].Guid = "Player-0-00000002";
        session.Lua.Evaluate("C_Calendar.EventSignUp();return ''");
        Assert.Equal(
            new WowCalendarEventSignUpRequestState(42, 7, false),
            calendar.LastEventSignUpRequest);
        Assert.Equal(1, calendar.EventSignUpRequestCount);
        Assert.True(calendar.IsActionPending);
        Assert.Equal("sentinel", calendar.LastError);

        session.Lua.Evaluate("C_Calendar.EventSignUp();return ''");
        Assert.Equal(1, calendar.EventSignUpRequestCount);
    }

    [Fact]
    public void EventSortInvitesMapsCriteriaTogglesDirectionAndSortsImmediately()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        Assert.Equal(
            "function:0",
            session.Lua.Evaluate(
                "return type(C_Calendar.EventSortInvites)..':'.." +
                "select('#',C_Calendar.EventSortInvites(42,false,'ignored'))"));
        Assert.Equal(string.Empty, calendar.InviteSortCriterion);

        calendar.IsEventOpen = true;
        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                InviteId = 1,
                Name = "Charlie",
                Level = 10,
                ClassName = "Warrior",
                InviteStatus = 0,
                Notes = "z"
            });
        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                InviteId = 2,
                Name = "Alice",
                Level = 30,
                ClassName = "Mage",
                InviteStatus = 3,
                IsInPlayerGroup = true,
                Notes = "a"
            });
        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                InviteId = 3,
                Name = "Bob",
                Level = 20,
                ClassName = "Druid",
                InviteStatus = 6,
                Notes = "m"
            });
        session.Lua.Evaluate(
            "SortListener=CreateFrame('Frame');SortEvents=0;" +
            "SortListener:RegisterEvent('CALENDAR_UPDATE_EVENT');" +
            "SortListener:SetScript('OnEvent',function() SortEvents=SortEvents+1 end);" +
            "return ''");

        session.Lua.Evaluate(
            "C_Calendar.EventSortInvites('level',false);return ''");
        Assert.Equal([1UL, 3UL, 2UL],
            calendar.EventInvites.Select(invite => invite.InviteId));
        Assert.Equal(
            "level:false:1",
            session.Lua.Evaluate(
                "local c,r=C_Calendar.EventGetInviteSortCriterion();" +
                "return c..':'..tostring(r)..':'..SortEvents"));

        session.Lua.Evaluate(
            "C_Calendar.EventSortInvites('level',true);return ''");
        Assert.Equal([2UL, 3UL, 1UL],
            calendar.EventInvites.Select(invite => invite.InviteId));
        session.Lua.Evaluate(
            "C_Calendar.EventSortInvites('level',false);return ''");
        Assert.Equal([2UL, 3UL, 1UL],
            calendar.EventInvites.Select(invite => invite.InviteId));

        session.Lua.Evaluate(
            "C_Calendar.EventSortInvites('LEVEL',nil);return ''");
        Assert.Equal("status", calendar.InviteSortCriterion);
        Assert.False(calendar.InviteSortReverse);
        Assert.Equal([2UL, 3UL, 1UL],
            calendar.EventInvites.Select(invite => invite.InviteId));

        session.Lua.Evaluate(
            "C_Calendar.EventSortInvites('class',false);return ''");
        Assert.Equal([3UL, 2UL, 1UL],
            calendar.EventInvites.Select(invite => invite.InviteId));
        session.Lua.Evaluate(
            "C_Calendar.EventSortInvites('party',false);return ''");
        Assert.Equal([2UL, 3UL, 1UL],
            calendar.EventInvites.Select(invite => invite.InviteId));
        session.Lua.Evaluate(
            "C_Calendar.EventSortInvites('notes',false);return ''");
        Assert.Equal([2UL, 3UL, 1UL],
            calendar.EventInvites.Select(invite => invite.InviteId));
        session.Lua.Evaluate(
            "C_Calendar.EventSortInvites('name',true);return ''");
        Assert.Equal([1UL, 3UL, 2UL],
            calendar.EventInvites.Select(invite => invite.InviteId));

        Assert.Equal(
            "false:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_Calendar.EventSortInvites))," +
                "tostring(pcall(C_Calendar.EventSortInvites,false,false))," +
                "tostring(pcall(C_Calendar.EventSortInvites,'name'))},':')"));
    }

    [Fact]
    public void EventTentativeSeparatesClubSignUpFromInviteResponsePackets()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        calendar.LastError = "sentinel";
        Assert.Equal(
            "function:0",
            session.Lua.Evaluate(
                "return type(C_Calendar.EventTentative)..':'.." +
                "select('#',C_Calendar.EventTentative('ignored'))"));
        Assert.Null(calendar.LastEventSignUpRequest);
        Assert.Null(calendar.LastEventInviteResponseRequest);
        Assert.Equal("sentinel", calendar.LastError);

        calendar.IsEventOpen = true;
        calendar.OpenEventId = 42;
        calendar.OpenEventClubId = 7;
        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                InviteId = 77,
                Guid = "Player-0000-1",
                Type = 0
            });
        session.Lua.Evaluate("C_Calendar.EventTentative();return ''");
        Assert.Equal(
            new WowCalendarEventInviteResponseRequestState(42, 77, 8),
            calendar.LastEventInviteResponseRequest);
        Assert.Equal((byte)8, calendar.LastEventInviteResponse);
        Assert.Equal(1, calendar.EventInviteResponseRequestCount);
        Assert.True(calendar.IsActionPending);

        calendar.IsActionPending = false;
        calendar.LastEventInviteResponse = null;
        calendar.LastEventInviteResponseRequest = null;
        calendar.OpenEventUsesSignUpStatusRules = true;
        calendar.EventInvites[0].Type = 1;
        session.Lua.Evaluate("C_Calendar.EventTentative();return ''");
        Assert.Null(calendar.LastEventInviteResponseRequest);
        Assert.False(calendar.IsActionPending);

        calendar.EventInvites.Clear();
        session.Lua.Clubs.SubscribedClubs.Add(
            new WowClubInfoState { ClubId = 7 });
        session.Lua.Evaluate("C_Calendar.EventTentative();return ''");
        Assert.Null(calendar.LastEventSignUpRequest);
        Assert.False(calendar.IsActionPending);

        session.Lua.Clubs.SelfMemberInfoByClubId[7] =
            new WowClubMemberInfoState { IsSelf = true };
        session.Lua.Evaluate("C_Calendar.EventTentative();return ''");
        Assert.Equal(
            new WowCalendarEventSignUpRequestState(42, 7, true),
            calendar.LastEventSignUpRequest);
        Assert.Equal(1, calendar.EventSignUpRequestCount);
        Assert.True(calendar.IsActionPending);
        Assert.Equal("sentinel", calendar.LastError);

        calendar.IsActionPending = false;
        calendar.DraftEvent =
            new WowCalendarDraftEventState(WowCalendarDraftKind.CommunitySignUp);
        calendar.LastEventSignUpRequest = null;
        session.Lua.Evaluate("C_Calendar.EventTentative();return ''");
        Assert.Null(calendar.LastEventSignUpRequest);
        Assert.False(calendar.IsActionPending);

        calendar.DraftEvent = null;
        session.Lua.Evaluate(
            "C_Calendar.EventAvailable();" +
            "C_Calendar.EventDecline();return ''");
        Assert.Equal(1, calendar.EventSignUpRequestCount);
        Assert.Null(calendar.LastEventInviteResponseRequest);
        Assert.False(calendar.IsActionPending);

        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                InviteId = 88,
                Guid = "Player-0-00000001",
                Type = 0
            });
        session.Lua.Evaluate("C_Calendar.EventTentative();return ''");
        Assert.Equal(
            new WowCalendarEventInviteResponseRequestState(42, 88, 8),
            calendar.LastEventInviteResponseRequest);
        Assert.Equal(2, calendar.EventInviteResponseRequestCount);
        Assert.True(calendar.IsActionPending);
    }

    [Fact]
    public void EventHasPendingInviteMatchesTheOpenEventId()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        calendar.PendingEventInviteIds.Add(42);
        Assert.Equal(
            "1:false",
            session.Lua.Evaluate(
                "return table.concat({select('#'," +
                "C_Calendar.EventHasPendingInvite())," +
                "tostring(C_Calendar.EventHasPendingInvite())},':')"));

        calendar.IsEventOpen = true;
        calendar.OpenEventId = 41;
        Assert.Equal(
            "false",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.EventHasPendingInvite())"));
        calendar.OpenEventId = 42;
        Assert.Equal(
            "1:true",
            session.Lua.Evaluate(
                "return table.concat({select('#'," +
                "C_Calendar.EventHasPendingInvite('ignored'))," +
                "tostring(C_Calendar.EventHasPendingInvite())},':')"));

        session.Lua.Evaluate("C_Calendar.CloseEvent(); return ''");
        Assert.Equal(0UL, calendar.OpenEventId);
        Assert.Equal(
            "false",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.EventHasPendingInvite())"));
    }

    [Fact]
    public void EventHaveSettingsChangedReadsOnlyTheOpenEventDirtyByte()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        calendar.IsEventDirty = true;

        Assert.Equal(
            "function:1:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(C_Calendar.EventHaveSettingsChanged)," +
                "select('#',C_Calendar.EventHaveSettingsChanged())," +
                "tostring(C_Calendar.EventHaveSettingsChanged())},':')"));

        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        Assert.True(calendar.IsEventDirty);
        Assert.Equal(
            "true",
            session.Lua.Evaluate(
                "return tostring(" +
                "C_Calendar.EventHaveSettingsChanged({},'ignored'))"));

        calendar.IsEventDirty = false;
        Assert.Equal(
            "false",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.EventHaveSettingsChanged(false))"));

        calendar.IsEventDirty = true;
        session.Lua.Evaluate("C_Calendar.CloseEvent(); return ''");
        Assert.False(calendar.IsEventDirty);
        Assert.Equal(
            "false",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.EventHaveSettingsChanged())"));
    }

    [Fact]
    public void EventInviteQueuesCanonicalizedNamesWithoutLocalMutation()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        calendar.BypassActionThrottles = true;
        session.Tick(3.0);

        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "return select('#',C_Calendar.EventInvite('aLiCe'))"));
        Assert.Equal("Alice", calendar.LastEventInviteRequestName);
        Assert.Equal(1, calendar.EventInviteRequestCount);
        Assert.True(calendar.IsActionPending);
        Assert.Single(calendar.EventInvites);

        calendar.IsActionPending = false;
        session.Lua.Evaluate("C_Calendar.EventInvite(123); return ''");
        Assert.Equal("123", calendar.LastEventInviteRequestName);
        calendar.IsActionPending = false;
        session.Lua.Evaluate("C_Calendar.EventInvite('éLÈNE'); return ''");
        Assert.Equal("Élène", calendar.LastEventInviteRequestName);

        calendar.IsActionPending = false;
        session.Lua.Evaluate(
            "C_Calendar.EventInvite('a'..string.rep('B',60)); return ''");
        Assert.Equal(
            "A" + new string('b', 47),
            calendar.LastEventInviteRequestName);
        Assert.Equal(48, Encoding.UTF8.GetByteCount(
            calendar.LastEventInviteRequestName!));
        Assert.Single(calendar.EventInvites);

        Assert.Equal(
            "false:false",
            session.Lua.Evaluate(
                "return table.concat({tostring(pcall(" +
                "C_Calendar.EventInvite)),tostring(pcall(" +
                "C_Calendar.EventInvite,true))},':')"));
    }

    [Fact]
    public void EventInvitePreservesGateOrderAndNativeErrors()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        calendar.BypassActionThrottles = true;
        session.Tick(3.0);

        session.Lua.Evaluate("C_Calendar.EventInvite('PLAYER'); return ''");
        Assert.Equal(
            "CALENDAR_ERROR_ALREADY_INVITED_TO_EVENT_S",
            calendar.LastEventInviteError);
        Assert.False(calendar.IsActionPending);
        Assert.Equal(0, calendar.EventInviteRequestCount);

        calendar.OpenEventInvitesDisabled = true;
        session.Lua.Evaluate("C_Calendar.EventInvite('Bob'); return ''");
        Assert.Equal(
            "CALENDAR_ERROR_INVITES_DISABLED",
            calendar.LastEventInviteError);

        calendar.OpenEventInvitesDisabled = false;
        calendar.EventInvites.Clear();
        for (var index = 0; index < 100; index++)
            calendar.EventInvites.Add(new WowCalendarEventInviteState());
        session.Lua.Evaluate("C_Calendar.EventInvite('Bob'); return ''");
        Assert.Equal(
            "CALENDAR_ERROR_INVITES_EXCEEDED",
            calendar.LastEventInviteError);

        calendar.EventInvites.Clear();
        session.Lua.Evaluate("C_Calendar.EventInvite('Bob'); return ''");
        Assert.Equal("Bob", calendar.LastEventInviteRequestName);
        Assert.Equal(1, calendar.EventInviteRequestCount);
        Assert.True(calendar.IsActionPending);

        calendar.IsActionPending = false;
        calendar.BypassActionThrottles = false;
        session.Lua.Evaluate("C_Calendar.EventInvite('Charlie'); return ''");
        Assert.Equal(
            "CALENDAR_ERROR_INVITE_THROTTLED",
            calendar.LastEventInviteError);
        Assert.False(calendar.IsActionPending);
        Assert.Equal(1, calendar.EventInviteRequestCount);

        calendar.BypassActionThrottles = true;
        calendar.CanEditOpenEvent = false;
        session.Lua.Evaluate("C_Calendar.EventInvite('Delta'); return ''");
        Assert.Null(calendar.LastEventInviteError);
        Assert.Equal(1, calendar.EventInviteRequestCount);

        calendar.CanEditOpenEvent = true;
        calendar.IsActionPending = true;
        session.Lua.Evaluate("C_Calendar.EventInvite('Echo'); return ''");
        Assert.Null(calendar.LastEventInviteError);
        Assert.Equal(1, calendar.EventInviteRequestCount);

        calendar.IsActionPending = false;
        session.Lua.Evaluate("C_Calendar.EventInvite(''); return ''");
        Assert.Null(calendar.LastEventInviteError);
        Assert.Equal(1, calendar.EventInviteRequestCount);
    }

    [Fact]
    public void EventRemoveInviteMutatesLocalDraftAndPreservesSelectionIdentity()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate(
            "C_Calendar.CreatePlayerEvent(); removeUpdates=0;" +
            "local listener=CreateFrame('Frame');" +
            "listener:RegisterEvent('CALENDAR_UPDATE_EVENT');" +
            "listener:SetScript('OnEvent',function() " +
            "removeUpdates=removeUpdates+1 end); return ''");
        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                InviteId = 2,
                Name = "Second",
                Guid = "Player-0-00000002"
            });
        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                InviteId = 3,
                Name = "Third",
                Guid = "Player-0-00000003"
            });
        calendar.InviteCount = 3;
        calendar.SelectedInviteIndex = 3;
        calendar.SelectedInviteId = 3;

        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "return select('#',C_Calendar.EventRemoveInvite(2))"));
        Assert.Equal(2, calendar.EventInvites.Count);
        Assert.Equal("Third", calendar.EventInvites[1].Name);
        Assert.Equal(2, calendar.SelectedInviteIndex);
        Assert.Equal("1", session.Lua.Evaluate("return removeUpdates"));

        session.Lua.Evaluate("C_Calendar.EventRemoveInvite(1); return ''");
        Assert.Equal(
            "CALENDAR_ERROR_DELETE_CREATOR_FAILED",
            calendar.LastEventInviteError);
        Assert.Equal(2, calendar.EventInvites.Count);
        Assert.Equal("1", session.Lua.Evaluate("return removeUpdates"));

        calendar.CanEditOpenEvent = false;
        calendar.EventInvites[1].InviteIsMine = true;
        calendar.SelectedInviteIndex = 2;
        calendar.SelectedInviteId = 3;
        session.Lua.Evaluate("C_Calendar.EventRemoveInvite(2); return ''");
        Assert.Single(calendar.EventInvites);
        Assert.Equal(0, calendar.SelectedInviteIndex);
        Assert.Equal("2", session.Lua.Evaluate("return removeUpdates"));

        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                Name = "Not Mine",
                ModeratorStatus = 0,
                InviteIsMine = false
            });
        session.Lua.Evaluate("C_Calendar.EventRemoveInvite(2); return ''");
        Assert.Equal(
            "CALENDAR_ERROR_PERMISSIONS",
            calendar.LastEventInviteError);
        Assert.Equal(2, calendar.EventInvites.Count);
        Assert.Equal(
            "true",
            session.Lua.Evaluate(
                "return tostring(pcall(" +
                "C_Calendar.EventRemoveInvite,0))"));
    }

    [Fact]
    public void EventRemoveInviteDefersServerMutationAndSetsPending()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        calendar.DraftEvent = null;
        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                InviteId = 99,
                Name = "Target",
                Guid = "Player-0-00000099"
            });
        calendar.InviteCount = 2;

        session.Lua.Evaluate("C_Calendar.EventRemoveInvite(2); return ''");
        Assert.Equal(
            new WowCalendarInviteRemovalRequestState(
                2,
                99,
                "Player-0-00000099"),
            calendar.LastEventInviteRemovalRequest);
        Assert.True(calendar.IsActionPending);
        Assert.Equal(2, calendar.EventInvites.Count);

        calendar.IsActionPending = false;
        calendar.CanEditOpenEvent = false;
        calendar.EventInvites[1].InviteIsMine = true;
        session.Lua.Evaluate("C_Calendar.EventRemoveInvite(2); return ''");
        Assert.NotNull(calendar.LastEventInviteRemovalRequest);
        Assert.True(calendar.IsActionPending);
        Assert.Equal(2, calendar.EventInvites.Count);

        calendar.IsActionPending = false;
        session.Lua.Evaluate("C_Calendar.EventRemoveInvite(3); return ''");
        Assert.Null(calendar.LastEventInviteRemovalRequest);
        Assert.Null(calendar.LastEventInviteError);
        Assert.False(calendar.IsActionPending);
    }

    [Fact]
    public void EventRemoveInviteByGuidUsesParsedPlayerGuidIdentityForLocalDrafts()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate(
            "C_Calendar.CreatePlayerEvent(); guidRemoveUpdates=0;" +
            "local listener=CreateFrame('Frame');" +
            "listener:RegisterEvent('CALENDAR_UPDATE_EVENT');" +
            "listener:SetScript('OnEvent',function() " +
            "guidRemoveUpdates=guidRemoveUpdates+1 end); return ''");
        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                InviteId = 42,
                Name = "Target",
                Guid = "Player-7-2-42-00000000000000AB"
            });
        calendar.InviteCount = 2;
        calendar.SelectedInviteIndex = 2;
        calendar.SelectedInviteId = 42;

        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "return select('#'," +
                "C_Calendar.EventRemoveInviteByGuid(" +
                "'player-0007-2-000042-0xAB'))"));
        Assert.Single(calendar.EventInvites);
        Assert.Equal(1, calendar.InviteCount);
        Assert.Equal(0, calendar.SelectedInviteIndex);
        Assert.Equal(
            "1",
            session.Lua.Evaluate("return guidRemoveUpdates"));

        Assert.Equal(
            "true:0",
            session.Lua.Evaluate(
                "local ok=pcall(" +
                "C_Calendar.EventRemoveInviteByGuid,7);" +
                "return tostring(ok)..':'..select('#'," +
                "C_Calendar.EventRemoveInviteByGuid('not-a-guid'))"));
        Assert.Equal(
            "false:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_Calendar.EventRemoveInviteByGuid))," +
                "tostring(pcall(" +
                "C_Calendar.EventRemoveInviteByGuid,false))," +
                "tostring(pcall(" +
                "C_Calendar.EventRemoveInviteByGuid,{}))},':')"));
    }

    [Fact]
    public void EventRemoveInviteByGuidDefersTheMatchedServerInvite()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        calendar.DraftEvent = null;
        calendar.CanEditOpenEvent = false;
        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                InviteId = 99,
                Name = "Target",
                Guid = "Player-0-0000000000000099",
                InviteIsMine = true
            });
        calendar.InviteCount = 2;

        session.Lua.Evaluate(
            "C_Calendar.EventRemoveInviteByGuid('Player-0000-99');" +
            "return ''");
        Assert.Equal(
            new WowCalendarInviteRemovalRequestState(
                2,
                99,
                "Player-0-0000000000000099"),
            calendar.LastEventInviteRemovalRequest);
        Assert.True(calendar.IsActionPending);
        Assert.Equal(2, calendar.EventInvites.Count);

        calendar.IsActionPending = false;
        session.Lua.Evaluate(
            "C_Calendar.EventRemoveInviteByGuid('Player-0-98');" +
            "return ''");
        Assert.Null(calendar.LastEventInviteRemovalRequest);
        Assert.Null(calendar.LastEventInviteError);
        Assert.False(calendar.IsActionPending);
        Assert.Equal(2, calendar.EventInvites.Count);
    }

    [Fact]
    public void EventSelectInviteStoresIdentityAndIgnoresPendingState()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        session.Lua.Evaluate("C_Calendar.CreatePlayerEvent(); return ''");
        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                InviteId = 10,
                Name = "Second",
                Guid = "Player-0-10"
            });
        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                InviteId = 20,
                Name = "Third",
                Guid = "Player-0-20"
            });
        calendar.InviteCount = 3;
        calendar.IsActionPending = true;

        Assert.Equal(
            "0:3",
            session.Lua.Evaluate(
                "local count=select('#'," +
                "C_Calendar.EventSelectInvite('3.9','ignored'));" +
                "return count..':'.." +
                "C_Calendar.EventGetSelectedInvite()"));
        Assert.Equal(20UL, calendar.SelectedInviteId);
        Assert.Equal(3, calendar.SelectedInviteIndex);
        Assert.True(calendar.IsActionPending);

        (calendar.EventInvites[1], calendar.EventInvites[2]) =
            (calendar.EventInvites[2], calendar.EventInvites[1]);
        Assert.Equal(
            "2",
            session.Lua.Evaluate(
                "C_Calendar.EventSelectInvite(9);" +
                "return C_Calendar.EventGetSelectedInvite()"));
        Assert.Equal(20UL, calendar.SelectedInviteId);

        Assert.Equal(
            "false:true:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_Calendar.EventSelectInvite))," +
                "tostring(pcall(C_Calendar.EventSelectInvite,0))," +
                "tostring(pcall(" +
                "C_Calendar.EventSelectInvite,false))},':')"));
    }

    [Fact]
    public void GetClubCalendarEventsUsesNativeBucketsLoadedWindowAndDayCap()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        calendar.Month = 1;
        calendar.Year = 2026;
        calendar.DayEvents[(0, 10, 1)] = ClubEvent(
            101,
            7,
            new DateTime(2026, 1, 10, 9, 29, 0));
        calendar.DayEvents[(0, 10, 2)] = ClubEvent(
            102,
            7,
            new DateTime(2026, 1, 10, 9, 30, 0));
        calendar.DayEvents[(0, 10, 3)] = ClubEvent(
            103,
            8,
            new DateTime(2026, 1, 10, 12, 0, 0));
        calendar.DayEvents[(0, 11, 2)] = ClubEvent(
            105,
            7,
            new DateTime(2026, 1, 11, 8, 0, 0));
        calendar.DayEvents[(0, 11, 1)] = ClubEvent(
            104,
            7,
            new DateTime(2026, 1, 11, 20, 0, 0));
        calendar.DayEvents[(0, 12, 1)] = ClubEvent(
            106,
            7,
            new DateTime(2026, 1, 12, 17, 45, 0));
        calendar.DayEvents[(0, 12, 2)] = ClubEvent(
            107,
            7,
            new DateTime(2026, 1, 12, 17, 46, 0));

        Assert.Equal(
            "function:1:table:4:102:104:105:106:Event 102:7",
            session.Lua.Evaluate(
                "local s={monthDay='10.9',month=1,weekday=7," +
                "year=2026,hour=9,minute=30};" +
                "local e={monthDay=12,month=1,weekday=2," +
                "year=2026,hour=17,minute=45};" +
                "local count=select('#'," +
                "C_Calendar.GetClubCalendarEvents(7,s,e,'ignored'));" +
                "local events=C_Calendar.GetClubCalendarEvents(7,s,e);" +
                "return table.concat({type(C_Calendar.GetClubCalendarEvents)," +
                "count,type(events),#events,events[1].eventID," +
                "events[2].eventID,events[3].eventID,events[4].eventID," +
                "events[1].title,events[1].clubID},':')"));

        const ulong largeClubId = 0x0020000000000000;
        calendar.DayEvents[(1, 28, 1)] = ClubEvent(
            200,
            largeClubId,
            new DateTime(2026, 2, 28, 12, 0, 0));
        calendar.DayEvents[(2, 1, 1)] = ClubEvent(
            201,
            largeClubId,
            new DateTime(2026, 3, 1, 12, 0, 0));
        Assert.Equal(
            "1:200:0",
            session.Lua.Evaluate(
                "local s={monthDay=1,month=1,weekday=5," +
                "year=2026,hour=0,minute=0};" +
                "local e={monthDay=30,month=4,weekday=5," +
                "year=2026,hour=23,minute=59};" +
                "local events=C_Calendar.GetClubCalendarEvents(" +
                "'0x0020000000000000',s,e);" +
                "local reversed=C_Calendar.GetClubCalendarEvents(" +
                "'0x0020000000000000',e,s);" +
                "return #events..':'..events[1].eventID..':'..#reversed"));

        Assert.Equal(
            "false:false:false:false:false:false",
            session.Lua.Evaluate(
                "local valid={monthDay=1,month=1,weekday=5," +
                "year=2026,hour=0,minute=0};" +
                "local noWeekday={monthDay=1,month=1," +
                "year=2026,hour=0,minute=0};" +
                "return table.concat({" +
                "tostring(pcall(C_Calendar.GetClubCalendarEvents))," +
                "tostring(pcall(C_Calendar.GetClubCalendarEvents,nil," +
                "valid,valid))," +
                "tostring(pcall(C_Calendar.GetClubCalendarEvents,7,1,valid))," +
                "tostring(pcall(C_Calendar.GetClubCalendarEvents,7," +
                "valid,1))," +
                "tostring(pcall(C_Calendar.GetClubCalendarEvents,7," +
                "noWeekday,valid))," +
                "tostring(pcall(C_Calendar.GetClubCalendarEvents,7," +
                "valid,{monthDay=1,month=1,weekday=5,year=2026," +
                "hour=0}))},':')"));
    }

    [Fact]
    public void GetEventIndexInfoScansNativeBucketsAndPreservesOptionalFilterQuirk()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        const ulong targetEventId = 0x0020000000000000;
        calendar.DayEvents[(0, 4, 1)] = ClubEvent(
            targetEventId,
            1,
            new DateTime(2026, 1, 4, 12, 0, 0));
        calendar.DayEvents[(-1, 31, 3)] = ClubEvent(
            targetEventId,
            1,
            new DateTime(2025, 12, 31, 12, 0, 0));
        calendar.DayEvents[(-1, 31, 1)] = ClubEvent(
            99,
            1,
            new DateTime(2025, 12, 31, 10, 0, 0));
        calendar.DayEvents[(1, 1, 1)] = ClubEvent(
            targetEventId,
            1,
            new DateTime(2026, 2, 1, 12, 0, 0));

        Assert.Equal(
            "function:1:table:-1:31:3:-1:31:3",
            session.Lua.Evaluate(
                "local count=select('#',C_Calendar.GetEventIndexInfo(" +
                "'0x0020000000000000',nil,nil,'ignored-surplus'));" +
                "local info=C_Calendar.GetEventIndexInfo(" +
                "'0x0020000000000000');" +
                "local nils=C_Calendar.GetEventIndexInfo(" +
                "'0x0020000000000000',nil,nil,'ignored');" +
                "return table.concat({" +
                "type(C_Calendar.GetEventIndexInfo),count,type(info)," +
                "info.offsetMonths,info.monthDay,info.eventIndex," +
                "nils.offsetMonths,nils.monthDay,nils.eventIndex},':')"));

        Assert.Equal(
            "1:nil:1:nil:1:nil:1:nil",
            session.Lua.Evaluate(
                "local function shape(...) " +
                "return select('#',...),tostring((...)) end;" +
                "local a,b=shape(C_Calendar.GetEventIndexInfo(123));" +
                "local c,d=shape(C_Calendar.GetEventIndexInfo(" +
                "'0x0020000000000000',-1));" +
                "local e,f=shape(C_Calendar.GetEventIndexInfo(" +
                "'0x0020000000000000',nil,31));" +
                "local g,h=shape(C_Calendar.GetEventIndexInfo(" +
                "'0x0020000000000000','-1.9'));" +
                "return table.concat({a,b,c,d,e,f,g,h},':')"));

        Assert.Equal(
            "false:false:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(pcall(C_Calendar.GetEventIndexInfo))," +
                "tostring(pcall(C_Calendar.GetEventIndexInfo,false))," +
                "tostring(pcall(C_Calendar.GetEventIndexInfo,1,false))," +
                "tostring(pcall(C_Calendar.GetEventIndexInfo,1,nil,false))" +
                "},':')"));
    }

    [Fact]
    public void GetEventInfoPushesTheExactOpenEventProjectionAndFallbacks()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;

        Assert.Equal(
            "function:0",
            session.Lua.Evaluate(
                "return type(C_Calendar.GetEventInfo)..':'.." +
                "select('#',C_Calendar.GetEventInfo('ignored'))"));

        calendar.IsEventOpen = true;
        calendar.OpenEventId = 9001;
        calendar.OpenEventTitle = "Native Title";
        calendar.OpenEventDescription = "Native Description";
        calendar.OpenEventCreatorName = "Creator-Realm";
        calendar.OpenEventType = 5;
        calendar.OpenEventRepeatOption = 4;
        calendar.OpenEventMaximumSize = 37;
        calendar.OpenEventTextureId = 8002;
        calendar.OpenEventDate = new WowCalendarEventDateState(7, 14, 26, 6);
        calendar.OpenEventTime = new WowCalendarEventTimeState(9, 45);
        calendar.OpenEventLockoutTime =
            new WowCalendarTimeValueState(20, 8, 4, 2026, 18, 30);
        calendar.IsEventLocked = true;
        calendar.IsAutoApproveEnabled = true;
        calendar.PendingEventInviteIds.Add(9001);
        calendar.OpenEventCalendarType = "COMMUNITY_EVENT";
        calendar.OpenEventClubId = 77;
        session.Lua.Clubs.ClubInfoById[77] =
            new WowClubInfoState { ClubId = 77, Name = "Test Club" };
        calendar.EventTexturesByType[1] =
            new List<WowCalendarEventTextureState>
            {
                new("First", 101, 0, null, null, null, 8001),
                new("Second", 102, 0, null, null, null, 8002)
            };
        calendar.EventInvites.Add(
            new WowCalendarEventInviteState
            {
                Guid = "Player-0-00000001",
                InviteStatus = 6,
                Type = 4
            });

        Assert.Equal(
            "1:table:Native Title:Native Description:Creator-Realm:" +
            "1:4:37:2:15:8:7:2026:9:45:20:8:4:2026:18:30:" +
            "true:true:true:6:4:COMMUNITY_EVENT:Test Club",
            session.Lua.Evaluate(
                "local count=select('#',C_Calendar.GetEventInfo(" +
                "'ignored')); local info=C_Calendar.GetEventInfo();" +
                "return table.concat({count,type(info),info.title," +
                "info.description,info.creator,info.eventType," +
                "info.repeatOption,info.maxSize,info.textureIndex," +
                "info.time.monthDay,info.time.month,info.time.weekday," +
                "info.time.year,info.time.hour,info.time.minute," +
                "info.lockoutTime.monthDay,info.lockoutTime.month," +
                "info.lockoutTime.weekday,info.lockoutTime.year," +
                "info.lockoutTime.hour,info.lockoutTime.minute," +
                "tostring(info.isLocked),tostring(info.isAutoApprove)," +
                "tostring(info.hasPendingInvite),info.inviteStatus," +
                "info.inviteType,info.calendarType,info.communityName},':')"));

        calendar.EventInvites.Clear();
        calendar.OpenEventUsesSignUpStatusRules = true;
        Assert.Equal(
            "7:1",
            session.Lua.Evaluate(
                "local info=C_Calendar.GetEventInfo();" +
                "return info.inviteStatus..':'..info.inviteType"));

        calendar.OpenEventUsesSignUpStatusRules = false;
        calendar.OpenEventType = 2;
        calendar.OpenEventTextureId = 9002;
        calendar.OpenEventCreatorName = null;
        calendar.OpenEventClubId = 999;
        calendar.EventTexturesByType[2] =
            new List<WowCalendarEventTextureState>
            {
                new("Ignored", 103, 0, null, null, null, 9002)
            };
        Assert.Equal(
            "2:nil:nil:nil:nil",
            session.Lua.Evaluate(
                "local info=C_Calendar.GetEventInfo();" +
                "return table.concat({info.eventType," +
                "tostring(info.textureIndex),tostring(info.creator)," +
                "tostring(info.inviteStatus)," +
                "tostring(info.communityName)},':')"));

        session.Lua.Evaluate(
            "C_Calendar.CloseEvent();" +
            "C_Calendar.CreateGuildAnnouncementEvent();return ''");
        Assert.Equal(
            ":Player:0:0:100:nil:0:0:0:1999:-1:-1:" +
            "1:1:1:2000:0:0:false:false:false:nil:nil:" +
            "GUILD_ANNOUNCEMENT:nil",
            session.Lua.Evaluate(
                "local info=C_Calendar.GetEventInfo();" +
                "return table.concat({info.title,info.creator," +
                "info.eventType,info.repeatOption,info.maxSize," +
                "tostring(info.textureIndex),info.time.monthDay," +
                "info.time.month,info.time.weekday,info.time.year," +
                "info.time.hour,info.time.minute," +
                "info.lockoutTime.monthDay,info.lockoutTime.month," +
                "info.lockoutTime.weekday,info.lockoutTime.year," +
                "info.lockoutTime.hour,info.lockoutTime.minute," +
                "tostring(info.isLocked),tostring(info.isAutoApprove)," +
                "tostring(info.hasPendingInvite)," +
                "tostring(info.inviteStatus),tostring(info.inviteType)," +
                "info.calendarType,tostring(info.communityName)},':')"));

        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "C_Calendar.CloseEvent();" +
                "return select('#',C_Calendar.GetEventInfo())"));
    }

    [Fact]
    public void GetGuildEventInfoUsesNativeVectorOrderAndExactProjection()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;

        Assert.Equal(
            "function:0:0:0:true:false:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(C_Calendar.GetGuildEventInfo)," +
                "select('#',C_Calendar.GetGuildEventInfo(1))," +
                "select('#',C_Calendar.GetGuildEventInfo(0))," +
                "select('#',C_Calendar.GetGuildEventInfo(99))," +
                "tostring(pcall(C_Calendar.GetGuildEventInfo,0))," +
                "tostring(pcall(C_Calendar.GetGuildEventInfo))," +
                "tostring(pcall(C_Calendar.GetGuildEventInfo,false))," +
                "tostring(pcall(C_Calendar.GetGuildEventInfo,-1))" +
                "},':')"));

        calendar.GuildEvents.Add(
            new WowCalendarGuildEventInfoState(
                11,
                new WowCalendarTimeValueState(2, 1, 6, 2026, 7, 8),
                2,
                "First",
                "GUILD_EVENT",
                0,
                3,
                22));
        calendar.GuildEvents.Add(
            new WowCalendarGuildEventInfoState(
                0xFEDCBA9876543210UL,
                new WowCalendarTimeValueState(31, 12, 4, 2027, 23, 59),
                5,
                "Second",
                "GUILD_ANNOUNCEMENT",
                987654,
                8,
                0xF123456789ABCDEFUL));

        Assert.Equal(
            "1:table:13:0xFEDCBA9876543210:2027:12:31:4:23:59:" +
            "1:Second:GUILD_ANNOUNCEMENT:987654:8:0xF123456789ABCDEF",
            session.Lua.Evaluate(
                "local count=select('#',C_Calendar.GetGuildEventInfo(" +
                "'2.9','ignored'));" +
                "local info=C_Calendar.GetGuildEventInfo('2.9');" +
                "local fields=0; for _ in pairs(info) do fields=fields+1 end;" +
                "return table.concat({count,type(info),fields,info.eventID," +
                "info.year,info.month,info.monthDay,info.weekday,info.hour," +
                "info.minute,info.eventType,info.title,info.calendarType," +
                "info.texture,info.inviteStatus,info.clubID},':')"));

        Assert.Equal(
            "11:First:nil:22:0",
            session.Lua.Evaluate(
                "local info=C_Calendar.GetGuildEventInfo(1);" +
                "return table.concat({info.eventID,info.title," +
                "tostring(info.texture),info.clubID," +
                "select('#',C_Calendar.GetGuildEventInfo(3))},':')"));
    }

    [Fact]
    public void GetGuildEventSelectionInfoUsesNativeLookupIdentityAndBuckets()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        calendar.Month = 8;
        calendar.Year = 2026;

        Assert.Equal(
            "function:0:0:true:false:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(C_Calendar.GetGuildEventSelectionInfo)," +
                "select('#',C_Calendar.GetGuildEventSelectionInfo(1))," +
                "select('#',C_Calendar.GetGuildEventSelectionInfo(0))," +
                "tostring(pcall(C_Calendar.GetGuildEventSelectionInfo,0))," +
                "tostring(pcall(C_Calendar.GetGuildEventSelectionInfo))," +
                "tostring(pcall(C_Calendar.GetGuildEventSelectionInfo,false))," +
                "tostring(pcall(C_Calendar.GetGuildEventSelectionInfo,-1))" +
                "},':')"));

        calendar.GuildEvents.Add(
            new WowCalendarGuildEventInfoState(
                42,
                new WowCalendarTimeValueState(15, 8, 7, 2026, 20, 0),
                1,
                "Event identity",
                "GUILD_EVENT",
                0,
                3,
                7));
        calendar.GuildEvents.Add(
            new WowCalendarGuildEventInfoState(
                777,
                new WowCalendarTimeValueState(3, 9, 5, 2026, 20, 0),
                1,
                "Map and difficulty identity",
                "GUILD_EVENT",
                0,
                3,
                7,
                DifficultyId: 23,
                MapId: 530,
                EventFlags: 0x80));
        calendar.GuildEvents.Add(
            new WowCalendarGuildEventInfoState(
                777,
                new WowCalendarTimeValueState(3, 9, 5, 2026, 20, 0),
                1,
                "Map-only identity",
                "GUILD_EVENT",
                0,
                3,
                7,
                DifficultyId: 99,
                MapId: 530));
        calendar.GuildEvents.Add(
            new WowCalendarGuildEventInfoState(
                42,
                new WowCalendarTimeValueState(1, 10, 5, 2026, 20, 0),
                1,
                "Outside loaded window",
                "GUILD_EVENT",
                0,
                3,
                7));

        calendar.DayEvents[(0, 15, 1)] =
            ClubEvent(99, 7, new DateTime(2026, 8, 15, 19, 0, 0));
        calendar.DayEvents[(0, 15, 2)] =
            ClubEvent(42, 7, new DateTime(2026, 8, 15, 20, 0, 0));
        calendar.DayEvents[(1, 3, 1)] =
            ClubEvent(777, 7, new DateTime(2026, 9, 3, 18, 0, 0)) with
            {
                MapId = 999,
                EventFlags = 0x80
            };
        calendar.DayEvents[(1, 3, 2)] =
            ClubEvent(999, 7, new DateTime(2026, 9, 3, 19, 0, 0)) with
            {
                Difficulty = 22,
                MapId = 530,
                EventFlags = 0x80
            };
        calendar.DayEvents[(1, 3, 3)] =
            ClubEvent(888, 7, new DateTime(2026, 9, 3, 20, 0, 0)) with
            {
                Difficulty = 23,
                MapId = 530,
                EventFlags = 0x80
            };

        Assert.Equal(
            "0:15:2:1:3:3:2:0",
            session.Lua.Evaluate(
                "local a=C_Calendar.GetGuildEventSelectionInfo(1);" +
                "local b=C_Calendar.GetGuildEventSelectionInfo('2.9','ignored');" +
                "local c=C_Calendar.GetGuildEventSelectionInfo(3);" +
                "return table.concat({a.offsetMonths,a.monthDay,a.eventIndex," +
                "b.offsetMonths,b.monthDay,b.eventIndex,c.eventIndex," +
                "select('#',C_Calendar.GetGuildEventSelectionInfo(4))},':')"));

        calendar.GuildEvents[1] = calendar.GuildEvents[1] with
        {
            EventId = 42,
            MapId = 998
        };
        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "return select('#'," +
                "C_Calendar.GetGuildEventSelectionInfo(2))"));
    }

    [Fact]
    public void GetNumGuildEventsReturnsNativeVectorCountAndIgnoresArguments()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;

        Assert.Equal(
            "function:1:0:0",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(C_Calendar.GetNumGuildEvents)," +
                "select('#',C_Calendar.GetNumGuildEvents())," +
                "C_Calendar.GetNumGuildEvents()," +
                "C_Calendar.GetNumGuildEvents(false,'ignored')" +
                "},':')"));

        calendar.GuildEvents.Add(
            new WowCalendarGuildEventInfoState(
                11,
                new WowCalendarTimeValueState(2, 1, 6, 2026, 7, 8),
                2,
                "First",
                "GUILD_EVENT",
                0,
                3,
                22));
        calendar.GuildEvents.Add(
            new WowCalendarGuildEventInfoState(
                12,
                new WowCalendarTimeValueState(3, 1, 7, 2026, 8, 9),
                1,
                "Second",
                "GUILD_EVENT",
                0,
                3,
                22));

        Assert.Equal(
            "1:2:2",
            session.Lua.Evaluate(
                "return table.concat({" +
                "select('#',C_Calendar.GetNumGuildEvents(123))," +
                "C_Calendar.GetNumGuildEvents()," +
                "C_Calendar.GetNumGuildEvents(nil,false,{})" +
                "},':')"));
    }

    [Fact]
    public void IsEventOpenReportsOpenEventPresenceAndIgnoresArguments()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;

        Assert.Equal(
            "function:1:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(C_Calendar.IsEventOpen)," +
                "select('#',C_Calendar.IsEventOpen())," +
                "tostring(C_Calendar.IsEventOpen())," +
                "tostring(C_Calendar.IsEventOpen(false,'ignored',{}))" +
                "},':')"));

        calendar.IsEventOpen = true;
        Assert.Equal(
            "true:true:1",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring(C_Calendar.IsEventOpen())," +
                "tostring(C_Calendar.IsEventOpen(nil,false))," +
                "select('#',C_Calendar.IsEventOpen(123))" +
                "},':')"));

        session.Lua.Evaluate("C_Calendar.CloseEvent(); return ''");
        Assert.Equal(
            "false",
            session.Lua.Evaluate("return tostring(C_Calendar.IsEventOpen())"));
    }

    [Fact]
    public void MassInviteCommunityMatchesNativeParsingPacketAndEventGates()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;

        Assert.Equal(
            "function:false:false:false:false:0",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(C_Calendar.MassInviteCommunity)," +
                "tostring(pcall(C_Calendar.MassInviteCommunity))," +
                "tostring(pcall(C_Calendar.MassInviteCommunity,false,1,1))," +
                "tostring(pcall(C_Calendar.MassInviteCommunity,1,-0.1,1))," +
                "tostring(pcall(C_Calendar.MassInviteCommunity,1,1,256))," +
                "select('#',C_Calendar.MassInviteCommunity(1,1,1))" +
                "},':')"));
        Assert.Equal(0, calendar.MassInviteRequestCount);
        Assert.Null(calendar.LastMassInviteRequest);

        calendar.IsEventOpen = true;
        calendar.IsOpenEventLocal = true;
        calendar.OpenEventFlags = WowCalendarEventFlags.Player;
        calendar.IsBackendAvailable = false;
        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "return select('#',C_Calendar.MassInviteCommunity(" +
                "'0xFEDCBA9876543210','12.9','70.8',nil,'ignored'))"));
        Assert.Equal(
            new WowCalendarMassInviteRequestState(
                0xFEDCBA9876543210,
                12,
                70,
                0),
            calendar.LastMassInviteRequest);
        Assert.Equal(1, calendar.MassInviteRequestCount);
        Assert.True(calendar.IsActionPending);

        calendar.IsActionPending = false;
        session.Lua.Evaluate(
            "C_Calendar.MassInviteCommunity(22,2,90,0); return ''");
        Assert.Equal(255, calendar.LastMassInviteRequest!
            .MaximumRankOrderZeroBased);
        Assert.Equal(2, calendar.MassInviteRequestCount);

        calendar.IsActionPending = false;
        session.Lua.Evaluate(
            "C_Calendar.MassInviteCommunity(23,3,91,257); return ''");
        Assert.Equal(0, calendar.LastMassInviteRequest!
            .MaximumRankOrderZeroBased);
        Assert.Equal(3, calendar.MassInviteRequestCount);

        calendar.IsActionPending = true;
        session.Lua.Evaluate(
            "C_Calendar.MassInviteCommunity(24,4,92,1); return ''");
        Assert.Equal(3, calendar.MassInviteRequestCount);

        calendar.IsActionPending = false;
        calendar.IsOpenEventLocal = false;
        session.Lua.Evaluate(
            "C_Calendar.MassInviteCommunity(25,5,93,1); return ''");
        Assert.Equal(3, calendar.MassInviteRequestCount);

        calendar.IsOpenEventLocal = true;
        calendar.OpenEventFlags = WowCalendarEventFlags.CommunityEvent;
        session.Lua.Evaluate(
            "C_Calendar.MassInviteCommunity(26,6,94,1); return ''");
        Assert.Equal(3, calendar.MassInviteRequestCount);

        calendar.OpenEventFlags =
            WowCalendarEventFlags.Player |
            WowCalendarEventFlags.Locked |
            WowCalendarEventFlags.AutoApprove;
        session.Lua.Evaluate(
            "C_Calendar.MassInviteCommunity(27,7,95,1); return ''");
        Assert.Equal(4, calendar.MassInviteRequestCount);
        Assert.True(calendar.IsActionPending);
    }

    [Fact]
    public void MassInviteGuildParsesBeforeMembershipAndUsesGuildPacketShape()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        calendar.IsPlayerInGuild = false;
        calendar.IsActionPending = true;

        Assert.Equal(
            "function:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(C_Calendar.MassInviteGuild)," +
                "tostring(pcall(C_Calendar.MassInviteGuild,1,2))," +
                "tostring(pcall(C_Calendar.MassInviteGuild,1,256,3))" +
                "},':')"));
        Assert.True(calendar.IsActionPending);
        Assert.Null(calendar.LastError);

        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "return select('#',C_Calendar.MassInviteGuild(" +
                "1,90,3,'ignored'))"));
        Assert.Equal("ERR_GUILD_PLAYER_NOT_IN_GUILD", calendar.LastError);
        Assert.False(calendar.IsActionPending);
        Assert.Equal(0, calendar.MassInviteRequestCount);

        calendar.IsPlayerInGuild = true;
        calendar.IsEventOpen = true;
        calendar.IsOpenEventLocal = true;
        calendar.OpenEventFlags = WowCalendarEventFlags.Player;
        session.Lua.Evaluate(
            "C_Calendar.MassInviteGuild('10.9','80.7','3.9','ignored');" +
            "return ''");
        Assert.Equal(
            new WowCalendarMassInviteRequestState(0, 10, 80, 2),
            calendar.LastMassInviteRequest);
        Assert.Equal(1, calendar.MassInviteRequestCount);
        Assert.True(calendar.IsActionPending);

        calendar.IsActionPending = false;
        session.Lua.Evaluate("C_Calendar.MassInviteGuild(1,2,0); return ''");
        Assert.Equal(255, calendar.LastMassInviteRequest!
            .MaximumRankOrderZeroBased);
        Assert.Equal(2, calendar.MassInviteRequestCount);

        calendar.IsActionPending = false;
        calendar.OpenEventFlags = WowCalendarEventFlags.GuildAnnouncement;
        session.Lua.Evaluate("C_Calendar.MassInviteGuild(1,2,1); return ''");
        Assert.Equal(2, calendar.MassInviteRequestCount);
        Assert.False(calendar.IsActionPending);
    }

    [Fact]
    public void OpenEventValidatesLoadedSelectionAndPreservesNativeTypeSplit()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;
        calendar.Month = 2;
        calendar.Year = 2026;
        calendar.DayEvents[(0, 10, 1)] =
            ClubEvent(100, 0, new DateTime(2026, 2, 10, 12, 0, 0)) with
            {
                CalendarType = "WRONG_PROVIDER_VALUE",
                EventFlags = 0x1
            };
        calendar.DayEvents[(0, 10, 2)] =
            ClubEvent(200, 0, new DateTime(2026, 2, 10, 13, 0, 0)) with
            {
                CalendarType = "WRONG_PROVIDER_VALUE",
                EventFlags = 0x4 | 0x80 | 0x200
            };
        calendar.DayEvents[(0, 30, 1)] =
            ClubEvent(300, 0, new DateTime(2026, 3, 2, 12, 0, 0)) with
            {
                EventFlags = 0x8
            };

        session.Lua.Evaluate(
            "calendarOpenCount=0; calendarOpenType='';" +
            "local frame=CreateFrame('Frame');" +
            "frame:RegisterEvent('CALENDAR_OPEN_EVENT');" +
            "frame:SetScript('OnEvent',function(_,_,calendarType)" +
            "calendarOpenCount=calendarOpenCount+1;" +
            "calendarOpenType=calendarType end); return ''");

        Assert.Equal(
            "function:false:false:false:false:false:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "type(C_Calendar.OpenEvent)," +
                "tostring(pcall(C_Calendar.OpenEvent))," +
                "tostring(pcall(C_Calendar.OpenEvent,false,1,1))," +
                "tostring(C_Calendar.OpenEvent(-2,10,1))," +
                "tostring(C_Calendar.OpenEvent(2,10,1))," +
                "tostring(C_Calendar.OpenEvent(0,30,1))," +
                "tostring(C_Calendar.OpenEvent(0,10,3))" +
                "},':')"));
        Assert.Equal(0, calendar.OpenEventRequestCount);

        calendar.IsBackendAvailable = false;
        Assert.Equal(
            "1:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "select('#',C_Calendar.OpenEvent(0,10,1,'ignored'))," +
                "tostring(C_Calendar.IsActionPending())},':')"));
        Assert.Equal(
            new WowCalendarOpenEventRequestState(100),
            calendar.LastOpenEventRequest);
        Assert.Equal(1, calendar.OpenEventRequestCount);
        Assert.True(calendar.IsActionPending);
        Assert.Equal("0:", session.Lua.Evaluate(
            "return calendarOpenCount..':'..calendarOpenType"));

        calendar.IsActionPending = false;
        Assert.Equal(
            "true:1:SYSTEM",
            session.Lua.Evaluate(
                "local success=C_Calendar.OpenEvent('0.9','10.9','2.9',{});" +
                "return table.concat({tostring(success),calendarOpenCount," +
                "calendarOpenType},':')"));
        Assert.Equal(
            new WowCalendarEventIndexState(0, 10, 2),
            calendar.EventIndex);
        Assert.False(calendar.IsActionPending);
        Assert.Equal(1, calendar.OpenEventRequestCount);

        calendar.IsActionPending = true;
        Assert.Equal(
            "false",
            session.Lua.Evaluate(
                "return tostring(C_Calendar.OpenEvent(0,10,2))"));
        Assert.Equal("1", session.Lua.Evaluate("return calendarOpenCount"));
    }

    [Fact]
    public void RemoveEventOnlyQueuesCurrentPlayersServerInviteRemoval()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;

        Assert.Equal(
            "function:0",
            session.Lua.Evaluate(
                "return type(C_Calendar.RemoveEvent)..':'.." +
                "select('#',C_Calendar.RemoveEvent('ignored'))"));
        Assert.Null(calendar.LastEventInviteRemovalRequest);

        calendar.IsEventOpen = true;
        calendar.IsOpenEventLocal = true;
        calendar.EventInvites.Add(new WowCalendarEventInviteState
        {
            InviteId = 10,
            Guid = "Player-0-00000002"
        });
        calendar.EventInvites.Add(new WowCalendarEventInviteState
        {
            InviteId = 20,
            Guid = "Player-0000-1",
            InviteIsMine = false,
            ModeratorStatus = 2
        });

        session.Lua.Evaluate("C_Calendar.RemoveEvent(); return ''");
        Assert.Null(calendar.LastEventInviteRemovalRequest);

        calendar.IsOpenEventLocal = false;
        calendar.IsActionPending = true;
        session.Lua.Evaluate("C_Calendar.RemoveEvent(); return ''");
        Assert.Null(calendar.LastEventInviteRemovalRequest);

        calendar.IsActionPending = false;
        calendar.CanEditOpenEvent = false;
        session.Lua.Evaluate("C_Calendar.RemoveEvent('ignored'); return ''");
        Assert.Equal(
            "CALENDAR_ERROR_DELETE_CREATOR_FAILED",
            calendar.LastEventInviteError);
        Assert.False(calendar.IsActionPending);
        Assert.Null(calendar.LastEventInviteRemovalRequest);

        calendar.EventInvites[1].ModeratorStatus = 0;
        calendar.IsBackendAvailable = false;
        session.Lua.Evaluate("C_Calendar.RemoveEvent({},false); return ''");
        Assert.Equal(
            new WowCalendarInviteRemovalRequestState(
                2,
                20,
                "Player-0000-1"),
            calendar.LastEventInviteRemovalRequest);
        Assert.True(calendar.IsActionPending);
        Assert.Equal(2, calendar.EventInvites.Count);
    }

    [Fact]
    public void UpdateEventMatchesNativePermissionDirtyDateAndPacketGates()
    {
        using var session = new EmulatorSession();
        var calendar = session.Lua.Calendar;

        Assert.Equal(
            "function:0",
            session.Lua.Evaluate(
                "return type(C_Calendar.UpdateEvent)..':'.." +
                "select('#',C_Calendar.UpdateEvent('ignored'))"));
        Assert.Null(calendar.LastUpdateEventRequest);

        calendar.IsEventOpen = true;
        calendar.IsOpenEventLocal = true;
        calendar.IsEventDirty = true;
        session.Lua.Evaluate("C_Calendar.UpdateEvent(); return ''");
        Assert.True(calendar.IsEventDirty);
        Assert.Null(calendar.LastUpdateEventRequest);

        calendar.IsOpenEventLocal = false;
        calendar.IsActionPending = true;
        session.Lua.Evaluate("C_Calendar.UpdateEvent(); return ''");
        Assert.True(calendar.IsEventDirty);
        Assert.Null(calendar.LastUpdateEventRequest);

        calendar.IsActionPending = false;
        calendar.OpenEventFlags = WowCalendarEventFlags.Player;
        calendar.CanEditOpenEvent = false;
        session.Lua.Evaluate("C_Calendar.UpdateEvent(); return ''");
        Assert.Equal("CALENDAR_ERROR_PERMISSIONS", calendar.LastError);
        Assert.True(calendar.IsEventDirty);
        Assert.False(calendar.IsActionPending);

        calendar.CanEditOpenEvent = true;
        calendar.IsEventDirty = false;
        session.Lua.Evaluate("C_Calendar.UpdateEvent(); return ''");
        Assert.Null(calendar.LastUpdateEventRequest);
        Assert.Equal(0, calendar.UpdateEventRequestCount);

        calendar.IsEventDirty = true;
        calendar.IsCurrentRealmDateValidForEvent = false;
        session.Lua.Evaluate("C_Calendar.UpdateEvent(); return ''");
        Assert.Equal("CALENDAR_ERROR_EVENT_PASSED", calendar.LastError);
        Assert.False(calendar.IsEventDirty);
        Assert.False(calendar.IsActionPending);
        Assert.Equal(0, calendar.UpdateEventRequestCount);

        calendar.IsCurrentRealmDateValidForEvent = true;
        calendar.IsEventDirty = true;
        calendar.IsBackendAvailable = false;
        calendar.OpenEventId = 0xFEDCBA9876543210UL;
        calendar.OpenEventClubId = 0x123456789ABCDEF0UL;
        calendar.OpenEventTitle = "Raid night";
        calendar.OpenEventDescription = "Bring flasks";
        calendar.OpenEventType = 5;
        calendar.OpenEventTextureId = 7002;
        calendar.OpenEventDate = new WowCalendarEventDateState(6, 30, 26, 5);
        calendar.OpenEventTime = new WowCalendarEventTimeState(20, 45);
        calendar.OpenEventFlags =
            WowCalendarEventFlags.Player |
            WowCalendarEventFlags.Locked;
        calendar.OpenEventMaximumSize = 25;

        session.Lua.Evaluate("C_Calendar.UpdateEvent({},false); return ''");
        Assert.Equal(
            new WowCalendarUpdateEventRequestState(
                0xFEDCBA9876543210UL,
                0x123456789ABCDEF0UL,
                "Raid night",
                "Bring flasks",
                1,
                7002,
                new WowCalendarEventDateState(6, 30, 26, 5),
                new WowCalendarEventTimeState(20, 45),
                WowCalendarEventFlags.Player |
                    WowCalendarEventFlags.Locked,
                25),
            calendar.LastUpdateEventRequest);
        Assert.Equal(1, calendar.UpdateEventRequestCount);
        Assert.False(calendar.IsEventDirty);
        Assert.True(calendar.IsActionPending);

        calendar.IsActionPending = false;
        calendar.IsEventDirty = true;
        calendar.OpenEventFlags = WowCalendarEventFlags.GuildAnnouncement;
        calendar.IsPlayerInGuild = true;
        calendar.CanEditGuildEvents = false;
        calendar.CanEditOpenEvent = true;
        session.Lua.Evaluate("C_Calendar.UpdateEvent(); return ''");
        Assert.Equal("CALENDAR_ERROR_PERMISSIONS", calendar.LastError);
        Assert.True(calendar.IsEventDirty);
        Assert.False(calendar.IsActionPending);
        Assert.Null(calendar.LastUpdateEventRequest);
        Assert.Equal(1, calendar.UpdateEventRequestCount);
    }

    private static WowCalendarDayEventState ClubEvent(
        ulong eventId,
        ulong clubId,
        DateTime startTime) =>
        new(
            EventId: eventId,
            Title: $"Event {eventId}",
            IsCustomTitle: false,
            StartTime: startTime,
            EndTime: startTime.AddHours(1),
            CalendarType: "COMMUNITY_EVENT",
            SequenceType: null,
            EventType: 4,
            IconTexture: null,
            ModeratorStatus: null,
            InviteStatus: 0,
            InvitedBy: string.Empty,
            Difficulty: 0,
            InviteType: 0,
            SequenceIndex: 0,
            NumberOfSequenceDays: 1,
            DifficultyName: null,
            DoNotDisplayBanner: false,
            DoNotDisplayEnd: false,
            ClubId: clubId,
            IsLocked: false);

    private static void TickMany(
        EmulatorSession session,
        int count,
        double deltaSeconds)
    {
        for (var index = 0; index < count; index++)
            session.Tick(deltaSeconds);
    }
}
