using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowDurationUtilApi : LuaApiModule
{
    private static readonly lua_CFunction CreateDurationCallback = CreateDuration;
    private static readonly lua_CFunction CreateDurationTextBindingCallback = CreateDurationTextBinding;
    private static readonly lua_CFunction CreateManualClockCallback = CreateManualClock;

    public override void Register(lua_State state)
    {
        WowDurationClockApi.Register(state);
        WowDurationTextBindingApi.Register(state);

        lua_newtable(state);
        lua_pushcfunction(state, CreateDurationCallback);
        lua_setfield(state, -2, "CreateDuration");
        lua_pushcfunction(state, CreateDurationTextBindingCallback);
        lua_setfield(state, -2, "CreateDurationTextBinding");
        lua_pushcfunction(state, CreateManualClockCallback);
        lua_setfield(state, -2, "CreateManualClock");
        lua_setglobal(state, "C_DurationUtil");
    }

    private static int CreateDuration(lua_State state)
    {
        WowDurationApi.PushDefault(state);
        return 1;
    }

    private static int CreateManualClock(lua_State state)
    {
        WowDurationClockApi.PushManual(state, new WowDurationClockState());
        return 1;
    }

    private static int CreateDurationTextBinding(lua_State state)
    {
        WowDurationTextBindingApi.Push(state);
        return 1;
    }
}
