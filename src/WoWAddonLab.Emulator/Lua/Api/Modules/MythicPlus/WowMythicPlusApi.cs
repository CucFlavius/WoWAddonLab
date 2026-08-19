using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowMythicPlusApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetCurrentAffixes", "GetCurrentSeason", "GetCurrentSeasonValues",
        "GetCurrentUIDisplaySeason", "GetEndOfRunGearSequenceLevel",
        "GetLastWeeklyBestInformation", "GetOwnedKeystoneChallengeMapID",
        "GetOwnedKeystoneLevel", "GetOwnedKeystoneMapID",
        "GetRewardLevelForDifficultyLevel", "GetRewardLevelFromKeystoneLevel",
        "GetRunHistory", "GetSeasonBestAffixScoreInfoForMap",
        "GetSeasonBestForMap", "GetSeasonBestMythicRatingFromThisExpansion",
        "GetWeeklyBestForMap", "GetWeeklyChestRewardLevel", "IsMythicPlusActive",
        "RequestCurrentAffixes", "RequestMapInfo", "RequestRewards"
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
        lua_setglobal(state, "C_MythicPlus");
    }

    private static int Dispatch(lua_State state)
    {
        var mythic = LuaBindings.GetRuntime(state).MythicPlus;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetCurrentAffixes":
                return PushCurrentAffixes(state, mythic.CurrentAffixes);
            case "GetCurrentSeason":
                lua_pushinteger(state, mythic.CurrentSeason);
                return 1;
            case "GetCurrentSeasonValues":
                lua_pushinteger(state, mythic.CurrentSeasonValues.DisplaySeason);
                lua_pushinteger(state, mythic.CurrentSeasonValues.MilestoneSeason);
                lua_pushinteger(state, mythic.CurrentSeasonValues.RewardSeason);
                return 3;
            case "GetCurrentUIDisplaySeason":
                PushOptionalInteger(state, mythic.CurrentUiDisplaySeason);
                return 1;
            case "GetEndOfRunGearSequenceLevel":
            {
                var level = RequiredInt32(
                    state,
                    1,
                    "Usage: local sequenceLevel = " +
                    "C_MythicPlus.GetEndOfRunGearSequenceLevel(keystoneLevel)");
                if (level < 2 ||
                    !mythic.EndOfRunGearSequenceLevels.TryGetValue(level, out var sequence))
                    lua_pushnil(state);
                else
                    PushOptionalInteger(state, sequence);
                return 1;
            }
            case "GetLastWeeklyBestInformation":
                return 0;
            case "GetOwnedKeystoneChallengeMapID":
                return PushZeroOrOneInteger(state, mythic.OwnedKeystoneChallengeMapId);
            case "GetOwnedKeystoneLevel":
                return PushZeroOrOneInteger(state, mythic.OwnedKeystoneLevel);
            case "GetOwnedKeystoneMapID":
                return PushZeroOrOneInteger(state, mythic.OwnedKeystoneMapId);
            case "GetRewardLevelForDifficultyLevel":
            {
                var difficulty = RequiredInt32(
                    state,
                    1,
                    "Usage: local weeklyRewardLevel, endOfRunRewardLevel = " +
                    "C_MythicPlus.GetRewardLevelForDifficultyLevel(difficultyLevel)");
                mythic.RewardLevelsByDifficulty.TryGetValue(difficulty, out var levels);
                levels ??= new WowMythicPlusRewardLevelsState(0, 0);
                lua_pushinteger(state, levels.WeeklyRewardLevel);
                lua_pushinteger(state, levels.EndOfRunRewardLevel);
                return 2;
            }
            case "GetRewardLevelFromKeystoneLevel":
            {
                var level = RequiredInt32(
                    state,
                    1,
                    "Usage: local rewardLevel = " +
                    "C_MythicPlus.GetRewardLevelFromKeystoneLevel(keystoneLevel)");
                if (level < 0)
                    lua_pushnil(state);
                else if (mythic.RewardLevelsByKeystoneLevel.TryGetValue(level, out var reward))
                    PushOptionalInteger(state, reward);
                else
                    lua_pushinteger(state, 0);
                return 1;
            }
            case "GetRunHistory":
                return PushRunHistory(state, mythic);
            case "GetSeasonBestAffixScoreInfoForMap":
            {
                var mapId = RequiredMapId(
                    state,
                    "Usage: local affixScores, bestOverAllScore = " +
                    "C_MythicPlus.GetSeasonBestAffixScoreInfoForMap(mapChallengeModeID)");
                if (!mythic.SeasonBestAffixScores.TryGetValue(mapId, out var info))
                    return 0;
                PushAffixScores(state, info.AffixScores);
                lua_pushinteger(state, info.BestOverallScore);
                return 2;
            }
            case "GetSeasonBestForMap":
            {
                var mapId = RequiredMapId(
                    state,
                    "Usage: local intimeInfo, overtimeInfo = " +
                    "C_MythicPlus.GetSeasonBestForMap(mapChallengeModeID)");
                mythic.SeasonBestsByMap.TryGetValue(mapId, out var best);
                PushOptionalBestRun(state, best?.InTime);
                PushOptionalBestRun(state, best?.OverTime);
                return 2;
            }
            case "GetSeasonBestMythicRatingFromThisExpansion":
                if (mythic.SeasonBestRatingFromExpansion is not { } rating)
                    return 0;
                lua_pushinteger(state, rating.BestSeason);
                lua_pushinteger(state, rating.BestRating);
                return 2;
            case "GetWeeklyBestForMap":
            {
                var mapId = RequiredMapId(
                    state,
                    "Usage: local durationSec, level, completionDate, affixIDs, " +
                    "members, dungeonScore = " +
                    "C_MythicPlus.GetWeeklyBestForMap(mapChallengeModeID)");
                if (!mythic.WeeklyBestsByMap.TryGetValue(mapId, out var best))
                    return 0;
                PushBestRunValues(state, best);
                return 6;
            }
            case "GetWeeklyChestRewardLevel":
                lua_pushinteger(state, mythic.WeeklyChestReward.CurrentWeekBest);
                lua_pushinteger(state, mythic.WeeklyChestReward.WeeklyRewardLevel);
                lua_pushinteger(state, mythic.WeeklyChestReward.NextDifficultyWeeklyRewardLevel);
                lua_pushinteger(state, mythic.WeeklyChestReward.NextBestLevel);
                return 4;
            case "IsMythicPlusActive":
                PushBoolean(state, mythic.IsActive);
                return 1;
            case "RequestCurrentAffixes":
                mythic.CurrentAffixRequestCount++;
                return 0;
            case "RequestMapInfo":
                mythic.MapInfoRequestCount++;
                return 0;
            case "RequestRewards":
                mythic.RewardsRequestCount++;
                return 0;
            default:
                return 0;
        }
    }

    private static int PushCurrentAffixes(
        lua_State state,
        IReadOnlyList<WowMythicPlusAffixState>? affixes)
    {
        if (affixes is null)
            return 0;
        lua_newtable(state);
        for (var index = 0; index < affixes.Count; index++)
        {
            lua_newtable(state);
            SetInteger(state, "id", affixes[index].Id);
            SetInteger(state, "seasonID", affixes[index].SeasonId);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int PushRunHistory(lua_State state, WowMythicPlusState mythic)
    {
        var includePreviousWeeks = OptionalTruthyBoolean(state, 1, true);
        var includeIncompleteRuns = OptionalTruthyBoolean(state, 2, true);
        var currentSeasonOnly = OptionalTruthyBoolean(state, 3, true);
        lua_newtable(state);
        var outputIndex = 1;
        foreach (var run in mythic.RunHistory)
        {
            if ((!run.ThisWeek && !includePreviousWeeks) ||
                (!run.Completed && !includeIncompleteRuns) ||
                (currentSeasonOnly && run.Season != mythic.CurrentSeason))
                continue;
            lua_newtable(state);
            SetInteger(state, "mapChallengeModeID", run.MapChallengeModeId);
            SetInteger(state, "level", run.Level);
            SetBoolean(state, "thisWeek", run.ThisWeek);
            SetBoolean(state, "completed", run.Completed);
            SetInteger(state, "runScore", run.RunScore);
            SetInteger(state, "durationSec", run.DurationSec);
            PushDate(state, run.CompletionDate);
            lua_setfield(state, -2, "completionDate");
            SetInteger(state, "season", run.Season);
            lua_rawseti(state, -2, outputIndex++);
        }
        return 1;
    }

    private static void PushAffixScores(
        lua_State state,
        IReadOnlyList<WowMythicPlusAffixScoreState> scores)
    {
        lua_newtable(state);
        for (var index = 0; index < scores.Count; index++)
        {
            var score = scores[index];
            lua_newtable(state);
            SetString(state, "name", score.Name);
            SetInteger(state, "score", score.Score);
            SetInteger(state, "level", score.Level);
            SetInteger(state, "durationSec", score.DurationSec);
            SetBoolean(state, "overTime", score.OverTime);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushOptionalBestRun(
        lua_State state,
        WowMythicPlusBestRunState? best)
    {
        if (best is null)
        {
            lua_pushnil(state);
            return;
        }
        lua_newtable(state);
        SetInteger(state, "durationSec", best.DurationSec);
        SetInteger(state, "level", best.Level);
        PushDate(state, best.CompletionDate);
        lua_setfield(state, -2, "completionDate");
        PushIntegerArray(state, best.AffixIds);
        lua_setfield(state, -2, "affixIDs");
        PushMembers(state, best.Members);
        lua_setfield(state, -2, "members");
        SetInteger(state, "dungeonScore", best.DungeonScore);
    }

    private static void PushBestRunValues(
        lua_State state,
        WowMythicPlusBestRunState best)
    {
        lua_pushinteger(state, best.DurationSec);
        lua_pushinteger(state, best.Level);
        PushDate(state, best.CompletionDate);
        PushIntegerArray(state, best.AffixIds);
        PushMembers(state, best.Members);
        lua_pushinteger(state, best.DungeonScore);
    }

    private static void PushDate(lua_State state, WowMythicPlusDateState date)
    {
        lua_newtable(state);
        SetInteger(state, "year", date.Year);
        SetInteger(state, "month", date.Month);
        SetInteger(state, "day", date.Day);
        SetInteger(state, "hour", date.Hour);
        SetInteger(state, "minute", date.Minute);
        SetInteger(state, "weekday", unchecked(date.ZeroBasedWeekday + 1));
    }

    private static void PushIntegerArray(lua_State state, IReadOnlyList<int> values)
    {
        lua_newtable(state);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushMembers(
        lua_State state,
        IReadOnlyList<WowMythicPlusMemberState> members)
    {
        lua_newtable(state);
        for (var index = 0; index < members.Count; index++)
        {
            lua_newtable(state);
            SetString(state, "name", members[index].Name);
            SetInteger(state, "specID", members[index].SpecId);
            SetInteger(state, "classID", members[index].ClassId);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static int RequiredMapId(lua_State state, string usage) =>
        RequiredInt32(state, 1, usage);

    private static int RequiredInt32(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)value);
    }

    private static bool OptionalTruthyBoolean(
        lua_State state,
        int index,
        bool defaultValue)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return defaultValue;
        return lua_toboolean(state, index) != 0;
    }

    private static int PushZeroOrOneInteger(lua_State state, int? value)
    {
        if (value is not { } integer)
            return 0;
        lua_pushinteger(state, integer);
        return 1;
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value is { } integer)
            lua_pushinteger(state, integer);
        else
            lua_pushnil(state);
    }

    private static void PushBoolean(lua_State state, bool value) =>
        lua_pushboolean(state, value ? 1 : 0);

    private static void SetInteger(lua_State state, string field, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetBoolean(lua_State state, string field, bool value)
    {
        PushBoolean(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetString(lua_State state, string field, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }
}
