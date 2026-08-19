using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowLootHistoryApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetAllEncounterInfos", "GetInfoForEncounter", "GetLootHistoryTime",
        "GetSortedDropsForEncounter", "GetSortedInfoForDrop"
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
        lua_setglobal(state, "C_LootHistory");
    }

    private static int Dispatch(lua_State state)
    {
        var history = LuaBindings.GetRuntime(state).LootHistory;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetAllEncounterInfos":
                lua_newtable(state);
                for (var index = 0; index < history.Encounters.Count; index++)
                {
                    PushEncounter(state, history.Encounters[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "GetInfoForEncounter":
            {
                var encounterId = RequiredInt32(state, 1, operation);
                var encounter = history.Encounters.FirstOrDefault(
                    value => value.EncounterId == encounterId);
                if (encounter is null)
                    lua_pushnil(state);
                else
                    PushEncounter(state, encounter);
                return 1;
            }
            case "GetLootHistoryTime":
                lua_pushinteger(state, history.Time);
                return 1;
            case "GetSortedDropsForEncounter":
            {
                var encounterId = RequiredInt32(state, 1, operation);
                if (!history.SortedDropsByEncounterId.TryGetValue(
                        encounterId,
                        out var drops))
                {
                    lua_pushnil(state);
                    return 1;
                }

                lua_newtable(state);
                for (var index = 0; index < drops.Count; index++)
                {
                    PushDrop(state, drops[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "GetSortedInfoForDrop":
            {
                var encounterId = RequiredInt32(state, 1, operation);
                var lootListId = RequiredByte(state, 2, operation);
                var drop = history.SortedDropsByEncounterId.TryGetValue(
                    encounterId,
                    out var drops)
                    ? drops.FirstOrDefault(value => value.LootListId == lootListId)
                    : null;
                if (drop is null)
                    lua_pushnil(state);
                else
                    PushDrop(state, drop);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static void PushEncounter(
        lua_State state,
        WowLootEncounterState encounter)
    {
        lua_newtable(state);
        SetString(state, "encounterName", encounter.EncounterName);
        SetInteger(state, "encounterID", encounter.EncounterId);
        SetInteger(state, "startTime", encounter.StartTime);
        SetInteger(state, "duration", encounter.Duration);
    }

    private static void PushDrop(lua_State state, WowLootDropState drop)
    {
        lua_newtable(state);
        SetInteger(state, "lootListID", drop.LootListId);
        SetString(state, "itemHyperlink", drop.ItemHyperlink);
        SetUnsigned(state, "playerRollState", drop.PlayerRollState);
        PushOptionalPlayerInfo(state, drop.CurrentLeader);
        lua_setfield(state, -2, "currentLeader");
        SetBoolean(state, "isTied", drop.IsTied);
        PushOptionalPlayerInfo(state, drop.Winner);
        lua_setfield(state, -2, "winner");
        SetBoolean(state, "allPassed", drop.AllPassed);
        lua_newtable(state);
        for (var index = 0; index < drop.RollInfos.Count; index++)
        {
            PushPlayerInfo(state, drop.RollInfos[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "rollInfos");
        SetInteger(state, "startTime", drop.StartTime);
        SetInteger(state, "duration", drop.Duration);
    }

    private static void PushOptionalPlayerInfo(
        lua_State state,
        WowLootPlayerInfoState? player)
    {
        if (player is null)
            lua_pushnil(state);
        else
            PushPlayerInfo(state, player);
    }

    private static void PushPlayerInfo(
        lua_State state,
        WowLootPlayerInfoState player)
    {
        lua_newtable(state);
        SetString(state, "playerName", player.PlayerName);
        SetString(state, "playerGUID", player.PlayerGuid);
        SetString(state, "playerClass", player.PlayerClass);
        SetBoolean(state, "isSelf", player.IsSelf);
        SetUnsigned(state, "state", player.State);
        SetBoolean(state, "isWinner", player.IsWinner);
        if (player.Roll is { } roll)
            lua_pushinteger(state, roll);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, "roll");
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string operation)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, $"Usage: C_LootHistory.{operation}(...)");
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, $"Usage: C_LootHistory.{operation}(...)");
        return unchecked((int)value);
    }

    private static byte RequiredByte(
        lua_State state,
        int index,
        string operation)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, $"Usage: C_LootHistory.{operation}(...)");
            return 0;
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < byte.MinValue || value > byte.MaxValue)
        {
            luaL_error(state, $"Usage: C_LootHistory.{operation}(...)");
            return 0;
        }
        return unchecked((byte)value);
    }

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetInteger(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetUnsigned(lua_State state, string name, uint value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBoolean(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }
}
