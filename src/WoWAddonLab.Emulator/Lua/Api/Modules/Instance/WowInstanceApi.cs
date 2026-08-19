using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowInstanceApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CanChangePlayerDifficulty",
        "CanMapChangeDifficulty",
        "CanShowResetInstances",
        "GetBaseDifficultyID",
        "GetDifficultyInfo",
        "GetDungeonDifficultyID",
        "GetInstanceBootTimeRemaining",
        "GetInstanceInfo",
        "GetInstanceLockTimeRemaining",
        "GetInstanceLockTimeRemainingEncounter",
        "GetLegacyRaidDifficultyID",
        "GetNumSavedInstances",
        "GetNumSavedWorldBosses",
        "GetRaidDifficultyID",
        "IsInInstance",
        "IsLegacyDifficulty",
        "RequestRaidInfo",
        "ResetInstances",
        "SetDungeonDifficultyID",
        "SetLegacyRaidDifficultyID",
        "SetRaidDifficultyID"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);

        lua_newtable(state);
        lua_pushstring(state, "GetModifiedInstanceInfoFromMapID");
        lua_pushcclosure(state, Callback, 1);
        lua_setfield(state, -2, "GetModifiedInstanceInfoFromMapID");
        lua_setglobal(state, "C_ModifiedInstance");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var instance = runtime.Instance;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "GetModifiedInstanceInfoFromMapID":
                return GetModifiedInstanceInfo(state, instance);
            case "CanChangePlayerDifficulty":
                lua_pushboolean(state, instance.CanChangeDifficulty ? 1 : 0);
                lua_pushboolean(state, instance.DifficultyChangeNotOnCooldown ? 1 : 0);
                return 2;
            case "CanMapChangeDifficulty":
                if (!TryReadOptionalInt32(state, 1, out var mapId))
                    return luaL_error(
                        state,
                        "Usage: local canChange = CanMapChangeDifficulty([mapID])");
                var canMapChange = mapId is { } specifiedMapId &&
                                   instance.MapDifficultyChanges.TryGetValue(
                                       specifiedMapId,
                                       out var mappedCanChange)
                    ? mappedCanChange
                    : instance.CanChangeDifficulty;
                lua_pushboolean(state, canMapChange ? 1 : 0);
                return 1;
            case "CanShowResetInstances":
                lua_pushboolean(state, instance.CanShowResetInstances ? 1 : 0);
                return 1;
            case "GetDungeonDifficultyID":
                lua_pushinteger(state, instance.DungeonDifficultyId);
                return 1;
            case "GetBaseDifficultyID":
                if (!TryReadRequiredUInt32(state, 1, out var baseDifficultyId))
                    return luaL_error(
                        state,
                        "Usage: local baseDifficultyID = GetBaseDifficultyID(difficultyID)");
                lua_pushinteger(state, baseDifficultyId == 233 ? 16 : baseDifficultyId);
                return 1;
            case "GetDifficultyInfo":
                if (!TryReadRequiredInt32(state, 1, out var difficultyId))
                    return luaL_error(
                        state,
                        "Usage: local name, instanceType, isHeroic, isChallengeMode, " +
                        "displayHeroic, displayMythic, toggleDifficultyID, isLFR, " +
                        "minPlayers, maxPlayers, isUserSelectable = GetDifficultyInfo(difficultyID)");
                if (!instance.Difficulties.TryGetValue(difficultyId, out var difficulty))
                    return 0;
                lua_pushstring(state, difficulty.Name);
                lua_pushstring(state, difficulty.InstanceType);
                lua_pushboolean(state, difficulty.IsHeroic ? 1 : 0);
                lua_pushboolean(state, difficulty.IsChallengeMode ? 1 : 0);
                lua_pushboolean(state, difficulty.DisplayHeroic ? 1 : 0);
                lua_pushboolean(state, difficulty.DisplayMythic ? 1 : 0);
                PushOptionalInteger(state, difficulty.ToggleDifficultyId);
                lua_pushboolean(state, difficulty.IsLookingForRaid ? 1 : 0);
                PushOptionalInteger(state, difficulty.MinimumPlayers);
                PushOptionalInteger(state, difficulty.MaximumPlayers);
                lua_pushboolean(state, difficulty.IsUserSelectable ? 1 : 0);
                return 11;
            case "GetInstanceBootTimeRemaining":
                lua_pushinteger(state, Math.Max(0, instance.InstanceBootTimeRemainingSeconds));
                return 1;
            case "GetRaidDifficultyID":
                PushOptionalInteger(state, instance.RaidDifficultyId);
                return 1;
            case "GetLegacyRaidDifficultyID":
                PushOptionalInteger(state, instance.LegacyRaidDifficultyId);
                return 1;
            case "GetNumSavedInstances":
                lua_pushinteger(state, instance.SavedInstanceCount);
                return 1;
            case "GetNumSavedWorldBosses":
                lua_pushinteger(state, instance.SavedWorldBossCount);
                return 1;
            case "IsInInstance":
                lua_pushboolean(state, instance.IsInInstance ? 1 : 0);
                lua_pushstring(state, instance.InstanceType);
                return 2;
            case "IsLegacyDifficulty":
                if (!TryReadRequiredUInt32(state, 1, out var legacyDifficultyId))
                    return luaL_error(
                        state,
                        "Usage: local result = IsLegacyDifficulty(difficultyID)");
                if (!instance.Difficulties.TryGetValue(
                        unchecked((int)legacyDifficultyId),
                        out var legacyDifficulty))
                {
                    lua_pushnil(state);
                    return 1;
                }
                lua_pushboolean(state, legacyDifficulty.IsLegacy ? 1 : 0);
                return 1;
            case "GetInstanceInfo":
                lua_pushstring(state, instance.Name);
                lua_pushstring(state, instance.InstanceType);
                lua_pushinteger(state, instance.DungeonDifficultyId);
                lua_pushstring(state, instance.DifficultyName);
                lua_pushinteger(state, instance.MaximumPlayers);
                lua_pushinteger(state, instance.DynamicDifficulty);
                if (instance.IsDynamic is { } isDynamic)
                    lua_pushboolean(state, isDynamic ? 1 : 0);
                else
                    lua_pushnil(state);
                lua_pushinteger(state, instance.InstanceId);
                lua_pushinteger(state, instance.InstanceGroupSize);
                PushOptionalInteger(state, instance.LfgDungeonId);
                lua_pushboolean(state, instance.IsRaid ? 1 : 0);
                return 11;
            case "GetInstanceLockTimeRemaining":
                lua_pushinteger(state, Math.Max(0, instance.InstanceLockTimeRemainingSeconds));
                lua_pushboolean(state, instance.IsInstanceLockExtending ? 1 : 0);
                lua_pushinteger(state, instance.InstanceLockEncounterCount);
                lua_pushinteger(state, instance.InstanceLockCompletedEncounterCount);
                return 4;
            case "GetInstanceLockTimeRemainingEncounter":
                if (!TryReadRequiredOneBasedIndex(state, 1, out var encounterIndex))
                    return luaL_error(
                        state,
                        "Usage: local encounterName, texture, isKilled, ineligible = " +
                        "GetInstanceLockTimeRemainingEncounter(encounterIndex)");
                if (encounterIndex > instance.LockEncounters.Count)
                    return 0;
                var encounter = instance.LockEncounters[(int)encounterIndex - 1];
                PushOptionalString(state, encounter.EncounterName);
                PushOptionalString(state, encounter.Texture);
                lua_pushboolean(state, encounter.IsKilled ? 1 : 0);
                lua_pushboolean(state, encounter.IsIneligible ? 1 : 0);
                return 4;
            case "SetDungeonDifficultyID":
                if (!TryReadRequiredUInt32(state, 1, out var dungeonDifficultyId))
                    return luaL_error(state, "Usage: SetDungeonDifficultyID(difficultyID)");
                if (instance.Difficulties.TryGetValue(
                        unchecked((int)dungeonDifficultyId),
                        out var dungeonDifficulty) &&
                    dungeonDifficulty.IsUserSelectable)
                {
                    instance.DungeonDifficultyId = unchecked((int)dungeonDifficultyId);
                    runtime.TriggerEvent("PLAYER_DIFFICULTY_CHANGED");
                }
                return 0;
            case "SetRaidDifficultyID":
                if (!TryReadRequiredUInt32(state, 1, out var raidDifficultyId) ||
                    !TryReadOptionalBoolean(state, 2, out _))
                    return luaL_error(
                        state,
                        "Usage: SetRaidDifficultyID(difficultyID [, force])");
                instance.RaidDifficultyId = unchecked((int)raidDifficultyId);
                runtime.TriggerEvent("PLAYER_DIFFICULTY_CHANGED");
                return 0;
            case "SetLegacyRaidDifficultyID":
                if (!TryReadRequiredUInt32(state, 1, out var legacyRaidDifficultyId) ||
                    !TryReadOptionalBoolean(state, 2, out _))
                    return luaL_error(
                        state,
                        "Usage: SetLegacyRaidDifficultyID(difficultyID [, force])");
                instance.LegacyRaidDifficultyId = unchecked((int)legacyRaidDifficultyId);
                runtime.TriggerEvent("PLAYER_DIFFICULTY_CHANGED");
                return 0;
            case "RequestRaidInfo":
                instance.RaidInfoRequestCount++;
                return 0;
            case "ResetInstances":
                return 0;
            default:
                return 0;
        }
    }

    private static int GetModifiedInstanceInfo(
        lua_State state,
        WowInstanceState instance)
    {
        if (!TryReadRequiredInt32(state, 1, out var mapId))
            return luaL_error(
                state,
                "Usage: local info = C_ModifiedInstance." +
                "GetModifiedInstanceInfoFromMapID(mapID)");
        if (!instance.ModifiedInstances.TryGetValue(mapId, out var info))
            return 0;

        lua_newtable(state);
        SetOptionalInteger(state, "lfrItemLevel", info.LfrItemLevel);
        SetOptionalInteger(state, "normalItemLevel", info.NormalItemLevel);
        SetOptionalInteger(state, "heroicItemLevel", info.HeroicItemLevel);
        SetOptionalInteger(state, "mythicItemLevel", info.MythicItemLevel);
        SetOptionalString(state, "uiTextureKit", info.UiTextureKit);
        SetOptionalString(state, "description", info.Description);
        return 1;
    }

    private static bool TryReadRequiredInt32(
        lua_State state,
        int index,
        out int value)
    {
        value = 0;
        if (lua_type(state, index) != LUA_TNUMBER)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number != Math.Truncate(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
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
        if (lua_type(state, index) != LUA_TNUMBER)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number != Math.Truncate(number) ||
            number < uint.MinValue ||
            number > uint.MaxValue)
            return false;
        value = (uint)number;
        return true;
    }

    private static bool TryReadRequiredOneBasedIndex(
        lua_State state,
        int index,
        out uint value) =>
        TryReadRequiredUInt32(state, index, out value) && value > 0;

    private static bool TryReadOptionalInt32(
        lua_State state,
        int index,
        out int? value)
    {
        value = null;
        if (lua_gettop(state) < index || lua_isnil(state, index) != 0)
            return true;
        if (!TryReadRequiredInt32(state, index, out var parsed))
            return false;
        value = parsed;
        return true;
    }

    private static bool TryReadOptionalBoolean(
        lua_State state,
        int index,
        out bool value)
    {
        value = false;
        if (lua_gettop(state) < index || lua_isnil(state, index) != 0)
            return true;
        value = lua_toboolean(state, index) != 0;
        return true;
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value is { } number)
            lua_pushinteger(state, number);
        else
            lua_pushnil(state);
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is not null)
            lua_pushstring(state, value);
        else
            lua_pushnil(state);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string field,
        int? value)
    {
        PushOptionalInteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        PushOptionalString(state, value);
        lua_setfield(state, -2, field);
    }
}
