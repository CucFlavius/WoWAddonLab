using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowHousingBasicModeApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CancelActiveEditing", "CommitDecorMovement", "CommitHouseExteriorPosition",
        "FinishPlacingNewDecor", "GetHoveredDecorInfo", "GetSelectedDecorInfo",
        "IsDecorSelected", "IsFreePlaceEnabled", "IsGridSnapEnabled", "IsGridVisible",
        "IsHouseExteriorHovered", "IsHouseExteriorSelected", "IsHoveringDecor",
        "IsPlacingNewDecor", "RemoveSelectedDecor", "RotateDecor",
        "RotateHouseExterior", "SetFreePlaceEnabled", "SetGridSnapEnabled",
        "SetGridVisible", "StartPlacingNewDecor", "StartPlacingPreviewDecor"
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
        lua_setglobal(state, "C_HousingBasicMode");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var housing = runtime.HousingBasicMode;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetHoveredDecorInfo":
            case "GetSelectedDecorInfo":
                lua_pushnil(state);
                return 1;
            case "IsDecorSelected":
                return PushBoolean(state, housing.DecorSelected);
            case "IsFreePlaceEnabled":
                return PushBoolean(state, housing.FreePlaceEnabled);
            case "IsGridSnapEnabled":
                return PushBoolean(state, housing.GridSnapEnabled);
            case "IsGridVisible":
                return PushBoolean(state, housing.GridVisible);
            case "IsHouseExteriorHovered":
                return PushBoolean(state, housing.HouseExteriorHovered);
            case "IsHouseExteriorSelected":
                return PushBoolean(state, housing.HouseExteriorSelected);
            case "IsHoveringDecor":
                return PushBoolean(state, housing.HoveringDecor);
            case "IsPlacingNewDecor":
                return PushBoolean(state, housing.PlacingNewDecor);
            case "SetFreePlaceEnabled":
                housing.FreePlaceEnabled = RequiredBoolean(state, 1, operation);
                runtime.TriggerEvent(
                    "HOUSING_DECOR_FREE_PLACE_STATUS_CHANGED",
                    housing.FreePlaceEnabled);
                return 0;
            case "SetGridSnapEnabled":
                housing.GridSnapEnabled = RequiredBoolean(state, 1, operation);
                runtime.TriggerEvent(
                    "HOUSING_DECOR_GRID_SNAP_STATUS_CHANGED",
                    housing.GridSnapEnabled);
                return 0;
            case "SetGridVisible":
                housing.GridVisible = RequiredBoolean(state, 1, operation);
                return 0;
            case "RotateDecor":
                housing.DecorRotationDegrees += RequiredNumber(state, 1, operation);
                return 0;
            case "RotateHouseExterior":
                housing.HouseRotationDegrees += RequiredNumber(state, 1, operation);
                return 0;
            case "StartPlacingNewDecor":
                if (lua_istable(state, 1) == 0)
                    return UsageError(state, operation);
                housing.PlacingNewDecor = true;
                runtime.TriggerEvent("HOUSING_BASIC_MODE_SELECTED_TARGET_CHANGED", true, 1, false);
                return 0;
            case "StartPlacingPreviewDecor":
                housing.PreviewDecorRecordId = (int)RequiredNumber(state, 1, operation);
                housing.PlacingNewDecor = true;
                runtime.TriggerEvent("HOUSING_BASIC_MODE_SELECTED_TARGET_CHANGED", true, 1, true);
                return 0;
            case "FinishPlacingNewDecor":
                housing.PlacingNewDecor = false;
                housing.PreviewDecorRecordId = null;
                return 0;
            case "CommitDecorMovement":
                housing.DecorSelected = false;
                return 0;
            case "CommitHouseExteriorPosition":
                housing.HouseExteriorSelected = false;
                return 0;
            case "RemoveSelectedDecor":
                housing.DecorSelected = false;
                return 0;
            case "CancelActiveEditing":
                housing.DecorSelected = false;
                housing.HouseExteriorSelected = false;
                housing.PlacingNewDecor = false;
                housing.PreviewDecorRecordId = null;
                return 0;
            default:
                return 0;
        }
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static bool RequiredBoolean(lua_State state, int index, string operation)
    {
        if (lua_isboolean(state, index) == 0)
        {
            UsageError(state, operation);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static double RequiredNumber(lua_State state, int index, string operation)
    {
        if (lua_isnumber(state, index) == 0)
        {
            UsageError(state, operation);
            return 0;
        }
        return lua_tonumber(state, index);
    }

    private static int UsageError(lua_State state, string operation) =>
        luaL_error(state, $"Usage: C_HousingBasicMode.{operation}(...)");
}
