using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSummonInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        RegisterEnums(state);
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "CancelSummon",
                     "ConfirmSummon",
                     "GetSummonConfirmAreaName",
                     "GetSummonConfirmSummoner",
                     "GetSummonConfirmTimeLeft",
                     "GetSummonReason",
                     "IsSummonSkippingStartExperience"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_SummonInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var summon = LuaBindings.GetRuntime(state).SummonInfo;
        switch (operation)
        {
            case "CancelSummon":
                summon.RejectRequestCount++;
                summon.LastResponseAccepted = false;
                summon.ClearPendingSummon();
                return 0;
            case "ConfirmSummon":
                if (summon.CanConfirm)
                {
                    summon.AcceptRequestCount++;
                    summon.LastResponseAccepted = true;
                    summon.ClearPendingSummon();
                }
                return 0;
            case "GetSummonConfirmAreaName":
                lua_pushstring(state, summon.AreaName);
                return 1;
            case "GetSummonConfirmSummoner":
                if (summon.Summoner is null)
                    lua_pushnil(state);
                else
                    lua_pushstring(state, summon.Summoner);
                return 1;
            case "GetSummonConfirmTimeLeft":
                lua_pushinteger(state, Math.Max(0, summon.ConfirmTimeLeft));
                return 1;
            case "GetSummonReason":
                lua_pushinteger(state, summon.Reason);
                return 1;
            case "IsSummonSkippingStartExperience":
                lua_pushboolean(
                    state,
                    summon.SkippingStartExperience ? 1 : 0);
                return 1;
            default:
                return 0;
        }
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_type(state, -1) != LUA_TTABLE)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Enum");
        }

        lua_createtable(state, 0, 2);
        SetInteger(state, "Spell", 0);
        SetInteger(state, "Scenario", 1);
        lua_setfield(state, -2, "SummonReason");

        lua_createtable(state, 0, 3);
        SetInteger(state, "NumValues", 2);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 1);
        lua_setfield(state, -2, "SummonReasonMeta");
        lua_pop(state, 1);
    }

    private static void SetInteger(lua_State state, string field, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }
}
