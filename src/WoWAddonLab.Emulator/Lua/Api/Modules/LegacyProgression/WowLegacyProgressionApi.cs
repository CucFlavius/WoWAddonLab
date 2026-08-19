using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowLegacyProgressionApi : LuaApiModule
{
    private const string AzeriteEnabledUsage =
        "Usage: local isEnabled = " +
        "C_AzeriteItem.IsAzeriteItemEnabled(azeriteItemLocation)";

    private const string MapUiInfoUsage =
        "Usage: local name, id, timeLimit, texture, backgroundTexture, mapID = " +
        "C_ChallengeMode.GetMapUIInfo(mapChallengeModeID)";

    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "FindActiveAzeriteItem",
                     "IsAzeriteItemAtMaxLevel",
                     "IsAzeriteItemEnabled"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_AzeriteItem");

        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetActiveChallengeMapID",
                     "GetActiveKeystoneInfo",
                     "CloseKeystoneFrame",
                     "ClearKeystone",
                     "GetLeaverPenaltyWarningTimeLeft",
                     "GetMapTable",
                     "GetMapUIInfo"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_ChallengeMode");
    }

    private static int Dispatch(lua_State state)
    {
        var progression = LuaBindings.GetRuntime(state).LegacyProgression;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "FindActiveAzeriteItem":
                if (progression.ActiveAzeriteItemLocation is not { } activeLocation)
                    return 0;
                WowItemApi.PushItemLocation(state, activeLocation);
                return 1;
            case "IsAzeriteItemAtMaxLevel":
                lua_pushboolean(
                    state,
                    progression.IsAzeriteItemAtMaxLevel ? 1 : 0);
                return 1;
            case "IsAzeriteItemEnabled":
            {
                var location = WowItemApi.RequiredItemLocation(
                    state,
                    AzeriteEnabledUsage);
                lua_pushboolean(
                    state,
                    progression.EnabledAzeriteItemLocations.Contains(location)
                        ? 1
                        : 0);
                return 1;
            }
            case "GetLeaverPenaltyWarningTimeLeft":
                lua_pushnumber(
                    state,
                    Math.Max(
                        0,
                        progression.ChallengeLeaverPenaltyWarningTimeLeft));
                return 1;
            case "GetActiveChallengeMapID":
                if (progression.ActiveChallengeMapId is { } activeMapId)
                    lua_pushnumber(state, activeMapId);
                else
                    lua_pushnil(state);
                return 1;
            case "GetActiveKeystoneInfo":
                lua_pushnumber(state, progression.ActiveKeystoneLevel);
                lua_newtable(state);
                for (var index = 0; index < progression.ActiveKeystoneAffixIds.Count; index++)
                {
                    lua_pushnumber(state, progression.ActiveKeystoneAffixIds[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                lua_pushboolean(state, progression.ActiveKeystoneWasEnergized ? 1 : 0);
                return 3;
            case "GetMapTable":
                PushMapTable(state, progression.ChallengeMaps.Keys);
                return 1;
            case "GetMapUIInfo":
            {
                var mapId = RequiredInt32(state, MapUiInfoUsage);
                if (!progression.ChallengeMaps.TryGetValue(mapId, out var map))
                    return 0;
                PushOptionalString(state, map.Name);
                lua_pushnumber(state, map.Id);
                lua_pushnumber(state, map.TimeLimitSeconds);
                PushOptionalInteger(state, map.TextureFileId);
                lua_pushnumber(state, map.BackgroundTextureFileId);
                lua_pushnumber(state, map.MapId);
                return 6;
            }
            case "CloseKeystoneFrame":
                progression.IsKeystoneFrameOpen = false;
                return 0;
            case "ClearKeystone":
                progression.SlottedKeystoneLocation = null;
                return 0;
            default:
                return 0;
        }
    }

    private static int RequiredInt32(lua_State state, string usage)
    {
        if (lua_isnumber(state, 1) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var number = lua_tonumber(state, 1);
        if (!double.IsFinite(number) ||
            number is < int.MinValue or > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (int)number;
    }

    private static void PushMapTable(
        lua_State state,
        IEnumerable<int> mapIds)
    {
        lua_newtable(state);
        var index = 1;
        foreach (var mapId in mapIds)
        {
            lua_pushnumber(state, mapId);
            lua_rawseti(state, -2, index++);
        }
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value is { } integer)
            lua_pushnumber(state, integer);
        else
            lua_pushnil(state);
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }
}
