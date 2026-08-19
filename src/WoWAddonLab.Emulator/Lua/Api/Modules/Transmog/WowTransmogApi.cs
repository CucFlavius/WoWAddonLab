using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTransmogApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CanHaveSecondaryAppearanceForSlotID",
        "ExtractTransmogIDList",
        "GetAllSetAppearancesByID",
        "GetItemIDForSource",
        "GetSlotForInventoryType",
        "GetSlotVisualInfo",
        "IsAtTransmogNPC"
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
        lua_setglobal(state, "C_Transmog");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "ExtractTransmogIDList":
            case "GetAllSetAppearancesByID":
                lua_newtable(state);
                return 1;
            case "CanHaveSecondaryAppearanceForSlotID":
            case "IsAtTransmogNPC":
                lua_pushboolean(state, 0);
                return 1;
            case "GetSlotVisualInfo":
                lua_newtable(state);
                foreach (var field in new[]
                         {
                             "baseSourceID", "baseVisualID", "appliedSourceID",
                             "appliedVisualID", "pendingSourceID", "pendingVisualID",
                             "itemSubclass"
                         })
                {
                    lua_pushinteger(state, 0);
                    lua_setfield(state, -2, field);
                }
                lua_pushboolean(state, 0);
                lua_setfield(state, -2, "hasUndo");
                lua_pushboolean(state, 0);
                lua_setfield(state, -2, "isHideVisual");
                return 1;
            case "GetSlotForInventoryType":
            case "GetItemIDForSource":
            default:
                return 0;
        }
    }
}
