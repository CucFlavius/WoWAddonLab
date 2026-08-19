using LuaNET.Lua51;

namespace WoWAddonLab.Emulator.Lua;

internal interface ILuaApiModule
{
    void Register(lua_State state);
}
