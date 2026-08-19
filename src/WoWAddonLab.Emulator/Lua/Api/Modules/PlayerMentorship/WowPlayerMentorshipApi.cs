using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPlayerMentorshipApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetMentorLevelRequirement",
        "GetMentorRequirements",
        "GetMentorshipStatus",
        "IsActivePlayerConsideredNewcomer",
        "IsMentorRestricted"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_PlayerMentorship");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var mentorship = runtime.PlayerMentorship;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "GetMentorLevelRequirement":
                if (mentorship.MentorLevelRequirement is { } level)
                    lua_pushinteger(state, level);
                else
                    lua_pushnil(state);
                return 1;
            case "GetMentorRequirements":
                PushIntegerTable(state, mentorship.RequiredAchievementIds);
                PushIntegerTable(state, mentorship.OptionalAchievementIds);
                lua_pushinteger(state, mentorship.OptionalCompleteAtLeastCount);
                return 3;
            case "GetMentorshipStatus":
                if (!TryReadPlayerLocationStatus(state, mentorship, out var status))
                    return luaL_error(
                        state,
                        "Usage: local status = C_PlayerMentorship.GetMentorshipStatus(playerLocation)");
                lua_pushinteger(state, status);
                return 1;
            case "IsActivePlayerConsideredNewcomer":
                lua_pushboolean(
                    state,
                    mentorship.ActivePlayerConsideredNewcomer ||
                    (GetRuleValue(runtime, 14) == 0 &&
                     mentorship.ActivePlayerStatus == 1)
                        ? 1
                        : 0);
                return 1;
            case "IsMentorRestricted":
                lua_pushboolean(state, mentorship.MentorRestricted ? 1 : 0);
                return 1;
            default:
                return 0;
        }
    }

    private static void PushIntegerTable(lua_State state, IList<int> values)
    {
        lua_newtable(state);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static bool TryReadPlayerLocationStatus(
        lua_State state,
        WowPlayerMentorshipState mentorship,
        out int status)
    {
        status = 0;
        if (lua_type(state, 1) != LUA_TTABLE)
            return false;

        if (TryReadStringField(state, 1, "unit", out var unit))
        {
            status = mentorship.StatusByUnitToken.TryGetValue(unit, out var unitStatus)
                ? unitStatus
                : mentorship.Status;
            return true;
        }

        if (TryReadStringField(state, 1, "guid", out var guid) ||
            TryReadStringField(state, 1, "communityClubInviterGUID", out guid))
        {
            status = mentorship.StatusByGuid.TryGetValue(guid, out var guidStatus)
                ? guidStatus
                : mentorship.Status;
            return true;
        }

        foreach (var field in new[]
                 {
                     "chatLineID",
                     "battlefieldScoreIndex",
                     "voiceMemberID",
                     "battleNetID",
                     "communityClubID"
                 })
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
        int tableIndex,
        string field,
        out string value)
    {
        lua_getfield(state, tableIndex, field);
        var valid = lua_type(state, -1) == LUA_TSTRING;
        value = valid ? lua_tostring(state, -1) ?? string.Empty : string.Empty;
        lua_pop(state, 1);
        return valid && value.Length > 0;
    }

    private static int GetRuleValue(LuaRuntime runtime, int ruleId)
    {
        if (runtime.GameRules.RuleValueOverrides.TryGetValue(ruleId, out var value))
            return value;
        return runtime.GameRules.UseProviderDefaults &&
               runtime.GameRuleProvider?.TryGetRule(ruleId, out var rule) == true
            ? rule.Value
            : 0;
    }
}
