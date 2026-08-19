using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowEventSchedulerApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CanShowEvents",
        "ClearReminder",
        "GetEventUiMapID",
        "GetEventZoneName",
        "GetOngoingEvents",
        "GetScheduledEvents",
        "HasData",
        "HasSavedReminders",
        "RequestEvents",
        "SetReminder"
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
        lua_setglobal(state, "C_EventScheduler");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var runtime = LuaBindings.GetRuntime(state);
        var scheduler = runtime.EventScheduler;
        switch (operation)
        {
            case "CanShowEvents":
                lua_pushboolean(state, scheduler.CanShowEvents ? 1 : 0);
                return 1;
            case "HasData":
                lua_pushboolean(state, scheduler.HasData ? 1 : 0);
                return 1;
            case "HasSavedReminders":
                lua_pushboolean(state, scheduler.SavedReminders.Count != 0 ? 1 : 0);
                return 1;
            case "GetEventUiMapID":
                return GetEventUiMapId(state, scheduler);
            case "GetEventZoneName":
                return GetEventZoneName(state, scheduler);
            case "GetOngoingEvents":
                return PushOngoingEvents(state, scheduler.OngoingEvents);
            case "GetScheduledEvents":
                return PushScheduledEvents(state, scheduler);
            case "ClearReminder":
                return ClearReminder(state, runtime);
            case "RequestEvents":
                scheduler.TryRequestEvents();
                return 0;
            case "SetReminder":
                return SetReminder(state, runtime);
            default:
                return 0;
        }
    }

    private static int GetEventUiMapId(lua_State state, WowEventSchedulerState scheduler)
    {
        const string usage =
            "Usage: local uiMapID = C_EventScheduler.GetEventUiMapID(areaPoiID)";
        RequireArgumentCount(state, 1, usage);
        var areaPoiId = ReadInt32(state, 1, usage);
        if (scheduler.UiMapIdByAreaPoiId.TryGetValue(areaPoiId, out var uiMapId))
            lua_pushinteger(state, uiMapId);
        else
            lua_pushnil(state);
        return 1;
    }

    private static int GetEventZoneName(
        lua_State state,
        WowEventSchedulerState scheduler)
    {
        const string usage =
            "Usage: local name = C_EventScheduler.GetEventZoneName(areaPoiID)";
        RequireArgumentCount(state, 1, usage);
        var areaPoiId = ReadInt32(state, 1, usage);
        if (scheduler.ZoneNameByAreaPoiId.TryGetValue(areaPoiId, out var name))
            lua_pushstring(state, name);
        else
            lua_pushnil(state);
        return 1;
    }

    private static int ClearReminder(lua_State state, LuaRuntime runtime)
    {
        const string usage = "Usage: C_EventScheduler.ClearReminder(eventKey)";
        RequireArgumentCount(state, 1, usage);
        var eventKey = ReadString(state, 1, usage);
        if (runtime.EventScheduler.SavedReminders.Remove(eventKey))
            runtime.TriggerEvent("EVENT_SCHEDULER_UPDATE");
        return 0;
    }

    private static int SetReminder(lua_State state, LuaRuntime runtime)
    {
        const string usage = "Usage: C_EventScheduler.SetReminder(eventKey)";
        RequireArgumentCount(state, 1, usage);
        var eventKey = ReadString(state, 1, usage);
        var scheduledEvent = runtime.EventScheduler.ScheduledEvents.FirstOrDefault(
            item => item.EventKey.Equals(eventKey, StringComparison.Ordinal));
        if (scheduledEvent is null ||
            scheduledEvent.StartTime <= runtime.DateAndTime.CurrentTime.ToUnixTimeSeconds() ||
            !runtime.EventScheduler.SavedReminders.Add(eventKey))
        {
            return 0;
        }

        runtime.TriggerEvent("EVENT_SCHEDULER_UPDATE");
        return 0;
    }

    private static int PushOngoingEvents(
        lua_State state,
        IReadOnlyList<WowOngoingEvent> events)
    {
        if (events.Count == 0)
            return 0;

        lua_createtable(state, events.Count, 0);
        for (var index = 0; index < events.Count; index++)
        {
            var item = events[index];
            lua_createtable(state, 0, 3);
            SetIntegerField(state, "areaPoiID", item.AreaPoiId);
            SetBooleanField(state, "rewardsClaimed", item.RewardsClaimed);
            PushDisplayInfo(state, item.DisplayInfo);
            lua_setfield(state, -2, "displayInfo");
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int PushScheduledEvents(
        lua_State state,
        WowEventSchedulerState scheduler)
    {
        if (scheduler.ScheduledEvents.Count == 0)
            return 0;

        lua_createtable(state, scheduler.ScheduledEvents.Count, 0);
        for (var index = 0; index < scheduler.ScheduledEvents.Count; index++)
        {
            var item = scheduler.ScheduledEvents[index];
            lua_createtable(state, 0, 9);
            SetStringField(state, "eventKey", item.EventKey);
            SetIntegerField(state, "eventID", item.EventId);
            SetIntegerField(state, "areaPoiID", item.AreaPoiId);
            SetNumberField(state, "startTime", item.StartTime);
            SetNumberField(state, "endTime", item.EndTime);
            SetNumberField(state, "duration", item.Duration);
            SetBooleanField(
                state,
                "hasReminder",
                scheduler.SavedReminders.Contains(item.EventKey));
            SetBooleanField(state, "rewardsClaimed", item.RewardsClaimed);
            PushDisplayInfo(state, item.DisplayInfo);
            lua_setfield(state, -2, "displayInfo");
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static void PushDisplayInfo(
        lua_State state,
        WowEventSchedulerDisplayInfo displayInfo)
    {
        lua_createtable(state, 0, 4);
        SetBooleanField(state, "hideTimeLeft", displayInfo.HideTimeLeft);
        SetBooleanField(state, "hideDescription", displayInfo.HideDescription);
        SetOptionalStringField(state, "overrideAtlas", displayInfo.OverrideAtlas);
        SetOptionalIntegerField(
            state,
            "overrideTooltipWidgetSetID",
            displayInfo.OverrideTooltipWidgetSetId);
    }

    private static void RequireArgumentCount(
        lua_State state,
        int expected,
        string usage)
    {
        if (lua_gettop(state) != expected)
            luaL_error(state, usage);
    }

    private static int ReadInt32(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (double.IsNaN(value) ||
            double.IsInfinity(value) ||
            value < int.MinValue ||
            value > int.MaxValue ||
            value != Math.Truncate(value))
        {
            return luaL_error(state, usage);
        }
        return checked((int)value);
    }

    private static string ReadString(lua_State state, int index, string usage)
    {
        if (lua_type(state, index) != LUA_TSTRING)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static void SetIntegerField(lua_State state, string name, long value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetNumberField(lua_State state, string name, long value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBooleanField(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

    private static void SetStringField(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalStringField(
        lua_State state,
        string name,
        string? value)
    {
        if (value is null)
            return;
        SetStringField(state, name, value);
    }

    private static void SetOptionalIntegerField(
        lua_State state,
        string name,
        int? value)
    {
        if (value is not { } integer)
            return;
        SetIntegerField(state, name, integer);
    }
}
