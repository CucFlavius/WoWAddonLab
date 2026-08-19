using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCharacterServicesPublicApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        lua_pushcclosure(state, Callback, 0);
        lua_setfield(state, -2, "ShouldSeeControlPopup");
        lua_setglobal(state, "C_CharacterServicesPublic");
    }

    private static int Dispatch(lua_State state)
    {
        lua_pushboolean(
            state,
            LuaBindings.GetRuntime(state)
                .CharacterServicesPublic
                .ShouldSeeControlPopup
                ? 1
                : 0);
        return 1;
    }
}
