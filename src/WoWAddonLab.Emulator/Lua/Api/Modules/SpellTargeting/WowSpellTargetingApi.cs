using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSpellTargetingApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "SpellCanTargetGarrisonFollower", "SpellCanTargetGarrisonFollowerAbility",
        "SpellCanTargetGarrisonMission", "SpellCanTargetItem", "SpellCanTargetItemID",
        "SpellCanTargetQuest", "SpellCanTargetUnit", "SpellIsTargeting",
        "SpellStopCasting", "SpellStopTargeting",
        "SpellTargetItem", "SpellTargetUnit"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setglobal(state, function);
        }
    }

    private static int Dispatch(lua_State state)
    {
        var targeting = LuaBindings.GetRuntime(state).SpellTargeting;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "SpellCanTargetGarrisonFollower":
            {
                var followerId = ParseFollowerId(state, 1);
                var result =
                    targeting.GarrisonFollowerResultById.TryGetValue(
                        followerId,
                        out var configured)
                        ? configured
                        : 2;
                PushBoolean(state, result == 0);
                lua_pushnumber(state, unchecked((uint)result));
                return 2;
            }
            case "SpellCanTargetGarrisonFollowerAbility":
            {
                const string usage =
                    "Usage: SpellCanTargetGarrisonFollowerAbility(" +
                    "followerID, garrAbilityID)";
                var followerId = ParseFollowerId(state, 1);
                var abilityId = RequiredLegacyUInt32(
                    state,
                    2,
                    usage);
                var result =
                    targeting.GarrisonFollowerAbilityResult.TryGetValue(
                        (followerId, abilityId),
                        out var configured)
                        ? configured
                        : 2;
                PushBoolean(state, result == 0);
                lua_pushnumber(state, unchecked((uint)(result + 1)));
                return 2;
            }
            case "SpellCanTargetGarrisonMission":
                PushBoolean(state, targeting.CanTargetGarrisonMission);
                return 1;
            case "SpellCanTargetItem":
                PushBoolean(state, targeting.CanTargetItem);
                return 1;
            case "SpellCanTargetItemID":
                PushBoolean(state, targeting.CanTargetItemId);
                return 1;
            case "SpellCanTargetQuest":
                PushBoolean(state, targeting.CanTargetQuest);
                return 1;
            case "SpellCanTargetUnit":
            {
                const string usage =
                    "Usage: SpellCanTargetUnit(\"unit\")";
                var unit = RequiredString(state, 1, usage);
                PushBoolean(
                    state,
                    targeting.CanTargetUnitByToken.TryGetValue(
                        unit,
                        out var canTarget) &&
                    canTarget);
                return 1;
            }
            case "SpellIsTargeting":
                PushBoolean(state, targeting.IsTargeting);
                return 1;
            case "SpellStopCasting":
            {
                var wasCasting = targeting.IsCasting;
                targeting.IsCasting = false;
                PushBoolean(state, wasCasting);
                return 1;
            }
            case "SpellStopTargeting":
            {
                var consumed =
                    targeting.IsTargeting ||
                    targeting.HasPendingTargetingCursor;
                targeting.Clear();
                PushBoolean(state, consumed);
                return 1;
            }
            case "SpellTargetItem":
            {
                const string usage =
                    "Usage: SpellTargetItem(itemID|\"name\"|\"itemlink\")";
                object item;
                if (lua_isnumber(state, 1) != 0)
                {
                    item = unchecked((int)lua_tonumber(state, 1));
                }
                else
                {
                    item = RequiredString(state, 1, usage);
                }

                targeting.TargetRequests.Add(
                    new WowSpellTargetRequest(operation, item));
                targeting.Clear();
                return 0;
            }
            case "SpellTargetUnit":
            {
                const string usage =
                    "Usage: SpellTargetUnit(\"unit\")";
                var unit = RequiredString(state, 1, usage);
                targeting.TargetRequests.Add(
                    new WowSpellTargetRequest(operation, unit));
                targeting.Clear();
                return 0;
            }
            default:
                return 0;
        }
    }

    private static ulong ParseFollowerId(lua_State state, int index)
    {
        if (lua_type(state, index) != LUA_TSTRING)
            return 0;

        var text = lua_tostring(state, index);
        if (text is null ||
            text.Length != 18 ||
            !text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ||
            !ulong.TryParse(
                text.AsSpan(2),
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out var value))
        {
            return 0;
        }
        return value;
    }

    private static uint RequiredLegacyUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return unchecked((uint)RaiseArgumentError(state, usage));
        return unchecked((uint)(int)lua_tonumber(state, index));
    }

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isstring(state, index) == 0)
        {
            RaiseArgumentError(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static int RaiseArgumentError(
        lua_State state,
        string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static void PushBoolean(lua_State state, bool value) =>
        lua_pushboolean(state, value ? 1 : 0);
}
