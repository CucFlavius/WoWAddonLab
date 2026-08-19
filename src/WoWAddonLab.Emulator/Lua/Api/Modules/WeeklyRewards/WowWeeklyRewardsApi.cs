using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowWeeklyRewardsApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AreRewardsForCurrentRewardPeriod", "CanClaimRewards",
        "ClaimReward", "CloseInteraction", "GetActivities",
        "GetActivityEncounterInfo", "GetConquestWeeklyProgress",
        "GetDifficultyIDForActivityTier", "GetExampleRewardItemHyperlinks",
        "GetItemHyperlink", "GetNextActivitiesIncrease",
        "GetNextMythicPlusIncrease", "GetNumCompletedDungeonRuns",
        "GetSortedProgressForActivity", "HasAvailableRewards",
        "HasGeneratedRewards", "HasInteraction", "IsWeeklyChestRetired",
        "OnUIInteract", "ShouldShowFinalRetirementMessage",
        "ShouldShowRetirementMessage"
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
        lua_setglobal(state, "C_WeeklyRewards");
    }

    private static int Dispatch(lua_State state)
    {
        var weekly = LuaBindings.GetRuntime(state).WeeklyRewards;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "AreRewardsForCurrentRewardPeriod":
                PushBoolean(state, weekly.AreRewardsForCurrentRewardPeriod);
                return 1;
            case "CanClaimRewards":
                PushBoolean(state, weekly.CanClaimRewards);
                return 1;
            case "ClaimReward":
            {
                var id = RequiredInt32(
                    state,
                    1,
                    "Usage: C_WeeklyRewards.ClaimReward(id)");
                if (weekly.CanClaimRewards && !weekly.ClaimInProgress)
                {
                    weekly.ClaimInProgress = true;
                    weekly.ClaimedRewardId = id;
                }
                return 0;
            }
            case "CloseInteraction":
                weekly.CloseInteractionRequests++;
                weekly.InteractionActive = false;
                return 0;
            case "GetActivities":
            {
                var type = OptionalActivityType(
                    state,
                    1,
                    "Usage: local activities = C_WeeklyRewards.GetActivities([type])");
                lua_newtable(state);
                var outputIndex = 1;
                foreach (var activity in weekly.Activities)
                {
                    if (type is not null && activity.Type != type)
                        continue;
                    PushActivity(state, activity);
                    lua_rawseti(state, -2, outputIndex++);
                }
                return 1;
            }
            case "GetActivityEncounterInfo":
            {
                const string usage =
                    "Usage: local info = C_WeeklyRewards.GetActivityEncounterInfo(type, index)";
                var type = RequiredActivityType(state, 1, usage);
                var index = RequiredOneBasedIndex(state, 2, usage);
                if (!weekly.EncounterInfo.TryGetValue((type, index), out var encounters))
                    return 0;
                lua_newtable(state);
                for (var i = 0; i < encounters.Count; i++)
                {
                    PushEncounter(state, encounters[i]);
                    lua_rawseti(state, -2, i + 1);
                }
                return 1;
            }
            case "GetConquestWeeklyProgress":
                PushConquestWeeklyProgress(state, weekly.ConquestWeeklyProgress);
                return 1;
            case "GetDifficultyIDForActivityTier":
            {
                var activityTierId = RequiredInt32(
                    state,
                    1,
                    "Usage: local difficultyID = " +
                    "C_WeeklyRewards.GetDifficultyIDForActivityTier(activityTierID)");
                if (!weekly.DifficultyIdsByActivityTier.TryGetValue(
                        unchecked((uint)activityTierId),
                        out var difficultyId))
                    return 0;
                lua_pushinteger(state, difficultyId);
                return 1;
            }
            case "GetExampleRewardItemHyperlinks":
            {
                var id = RequiredInt32(
                    state,
                    1,
                    "Usage: local hyperlink, upgradeHyperlink = " +
                    "C_WeeklyRewards.GetExampleRewardItemHyperlinks(id)");
                if (!weekly.ExampleRewardItemHyperlinks.TryGetValue(id, out var hyperlinks))
                    return 0;
                lua_pushstring(state, hyperlinks.Hyperlink);
                lua_pushstring(state, hyperlinks.UpgradeHyperlink);
                return 2;
            }
            case "GetItemHyperlink":
            {
                var itemDbId = RequiredUInt64(
                    state,
                    1,
                    "Usage: local hyperlink = C_WeeklyRewards.GetItemHyperlink(itemDBID)");
                if (!weekly.ItemHyperlinks.TryGetValue(itemDbId, out var hyperlink))
                    return 0;
                PushOptionalString(state, hyperlink);
                return 1;
            }
            case "GetNextActivitiesIncrease":
            {
                const string usage =
                    "Usage: local hasSeasonData, nextActivityTierID, nextLevel, itemLevel = " +
                    "C_WeeklyRewards.GetNextActivitiesIncrease(activityTierID, level)";
                var activityTierId = RequiredInt32(state, 1, usage);
                var level = RequiredInt32(state, 2, usage);
                weekly.NextActivitiesIncreases.TryGetValue(
                    (activityTierId, level),
                    out var increase);
                increase ??= new WowNextActivitiesIncreaseState(false, null, null, null);
                PushBoolean(state, increase.HasSeasonData);
                PushOptionalInteger(state, increase.NextActivityTierId);
                PushOptionalInteger(state, increase.NextLevel);
                PushOptionalInteger(state, increase.ItemLevel);
                return 4;
            }
            case "GetNextMythicPlusIncrease":
            {
                var level = RequiredInt32(
                    state,
                    1,
                    "Usage: local hasSeasonData, nextMythicPlusLevel, itemLevel = " +
                    "C_WeeklyRewards.GetNextMythicPlusIncrease(mythicPlusLevel)");
                weekly.NextMythicPlusIncreases.TryGetValue(level, out var increase);
                increase ??= new WowNextMythicPlusIncreaseState(false, null, null);
                PushBoolean(state, increase.HasSeasonData);
                PushOptionalInteger(state, increase.NextMythicPlusLevel);
                PushOptionalInteger(state, increase.ItemLevel);
                return 3;
            }
            case "GetNumCompletedDungeonRuns":
                lua_pushinteger(state, weekly.CompletedDungeonRuns.Heroic);
                lua_pushinteger(state, weekly.CompletedDungeonRuns.Mythic);
                lua_pushinteger(state, weekly.CompletedDungeonRuns.MythicPlus);
                return 3;
            case "GetSortedProgressForActivity":
            {
                const string usage =
                    "Usage: local progress = " +
                    "C_WeeklyRewards.GetSortedProgressForActivity(type, combineSharedDifficulty)";
                var type = RequiredActivityType(state, 1, usage);
                var combineSharedDifficulty = RequiredTruthyBoolean(state, 2, usage);
                weekly.SortedProgress.TryGetValue(
                    (type, combineSharedDifficulty),
                    out var progress);
                progress ??= [];
                lua_newtable(state);
                for (var i = 0; i < progress.Count; i++)
                {
                    PushActivityTierProgress(state, progress[i]);
                    lua_rawseti(state, -2, i + 1);
                }
                return 1;
            }
            case "HasAvailableRewards":
                PushBoolean(state, weekly.HasAvailableRewards);
                return 1;
            case "HasGeneratedRewards":
                PushBoolean(state, weekly.HasGeneratedRewards);
                return 1;
            case "HasInteraction":
                PushBoolean(state, weekly.HasInteraction);
                return 1;
            case "IsWeeklyChestRetired":
                PushBoolean(state, weekly.IsWeeklyChestRetired);
                return 1;
            case "OnUIInteract":
                weekly.UiInteractRequests++;
                return 0;
            case "ShouldShowFinalRetirementMessage":
                PushBoolean(state, weekly.ShouldShowFinalRetirementMessage);
                return 1;
            case "ShouldShowRetirementMessage":
                PushBoolean(state, weekly.ShouldShowRetirementMessage);
                return 1;
            default:
                return 0;
        }
    }

    private static byte? OptionalActivityType(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return null;
        return RequiredActivityType(state, index, usage);
    }

    private static byte RequiredActivityType(
        lua_State state,
        int index,
        string usage)
    {
        var value = unchecked((byte)RequiredInt32(state, index, usage));
        if (value > 6)
            return unchecked((byte)luaL_error(state, usage));
        return value;
    }

    private static int RequiredOneBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredNumber(state, index, usage);
        if (value < 0 || value > uint.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)(value - 1d));
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredNumber(state, index, usage);
        if (value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)value);
    }

    private static ulong RequiredUInt64(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredNumber(state, index, usage);
        if (value < 0 || value > 9007199254740991d)
            return unchecked((ulong)luaL_error(state, usage));
        return (ulong)value;
    }

    private static double RequiredNumber(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value))
            return luaL_error(state, usage);
        return value;
    }

    private static bool RequiredTruthyBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) == LUA_TNONE)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static void PushActivity(
        lua_State state,
        WowWeeklyRewardActivityState activity)
    {
        lua_newtable(state);
        SetInteger(state, "type", activity.Type);
        SetInteger(state, "index", unchecked(activity.ZeroBasedIndex + 1));
        SetInteger(state, "threshold", activity.Threshold);
        SetInteger(state, "progress", activity.Progress);
        SetInteger(state, "id", activity.Id);
        SetInteger(state, "activityTierID", activity.ActivityTierId);
        SetInteger(state, "level", activity.Level);
        SetOptionalInteger(state, "claimID", activity.ClaimId);
        SetOptionalString(state, "raidString", activity.RaidString);
        lua_newtable(state);
        for (var i = 0; i < activity.Rewards.Count; i++)
        {
            PushReward(state, activity.Rewards[i]);
            lua_rawseti(state, -2, i + 1);
        }
        lua_setfield(state, -2, "rewards");
    }

    private static void PushReward(lua_State state, WowWeeklyRewardState reward)
    {
        lua_newtable(state);
        SetInteger(state, "type", reward.Type);
        SetInteger(state, "id", reward.Id);
        SetInteger(state, "quantity", reward.Quantity);
        SetOptionalInteger(state, "itemDBID", reward.ItemDbId);
    }

    private static void PushEncounter(
        lua_State state,
        WowWeeklyRewardEncounterState encounter)
    {
        lua_newtable(state);
        SetInteger(state, "encounterID", encounter.EncounterId);
        SetInteger(state, "bestDifficulty", encounter.BestDifficulty);
        SetInteger(state, "uiOrder", encounter.UiOrder);
        SetInteger(state, "instanceID", encounter.InstanceId);
    }

    private static void PushConquestWeeklyProgress(
        lua_State state,
        WowConquestWeeklyProgressState progress)
    {
        lua_newtable(state);
        SetInteger(state, "progress", progress.Progress);
        SetInteger(state, "maxProgress", progress.MaxProgress);
        SetInteger(state, "displayType", progress.DisplayType);
        SetInteger(state, "unlocksCompleted", progress.UnlocksCompleted);
        SetInteger(state, "maxUnlocks", progress.MaxUnlocks);
        SetString(state, "sampleItemHyperlink", progress.SampleItemHyperlink);
    }

    private static void PushActivityTierProgress(
        lua_State state,
        WowWeeklyRewardActivityTierProgressState progress)
    {
        lua_newtable(state);
        SetInteger(state, "activityTierID", progress.ActivityTierId);
        SetInteger(state, "difficulty", progress.Difficulty);
        SetInteger(state, "numPoints", progress.NumPoints);
    }

    private static void PushBoolean(lua_State state, bool value) =>
        lua_pushboolean(state, value ? 1 : 0);

    private static void PushOptionalInteger(lua_State state, long? value)
    {
        if (value is { } integer)
            lua_pushinteger(state, integer);
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

    private static void SetInteger(
        lua_State state,
        string field,
        long value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string field,
        long? value)
    {
        PushOptionalInteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetString(
        lua_State state,
        string field,
        string value)
    {
        lua_pushstring(state, value);
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
