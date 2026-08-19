using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSoundGameSystemApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(
            state,
            "Sound_GameSystem_GetNumOutputDrivers",
            Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "Sound_GameSystem_GetOutputDriverNameByIndex",
            Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var sound = LuaBindings.GetRuntime(state).SoundGameSystem;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "Sound_GameSystem_GetNumOutputDrivers":
                var count = sound.Available && sound.OutputDriverNames.Count > 0
                    ? sound.OutputDriverNames.Count + 1
                    : 0;
                lua_pushnumber(state, count);
                return 1;
            case "Sound_GameSystem_GetOutputDriverNameByIndex":
            {
                const string usage =
                    "Usage: Sound_GetOutputDriverNameByIndex(OutputDriverIndex)";
                var index = Math.Max(0, RequiredInt32(state, 1, usage));
                var name = sound.NoneName;
                if (sound.Available)
                {
                    if (index == 0)
                        name = sound.SystemDefaultName;
                    else if (index <= sound.OutputDriverNames.Count)
                        name = sound.OutputDriverNames[index - 1];
                }
                lua_pushstring(state, name);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)value);
    }
}
