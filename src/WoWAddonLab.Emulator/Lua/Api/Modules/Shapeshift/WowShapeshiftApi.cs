using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowShapeshiftApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        foreach (var function in new[]
                 {
                     "GetNumShapeshiftForms",
                     "GetShapeshiftForm",
                     "GetShapeshiftFormID",
                     "GetShapeshiftFormInfo"
                 })
        {
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
        }
    }

    private static int Dispatch(lua_State state)
    {
        var shapeshift = LuaBindings.GetRuntime(state).Shapeshift;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetNumShapeshiftForms":
                lua_pushinteger(state, shapeshift.Forms.Count);
                return 1;
            case "GetShapeshiftForm":
            {
                var excludeTemporaryForms = lua_toboolean(state, 1) != 0;
                lua_pushnumber(
                    state,
                    excludeTemporaryForms
                        ? shapeshift.CurrentFormIndexExcludingTemporaryForms ??
                          shapeshift.CurrentFormIndex
                        : shapeshift.CurrentFormIndex);
                return 1;
            }
            case "GetShapeshiftFormID":
                if (shapeshift.CurrentFormId is not { } formId)
                {
                    lua_pushnil(state);
                    return 1;
                }
                lua_pushinteger(state, formId);
                return 1;
            case "GetShapeshiftFormInfo":
            {
                if (lua_isnumber(state, 1) == 0)
                {
                    luaL_error(state, "Usage: GetShapeshiftFormInfo(index)");
                    return 0;
                }
                var value = lua_tonumber(state, 1);
                if (!double.IsFinite(value) ||
                    value < int.MinValue ||
                    value > int.MaxValue)
                {
                    luaL_error(state, "Usage: GetShapeshiftFormInfo(index)");
                    return 0;
                }
                var index = unchecked((int)value);
                if (index < 1 || index > shapeshift.Forms.Count)
                    return 0;
                var form = shapeshift.Forms[index - 1];
                if (form.Icon is null)
                    lua_pushnil(state);
                else
                    lua_pushstring(state, form.Icon);
                lua_pushboolean(state, form.Active ? 1 : 0);
                lua_pushboolean(state, form.Castable ? 1 : 0);
                lua_pushnumber(state, form.SpellId);
                return 4;
            }
            default:
                return 0;
        }
    }
}
