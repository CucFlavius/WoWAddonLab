using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowAutoCompleteApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetAutoCompletePresenceID",
        "GetAutoCompleteRealms",
        "GetAutoCompleteResults",
        "IsRecognizedName"
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
        lua_setglobal(state, "C_AutoComplete");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetAutoCompleteRealms":
                lua_newtable(state);
                for (var index = 0; index < runtime.AutoComplete.RealmNames.Count; index++)
                {
                    lua_pushstring(state, runtime.AutoComplete.RealmNames[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "GetAutoCompleteResults":
                lua_newtable(state);
                return 1;
            case "GetAutoCompletePresenceID":
                lua_pushnil(state);
                return 1;
            case "IsRecognizedName":
            {
                var name = lua_isstring(state, 1) != 0
                    ? lua_tostring(state, 1)
                    : null;
                var recognized = name is not null &&
                    runtime.AutoComplete.RealmNames.Contains(
                        name,
                        StringComparer.OrdinalIgnoreCase);
                lua_pushboolean(state, recognized ? 1 : 0);
                return 1;
            }
            default:
                return 0;
        }
    }
}
