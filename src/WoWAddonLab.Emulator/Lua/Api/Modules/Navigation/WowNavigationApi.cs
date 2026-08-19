using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowNavigationApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        RegisterEnums(state);

        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetDistance",
                     "GetFrame",
                     "GetNearestPartyMemberToken",
                     "GetTargetState",
                     "HasValidScreenPosition",
                     "WasClampedToScreen"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Navigation");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var navigation = runtime.Navigation;
        switch (lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty)
        {
            case "GetDistance":
                lua_pushnumber(state, (float)navigation.Distance);
                return 1;
            case "GetFrame":
                runtime.PushObject(
                    navigation.FrameId is { } frameId ? runtime.Ui.Find(frameId) : null);
                return 1;
            case "GetNearestPartyMemberToken":
                if (navigation.NearestPartyMemberToken is { } unitToken)
                    lua_pushstring(state, unitToken);
                else
                    lua_pushnil(state);
                return 1;
            case "GetTargetState":
                lua_pushinteger(
                    state,
                    navigation.TargetState is >= 0 and <= 3
                        ? navigation.TargetState
                        : 0);
                return 1;
            case "HasValidScreenPosition":
                lua_pushboolean(state, navigation.HasValidScreenPosition ? 1 : 0);
                return 1;
            case "WasClampedToScreen":
                lua_pushboolean(state, navigation.WasClampedToScreen ? 1 : 0);
                return 1;
            default:
                return 0;
        }
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Enum");
        }

        lua_createtable(state, 0, 4);
        SetInteger(state, "Invalid", 0);
        SetInteger(state, "Occluded", 1);
        SetInteger(state, "InRange", 2);
        SetInteger(state, "Disabled", 3);
        lua_setfield(state, -2, "NavigationState");

        lua_createtable(state, 0, 3);
        SetInteger(state, "NumValues", 4);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 3);
        lua_setfield(state, -2, "NavigationStateMeta");
        lua_pop(state, 1);
    }

    private static void SetInteger(lua_State state, string field, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }
}
