using LuaNET.Lua51;

namespace WoWAddonLab.Emulator.Lua;

internal abstract class LuaApiModule : ILuaApiModule
{
    public abstract void Register(lua_State state);
}
