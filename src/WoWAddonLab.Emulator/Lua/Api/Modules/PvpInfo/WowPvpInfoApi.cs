using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPvpInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetActiveMatchState",
        "GetArenaCrowdControlInfo",
        "GetBattlefieldFlagPosition",
        "GetBattlefieldVehicleInfo",
        "GetBattlefieldVehicles",
        "GetOutdoorPvPWaitTime",
        "GetZonePVPInfo",
        "IsActiveBattlefield",
        "IsInBrawl",
        "IsMatchActive",
        "IsMatchComplete",
        "IsMatchConsideredArena",
        "IsPVPMap",
        "ArePvpTalentsUnlocked",
        "CanToggleWarMode", "CanToggleWarModeInArea",
        "IsWarModeActive", "IsWarModeDesired", "IsWarModeFeatureEnabled",
        "ToggleWarMode",
        "CanPlayerUseRatedPVPUI", "CanPlayerUseTrainingGroundsUI",
        "GetRandomBGInfo",
        "GetHonorRewardInfo", "GetNextHonorLevelForReward",
        "GetPvpTalentsUnlockedLevel",
        "GetWarModeRewardBonusDefault",
        "GetWarModeRewardBonus",
        "AreTrainingGroundsEnabled",
        "IsBattlegroundEnlistmentBonusActive",
        "GetArenaRewards", "GetArenaSkirmishRewards", "GetBrawlRewards",
        "GetAvailableBrawlInfo", "GetBattlegroundInfo",
        "GetPersonalRatedBGBlitzSpecStats", "GetPersonalRatedSoloShuffleSpecStats",
        "GetPVPSeasonRewardAchievementID", "GetPvpTierInfo",
        "GetRandomBGRewards", "GetRandomEpicBGInfo", "GetRandomEpicBGRewards",
        "GetRandomTrainingGroundRewards", "GetRatedBGRewards",
        "GetRatedSoloRBGRewards", "GetRatedSoloShuffleRewards",
        "GetRatedSoloRBGMinItemLevel", "GetRatedSoloShuffleMinItemLevel",
        "GetRewardItemLevelsByTierEnum", "GetSeasonBestInfo", "GetSkirmishInfo",
        "GetSpecialEventBrawlInfo", "GetTrainingGrounds",
        "JoinBattlefield", "JoinBrawl", "JoinRandomTrainingGround",
        "JoinRatedBGBlitz", "JoinTrainingGround",
        "RequestCrowdControlSpell"
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
        lua_setglobal(state, "C_PvP");
        foreach (var function in new[]
                 {
                     "GetArenaOpponentSpec",
                     "GetNumArenaOpponentSpecs",
                     "GetNumArenaOpponents",
                     "IsInActiveWorldPVP",
                     "IsPVPTimerRunning",
                     "RequestPVPRewards",
                     "RequestPVPOptionsEnabled",
                     "RequestRandomBattlegroundInstanceInfo",
                     "RequestRatedInfo"
                 })
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetPVPRoles", Callback);
        LuaBindings.RegisterClosureGlobal(state, "HaveQuestRewardData", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetCurrentArenaSeason", Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var pvp = LuaBindings.GetRuntime(state).Pvp;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetActiveMatchState":
                lua_pushinteger(state, pvp.ActiveMatchState);
                return 1;
            case "GetZonePVPInfo":
                if (pvp.ZonePvpType is null)
                    return 0;
                lua_pushstring(state, pvp.ZonePvpType);
                lua_pushboolean(state, pvp.IsSubZonePvp ? 1 : 0);
                if (pvp.ZoneFactionName is { } factionName)
                    lua_pushstring(state, factionName);
                else
                    lua_pushnil(state);
                return 3;
            case "GetArenaCrowdControlInfo":
            {
                const string usage =
                    "Usage: local spellID, startTime, duration = " +
                    "C_PvP.GetArenaCrowdControlInfo(playerToken)";
                var unitToken = RequiredUnitToken(state, 1, usage);
                if (!pvp.ArenaCrowdControlByUnitToken.TryGetValue(
                        unitToken,
                        out var crowdControl))
                {
                    return 0;
                }

                lua_pushinteger(state, crowdControl.SpellId);
                lua_pushnumber(state, crowdControl.StartTime);
                lua_pushnumber(state, crowdControl.Duration);
                return 3;
            }
            case "GetBattlefieldFlagPosition":
            {
                const string usage =
                    "Usage: local uiPosx, uiPosy, flagTexture = " +
                    "C_PvP.GetBattlefieldFlagPosition(flagIndex, uiMapId)";
                var zeroBasedIndex = RequiredOneBasedIndex(state, 1, usage);
                var mapId = RequiredUInt32(state, 2, usage);
                if (!pvp.BattlefieldFlagPositions.TryGetValue(
                        (mapId, zeroBasedIndex + 1),
                        out var position))
                {
                    lua_pushnil(state);
                    lua_pushnil(state);
                    lua_pushinteger(state, 0);
                    return 3;
                }

                PushOptionalNumber(state, position.X);
                PushOptionalNumber(state, position.Y);
                lua_pushinteger(state, position.FlagTexture);
                return 3;
            }
            case "GetBattlefieldVehicleInfo":
            {
                const string usage =
                    "Usage: local info = " +
                    "C_PvP.GetBattlefieldVehicleInfo(vehicleIndex, uiMapID)";
                var zeroBasedIndex = RequiredOneBasedIndex(state, 1, usage);
                var mapId = RequiredInt32(state, 2, usage);
                if (mapId == 0 ||
                    !pvp.BattlefieldVehiclesByMapId.TryGetValue(
                        mapId,
                        out var vehicles) ||
                    zeroBasedIndex < 0 ||
                    zeroBasedIndex >= vehicles.Count)
                {
                    return 0;
                }

                PushBattlefieldVehicleInfo(state, vehicles[zeroBasedIndex]);
                return 1;
            }
            case "GetBattlefieldVehicles":
            {
                const string usage =
                    "Usage: local vehicles = " +
                    "C_PvP.GetBattlefieldVehicles(uiMapID)";
                var mapId = RequiredInt32(state, 1, usage);
                if (mapId == 0)
                    return 0;

                lua_newtable(state);
                if (pvp.BattlefieldVehiclesByMapId.TryGetValue(
                        mapId,
                        out var vehicles))
                {
                    for (var index = 0; index < vehicles.Count; index++)
                    {
                        lua_pushinteger(state, index + 1);
                        PushBattlefieldVehicleInfo(state, vehicles[index]);
                        lua_settable(state, -3);
                    }
                }
                return 1;
            }
            case "GetOutdoorPvPWaitTime":
            {
                const string usage =
                    "Usage: local pvpWaitTime = " +
                    "C_PvP.GetOutdoorPvPWaitTime(uiMapID)";
                var mapId = RequiredInt32(state, 1, usage);
                if (pvp.OutdoorPvpWaitTimes.TryGetValue(
                        mapId,
                        out var waitTime))
                {
                    lua_pushnumber(state, waitTime);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            case "IsInBrawl":
                lua_pushboolean(state, pvp.IsInBrawl ? 1 : 0);
                return 1;
            case "IsActiveBattlefield":
                lua_pushboolean(state, pvp.IsActiveBattlefield ? 1 : 0);
                return 1;
            case "IsMatchActive":
                lua_pushboolean(
                    state,
                    pvp.ActiveMatchState is >= 1 and <= 4 ? 1 : 0);
                return 1;
            case "IsMatchComplete":
                lua_pushboolean(state, pvp.ActiveMatchState == 5 ? 1 : 0);
                return 1;
            case "IsMatchConsideredArena":
                lua_pushboolean(state, pvp.IsMatchConsideredArena ? 1 : 0);
                return 1;
            case "IsPVPMap":
                lua_pushboolean(state, pvp.IsPvpMap ? 1 : 0);
                return 1;
            case "ArePvpTalentsUnlocked":
                lua_pushboolean(state, pvp.ArePvpTalentsUnlocked ? 1 : 0);
                return 1;
            case "CanToggleWarMode":
            {
                var targetEnabled = RequiredTruthyBoolean(
                    state,
                    1,
                    "Usage: local canTogglePvP = C_PvP.CanToggleWarMode(toggle)");
                lua_pushboolean(
                    state,
                    CanToggleWarMode(pvp, targetEnabled) ? 1 : 0);
                return 1;
            }
            case "CanToggleWarModeInArea":
                lua_pushboolean(state, pvp.CanToggleWarModeInArea ? 1 : 0);
                return 1;
            case "IsWarModeActive":
                lua_pushboolean(state, pvp.IsWarModeActive ? 1 : 0);
                return 1;
            case "IsWarModeDesired":
                lua_pushboolean(state, pvp.IsWarModeDesired ? 1 : 0);
                return 1;
            case "IsWarModeFeatureEnabled":
                lua_pushboolean(state, pvp.IsWarModeFeatureEnabled ? 1 : 0);
                return 1;
            case "ToggleWarMode":
            {
                var targetEnabled = !pvp.IsWarModeDesired;
                if (CanToggleWarMode(pvp, targetEnabled))
                    pvp.IsWarModeDesired = targetEnabled;
                return 0;
            }
            case "CanPlayerUseRatedPVPUI":
                lua_pushboolean(state, pvp.CanPlayerUseRatedPvpUi ? 1 : 0);
                lua_pushstring(state, pvp.RatedPvpUiFailureReason);
                return 2;
            case "CanPlayerUseTrainingGroundsUI":
                lua_pushboolean(state, pvp.CanPlayerUseTrainingGroundsUi ? 1 : 0);
                lua_pushstring(state, pvp.TrainingGroundsUiFailureReason);
                return 2;
            case "AreTrainingGroundsEnabled":
                lua_pushboolean(state, pvp.AreTrainingGroundsEnabled ? 1 : 0);
                return 1;
            case "IsBattlegroundEnlistmentBonusActive":
                lua_pushboolean(
                    state,
                    pvp.BattlegroundEnlistmentBonusActive ? 1 : 0);
                lua_pushboolean(state, pvp.BrawlEnlistmentBonusActive ? 1 : 0);
                return 2;
            case "GetPVPRoles":
                lua_pushboolean(state, pvp.PlayerRoleTank ? 1 : 0);
                lua_pushboolean(state, pvp.PlayerRoleHealer ? 1 : 0);
                lua_pushboolean(state, pvp.PlayerRoleDamage ? 1 : 0);
                return 3;
            case "GetNumArenaOpponentSpecs":
                lua_pushinteger(state, pvp.ArenaOpponentSpecCount);
                return 1;
            case "GetCurrentArenaSeason":
                lua_pushinteger(state, pvp.CurrentArenaSeason);
                return 1;
            case "GetPvpTalentsUnlockedLevel":
                lua_pushinteger(state, pvp.PvpTalentsUnlockedLevel);
                return 1;
            case "GetWarModeRewardBonusDefault":
                lua_pushinteger(state, pvp.WarModeRewardBonusDefault);
                return 1;
            case "GetWarModeRewardBonus":
                lua_pushinteger(state, pvp.WarModeRewardBonus);
                return 1;
            case "GetNumArenaOpponents":
                lua_pushinteger(state, pvp.ArenaOpponentCount);
                return 1;
            case "IsInActiveWorldPVP":
                lua_pushboolean(state, pvp.IsInActiveWorldPvp ? 1 : 0);
                return 1;
            case "IsPVPTimerRunning":
                lua_pushboolean(state, pvp.IsPvpTimerRunning ? 1 : 0);
                return 1;
            case "GetArenaOpponentSpec":
            {
                if (lua_isnumber(state, 1) == 0)
                    return 0;

                var index = unchecked((int)lua_tonumber(state, 1));
                if (index < 1 || index > pvp.ArenaOpponentSpecCount)
                    return 0;

                lua_pushinteger(
                    state,
                    pvp.ArenaOpponentSpecializations.TryGetValue(
                        index,
                        out var specialization)
                            ? specialization
                            : 0);
                lua_pushinteger(
                    state,
                    pvp.ArenaOpponentGenders.TryGetValue(index, out var gender)
                        ? gender
                        : 0);
                return 2;
            }
            case "JoinBattlefield":
            {
                const string usage =
                    "Usage: C_PvP.JoinBattlefield(battlemasterListId)";
                var battlemasterListId =
                    RequiredUInt32(state, 1, usage);
                pvp.BattlefieldJoinRequests.Add(
                    unchecked((ushort)battlemasterListId));
                return 0;
            }
            case "JoinBrawl":
                pvp.BrawlJoinRequests.Add(
                    OptionalTruthyBoolean(state, 1, false));
                return 0;
            case "JoinRandomTrainingGround":
                pvp.RandomTrainingGroundJoinRequestCount++;
                return 0;
            case "JoinRatedBGBlitz":
                pvp.RatedBgBlitzJoinRequestCount++;
                return 0;
            case "JoinTrainingGround":
            {
                const string usage =
                    "Usage: C_PvP.JoinTrainingGround(trainingGroundID)";
                var trainingGroundId =
                    RequiredInt32(state, 1, usage);
                pvp.TrainingGroundJoinRequests.Add(
                    unchecked((uint)trainingGroundId));
                return 0;
            }
            case "RequestCrowdControlSpell":
            {
                const string usage =
                    "Usage: C_PvP.RequestCrowdControlSpell(playerToken)";
                var unitToken = RequiredUnitToken(state, 1, usage);
                pvp.CrowdControlSpellRequestUnitTokens.Add(unitToken);
                return 0;
            }
            case "RequestPVPRewards":
                pvp.PvpRewardsRequestCount++;
                return 0;
            case "RequestPVPOptionsEnabled":
                pvp.PvpOptionsEnabledRequestCount++;
                return 0;
            case "RequestRandomBattlegroundInstanceInfo":
                pvp.RandomBattlegroundInstanceInfoRequestCount++;
                return 0;
            case "RequestRatedInfo":
                pvp.RatedInfoRequestCount++;
                return 0;
            case "HaveQuestRewardData":
            {
                if (lua_isnumber(state, 1) == 0)
                {
                    luaL_error(
                        state,
                        "Usage: HaveQuestRewardData(questID)");
                    return 0;
                }

                var questId = unchecked((int)lua_tonumber(state, 1));
                var available =
                    questId > 0 &&
                    pvp.QuestRewardDataAvailable.Contains(questId);
                if (questId > 0 && !available)
                    pvp.RequestedQuestRewardDataIds.Add(questId);
                lua_pushboolean(state, available ? 1 : 0);
                return 1;
            }
            case "GetRandomBGInfo":
                PushRandomBattlegroundInfo(state, pvp.RandomBattlegroundInfo);
                return 1;
            case "GetRandomEpicBGInfo":
                PushRandomBattlegroundInfo(
                    state,
                    pvp.RandomEpicBattlegroundInfo);
                return 1;
            case "GetBattlegroundInfo":
            {
                const string usage =
                    "Usage: local battlegroundInfo = " +
                    "C_PvP.GetBattlegroundInfo(battlegroundIndex)";
                var zeroBasedIndex = RequiredOneBasedIndex(state, 1, usage);
                if (zeroBasedIndex < 0 ||
                    zeroBasedIndex >= pvp.Battlegrounds.Count)
                {
                    lua_pushnil(state);
                    return 1;
                }

                PushBattlegroundInfo(state, pvp.Battlegrounds[zeroBasedIndex]);
                return 1;
            }
            case "GetTrainingGrounds":
                lua_newtable(state);
                for (var index = 0; index < pvp.TrainingGrounds.Count; index++)
                {
                    lua_pushinteger(state, index + 1);
                    PushBattlegroundInfo(state, pvp.TrainingGrounds[index]);
                    lua_settable(state, -3);
                }
                return 1;
            case "GetSkirmishInfo":
            {
                const string usage =
                    "Usage: local battlemasterListInfo = " +
                    "C_PvP.GetSkirmishInfo(pvpBracket)";
                var bracket = RequiredInt32(state, 1, usage);
                if (bracket != 4 ||
                    !pvp.SkirmishInfoByBracket.TryGetValue(
                        bracket,
                        out var info))
                {
                    return 0;
                }

                PushSkirmishInfo(state, info);
                return 1;
            }
            case "GetAvailableBrawlInfo":
                PushOptionalBrawlInfo(state, pvp.AvailableBrawlInfo);
                return 1;
            case "GetSpecialEventBrawlInfo":
                PushOptionalBrawlInfo(state, pvp.SpecialEventBrawlInfo);
                return 1;
            case "GetHonorRewardInfo":
            {
                const string usage =
                    "Usage: local info = " +
                    "C_PvP.GetHonorRewardInfo(honorLevel)";
                var honorLevel = RequiredInt32(state, 1, usage);
                pvp.HonorRewardsByLevel.TryGetValue(
                    unchecked((uint)honorLevel),
                    out var info);
                PushOptionalHonorRewardInfo(state, info);
                return 1;
            }
            case "GetNextHonorLevelForReward":
            {
                const string usage =
                    "Usage: local nextHonorLevelWithReward = " +
                    "C_PvP.GetNextHonorLevelForReward(honorLevel)";
                var honorLevel = RequiredInt32(state, 1, usage);
                int? nextLevel = null;
                foreach (var rewardLevel in pvp.HonorLevelsWithRewards)
                {
                    if (rewardLevel <= honorLevel)
                        continue;
                    if (nextLevel is null || rewardLevel < nextLevel)
                        nextLevel = rewardLevel;
                }
                if (nextLevel is { } level)
                    lua_pushinteger(state, level);
                else
                    lua_pushnil(state);
                return 1;
            }
            case "GetPersonalRatedBGBlitzSpecStats":
                PushOptionalPvpSpecStats(
                    state,
                    pvp.PersonalRatedBgBlitzSpecStats,
                    "weeklyMostPlayedSpecGames",
                    "seasonMostPlayedSpecGames");
                return 1;
            case "GetPersonalRatedSoloShuffleSpecStats":
                PushOptionalPvpSpecStats(
                    state,
                    pvp.PersonalRatedSoloShuffleSpecStats,
                    "weeklyMostPlayedSpecRounds",
                    "seasonMostPlayedSpecRounds");
                return 1;
            case "GetPVPSeasonRewardAchievementID":
                if (pvp.PvpSeasonRewardAchievementId <= 0)
                    return 0;
                lua_pushinteger(
                    state,
                    pvp.PvpSeasonRewardAchievementId);
                return 1;
            case "GetPvpTierInfo":
            {
                const string usage =
                    "Usage: local pvpTierInfo = " +
                    "C_PvP.GetPvpTierInfo(tierID)";
                var tierId = RequiredInt32(state, 1, usage);
                pvp.PvpTiersById.TryGetValue(
                    unchecked((uint)tierId),
                    out var info);
                PushOptionalPvpTierInfo(state, info);
                return 1;
            }
            case "GetArenaRewards":
            {
                const string usage =
                    "Usage: local honor, experience, itemRewards, " +
                    "currencyRewards, roleShortageBonus = " +
                    "C_PvP.GetArenaRewards(teamSize)";
                var teamSize = RequiredInt32(state, 1, usage);
                if (teamSize is not (2 or 3))
                    return 0;

                if (!pvp.ArenaRewardsByTeamSize.TryGetValue(
                        teamSize,
                        out var rewards))
                {
                    rewards = EmptyRewards();
                }
                return PushRewards(state, rewards);
            }
            case "GetArenaSkirmishRewards":
                return PushRewards(state, pvp.ArenaSkirmishRewards);
            case "GetRandomBGRewards":
                return PushRewards(state, pvp.RandomBattlegroundRewards);
            case "GetRandomEpicBGRewards":
                return PushRewards(state, pvp.RandomEpicBattlegroundRewards);
            case "GetRandomTrainingGroundRewards":
                return PushRewards(state, pvp.RandomTrainingGroundRewards);
            case "GetRatedBGRewards":
                return PushRewards(state, pvp.RatedBattlegroundRewards);
            case "GetRatedSoloRBGRewards":
                return PushRewards(state, pvp.RatedSoloRbgRewards);
            case "GetRatedSoloShuffleRewards":
                return PushRewards(state, pvp.RatedSoloShuffleRewards);
            case "GetBrawlRewards":
            {
                const string usage =
                    "Usage: local honor, experience, itemRewards, " +
                    "currencyRewards, roleShortageBonus, hasWon = " +
                    "C_PvP.GetBrawlRewards(brawlType)";
                var brawlType = RequiredInt32(state, 1, usage);
                if (brawlType is < 0 or > 5)
                    return luaL_error(state, usage);
                if (brawlType == 0)
                    return 0;

                if (!pvp.BrawlRewardsByType.TryGetValue(
                        brawlType,
                        out var brawlRewards))
                {
                    brawlRewards = new WowBrawlRewardState(
                        EmptyRewards(),
                        false);
                }
                var resultCount = PushRewards(state, brawlRewards.Rewards);
                lua_pushboolean(state, brawlRewards.HasWon ? 1 : 0);
                return resultCount + 1;
            }
            case "GetRatedSoloRBGMinItemLevel":
                lua_pushinteger(state, pvp.RatedSoloRbgMinItemLevel);
                return 1;
            case "GetRatedSoloShuffleMinItemLevel":
                lua_pushinteger(state, pvp.RatedSoloShuffleMinItemLevel);
                return 1;
            case "GetRewardItemLevelsByTierEnum":
            {
                const string usage =
                    "Usage: local activityItemLevel, weeklyItemLevel = " +
                    "C_PvP.GetRewardItemLevelsByTierEnum(pvpTierEnum)";
                var tier = RequiredInt32(state, 1, usage);
                pvp.RewardItemLevelsByTier.TryGetValue(
                    tier,
                    out var levels);
                lua_pushinteger(state, levels?.ActivityItemLevel ?? 0);
                lua_pushinteger(state, levels?.WeeklyItemLevel ?? 0);
                return 2;
            }
            case "GetSeasonBestInfo":
                lua_pushinteger(state, pvp.SeasonBestTier);
                if (pvp.SeasonBestRewardId is { } rewardId)
                    lua_pushinteger(state, rewardId);
                else
                    lua_pushnil(state);
                return 2;
            default:
                return 0;
        }
    }

    private static bool CanToggleWarMode(
        WowPvpState pvp,
        bool targetEnabled) =>
        targetEnabled
            ? pvp.CanEnableWarMode ?? pvp.CanToggleWarMode
            : pvp.CanDisableWarMode ?? pvp.CanToggleWarMode;

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

    private static bool OptionalTruthyBoolean(
        lua_State state,
        int index,
        bool defaultValue) =>
        lua_type(state, index) == LUA_TNONE
            ? defaultValue
            : lua_toboolean(state, index) != 0;

    private static string RequiredUnitToken(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }

        var unitToken = lua_tostring(state, index) ?? string.Empty;
        if (!LuaBindings.IsRecognizedUnitToken(unitToken))
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return unitToken;
    }

    private static int RequiredOneBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredNumber(state, index, usage);
        if (value < 0 || value > uint.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)(value - 1));
    }

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredNumber(state, index, usage);
        if (value < 0 || value > uint.MaxValue)
            return unchecked((uint)luaL_error(state, usage));
        return unchecked((uint)value);
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

    private static void PushOptionalNumber(
        lua_State state,
        double? value)
    {
        if (value is { } number)
            lua_pushnumber(state, number);
        else
            lua_pushnil(state);
    }

    private static void PushBattlefieldVehicleInfo(
        lua_State state,
        WowBattlefieldVehicleInfoState vehicle)
    {
        lua_newtable(state);
        SetNumberField(state, "x", vehicle.X);
        SetNumberField(state, "y", vehicle.Y);
        SetStringField(state, "name", vehicle.Name);
        SetBooleanField(state, "isOccupied", vehicle.IsOccupied);
        SetStringField(state, "atlas", vehicle.Atlas);
        SetIntegerField(state, "textureWidth", vehicle.TextureWidth);
        SetIntegerField(state, "textureHeight", vehicle.TextureHeight);
        SetNumberField(state, "facing", vehicle.Facing);
        SetBooleanField(state, "isPlayer", vehicle.IsPlayer);
        SetBooleanField(state, "isAlive", vehicle.IsAlive);
        SetBooleanField(
            state,
            "shouldDrawBelowPlayerBlips",
            vehicle.ShouldDrawBelowPlayerBlips);
    }

    private static void PushRandomBattlegroundInfo(
        lua_State state,
        WowRandomBattlegroundInfoState info)
    {
        lua_newtable(state);
        SetBooleanField(state, "canQueue", info.CanQueue);
        SetIntegerField(state, "bgID", info.BattlegroundId);
        SetIntegerField(state, "bgIndex", info.BattlegroundIndex);
        SetBooleanField(
            state,
            "hasRandomWinToday",
            info.HasRandomWinToday);
        SetIntegerField(state, "minLevel", info.MinLevel);
        SetIntegerField(state, "maxLevel", info.MaxLevel);
        SetStringField(state, "name", info.Name);
    }

    private static void PushBattlegroundInfo(
        lua_State state,
        WowBattlegroundInfoState info)
    {
        lua_newtable(state);
        SetOptionalStringField(state, "name", info.Name);
        SetOptionalIntegerField(state, "icon", info.Icon);
        SetOptionalStringField(state, "gameType", info.GameType);
        SetOptionalStringField(
            state,
            "shortDescription",
            info.ShortDescription);
        SetOptionalStringField(
            state,
            "longDescription",
            info.LongDescription);
        SetOptionalStringField(
            state,
            "mapDescription",
            info.MapDescription);
        SetIntegerField(state, "maxPlayers", info.MaxPlayers);
        SetOptionalIntegerField(
            state,
            "battlegroundID",
            info.BattlegroundId);
        SetOptionalIntegerField(
            state,
            "lfgDungeonID",
            info.LfgDungeonId);
        SetOptionalIntegerField(state, "mapID", info.MapId);
        SetBooleanField(state, "isHoliday", info.IsHoliday);
        SetBooleanField(state, "isRandom", info.IsRandom);
        SetBooleanField(state, "canEnter", info.CanEnter);
        SetBooleanField(
            state,
            "isTrainingGround",
            info.IsTrainingGround);
    }

    private static void PushSkirmishInfo(
        lua_State state,
        WowSkirmishInfoState info)
    {
        lua_newtable(state);
        SetStringField(state, "name", info.Name);
        SetIntegerField(state, "matchmakingType", info.MatchmakingType);
        SetIntegerField(state, "minPlayers", info.MinPlayers);
        SetIntegerField(state, "maxPlayers", info.MaxPlayers);
        SetIntegerField(state, "icon", info.Icon);
        SetStringField(state, "longDescription", info.LongDescription);
        SetStringField(state, "shortDescription", info.ShortDescription);
    }

    private static void PushOptionalBrawlInfo(
        lua_State state,
        WowBrawlInfoState? info)
    {
        if (info is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_newtable(state);
        SetIntegerField(state, "brawlID", info.BrawlId);
        SetStringField(state, "name", info.Name);
        SetStringField(state, "shortDescription", info.ShortDescription);
        SetStringField(state, "longDescription", info.LongDescription);
        SetBooleanField(state, "canQueue", info.CanQueue);
        SetIntegerField(state, "minLevel", info.MinLevel);
        SetIntegerField(state, "maxLevel", info.MaxLevel);
        SetBooleanField(state, "groupsAllowed", info.GroupsAllowed);
        SetBooleanField(
            state,
            "crossFactionAllowed",
            info.CrossFactionAllowed);
        SetOptionalIntegerField(
            state,
            "timeLeftUntilNextChange",
            info.TimeLeftUntilNextChange);
        SetIntegerField(state, "brawlType", info.BrawlType);
        lua_newtable(state);
        for (var index = 0; index < info.MapNames.Count; index++)
        {
            lua_pushinteger(state, index + 1);
            lua_pushstring(state, info.MapNames[index]);
            lua_settable(state, -3);
        }
        lua_setfield(state, -2, "mapNames");
        SetBooleanField(
            state,
            "includesAllArenas",
            info.IncludesAllArenas);
        SetIntegerField(state, "minItemLevel", info.MinItemLevel);
        SetBooleanField(
            state,
            "shouldHideRewardIcon",
            info.ShouldHideRewardIcon);
    }

    private static WowPvpRewardState EmptyRewards() =>
        new(0, 0, null, null, null);

    private static void PushOptionalHonorRewardInfo(
        lua_State state,
        WowHonorRewardInfoState? info)
    {
        if (info is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_newtable(state);
        SetStringField(state, "honorLevelName", info.HonorLevelName);
        if (info.BadgeFileDataId == 0)
            SetNilField(state, "badgeFileDataID");
        else
            SetIntegerField(state, "badgeFileDataID", info.BadgeFileDataId);
        SetIntegerField(
            state,
            "achievementRewardedID",
            info.AchievementRewardedId);
    }

    private static void PushOptionalPvpSpecStats(
        lua_State state,
        WowPvpSpecStatsState? stats,
        string weeklyCountField,
        string seasonCountField)
    {
        if (stats is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_newtable(state);
        SetIntegerField(
            state,
            "weeklyMostPlayedSpecID",
            stats.WeeklyMostPlayedSpecId);
        SetIntegerField(
            state,
            weeklyCountField,
            stats.WeeklyMostPlayedSpecCount);
        SetIntegerField(
            state,
            "seasonMostPlayedSpecID",
            stats.SeasonMostPlayedSpecId);
        SetIntegerField(
            state,
            seasonCountField,
            stats.SeasonMostPlayedSpecCount);
    }

    private static void PushOptionalPvpTierInfo(
        lua_State state,
        WowPvpTierInfoState? info)
    {
        if (info is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_newtable(state);
        SetStringField(state, "name", info.Name);
        SetIntegerField(state, "descendRating", info.DescendRating);
        SetIntegerField(state, "ascendRating", info.AscendRating);
        SetIntegerField(state, "descendTier", info.DescendTier);
        SetIntegerField(state, "ascendTier", info.AscendTier);
        SetIntegerField(state, "pvpTierEnum", info.PvpTierEnum);
        if (info.TierIconId == 0)
            SetNilField(state, "tierIconID");
        else
            SetIntegerField(state, "tierIconID", info.TierIconId);
    }

    private static int PushRewards(
        lua_State state,
        WowPvpRewardState rewards)
    {
        lua_pushinteger(state, rewards.Honor);
        lua_pushinteger(state, rewards.Experience);
        PushOptionalRewardItems(state, rewards.ItemRewards);
        PushOptionalCurrencyRewards(state, rewards.CurrencyRewards);
        PushOptionalRoleShortageBonus(state, rewards.RoleShortageBonus);
        return 5;
    }

    private static void PushOptionalRewardItems(
        lua_State state,
        IReadOnlyList<WowPvpRewardItemState>? rewards)
    {
        if (rewards is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_newtable(state);
        for (var index = 0; index < rewards.Count; index++)
        {
            var reward = rewards[index];
            lua_pushinteger(state, index + 1);
            lua_newtable(state);
            SetIntegerField(state, "id", reward.Id);
            SetOptionalStringField(state, "name", reward.Name);
            if (reward.Texture == 0)
                SetNilField(state, "texture");
            else
                SetIntegerField(state, "texture", reward.Texture);
            SetIntegerField(state, "quantity", reward.Quantity);
            lua_settable(state, -3);
        }
    }

    private static void PushOptionalCurrencyRewards(
        lua_State state,
        IReadOnlyList<WowPvpCurrencyRewardState>? rewards)
    {
        if (rewards is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_newtable(state);
        for (var index = 0; index < rewards.Count; index++)
        {
            var reward = rewards[index];
            lua_pushinteger(state, index + 1);
            lua_newtable(state);
            SetIntegerField(state, "id", reward.Id);
            SetIntegerField(state, "quantity", reward.Quantity);
            lua_settable(state, -3);
        }
    }

    private static void PushOptionalRoleShortageBonus(
        lua_State state,
        WowPvpRoleShortageBonusState? bonus)
    {
        if (bonus is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_newtable(state);
        lua_newtable(state);
        for (var index = 0; index < bonus.ValidRoles.Count; index++)
        {
            lua_pushinteger(state, index + 1);
            if (bonus.ValidRoles[index] is { } role)
                lua_pushstring(state, role);
            else
                lua_pushnil(state);
            lua_settable(state, -3);
        }
        lua_setfield(state, -2, "validRoles");
        SetIntegerField(state, "rewardSpellID", bonus.RewardSpellId);
        SetIntegerField(state, "rewardItemID", bonus.RewardItemId);
    }

    private static void SetNilField(
        lua_State state,
        string name)
    {
        lua_pushnil(state);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalStringField(
        lua_State state,
        string name,
        string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalIntegerField(
        lua_State state,
        string name,
        int? value)
    {
        if (value is { } integer)
            lua_pushinteger(state, integer);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, name);
    }

    private static void SetStringField(
        lua_State state,
        string name,
        string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetIntegerField(
        lua_State state,
        string name,
        int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetNumberField(
        lua_State state,
        string name,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBooleanField(
        lua_State state,
        string name,
        bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }
}
