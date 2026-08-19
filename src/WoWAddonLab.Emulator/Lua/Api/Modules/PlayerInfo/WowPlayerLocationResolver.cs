using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal static class WowPlayerLocationResolver
{
    private static readonly string[] OtherLocationFields =
    [
        "chatLineID",
        "battlefieldScoreIndex",
        "voiceMemberID",
        "battleNetID",
        "communityClubID"
    ];

    public static bool TryResolve(
        lua_State state,
        WowUnitStateCollection units,
        out WowUnitState? unit)
    {
        unit = null;
        if (lua_type(state, 1) != LUA_TTABLE)
            return false;

        if (TryReadStringField(state, "unit", out var unitToken))
        {
            unit = units.Find(unitToken);
            return true;
        }

        if (TryReadStringField(state, "guid", out var guid) ||
            TryReadStringField(state, "communityClubInviterGUID", out guid))
        {
            unit = units.FindByGuid(guid);
            return true;
        }

        foreach (var field in OtherLocationFields)
        {
            lua_getfield(state, 1, field);
            var present = lua_type(state, -1) != LUA_TNIL;
            lua_pop(state, 1);
            if (present)
                return true;
        }

        return false;
    }

    private static bool TryReadStringField(
        lua_State state,
        string field,
        out string value)
    {
        lua_getfield(state, 1, field);
        var valid = lua_type(state, -1) == LUA_TSTRING;
        value = valid ? lua_tostring(state, -1) ?? string.Empty : string.Empty;
        lua_pop(state, 1);
        return valid && value.Length > 0;
    }
}
