using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowXmlApi : LuaApiModule
{
    private static readonly lua_CFunction GetTemplateInfoCallback = GetTemplateInfo;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        lua_pushcfunction(state, GetTemplateInfoCallback);
        lua_setfield(state, -2, "GetTemplateInfo");
        lua_setglobal(state, "C_XMLUtil");
    }

    private static int GetTemplateInfo(lua_State state)
    {
        const string usage = "Usage: local info = C_XMLUtil.GetTemplateInfo(name)";
        if (lua_gettop(state) != 1 || lua_type(state, 1) != LUA_TSTRING)
            return luaL_error(state, usage);

        var runtime = LuaBindings.GetRuntime(state);
        var name = lua_tostring(state, 1) ?? string.Empty;
        if (!runtime.TryGetXmlTemplateInfo(name, out var info))
            return 0;

        lua_newtable(state);
        SetString(state, "type", info.Type);
        SetNumber(state, "width", info.Width);
        SetNumber(state, "height", info.Height);
        if (info.Inherits is { } inherits)
            SetString(state, "inherits", inherits);
        SetString(state, "sourceLocation", info.SourceLocation);

        lua_newtable(state);
        for (var index = 0; index < info.KeyValues.Count; index++)
        {
            var keyValue = info.KeyValues[index];
            lua_newtable(state);
            SetString(state, "key", keyValue.Key);
            SetString(state, "keyType", keyValue.KeyType);
            SetString(state, "type", keyValue.Type);
            SetString(state, "value", keyValue.Value);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "keyValues");
        return 1;
    }

    private static void SetNumber(lua_State state, string field, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetString(lua_State state, string field, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }
}
