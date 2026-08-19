using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowDamageMeterApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetAvailableCombatSessions", "GetCombatSessionFromID",
        "GetCombatSessionFromType", "GetCombatSessionSourceFromID",
        "GetCombatSessionSourceFromType", "GetSessionDurationSeconds",
        "IsDamageMeterAvailable", "ResetAllCombatSessions"
    ];

    private static readonly WowDamageMeterCombatSession EmptySession =
        new(Array.Empty<WowDamageMeterCombatSource>(), 0, 0, null);

    private static readonly WowDamageMeterCombatSessionSource EmptySource =
        new(Array.Empty<WowDamageMeterCombatSpell>(), 0, 0);

    public override void Register(lua_State state)
    {
        RegisterEnums(state);

        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_DamageMeter");
    }

    private static int Dispatch(lua_State state)
    {
        var damageMeter = LuaBindings.GetRuntime(state).DamageMeter;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "GetAvailableCombatSessions":
                lua_createtable(
                    state,
                    damageMeter.AvailableSessions.Count,
                    0);
                for (var index = 0;
                     index < damageMeter.AvailableSessions.Count;
                     index++)
                {
                    PushAvailableSession(
                        state,
                        damageMeter.AvailableSessions[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "GetCombatSessionFromID":
            {
                var usage = Usage(
                    operation,
                    "sessionID, type");
                var sessionId = RequiredUInt32(state, 1, usage);
                var type = RequiredDamageMeterType(state, 2, usage);
                damageMeter.SessionsById.TryGetValue(
                    (sessionId, type),
                    out var session);
                PushCombatSession(state, session ?? EmptySession);
                return 1;
            }
            case "GetCombatSessionFromType":
            {
                var usage = Usage(
                    operation,
                    "sessionType, type");
                var sessionType = RequiredSessionType(state, 1, usage);
                var type = RequiredDamageMeterType(state, 2, usage);
                WowDamageMeterCombatSession? session = null;
                if (sessionType != WowDamageMeterSessionType.Expired)
                {
                    damageMeter.SessionsByType.TryGetValue(
                        (sessionType, type),
                        out session);
                }
                PushCombatSession(state, session ?? EmptySession);
                return 1;
            }
            case "GetCombatSessionSourceFromID":
            {
                var usage = Usage(
                    operation,
                    "sessionID, type [, sourceGUID, sourceCreatureID]");
                var sessionId = RequiredUInt32(state, 1, usage);
                var type = RequiredDamageMeterType(state, 2, usage);
                var sourceGuid = OptionalGuid(state, 3, usage);
                var sourceCreatureId = OptionalInt32(state, 4, usage);
                damageMeter.SourcesById.TryGetValue(
                    (sessionId, type, sourceGuid, sourceCreatureId),
                    out var source);
                PushCombatSessionSource(state, source ?? EmptySource);
                return 1;
            }
            case "GetCombatSessionSourceFromType":
            {
                var usage = Usage(
                    operation,
                    "sessionType, type [, sourceGUID, sourceCreatureID]");
                var sessionType = RequiredSessionType(state, 1, usage);
                var type = RequiredDamageMeterType(state, 2, usage);
                var sourceGuid = OptionalGuid(state, 3, usage);
                var sourceCreatureId = OptionalInt32(state, 4, usage);
                WowDamageMeterCombatSessionSource? source = null;
                if (sessionType != WowDamageMeterSessionType.Expired)
                {
                    damageMeter.SourcesByType.TryGetValue(
                        (sessionType, type, sourceGuid, sourceCreatureId),
                        out source);
                }
                PushCombatSessionSource(state, source ?? EmptySource);
                return 1;
            }
            case "GetSessionDurationSeconds":
            {
                var sessionType = RequiredSessionType(
                    state,
                    1,
                    Usage(operation, "sessionType"));
                if (sessionType != WowDamageMeterSessionType.Expired &&
                    damageMeter.SessionDurations.TryGetValue(
                        sessionType,
                        out var duration))
                {
                    lua_pushnumber(state, duration);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            case "IsDamageMeterAvailable":
                lua_pushboolean(state, damageMeter.IsAvailable ? 1 : 0);
                lua_pushstring(state, damageMeter.AvailabilityReason);
                return 2;
            case "ResetAllCombatSessions":
                damageMeter.ResetAllCombatSessions();
                return 0;
            default:
                return 0;
        }
    }

    private static string Usage(string operation, string arguments) =>
        $"Usage: C_DamageMeter.{operation}({arguments})";

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < uint.MinValue ||
            number > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (uint)number;
    }

    private static int RequiredNumericEnum(
        lua_State state,
        int index,
        int maximum,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }

        var value = (int)number;
        if (value < 0 || value > maximum)
        {
            luaL_error(state, usage);
            return 0;
        }
        return value;
    }

    private static WowDamageMeterSessionType RequiredSessionType(
        lua_State state,
        int index,
        string usage) =>
        (WowDamageMeterSessionType)RequiredNumericEnum(
            state,
            index,
            2,
            usage);

    private static WowDamageMeterType RequiredDamageMeterType(
        lua_State state,
        int index,
        string usage) =>
        (WowDamageMeterType)RequiredNumericEnum(
            state,
            index,
            10,
            usage);

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return null;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            luaL_error(state, usage);
            return null;
        }
        return (int)number;
    }

    private static string? OptionalGuid(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return null;
        }
        return lua_tostring(state, index);
    }

    private static void PushAvailableSession(
        lua_State state,
        WowDamageMeterAvailableCombatSession session)
    {
        lua_createtable(state, 0, 3);
        lua_pushinteger(state, session.SessionId);
        lua_setfield(state, -2, "sessionID");
        PushOptionalString(state, session.Name);
        lua_setfield(state, -2, "name");
        PushOptionalNumber(state, session.DurationSeconds);
        lua_setfield(state, -2, "durationSeconds");
    }

    private static void PushCombatSession(
        lua_State state,
        WowDamageMeterCombatSession session)
    {
        lua_createtable(state, 0, 4);
        lua_createtable(state, session.CombatSources.Count, 0);
        for (var index = 0; index < session.CombatSources.Count; index++)
        {
            PushCombatSource(state, session.CombatSources[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "combatSources");
        lua_pushinteger(state, session.MaxAmount);
        lua_setfield(state, -2, "maxAmount");
        lua_pushinteger(state, session.TotalAmount);
        lua_setfield(state, -2, "totalAmount");
        PushOptionalNumber(state, session.DurationSeconds);
        lua_setfield(state, -2, "durationSeconds");
    }

    private static void PushCombatSource(
        lua_State state,
        WowDamageMeterCombatSource source)
    {
        lua_createtable(state, 0, 13);
        PushOptionalString(state, source.SourceGuid);
        lua_setfield(state, -2, "sourceGUID");
        PushOptionalInteger(state, source.SourceCreatureId);
        lua_setfield(state, -2, "sourceCreatureID");
        PushOptionalString(state, source.Name);
        lua_setfield(state, -2, "name");
        PushOptionalString(state, source.ClassFilename);
        lua_setfield(state, -2, "classFilename");
        lua_pushinteger(state, source.SpecIconId);
        lua_setfield(state, -2, "specIconID");
        lua_pushinteger(state, source.TotalAmount);
        lua_setfield(state, -2, "totalAmount");
        lua_pushnumber(state, source.AmountPerSecond);
        lua_setfield(state, -2, "amountPerSecond");
        lua_pushboolean(state, source.IsLocalPlayer ? 1 : 0);
        lua_setfield(state, -2, "isLocalPlayer");
        lua_pushnumber(state, source.DeathRecapId);
        lua_setfield(state, -2, "deathRecapID");
        lua_pushnumber(state, source.DeathTimeSeconds);
        lua_setfield(state, -2, "deathTimeSeconds");
        PushOptionalString(state, source.Classification);
        lua_setfield(state, -2, "classification");
        lua_pushinteger(state, (int)source.SourceDisplayType);
        lua_setfield(state, -2, "sourceDisplayType");
        PushOptionalString(state, source.FactionGroup);
        lua_setfield(state, -2, "factionGroup");
    }

    private static void PushCombatSessionSource(
        lua_State state,
        WowDamageMeterCombatSessionSource source)
    {
        lua_createtable(state, 0, 3);
        lua_createtable(state, source.CombatSpells.Count, 0);
        for (var index = 0; index < source.CombatSpells.Count; index++)
        {
            PushCombatSpell(state, source.CombatSpells[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "combatSpells");
        lua_pushinteger(state, source.MaxAmount);
        lua_setfield(state, -2, "maxAmount");
        lua_pushinteger(state, source.TotalAmount);
        lua_setfield(state, -2, "totalAmount");
    }

    private static void PushCombatSpell(
        lua_State state,
        WowDamageMeterCombatSpell spell)
    {
        lua_createtable(state, 0, 8);
        lua_pushnumber(state, spell.SpellId);
        lua_setfield(state, -2, "spellID");
        lua_pushinteger(state, spell.TotalAmount);
        lua_setfield(state, -2, "totalAmount");
        lua_pushnumber(state, spell.AmountPerSecond);
        lua_setfield(state, -2, "amountPerSecond");
        PushOptionalString(state, spell.CreatureName);
        lua_setfield(state, -2, "creatureName");
        lua_pushinteger(state, spell.OverkillAmount);
        lua_setfield(state, -2, "overkillAmount");
        lua_pushboolean(state, spell.IsAvoidable ? 1 : 0);
        lua_setfield(state, -2, "isAvoidable");
        lua_pushboolean(state, spell.IsDeadly ? 1 : 0);
        lua_setfield(state, -2, "isDeadly");
        PushCombatSpellDetails(state, spell.CombatSpellDetails);
        lua_setfield(state, -2, "combatSpellDetails");
    }

    private static void PushCombatSpellDetails(
        lua_State state,
        WowDamageMeterCombatSpellDetails details)
    {
        lua_createtable(state, 0, 7);
        PushOptionalString(state, details.UnitName);
        lua_setfield(state, -2, "unitName");
        PushOptionalString(state, details.UnitClassFilename);
        lua_setfield(state, -2, "unitClassFilename");
        PushOptionalString(state, details.Classification);
        lua_setfield(state, -2, "classification");
        lua_pushboolean(state, details.IsPet ? 1 : 0);
        lua_setfield(state, -2, "isPet");
        lua_pushboolean(state, details.IsMob ? 1 : 0);
        lua_setfield(state, -2, "isMob");
        lua_pushinteger(state, details.Amount);
        lua_setfield(state, -2, "amount");
        lua_pushinteger(state, details.SpecIconId);
        lua_setfield(state, -2, "specIconID");
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushinteger(state, value.Value);
    }

    private static void PushOptionalNumber(lua_State state, double? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushnumber(state, value.Value);
    }

    private static void RegisterEnums(lua_State state)
    {
        EnsureGlobalTable(state, "Enum");

        SetEnum(
            state,
            "DamageMeterCombineSessionType",
            [
                ("None", 0), ("ChallengeMode", 1), ("Arena", 2),
                ("ArenaMultiRound", 3)
            ]);
        SetEnumMeta(state, "DamageMeterCombineSessionTypeMeta", 4, 0, 3);

        SetEnum(
            state,
            "DamageMeterOverrideType",
            [
                ("Ignore", 0), ("AllowFriendlyFire", 1),
                ("RedirectSourceToOwner", 2),
                ("RedirectSourceToAuraCaster", 3),
                ("IgnoreForAbsorbSpell", 4)
            ]);
        SetEnumMeta(state, "DamageMeterOverrideTypeMeta", 5, 0, 4);

        SetEnum(
            state,
            "DamageMeterSessionType",
            [("Overall", 0), ("Current", 1), ("Expired", 2)]);
        SetEnumMeta(state, "DamageMeterSessionTypeMeta", 3, 0, 2);

        SetEnum(
            state,
            "DamageMeterSourceDisplayType",
            [("None", 0), ("Ally", 1), ("Enemy", 2)]);
        SetEnumMeta(state, "DamageMeterSourceDisplayTypeMeta", 3, 0, 2);

        SetEnum(
            state,
            "DamageMeterSpellDetailsDisplayType",
            [
                ("SpellCasted", 0), ("UnitSpecificSpellCasted", 1),
                ("SpellAffected", 2), ("Deaths", 3),
                ("EnemyDamageTaken", 4)
            ]);
        SetEnumMeta(
            state,
            "DamageMeterSpellDetailsDisplayTypeMeta",
            5,
            0,
            4);

        SetEnum(
            state,
            "DamageMeterStorageType",
            [
                ("Damage", 0), ("HealingAndAbsorbs", 1), ("Absorbs", 2),
                ("Interrupts", 3), ("Dispels", 4), ("DamageTaken", 5),
                ("AvoidableDamageTaken", 6), ("Deaths", 7),
                ("EnemyDamageTaken", 8)
            ]);
        SetEnumMeta(state, "DamageMeterStorageTypeMeta", 9, 0, 8);

        SetEnum(
            state,
            "DamageMeterType",
            [
                ("DamageDone", 0), ("Dps", 1), ("HealingDone", 2),
                ("Hps", 3), ("Absorbs", 4), ("Interrupts", 5),
                ("Dispels", 6), ("DamageTaken", 7),
                ("AvoidableDamageTaken", 8), ("Deaths", 9),
                ("EnemyDamageTaken", 10)
            ]);
        SetEnumMeta(state, "DamageMeterTypeMeta", 11, 0, 10);
        lua_pop(state, 1);
    }

    private static void EnsureGlobalTable(lua_State state, string name)
    {
        lua_getglobal(state, name);
        if (lua_istable(state, -1) != 0)
            return;
        lua_pop(state, 1);
        lua_newtable(state);
        lua_pushvalue(state, -1);
        lua_setglobal(state, name);
    }

    private static void SetEnum(
        lua_State state,
        string name,
        IReadOnlyList<(string Name, int Value)> members)
    {
        lua_createtable(state, 0, members.Count);
        foreach (var (memberName, value) in members)
        {
            lua_pushinteger(state, value);
            lua_setfield(state, -2, memberName);
        }
        lua_setfield(state, -2, name);
    }

    private static void SetEnumMeta(
        lua_State state,
        string name,
        int count,
        int minimum,
        int maximum)
    {
        lua_createtable(state, 0, 3);
        lua_pushinteger(state, count);
        lua_setfield(state, -2, "NumValues");
        lua_pushinteger(state, minimum);
        lua_setfield(state, -2, "MinValue");
        lua_pushinteger(state, maximum);
        lua_setfield(state, -2, "MaxValue");
        lua_setfield(state, -2, name);
    }
}
