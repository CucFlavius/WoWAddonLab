using System.Collections;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowDeathRecapApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] Functions =
    [
        "GetRecapEvents",
        "GetRecapLink",
        "GetRecapMaxHealth",
        "HasRecapEvents"
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
        lua_setglobal(state, "C_DeathRecap");
    }

    private static int Dispatch(lua_State state)
    {
        var deathRecap = LuaBindings.GetRuntime(state).DeathRecap;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "GetRecapEvents":
            {
                var recapId = LooseOptionalInt32(state, 1);
                var recap = ResolveRecap(deathRecap, recapId);
                if (recap is null)
                    return 0;

                PushRecapEvents(state, recap.Events);
                return 1;
            }
            case "GetRecapLink":
            {
                const string usage =
                    "Usage: local link = " +
                    "C_DeathRecap.GetRecapLink([recapID])";
                var recapId = OptionalInt32(state, 1, usage);
                var recap = ResolveRecap(deathRecap, recapId);
                lua_pushstring(
                    state,
                    recap?.Link ?? deathRecap.EmptyRecapText);
                return 1;
            }
            case "GetRecapMaxHealth":
            {
                const string usage =
                    "Usage: local maxHealth = " +
                    "C_DeathRecap.GetRecapMaxHealth([recapID])";
                var recapId = OptionalInt32(state, 1, usage);
                var recap = ResolveRecap(deathRecap, recapId);
                lua_pushnumber(state, recap?.MaxHealth ?? 0);
                return 1;
            }
            case "HasRecapEvents":
            {
                const string usage =
                    "Usage: local hasEvents = " +
                    "C_DeathRecap.HasRecapEvents([recapID])";
                var recapId = OptionalInt32(state, 1, usage);
                lua_pushboolean(
                    state,
                    ResolveRecap(deathRecap, recapId) is null ? 0 : 1);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static WowDeathRecapRecordState? ResolveRecap(
        WowDeathRecapState deathRecap,
        int? recapId)
    {
        var resolvedId = recapId ?? deathRecap.MostRecentRecapId;
        if (!resolvedId.HasValue || resolvedId.Value == -1)
            return null;

        deathRecap.RecapsById.TryGetValue(
            resolvedId.Value,
            out var recap);
        return recap;
    }

    private static void PushRecapEvents(
        lua_State state,
        IList<WowDeathRecapEventState> events)
    {
        var count = Math.Min(events.Count, 10);
        lua_createtable(state, count, 0);
        for (var index = 0; index < count; index++)
        {
            var recapEvent = events[index];
            lua_createtable(state, 0, recapEvent.Fields.Count + 1);
            SetNumber(state, "currentHP", recapEvent.CurrentHp);
            foreach (var field in recapEvent.Fields)
            {
                PushValue(state, field.Value);
                lua_setfield(state, -2, field.Key);
            }
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushValue(lua_State state, object? value)
    {
        switch (value)
        {
            case null:
                lua_pushnil(state);
                return;
            case bool boolean:
                lua_pushboolean(state, boolean ? 1 : 0);
                return;
            case string text:
                lua_pushstring(state, text);
                return;
            case byte or sbyte or short or ushort or int or uint or
                long or ulong or float or double or decimal:
                lua_pushnumber(state, Convert.ToDouble(value));
                return;
            case IDictionary<string, object?> fields:
                lua_createtable(state, 0, fields.Count);
                foreach (var field in fields)
                {
                    PushValue(state, field.Value);
                    lua_setfield(state, -2, field.Key);
                }
                return;
            case IList values:
                lua_createtable(state, values.Count, 0);
                for (var index = 0; index < values.Count; index++)
                {
                    PushValue(state, values[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return;
            default:
                lua_pushnil(state);
                return;
        }
    }

    private static int? LooseOptionalInt32(
        lua_State state,
        int index)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;

        if (lua_isnumber(state, index) == 0)
            return 0;

        var value = lua_tonumber(state, index);
        if (double.IsNaN(value))
            return int.MinValue;
        if (value < int.MinValue || value > int.MaxValue)
            return RaiseArgumentError(state, string.Empty);
        return unchecked((int)value);
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        return RequiredInt32(state, index, usage);
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return RaiseArgumentError(state, usage);

        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return RaiseArgumentError(state, usage);
        }
        return unchecked((int)value);
    }

    private static int RaiseArgumentError(
        lua_State state,
        string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static void SetNumber(
        lua_State state,
        string field,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }
}
