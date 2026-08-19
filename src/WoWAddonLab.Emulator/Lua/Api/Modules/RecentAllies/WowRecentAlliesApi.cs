using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowRecentAlliesApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CanSetRecentAllyNote", "GetRecentAllies", "GetRecentAllyByFullName",
        "GetRecentAllyByGUID", "IsRecentAllyByFullName", "IsRecentAllyByGUID",
        "IsRecentAllyDataReady", "IsRecentAllyPinned", "IsSystemEnabled",
        "IsSystemSupported", "SetRecentAllyNote", "SetRecentAllyPinned",
        "TryRequestRecentAlliesData"
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
        lua_setglobal(state, "C_RecentAllies");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var recent = runtime.RecentAllies;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "IsSystemEnabled":
                lua_pushboolean(state, recent.IsEnabled ? 1 : 0);
                return 1;
            case "IsSystemSupported":
                lua_pushboolean(state, recent.IsSupported ? 1 : 0);
                return 1;
            case "IsRecentAllyDataReady":
                if (!IsAvailable(recent))
                    return 0;
                lua_pushboolean(state, recent.IsDataReady ? 1 : 0);
                return 1;
            case "TryRequestRecentAlliesData":
                if (IsAvailable(recent))
                    recent.DataRequests++;
                return 0;
            case "GetRecentAllies":
                if (!IsAvailable(recent))
                    return 0;
                lua_newtable(state);
                for (var index = 0; index < recent.Allies.Count; index++)
                {
                    PushAlly(state, recent.Allies[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "GetRecentAllyByGUID":
            case "IsRecentAllyByGUID":
            {
                if (!IsAvailable(recent))
                    return 0;
                var guid = RequiredString(
                    state,
                    1,
                    Usage(operation, "characterGUID"));
                var ally = recent.Allies.FirstOrDefault(
                    value => value.Guid.Equals(guid, StringComparison.OrdinalIgnoreCase));
                return PushLookupResult(state, operation, ally);
            }
            case "GetRecentAllyByFullName":
            case "IsRecentAllyByFullName":
            {
                if (!IsAvailable(recent))
                    return 0;
                var fullName = RequiredString(
                    state,
                    1,
                    Usage(operation, "fullCharacterName"));
                var ally = recent.Allies.FirstOrDefault(
                    value => value.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));
                return PushLookupResult(state, operation, ally);
            }
            case "CanSetRecentAllyNote":
                if (!IsAvailable(recent))
                    return 0;
                lua_pushboolean(
                    state,
                    FindByGuid(
                        recent,
                        RequiredString(
                            state,
                            1,
                            Usage(operation, "characterGUID")))
                        ?.PinExpirationDate is not null
                        ? 1
                        : 0);
                return 1;
            case "IsRecentAllyPinned":
                if (!IsAvailable(recent))
                    return 0;
                lua_pushboolean(
                    state,
                    FindByGuid(
                        recent,
                        RequiredString(
                            state,
                            1,
                            Usage(operation, "characterGUID")))
                        ?.PinExpirationDate is not null
                        ? 1
                        : 0);
                return 1;
            case "SetRecentAllyNote":
            {
                if (!IsAvailable(recent))
                    return 0;
                var guid = RequiredString(
                    state,
                    1,
                    Usage(operation, "characterGUID, note"));
                var note = RequiredString(
                    state,
                    2,
                    Usage(operation, "characterGUID, note"));
                var ally = FindByGuid(recent, guid);
                if (ally is not null)
                    recent.NoteRequests.Add(new WowRecentAllyNoteRequest(ally.Guid, note));
                return 0;
            }
            case "SetRecentAllyPinned":
            {
                if (!IsAvailable(recent))
                    return 0;
                var usage = Usage(operation, "characterGUID, isPinned");
                var guid = RequiredString(state, 1, usage);
                if (lua_type(state, 2) != LUA_TBOOLEAN)
                {
                    luaL_error(state, usage);
                    return 0;
                }
                var pinned = lua_toboolean(state, 2) != 0;
                var ally = FindByGuid(recent, guid);
                if (ally is not null &&
                    (ally.PinExpirationDate is not null) != pinned)
                {
                    recent.PinnedRequests.Add(
                        new WowRecentAllyPinnedRequest(ally.Guid, pinned));
                }
                return 0;
            }
            default:
                return 0;
        }
    }

    private static int PushLookupResult(
        lua_State state,
        string operation,
        WowRecentAllyState? ally)
    {
        if (operation.StartsWith("Is", StringComparison.Ordinal))
        {
            lua_pushboolean(state, ally is not null ? 1 : 0);
            return 1;
        }
        if (ally is null)
        {
            lua_pushnil(state);
            return 1;
        }
        PushAlly(state, ally);
        return 1;
    }

    private static WowRecentAllyState? FindByGuid(WowRecentAlliesState state, string guid) =>
        state.Allies.FirstOrDefault(
            value => value.Guid.Equals(guid, StringComparison.OrdinalIgnoreCase));

    private static void PushAlly(lua_State state, WowRecentAllyState ally)
    {
        lua_newtable(state);

        lua_newtable(state);
        SetBoolean(state, "isOnline", ally.IsOnline);
        SetBoolean(state, "isDND", ally.IsDnd);
        SetBoolean(state, "isAFK", ally.IsAfk);
        if (ally.PinExpirationDate is { } expiration)
            SetNumber(state, "pinExpirationDate", expiration);
        SetBoolean(state, "friendRequestSentThisSession", ally.FriendRequestSentThisSession);
        if (ally.CurrentLocation is not null)
            SetString(state, "currentLocation", ally.CurrentLocation);
        lua_setfield(state, -2, "stateData");

        lua_newtable(state);
        SetString(state, "guid", ally.Guid);
        SetString(state, "name", ally.Name);
        SetString(state, "fullName", ally.FullName);
        SetString(state, "realmName", ally.RealmName);
        SetNumber(state, "level", ally.Level);
        SetNumber(state, "classID", ally.ClassId);
        SetNumber(state, "raceID", ally.RaceId);
        SetNumber(state, "sex", ally.Sex);
        lua_setfield(state, -2, "characterData");

        lua_newtable(state);
        lua_createtable(state, ally.Interactions.Count, 0);
        for (var index = 0; index < ally.Interactions.Count; index++)
        {
            PushInteraction(state, ally.Interactions[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "interactions");
        if (ally.Note is not null)
            SetString(state, "note", ally.Note);
        lua_setfield(state, -2, "interactionData");
    }

    private static void PushInteraction(
        lua_State state,
        WowRecentAllyInteractionState interaction)
    {
        lua_createtable(state, 0, 4);
        SetNumber(state, "type", interaction.Type);
        if (interaction.Description is not null)
            SetString(state, "description", interaction.Description);
        SetNumber(state, "timestamp", interaction.Timestamp);
        lua_createtable(state, 0, 4);
        if (interaction.ContextData.ItemId is { } itemId)
            SetNumber(state, "itemID", itemId);
        if (interaction.ContextData.LocationName is { } location)
            SetString(state, "locationName", location);
        if (interaction.ContextData.ActivityDifficultyId is { } difficultyId)
            SetNumber(state, "activityDifficultyID", difficultyId);
        if (interaction.ContextData.ActivityDifficultyLevel is { } difficultyLevel)
            SetNumber(state, "activityDifficultyLevel", difficultyLevel);
        lua_setfield(state, -2, "contextData");
    }

    private static bool IsAvailable(WowRecentAlliesState state) =>
        state.IsSupported && state.IsEnabled;

    private static string Usage(string operation, string arguments) =>
        $"Usage: C_RecentAllies.{operation}({arguments})";

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TSTRING)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetNumber(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBoolean(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }
}
