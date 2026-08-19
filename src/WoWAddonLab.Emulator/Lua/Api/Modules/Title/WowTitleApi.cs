using LuaNET.Lua51;
using System.Text;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTitleApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        foreach (var function in new[]
                 {
                     "GetNumTitles", "GetCurrentTitle", "GetTitleName",
                     "IsTitleKnown", "SetCurrentTitle"
                 })
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var titles = runtime.Titles;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? "";
        switch (operation)
        {
            case "GetNumTitles":
                lua_pushinteger(state, titles.Titles.Count == 0
                    ? 0
                    : titles.Titles.Max(value => value.Id));
                return 1;
            case "GetCurrentTitle":
                lua_pushinteger(state, titles.CurrentTitleId);
                return 1;
            case "GetTitleName":
            {
                if (!TryReadRequiredInt32(state, 1, out var id))
                    return luaL_error(
                        state,
                        "Usage: local titleString, playerTitle = GetTitleName(titleMaskID)");
                var title = titles.Titles.FirstOrDefault(value => value.Id == id);
                if (title is null)
                    lua_pushnil(state);
                else
                    lua_pushstring(state, NormalizeTitleName(title.Name));
                lua_pushboolean(state, title?.IsPlayerTitle == true ? 1 : 0);
                return 2;
            }
            case "IsTitleKnown":
                if (!TryReadRequiredUInt32(state, 1, out var knownTitleId))
                    return luaL_error(state, "Usage: local isKnown = IsTitleKnown(titleMaskID)");
                lua_pushboolean(
                    state,
                    knownTitleId <= int.MaxValue &&
                    titles.Titles.Any(
                        value => value.Id == (int)knownTitleId && value.IsKnown)
                        ? 1
                        : 0);
                return 1;
            case "SetCurrentTitle":
                if (!TryReadRequiredInt32(state, 1, out var requestedTitleId))
                    return luaL_error(state, "Usage: SetCurrentTitle(titleMaskID)");
                if (!runtime.Client.InCombatLockdown)
                    titles.RequestedTitleId = requestedTitleId;
                return 0;
            default:
                return 0;
        }
    }

    private static bool TryReadRequiredInt32(
        lua_State state,
        int index,
        out int value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < int.MinValue or > int.MaxValue)
            return false;
        value = (int)number;
        return true;
    }

    private static bool TryReadRequiredUInt32(
        lua_State state,
        int index,
        out uint value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < uint.MinValue or > uint.MaxValue)
            return false;
        value = (uint)number;
        return true;
    }

    private static string NormalizeTitleName(string format)
    {
        var result = new StringBuilder(format.Length);
        var skippingPlaceholder = false;
        foreach (var character in format)
        {
            if (character == '%')
            {
                skippingPlaceholder = true;
            }
            else if (skippingPlaceholder)
            {
                if (character is ' ' or '\uFF0C')
                    skippingPlaceholder = false;
            }
            else
            {
                result.Append(character);
            }
        }
        return result.ToString();
    }
}
