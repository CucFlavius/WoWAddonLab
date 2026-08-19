using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowLfgListApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetApplicationInfo",
        "GetApplications",
        "GetAvailableCategories",
        "GetAvailableRoles",
        "GetNumApplications",
        "GetPremadeGroupFinderStyle",
        "GetRoleCheckInfo",
        "HasActiveEntryInfo",
        "HasActivityList",
        "RequestAvailableActivities"
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
        lua_setglobal(state, "C_LFGList");
    }

    private static int Dispatch(lua_State state)
    {
        var lfgList = LuaBindings.GetRuntime(state).LfgList;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "HasActiveEntryInfo":
                RequireArgumentCount(
                    state,
                    0,
                    "Usage: local hasActiveEntryInfo = C_LFGList.HasActiveEntryInfo()");
                lua_pushboolean(state, lfgList.HasActiveEntry ? 1 : 0);
                return 1;
            case "HasActivityList":
                lua_pushboolean(state, lfgList.HasActivityList ? 1 : 0);
                return 1;
            case "GetRoleCheckInfo":
                if (lfgList.RoleCheckActivityId is not { } roleCheckActivityId)
                {
                    lua_pushboolean(state, 0);
                    return 1;
                }
                lua_pushboolean(state, 1);
                lua_pushinteger(state, roleCheckActivityId);
                return 2;
            case "GetApplicationInfo":
                return GetApplicationInfo(state, lfgList);
            case "GetApplications":
                return GetApplications(state, lfgList);
            case "GetAvailableCategories":
                PushIntegerArray(state, lfgList.AvailableCategories);
                return 1;
            case "GetNumApplications":
                lua_pushinteger(state, lfgList.Applications.Count);
                lua_pushinteger(
                    state,
                    lfgList.Applications.Count(application =>
                        application.Status is 1 or 2));
                return 2;
            case "GetPremadeGroupFinderStyle":
                RequireArgumentCount(
                    state,
                    0,
                    "Usage: local style = C_LFGList.GetPremadeGroupFinderStyle()");
                lua_pushinteger(state, lfgList.PremadeGroupFinderStyle);
                return 1;
            case "GetAvailableRoles":
                lua_pushboolean(state, lfgList.CanTank ? 1 : 0);
                lua_pushboolean(state, lfgList.CanHeal ? 1 : 0);
                lua_pushboolean(state, lfgList.CanDamage ? 1 : 0);
                return 3;
            case "RequestAvailableActivities":
                lfgList.AvailableActivitiesRequestCount++;
                return 0;
            default:
                return 0;
        }
    }

    private static int GetApplicationInfo(lua_State state, WowLfgListState lfgList)
    {
        const string usage =
            "Usage: local searchResultID, applicationStatus, pendingApplicationStatus, " +
            "duration, role = C_LFGList.GetApplicationInfo(resultID)";
        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, usage);

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        var resultId = (int)value;
        var application = lfgList.Applications.FirstOrDefault(
            candidate => candidate.ResultId == resultId);
        if (application is null)
            return 0;

        lua_pushinteger(state, application.ResultId);
        lua_pushstring(state, ApplicationStatusToString(application.Status));
        if (application.PendingStatus == 0)
            lua_pushnil(state);
        else
            lua_pushstring(state, ApplicationStatusToString(application.PendingStatus));
        lua_pushnumber(state, Math.Max(application.Duration, 0));
        lua_pushstring(
            state,
            application.Role.ToUpperInvariant() switch
            {
                "TANK" => "TANK",
                "HEALER" => "HEALER",
                "DAMAGER" => "DAMAGER",
                _ => "NONE"
            });
        return 5;
    }

    private static int GetApplications(lua_State state, WowLfgListState lfgList)
    {
        var resultIds = lfgList.Applications
            .Where(application => application.Status != 0 || application.PendingStatus != 0)
            .Select(application => application.ResultId)
            .ToArray();
        PushIntegerArray(state, resultIds);
        return 1;
    }

    private static string ApplicationStatusToString(int status) =>
        status switch
        {
            1 => "applied",
            2 => "invited",
            3 => "failed",
            4 => "cancelled",
            5 => "declined",
            6 => "declined_full",
            7 => "declined_delisted",
            8 => "timedout",
            9 => "invitedeclined",
            10 => "inviteaccepted",
            _ => "none"
        };

    private static void PushIntegerArray(lua_State state, IReadOnlyList<int> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void RequireArgumentCount(
        lua_State state,
        int expected,
        string usage)
    {
        if (lua_gettop(state) != expected)
            luaL_error(state, usage);
    }
}
