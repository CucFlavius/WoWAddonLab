using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowWowLabsDataManagerApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetConfirmedWoWLabsArea",
        "GetWoWLabsAreaInfo",
        "IsInPrematch",
        "PushCircleInfoToLua",
        "QuerySelectedWoWLabsArea",
        "QueryWoWLabsAreaInfo",
        "SelectWoWLabsArea"
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
        lua_setglobal(state, "C_WowLabsDataManager");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var wowLabs = runtime.WowLabsDataManager;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetConfirmedWoWLabsArea":
                RequireArgumentCount(state, 0, operation);
                if (wowLabs.ConfirmedAreaId is { } confirmedAreaId)
                    lua_pushinteger(state, confirmedAreaId);
                else
                    lua_pushnil(state);
                return 1;
            case "GetWoWLabsAreaInfo":
                RequireArgumentCount(state, 0, operation);
                PushAreaInfo(state, wowLabs.Areas);
                return 1;
            case "IsInPrematch":
                RequireArgumentCount(state, 0, operation);
                lua_pushboolean(state, wowLabs.InPrematch ? 1 : 0);
                return 1;
            case "PushCircleInfoToLua":
                RequireArgumentCount(state, 0, operation);
                wowLabs.CircleInfoDirty = true;
                wowLabs.CircleInfoPushRequestCount++;
                return 0;
            case "QuerySelectedWoWLabsArea":
                RequireArgumentCount(state, 0, operation);
                if (IsCharacterlessLoginActive(runtime))
                    wowLabs.SelectedAreaQueryCount++;
                return 0;
            case "QueryWoWLabsAreaInfo":
                RequireArgumentCount(state, 0, operation);
                if (IsCharacterlessLoginActive(runtime))
                    wowLabs.AreaInfoQueryCount++;
                return 0;
            case "SelectWoWLabsArea":
            {
                const string usage =
                    "Usage: C_WowLabsDataManager.SelectWoWLabsArea(wowLabsAreaID)";
                if (lua_gettop(state) != 1 || lua_isnumber(state, 1) == 0)
                    return luaL_error(state, usage);
                var value = lua_tonumber(state, 1);
                if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
                    return luaL_error(state, usage);
                if (IsCharacterlessLoginActive(runtime))
                    wowLabs.SelectedAreaRequests.Add((int)value);
                return 0;
            }
            default:
                return 0;
        }
    }

    internal static void Tick(LuaRuntime runtime)
    {
        var wowLabs = runtime.WowLabsDataManager;
        if (!wowLabs.CircleInfoDirty)
            return;

        wowLabs.CircleInfoDirty = false;
        runtime.TriggerEvent(
            "WOW_LABS_DATA_BR_CIRCLE",
            -1,
            -1,
            null,
            null,
            0,
            0,
            0,
            null,
            0,
            0);
    }

    private static void PushAreaInfo(
        lua_State state,
        IReadOnlyList<WowLabsAreaInfo> areas)
    {
        lua_createtable(state, areas.Count, 0);
        for (var index = 0; index < areas.Count; index++)
        {
            var area = areas[index];
            lua_createtable(state, 0, 5);
            SetNumber(state, "wowLabsAreaID", area.WowLabsAreaId);
            SetNumber(state, "areaType", area.AreaType);
            SetNumber(state, "x", area.X);
            SetNumber(state, "y", area.Y);
            SetNumber(state, "z", area.Z);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void SetNumber(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void RequireArgumentCount(
        lua_State state,
        int expected,
        string operation)
    {
        if (lua_gettop(state) != expected)
            luaL_error(state, $"Usage: C_WowLabsDataManager.{operation}()");
    }

    private static bool IsCharacterlessLoginActive(LuaRuntime runtime)
    {
        if (runtime.GameRules.RuleValueOverrides.TryGetValue(14, out var value))
            return value != 0;
        return runtime.GameRules.UseProviderDefaults &&
               runtime.GameRuleProvider?.TryGetRule(14, out var rule) == true &&
               rule.Value != 0;
    }
}
