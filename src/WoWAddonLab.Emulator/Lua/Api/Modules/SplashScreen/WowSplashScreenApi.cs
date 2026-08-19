using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSplashScreenApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "AcknowledgeSplash", "CanViewSplashScreen", "RequestLatestSplashScreen",
                     "SendSplashScreenActionLaunchedTelem", "SendSplashScreenCloseTelem"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_SplashScreen");

        lua_newtable(state);
        lua_pushstring(state, "SetConversationsDeferred");
        lua_pushcclosure(state, Callback, 1);
        lua_setfield(state, -2, "SetConversationsDeferred");
        lua_setglobal(state, "C_TalkingHead");
    }

    private static int Dispatch(lua_State state)
    {
        if (lua_tostring(state, lua_upvalueindex(1)) == "CanViewSplashScreen")
        {
            lua_pushboolean(state, 0);
            return 1;
        }

        return 0;
    }
}
