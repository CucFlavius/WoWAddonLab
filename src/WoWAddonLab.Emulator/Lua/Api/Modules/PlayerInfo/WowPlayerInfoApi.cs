using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPlayerInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CanPlayerUseMountEquipment",
        "GetAlternateFormInfo",
        "GetDisplayID",
        "GetGlidingInfo",
        "GetSex",
        "HasAccountInventoryLock",
        "GetNativeDisplayID",
        "IsDisplayRaceNative",
        "IsMirrorImage",
        "IsPlayerInRPE",
        "IsPlayerNPERestricted",
        "IsTradingPostAvailable",
        "IsTravelersLogAvailable",
        "IsTutorialsTabAvailable"
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
        lua_setglobal(state, "C_PlayerInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var player = runtime.PlayerInfo;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "CanPlayerUseMountEquipment":
                lua_pushboolean(state, player.CanUseMountEquipment ? 1 : 0);
                lua_pushstring(state, player.MountEquipmentError);
                return 2;
            case "GetAlternateFormInfo":
                lua_pushboolean(state, player.HasAlternateForm ? 1 : 0);
                lua_pushboolean(state, player.IsAlternateForm ? 1 : 0);
                return 2;
            case "GetDisplayID":
                lua_pushnumber(state, player.DisplayId);
                return 1;
            case "GetNativeDisplayID":
                lua_pushnumber(state, player.NativeDisplayId);
                return 1;
            case "GetGlidingInfo":
                lua_pushboolean(state, player.IsGliding ? 1 : 0);
                lua_pushboolean(state, player.CanGlide ? 1 : 0);
                lua_pushnumber(state, player.GlideValue);
                return 3;
            case "GetSex":
                if (!WowPlayerLocationResolver.TryResolve(state, runtime.Units, out var unit))
                    return luaL_error(
                        state,
                        "Usage: local sex = C_PlayerInfo.GetSex(playerLocation)");
                if (unit is null)
                    lua_pushnil(state);
                else
                    lua_pushinteger(state, unit.Sex);
                return 1;
            case "HasAccountInventoryLock":
                lua_pushboolean(
                    state,
                    player.HasAccountInventoryLock ? 1 : 0);
                return 1;
            case "IsDisplayRaceNative":
                lua_pushboolean(state, player.IsDisplayRaceNative ? 1 : 0);
                return 1;
            case "IsMirrorImage":
                lua_pushboolean(state, player.IsMirrorImage ? 1 : 0);
                return 1;
            case "IsPlayerInRPE":
                lua_pushboolean(state, player.IsPlayerInRpe ? 1 : 0);
                return 1;
            case "IsPlayerNPERestricted":
                lua_pushboolean(state, player.IsPlayerNpeRestricted ? 1 : 0);
                return 1;
            case "IsTradingPostAvailable":
                lua_pushboolean(state, player.IsTradingPostAvailable ? 1 : 0);
                return 1;
            case "IsTravelersLogAvailable":
                lua_pushboolean(state, player.IsTravelersLogAvailable ? 1 : 0);
                return 1;
            case "IsTutorialsTabAvailable":
                lua_pushboolean(state, player.IsTutorialsTabAvailable ? 1 : 0);
                return 1;
            default:
                return 0;
        }
    }
}
