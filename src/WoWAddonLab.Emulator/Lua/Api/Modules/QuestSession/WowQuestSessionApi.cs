using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowQuestSessionApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CanStart",
        "CanStop",
        "Exists",
        "GetAvailableSessionCommand",
        "GetPendingCommand",
        "GetProposedMaxLevelForSession",
        "GetSessionBeginDetails",
        "GetSuperTrackedQuest",
        "HasJoined",
        "HasPendingCommand",
        "RequestSessionStart",
        "RequestSessionStop",
        "SendSessionBeginResponse",
        "SetQuestIsSuperTracked"
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
        lua_setglobal(state, "C_QuestSession");
    }

    private static int Dispatch(lua_State state)
    {
        var session = LuaBindings.GetRuntime(state).QuestSession;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "CanStart":
                return PushBoolean(state, session.CanStart);
            case "CanStop":
                return PushBoolean(state, session.CanStop);
            case "Exists":
                return PushBoolean(state, session.Exists);
            case "HasJoined":
                return PushBoolean(state, session.HasJoined);
            case "HasPendingCommand":
                return PushBoolean(state, session.PendingCommand != 0);
            case "GetAvailableSessionCommand":
                lua_pushnumber(state, AvailableCommand(session));
                return 1;
            case "GetPendingCommand":
                lua_pushnumber(state, session.PendingCommand);
                return 1;
            case "GetProposedMaxLevelForSession":
                lua_pushinteger(state, session.ProposedMaximumLevel);
                return 1;
            case "GetSuperTrackedQuest":
                if (session.SuperTrackedQuestId is { } questId)
                    lua_pushinteger(state, questId);
                else
                    lua_pushnil(state);
                return 1;
            case "GetSessionBeginDetails":
                if (session.BeginDetails is not { } details)
                {
                    lua_pushnil(state);
                }
                else
                {
                    lua_newtable(state);
                    lua_pushstring(state, details.Name);
                    lua_setfield(state, -2, "name");
                    lua_pushstring(state, details.Guid);
                    lua_setfield(state, -2, "guid");
                }
                return 1;
            case "SetQuestIsSuperTracked":
            {
                const string usage =
                    "Usage: C_QuestSession.SetQuestIsSuperTracked(" +
                    "questID, superTrack)";
                var trackedQuestId = RequiredInt32(state, 1, usage);
                var superTrack = RequiredBoolean(state, 2, usage);
                if (superTrack)
                    session.SuperTrackedQuestId = trackedQuestId;
                else
                    session.SuperTrackedQuestId = null;
                return 0;
            }
            case "RequestSessionStart":
                if (session.IsAvailable &&
                    session.PendingCommand == 0 &&
                    session.CanStart)
                {
                    session.PendingCommand = 1;
                    session.StartRequestCount++;
                }
                return 0;
            case "RequestSessionStop":
                if (session.IsAvailable &&
                    session.PendingCommand == 0 &&
                    session.CanStop)
                {
                    session.PendingCommand = 2;
                    session.StopRequestCount++;
                }
                return 0;
            case "SendSessionBeginResponse":
            {
                const string usage =
                    "Usage: C_QuestSession.SendSessionBeginResponse(" +
                    "beginSession)";
                var beginSession = RequiredBoolean(state, 1, usage);
                if (session.BeginDetails is not null)
                {
                    session.LastBeginResponse = beginSession;
                    session.BeginDetails = null;
                }
                return 0;
            }
            default:
                return 0;
        }
    }

    private static uint AvailableCommand(WowQuestSessionState session)
    {
        if (!session.IsAvailable)
            return 0;
        if (session.CanStart)
            return 1;
        if (session.CanStop)
            return 2;
        return session.HasJoined ? 3u : 0;
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

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_type(state, index) == LUA_TNIL)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }
}
