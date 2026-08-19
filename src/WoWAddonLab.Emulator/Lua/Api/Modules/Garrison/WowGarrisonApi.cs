using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowGarrisonApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly (int Value, string GlobalString)[] AutoCombatDamageClasses =
    [
        (127, "STRING_SCHOOL_ALL"),
        (1, "STRING_SCHOOL_PHYSICAL"),
        (2, "STRING_SCHOOL_HOLY"),
        (4, "STRING_SCHOOL_FIRE"),
        (8, "STRING_SCHOOL_NATURE"),
        (16, "STRING_SCHOOL_FROST"),
        (32, "STRING_SCHOOL_SHADOW"),
        (64, "STRING_SCHOOL_ARCANE"),
        (5, "STRING_SCHOOL_FLAMESTRIKE"),
        (17, "STRING_SCHOOL_FROSTSTRIKE"),
        (65, "STRING_SCHOOL_SPELLSTRIKE"),
        (33, "STRING_SCHOOL_SHADOWSTRIKE"),
        (9, "STRING_SCHOOL_STORMSTRIKE"),
        (3, "STRING_SCHOOL_HOLYSTRIKE"),
        (20, "STRING_SCHOOL_FROSTFIRE"),
        (68, "STRING_SCHOOL_SPELLFIRE"),
        (12, "STRING_SCHOOL_FIRESTORM"),
        (36, "STRING_SCHOOL_SHADOWFLAME"),
        (6, "STRING_SCHOOL_HOLYFIRE"),
        (80, "STRING_SCHOOL_SPELLFROST"),
        (24, "STRING_SCHOOL_FROSTSTORM"),
        (48, "STRING_SCHOOL_SHADOWFROST"),
        (18, "STRING_SCHOOL_HOLYFROST"),
        (72, "STRING_SCHOOL_SPELLSTORM"),
        (96, "STRING_SCHOOL_SPELLSHADOW"),
        (66, "STRING_SCHOOL_DIVINE"),
        (40, "STRING_SCHOOL_SHADOWSTORM"),
        (10, "STRING_SCHOOL_HOLYSTORM"),
        (34, "STRING_SCHOOL_SHADOWLIGHT"),
        (28, "STRING_SCHOOL_ELEMENTAL"),
        (62, "STRING_SCHOOL_CHROMATIC"),
        (126, "STRING_SCHOOL_MAGIC"),
        (124, "STRING_SCHOOL_CHAOS"),
        (106, "STRING_SCHOOL_COSMIC")
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetLandingPageGarrisonType",
                     "GetGarrisonPlotsInstancesForMap",
                     "GetAllEncounterThreats",
                     "GetAutoCombatDamageClassValues",
                     "GetBuildingSizes",
                     "GetFollowerXPTable",
                     "GetFollowerQualityTable",
                     "GetFollowerAbilityCountersForMechanicTypes",
                     "GetCurrencyTypes",
                     "GetInProgressMissions",
                     "GetCompleteMissions",
                     "GetAvailableMissions",
                     "GetBuildings",
                     "GetBuildingsForSize",
                     "GetCombatAllyMission",
                     "GetCurrentGarrTalentTreeID",
                     "GetTalentInfo",
                     "GetTalentTreeInfo",
                     "GetTalentTreeIDsByClassID",
                     "GetRecruiterAbilityCategories",
                     "GetNumFollowers",
                     "GetFollowers",
                     "GetAvailableRecruits",
                     "GetAllBonusAbilityEffects",
                     "GetLandingPageItems",
                     "GetAutoTroops",
                     "CanGenerateRecruits",
                     "CanSetRecruitmentPreference",
                     "GetGarrisonInfo",
                     "IsAtGarrisonMissionNPC",
                     "IsLandingPageMinimapButtonVisible",
                     "IsVisitGarrisonAvailable",
                     "CloseArchitect",
                     "CloseTalentNPC",
                     "CloseMissionNPC",
                     "CloseTradeskillCrafter",
                     "CloseGarrisonTradeskillNPC",
                     "CloseRecruitmentNPC",
                     "RequestLandingPageShipmentInfo",
                     "RequestGarrisonUpgradeable"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Garrison");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var garrison = runtime.Garrison;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "IsVisitGarrisonAvailable":
                lua_pushboolean(state, runtime.Garrison.IsVisitGarrisonAvailable ? 1 : 0);
                return 1;
            case "CanGenerateRecruits":
                lua_pushboolean(state, garrison.CanGenerateRecruits ? 1 : 0);
                return 1;
            case "CanSetRecruitmentPreference":
                lua_pushboolean(
                    state,
                    garrison.CanSetRecruitmentPreference ? 1 : 0);
                return 1;
            case "CloseArchitect":
                ClearInteraction(runtime.PlayerInteractions, 30);
                return 0;
            case "CloseGarrisonTradeskillNPC":
                if (runtime.PlayerInteractions.CurrentInteractionType is 31 or 59)
                {
                    ClearInteraction(
                        runtime.PlayerInteractions,
                        runtime.PlayerInteractions.CurrentInteractionType);
                }
                return 0;
            case "CloseMissionNPC":
                if (runtime.PlayerInteractions.CurrentInteractionType is 28 or 32)
                {
                    ClearInteraction(
                        runtime.PlayerInteractions,
                        runtime.PlayerInteractions.CurrentInteractionType);
                }
                return 0;
            case "CloseRecruitmentNPC":
                ClearInteraction(runtime.PlayerInteractions, 34);
                return 0;
            case "CloseTalentNPC":
                ClearInteraction(runtime.PlayerInteractions, 35);
                return 0;
            case "CloseTradeskillCrafter":
                ClearInteraction(runtime.PlayerInteractions, 33);
                return 0;
            case "GetAllBonusAbilityEffects":
            {
                var followerType = lua_isnumber(state, 1) != 0
                    ? RequiredInt32(
                        state,
                        1,
                        "Usage: GetAllBonusAbilityEffects([garrFollowerTypeID])")
                    : 0;
                if (!IsKnownFollowerType(garrison, followerType))
                {
                    return 0;
                }
                lua_newtable(state);
                if (garrison.BonusAbilityEffectsByFollowerType.TryGetValue(
                        followerType,
                        out var effects))
                {
                    for (var index = 0; index < effects.Count; index++)
                    {
                        PushBonusAbilityEffect(state, effects[index]);
                        lua_rawseti(state, -2, index + 1);
                    }
                }
                return 1;
            }
            case "GetAllEncounterThreats":
            {
                var followerType = RequiredInt32(
                    state,
                    1,
                    "Usage: GetAllEncounterThreats(garrFollowerTypeID)");
                lua_newtable(state);
                if (garrison.EncounterThreatsByFollowerType.TryGetValue(
                        followerType,
                        out var threats))
                {
                    for (var index = 0; index < threats.Count; index++)
                    {
                        PushEncounterThreat(state, threats[index]);
                        lua_rawseti(state, -2, index + 1);
                    }
                }
                return 1;
            }
            case "GetAvailableMissions":
                return PushMissionList(
                    state,
                    garrison,
                    garrison.AvailableMissionsByType,
                    "Usage: GetAvailableMissions([missionList,] garrFollowerTypeID)",
                    reuseSuppliedTable: true);
            case "GetAvailableRecruits":
                lua_newtable(state);
                for (var index = 0;
                     index < garrison.AvailableRecruits.Count;
                     index++)
                {
                    PushRecruitFollower(
                        state,
                        garrison.AvailableRecruits[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "GetBuildingSizes":
                lua_newtable(state);
                for (var index = 0; index < garrison.BuildingSizes.Count; index++)
                {
                    var size = garrison.BuildingSizes[index];
                    lua_newtable(state);
                    SetInteger(state, "id", size.Id);
                    SetOptionalString(state, "name", size.Name);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "GetBuildings":
            {
                var garrisonType = RequiredInt32(
                    state,
                    1,
                    "Usage: GetBuildings(garrisonType)");
                lua_newtable(state);
                if (garrison.BuildingsByGarrisonType.TryGetValue(
                        garrisonType,
                        out var buildings))
                {
                    for (var index = 0; index < buildings.Count; index++)
                    {
                        PushBuilding(state, buildings[index]);
                        lua_rawseti(state, -2, index + 1);
                    }
                }
                return 1;
            }
            case "GetBuildingsForSize":
            {
                const string usage =
                    "Usage: GetBuildingsForSize(garrisonType, uiCategoryID)";
                var garrisonType = RequiredInt32(state, 1, usage);
                var uiCategoryId = RequiredInt32(state, 2, usage);
                if (!IsKnownGarrisonType(garrison, garrisonType))
                {
                    return luaL_error(state, "Unknown garrison type");
                }
                lua_newtable(state);
                if (garrison.BuildingsBySize.TryGetValue(
                        (garrisonType, uiCategoryId),
                        out var buildings))
                {
                    for (var index = 0; index < buildings.Count; index++)
                    {
                        PushAvailableBuilding(state, buildings[index]);
                        lua_rawseti(state, -2, index + 1);
                    }
                }
                return 1;
            }
            case "GetCombatAllyMission":
            {
                var followerType = RequiredInt32(
                    state,
                    1,
                    "Usage: GetCombatAllyMission(garrFollowerTypeID)");
                if (!IsKnownFollowerType(garrison, followerType) ||
                    !garrison.CombatAllyMissionsByType.TryGetValue(
                        followerType,
                        out var mission))
                {
                    return 0;
                }
                PushMission(state, mission);
                return 1;
            }
            case "GetCompleteMissions":
                return PushMissionList(
                    state,
                    garrison,
                    garrison.CompleteMissionsByType,
                    "Usage: GetCompleteMissions([missionList,] garrFollowerTypeID)",
                    reuseSuppliedTable: false);
            case "GetAutoCombatDamageClassValues":
                PushAutoCombatDamageClasses(state, runtime);
                return 1;
            case "GetAutoTroops":
            {
                var followerType = RequiredInt32(
                    state,
                    1,
                    "Usage: local autoTroops = C_Garrison.GetAutoTroops(followerType)");
                lua_newtable(state);
                if (garrison.AutoTroopsByFollowerType.TryGetValue(
                        followerType,
                        out var troops))
                {
                    for (var index = 0; index < troops.Count; index++)
                    {
                        PushAutoTroop(state, troops[index]);
                        lua_rawseti(state, -2, index + 1);
                    }
                }
                return 1;
            }
            case "GetCurrencyTypes":
            {
                var garrisonType = RequiredInt32(
                    state,
                    1,
                    "Usage: GetCurrencyTypes(garrType)");
                if (!garrison.CurrencyTypesByGarrisonType.TryGetValue(
                        garrisonType,
                        out var currencyTypes))
                {
                    return 0;
                }
                lua_pushinteger(state, currencyTypes.FirstCurrencyType);
                lua_pushinteger(state, currencyTypes.SecondCurrencyType);
                return 2;
            }
            case "GetGarrisonInfo":
            {
                var garrisonType = RequiredInt32(
                    state,
                    1,
                    "Usage: GetGarrisonInfo(garrisonType)");
                if (!garrison.GarrisonInfoByType.TryGetValue(
                        garrisonType,
                        out var info))
                {
                    return 0;
                }
                lua_pushinteger(state, info.GarrisonLevel);
                if (info.GarrisonName is null)
                {
                    return 1;
                }
                lua_pushstring(state, info.GarrisonName);
                lua_pushnumber(state, info.MapX);
                lua_pushnumber(state, info.MapY);
                return 4;
            }
            case "GetNumFollowers":
            {
                var followerType = lua_isnumber(state, 1) != 0
                    ? RequiredInt32(
                        state,
                        1,
                        "Usage: GetNumFollowers([garrFollowerTypeID])")
                    : 4;
                lua_pushinteger(
                    state,
                    garrison.FollowerCountsByType.TryGetValue(
                        followerType,
                        out var followerCount)
                        ? followerCount
                        : 0);
                return 1;
            }
            case "GetFollowerAbilityCountersForMechanicTypes":
            {
                var followerType = RequiredInt32(
                    state,
                    1,
                    "Usage: GetFollowerAbilityCountersForMechanicTypes(garrFollowerTypeID)");
                if (followerType is not 4 and not 22)
                {
                    return 0;
                }
                lua_newtable(state);
                if (garrison.FollowerAbilityCountersByMechanicAndType.TryGetValue(
                        followerType,
                        out var counters))
                {
                    foreach (var (mechanicTypeId, ability) in counters)
                    {
                        lua_pushinteger(state, mechanicTypeId);
                        PushTalentAbility(state, ability);
                        lua_settable(state, -3);
                    }
                }
                return 1;
            }
            case "GetFollowerQualityTable":
            {
                var followerType = RequiredInt32(
                    state,
                    1,
                    "Usage: GetFollowerQualityTable(garrFollowerTypeID)");
                PushIntegerMap(
                    state,
                    garrison.FollowerXpByQualityAndType.TryGetValue(
                        followerType,
                        out var qualityXp)
                        ? qualityXp
                        : null);
                return 1;
            }
            case "GetFollowerXPTable":
            {
                var followerType = RequiredInt32(
                    state,
                    1,
                    "Usage: GetFollowerXPTable(garrFollowerTypeID)");
                PushIntegerMap(
                    state,
                    garrison.FollowerXpByLevelAndType.TryGetValue(
                        followerType,
                        out var levelXp)
                        ? levelXp
                        : null);
                return 1;
            }
            case "GetFollowers":
            {
                var followerType = lua_isnumber(state, 1) != 0
                    ? RequiredInt32(
                        state,
                        1,
                        "Usage: GetFollowers([garrFollowerTypeID])")
                    : 4;
                if (!IsKnownFollowerType(garrison, followerType))
                {
                    return 0;
                }
                lua_newtable(state);
                if (garrison.FollowersByType.TryGetValue(
                        followerType,
                        out var followers))
                {
                    for (var index = 0; index < followers.Count; index++)
                    {
                        PushOwnedFollower(state, followers[index]);
                        lua_rawseti(state, -2, index + 1);
                    }
                }
                return 1;
            }
            case "GetInProgressMissions":
                return PushMissionList(
                    state,
                    garrison,
                    garrison.InProgressMissionsByType,
                    "Usage: GetInProgressMissions([missionList,] garrFollowerTypeID)",
                    reuseSuppliedTable: true);
            case "GetLandingPageItems":
            {
                var garrisonType = RequiredInt32(
                    state,
                    1,
                    "Usage: GetLandingPageItems(garrTypeID [, noSort])");
                if (!IsKnownGarrisonType(garrison, garrisonType))
                {
                    return 0;
                }
                lua_newtable(state);
                if (garrison.LandingPageItemsByGarrisonType.TryGetValue(
                        garrisonType,
                        out var items))
                {
                    for (var index = 0; index < items.Count; index++)
                    {
                        PushLandingPageItem(state, items[index]);
                        lua_rawseti(state, -2, index + 1);
                    }
                }
                return 1;
            }
            case "GetRecruiterAbilityCategories":
                lua_newtable(state);
                var categories = garrison.RecruiterAbilityCategories
                    .OrderBy(category => category, StringComparer.Ordinal)
                    .ToArray();
                for (var index = 0; index < categories.Length; index++)
                {
                    if (categories[index] is { } category)
                    {
                        lua_pushstring(state, category);
                    }
                    else
                    {
                        lua_pushnil(state);
                    }
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "GetCurrentGarrTalentTreeID":
                if (runtime.PlayerInteractions.CurrentInteractionType is 35 or 51 &&
                    garrison.CurrentGarrTalentTreeId is { } currentTreeId)
                {
                    lua_pushinteger(state, currentTreeId);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            case "GetGarrisonPlotsInstancesForMap":
            {
                var uiMapId = RequiredInt32(
                    state,
                    1,
                    "Usage: local plotInstances = C_Garrison.GetGarrisonPlotsInstancesForMap(uiMapID)");
                lua_newtable(state);
                if (garrison.PlotInstancesByMapId.TryGetValue(
                        uiMapId,
                        out var plots))
                {
                    for (var index = 0; index < plots.Count; index++)
                    {
                        PushPlotInstance(state, plots[index]);
                        lua_rawseti(state, -2, index + 1);
                    }
                }
                return 1;
            }
            case "GetTalentTreeIDsByClassID":
            {
                const string usage =
                    "Usage: local treeIDs = C_Garrison.GetTalentTreeIDsByClassID(garrType, classID)";
                var garrisonType = RequiredInt32(state, 1, usage);
                var classId = RequiredInt32(state, 2, usage);
                if (garrisonType == 9)
                {
                    classId = 0;
                }
                if (!garrison.TalentTreeIdsByGarrisonTypeAndClassId.TryGetValue(
                        (garrisonType, classId),
                        out var treeIds) ||
                    treeIds.Count == 0)
                {
                    return 0;
                }
                PushIntegerArray(state, treeIds);
                return 1;
            }
            case "GetTalentInfo":
            {
                var talentId = RequiredInt32(
                    state,
                    1,
                    "Usage: local info = C_Garrison.GetTalentInfo(talentID)");
                var talent = garrison.TalentsById.TryGetValue(
                    talentId,
                    out var configured)
                    ? configured
                    : new WowGarrisonTalentState();
                PushTalent(state, talent);
                return 1;
            }
            case "GetTalentTreeInfo":
            {
                var treeId = RequiredInt32(
                    state,
                    1,
                    "Usage: local treeInfo = C_Garrison.GetTalentTreeInfo(treeID)");
                var tree = garrison.TalentTreesById.TryGetValue(
                    treeId,
                    out var configured)
                    ? configured
                    : new WowGarrisonTalentTreeState();
                PushTalentTree(state, tree);
                return 1;
            }
            case "IsAtGarrisonMissionNPC":
                lua_pushboolean(
                    state,
                    runtime.PlayerInteractions.CurrentInteractionType == 32 ? 1 : 0);
                return 1;
            case "IsLandingPageMinimapButtonVisible":
            {
                var garrisonType = RequiredInt32(
                    state,
                    1,
                    "Usage: local visible = C_Garrison.IsLandingPageMinimapButtonVisible(garrType)");
                lua_pushboolean(
                    state,
                    garrison.VisibleLandingPageGarrisonTypes.Contains(garrisonType)
                        ? 1
                        : 0);
                return 1;
            }
            case "RequestGarrisonUpgradeable":
            {
                var followerType = RequiredInt32(
                    state,
                    1,
                    "Usage: RequestGarrisonUpgradeable(followerType)");
                if (!garrison.GarrisonIdsByFollowerType.ContainsKey(followerType))
                {
                    return luaL_error(state, "Unknown follower type");
                }
                garrison.GarrisonUpgradeableRequests.Add(followerType);
                return 0;
            }
            case "RequestLandingPageShipmentInfo":
                garrison.LandingPageShipmentInfoRequestCount++;
                return 0;
        }
        if (operation == "GetLandingPageGarrisonType")
        {
            lua_pushinteger(state, garrison.LandingPageGarrisonType);
            return 1;
        }
        lua_pushnil(state);
        return 1;
    }

    private static int PushMissionList(
        lua_State state,
        WowGarrisonState garrison,
        IDictionary<int, IList<WowGarrisonMissionState>> missionsByType,
        string usage,
        bool reuseSuppliedTable)
    {
        var suppliedTable = lua_type(state, 1) == LUA_TTABLE;
        var followerType = RequiredInt32(
            state,
            suppliedTable ? 2 : 1,
            usage);
        if (!IsKnownFollowerType(garrison, followerType))
        {
            return 0;
        }

        if (reuseSuppliedTable && suppliedTable)
        {
            ClearArray(state, 1);
            lua_pushvalue(state, 1);
        }
        else
        {
            lua_newtable(state);
        }

        if (missionsByType.TryGetValue(followerType, out var missions))
        {
            for (var index = 0; index < missions.Count; index++)
            {
                PushMission(state, missions[index]);
                lua_rawseti(state, -2, index + 1);
            }
        }
        return 1;
    }

    private static bool IsKnownFollowerType(
        WowGarrisonState garrison,
        int followerType) =>
        garrison.GarrisonIdsByFollowerType.ContainsKey(followerType) ||
        garrison.FollowersByType.ContainsKey(followerType) ||
        garrison.AvailableMissionsByType.ContainsKey(followerType) ||
        garrison.InProgressMissionsByType.ContainsKey(followerType) ||
        garrison.CompleteMissionsByType.ContainsKey(followerType) ||
        garrison.CombatAllyMissionsByType.ContainsKey(followerType) ||
        garrison.BonusAbilityEffectsByFollowerType.ContainsKey(followerType);

    private static bool IsKnownGarrisonType(
        WowGarrisonState garrison,
        int garrisonType) =>
        garrison.KnownGarrisonTypes.Contains(garrisonType) ||
        garrison.GarrisonInfoByType.ContainsKey(garrisonType) ||
        garrison.CurrencyTypesByGarrisonType.ContainsKey(garrisonType) ||
        garrison.BuildingsByGarrisonType.ContainsKey(garrisonType) ||
        garrison.LandingPageItemsByGarrisonType.ContainsKey(garrisonType);

    private static void ClearInteraction(
        WowPlayerInteractionManagerState interactions,
        int interactionType)
    {
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType = interactionType;
        if (!interactions.HasActiveInteraction ||
            interactions.CurrentInteractionType != interactionType)
        {
            return;
        }

        interactions.HasActiveInteraction = false;
        interactions.HasPendingInteraction = false;
        interactions.CurrentInteractionType = 0;
        interactions.PendingInteractionType = 0;
        interactions.ValidNpcInteractionTypes.Clear();
    }

    private static int RequiredInt32(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            return luaL_error(state, usage);
        }

        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
        {
            return luaL_error(state, usage);
        }

        return (int)value;
    }

    private static void PushAutoCombatDamageClasses(
        lua_State state,
        LuaRuntime runtime)
    {
        lua_newtable(state);
        for (var index = 0; index < AutoCombatDamageClasses.Length; index++)
        {
            var damageClass = AutoCombatDamageClasses[index];
            lua_newtable(state);
            SetInteger(state, "damageClassValue", damageClass.Value);
            var localized = runtime.GlobalStringProvider?.Strings.TryGetValue(
                damageClass.GlobalString,
                out var resolved) == true
                ? resolved
                : damageClass.GlobalString;
            SetString(state, "locString", localized);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushPlotInstance(
        lua_State state,
        WowGarrisonPlotInstanceState plot)
    {
        lua_newtable(state);
        SetInteger(
            state,
            "buildingPlotInstanceID",
            plot.BuildingPlotInstanceId);
        lua_newtable(state);
        SetNumber(state, "x", plot.X);
        SetNumber(state, "y", plot.Y);
        lua_setfield(state, -2, "position");
        SetOptionalString(state, "name", plot.Name);
        SetString(state, "atlasName", plot.AtlasName);
    }

    private static void PushEncounterThreat(
        lua_State state,
        WowGarrisonEncounterThreatState threat)
    {
        lua_newtable(state);
        SetInteger(state, "id", threat.Id);
        SetOptionalString(state, "name", threat.Name);
        SetOptionalInteger(state, "icon", threat.Icon);
        SetNumber(state, "factor", threat.Factor);
    }

    private static void PushBuilding(
        lua_State state,
        WowGarrisonBuildingState building)
    {
        lua_newtable(state);
        SetInteger(state, "buildingID", building.BuildingId);
        SetInteger(state, "plotID", building.PlotId);
        SetInteger(state, "uiTab", building.UiTab);
        SetOptionalString(state, "textureKit", building.TextureKit);
    }

    private static void PushAvailableBuilding(
        lua_State state,
        WowGarrisonAvailableBuildingState building)
    {
        lua_newtable(state);
        SetInteger(state, "buildingID", building.BuildingId);
        SetOptionalInteger(state, "plotID", building.PlotId);
        SetOptionalString(state, "name", building.Name);
        SetOptionalInteger(state, "icon", building.Icon);
        SetBoolean(state, "needsPlan", building.NeedsPlan);
        SetInteger(state, "cost", building.Cost);
        SetInteger(state, "goldCost", building.GoldCost);
        SetString(state, "buildTime", building.BuildTime);
    }

    private static void PushBonusAbilityEffect(
        lua_State state,
        WowGarrisonBonusAbilityEffectState effect)
    {
        lua_newtable(state);
        SetInteger(state, "bonusAbilityID", effect.BonusAbilityId);
        SetString(state, "textureKit", effect.TextureKit);
        SetNumber(state, "posX", effect.PosX);
        SetNumber(state, "posY", effect.PosY);
        SetNumber(state, "startTime", effect.StartTime);
        SetInteger(state, "duration", effect.Duration);
        SetNumber(state, "radius", effect.Radius);
        SetOptionalString(state, "name", effect.Name);
        SetOptionalString(state, "description", effect.Description);
        SetOptionalInteger(state, "icon", effect.Icon);
    }

    private static void PushLegacyFollowerDisplays(
        lua_State state,
        IReadOnlyList<WowGarrisonLegacyFollowerDisplayState> displays)
    {
        lua_newtable(state);
        for (var index = 0; index < displays.Count; index++)
        {
            var display = displays[index];
            lua_newtable(state);
            SetInteger(state, "id", display.Id);
            SetNumber(state, "followerPageScale", display.FollowerPageScale);
            SetOptionalBoolean(state, "showWeapon", display.ShowWeapon);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushRecruitFollower(
        lua_State state,
        WowGarrisonFollowerState follower)
    {
        lua_newtable(state);
        SetIdentifier(state, "followerID", follower.FollowerId);
        SetOptionalString(state, "name", follower.Name);
        SetInteger(state, "level", follower.Level);
        SetBoolean(state, "isMaxLevel", follower.IsMaxLevel);
        SetInteger(state, "iLevel", follower.ItemLevel);
        PushLegacyFollowerDisplays(state, follower.DisplayIds);
        lua_setfield(state, -2, "displayIDs");
        SetInteger(state, "portraitIconID", follower.PortraitIconId);
        SetOptionalNumber(state, "scale", follower.Scale);
        SetOptionalNumber(state, "height", follower.Height);
        SetOptionalNumber(state, "displayScale", follower.DisplayScale);
        SetOptionalNumber(state, "displayHeight", follower.DisplayHeight);
        SetInteger(state, "quality", follower.Quality);
        SetOptionalInteger(state, "classSpec", follower.ClassSpec);
        SetOptionalString(state, "className", follower.ClassName);
        SetOptionalString(state, "classAtlas", follower.ClassAtlas);
        SetBoolean(state, "isFavorite", follower.IsFavorite);
        SetOptionalString(state, "textureKit", follower.TextureKit);
        SetOptionalString(state, "flavorText", follower.FlavorText);
        SetInteger(state, "followerTypeID", follower.FollowerTypeId);
    }

    private static void PushOwnedFollower(
        lua_State state,
        WowGarrisonFollowerState follower)
    {
        lua_newtable(state);
        SetBoolean(state, "isCollected", follower.IsCollected);
        SetIdentifier(state, "followerID", follower.FollowerId);
        SetInteger(state, "garrFollowerID", follower.GarrFollowerId);
        SetOptionalString(state, "name", follower.Name);
        SetInteger(state, "level", follower.Level);
        SetBoolean(state, "isMaxLevel", follower.IsMaxLevel);
        SetInteger(state, "iLevel", follower.ItemLevel);
        PushLegacyFollowerDisplays(state, follower.DisplayIds);
        lua_setfield(state, -2, "displayIDs");
        SetInteger(state, "portraitIconID", follower.PortraitIconId);
        SetOptionalInteger(
            state,
            "zoneSupportSpellID",
            follower.ZoneSupportSpellId);
        SetOptionalNumber(state, "scale", follower.Scale);
        SetOptionalNumber(state, "height", follower.Height);
        SetOptionalNumber(state, "displayScale", follower.DisplayScale);
        SetOptionalNumber(state, "displayHeight", follower.DisplayHeight);
        SetInteger(state, "quality", follower.Quality);
        SetInteger(state, "xp", follower.Xp);
        SetInteger(state, "levelXP", follower.LevelXp);
        SetOptionalString(state, "status", follower.Status);
        SetOptionalInteger(state, "classSpec", follower.ClassSpec);
        SetOptionalString(state, "className", follower.ClassName);
        SetOptionalString(state, "classAtlas", follower.ClassAtlas);
        SetBoolean(state, "isFavorite", follower.IsFavorite);
        SetOptionalString(state, "textureKit", follower.TextureKit);
        SetInteger(state, "slotSoundKitID", follower.SlotSoundKitId);
        SetBoolean(state, "isTroop", follower.IsTroop);
        SetInteger(state, "durability", follower.Durability);
        SetInteger(state, "maxDurability", follower.MaxDurability);
        SetBoolean(state, "isAutoTroop", follower.IsAutoTroop);
        SetBoolean(state, "isSoulbind", follower.IsSoulbind);
        SetOptionalString(state, "flavorText", follower.FlavorText);
        SetInteger(state, "followerTypeID", follower.FollowerTypeId);
        SetInteger(state, "health", follower.Health);
        SetInteger(state, "maxHealth", follower.MaxHealth);
        SetInteger(state, "role", follower.Role);
    }

    private static void PushMission(
        lua_State state,
        WowGarrisonMissionState mission)
    {
        lua_newtable(state);
        SetInteger(state, "missionID", mission.MissionId);
        SetInteger(state, "followerTypeID", mission.FollowerTypeId);
        SetOptionalString(state, "name", mission.Name);
        SetOptionalString(state, "description", mission.Description);
        SetOptionalString(state, "location", mission.Location);
        SetOptionalString(state, "locTextureKit", mission.LocationTextureKit);
        SetInteger(state, "level", mission.Level);
        SetInteger(state, "xp", mission.Xp);
        SetBoolean(state, "isMaxLevel", mission.IsMaxLevel);
        SetInteger(state, "iLevel", mission.ItemLevel);
        SetInteger(state, "numFollowers", mission.NumFollowers);
        SetInteger(
            state,
            "requiredChampionCount",
            mission.RequiredChampionCount);
        SetInteger(state, "requiredChampions", mission.RequiredChampions);
        SetInteger(
            state,
            "requiredSuccessChance",
            mission.RequiredSuccessChance);
        SetOptionalString(state, "duration", mission.Duration);
        SetInteger(state, "durationSeconds", mission.DurationSeconds);
        SetBoolean(state, "isRare", mission.IsRare);
        SetBoolean(state, "isZoneSupport", mission.IsZoneSupport);
        SetInteger(state, "areaID", mission.AreaId);
        SetInteger(state, "cost", mission.Cost);
        SetInteger(state, "basecost", mission.BaseCost);
        SetInteger(
            state,
            "costCurrencyTypesID",
            mission.CostCurrencyTypesId);
        SetOptionalString(
            state,
            "offerTimeRemaining",
            mission.OfferTimeRemaining);
        SetOptionalNumber(state, "offerEndTime", mission.OfferEndTime);
        lua_newtable(state);
        for (var index = 0; index < mission.Followers.Count; index++)
        {
            lua_pushstring(state, mission.Followers[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "followers");
        SetBoolean(state, "inProgress", mission.InProgress);
        SetBoolean(state, "completed", mission.Completed);
        PushMissionRewards(state, mission.Rewards);
        lua_setfield(state, -2, "rewards");
        PushMissionRewards(state, mission.OvermaxRewards);
        lua_setfield(state, -2, "overmaxRewards");
        SetBoolean(state, "overmaxSucceeded", mission.OvermaxSucceeded);
        SetNumber(state, "mapPosX", mission.MapPosX);
        SetNumber(state, "mapPosY", mission.MapPosY);
        SetBoolean(state, "canStart", mission.CanStart);
        SetInteger(
            state,
            "offeredGarrMissionTextureID",
            mission.OfferedGarrMissionTextureId);
        SetOptionalString(state, "timeLeft", mission.TimeLeft);
        SetOptionalInteger(state, "timeLeftSeconds", mission.TimeLeftSeconds);
        SetOptionalNumber(state, "missionEndTime", mission.MissionEndTime);
        SetOptionalString(state, "type", mission.Type);
        SetOptionalString(state, "typeAtlas", mission.TypeAtlas);
        SetOptionalString(state, "typeTextureKit", mission.TypeTextureKit);
        SetBoolean(state, "hasBonusEffect", mission.HasBonusEffect);
        SetInteger(state, "missionScalar", mission.MissionScalar);
        SetOptionalBoolean(
            state,
            "isTutorialMission",
            mission.IsTutorialMission);
    }

    private static void PushMissionRewards(
        lua_State state,
        IReadOnlyList<WowGarrisonMissionRewardState> rewards)
    {
        lua_newtable(state);
        for (var index = 0; index < rewards.Count; index++)
        {
            var reward = rewards[index];
            lua_newtable(state);
            SetOptionalInteger(state, "itemID", reward.ItemId);
            SetOptionalInteger(state, "quantity", reward.Quantity);
            SetOptionalString(state, "itemLink", reward.ItemLink);
            SetOptionalInteger(state, "followerXP", reward.FollowerXp);
            SetOptionalScalar(state, "icon", reward.Icon);
            SetOptionalString(state, "title", reward.Title);
            SetOptionalString(state, "tooltip", reward.Tooltip);
            SetOptionalString(state, "name", reward.Name);
            SetOptionalInteger(state, "currencyID", reward.CurrencyId);
            SetOptionalInteger(
                state,
                "bonusAbilityID",
                reward.BonusAbilityId);
            SetOptionalString(state, "textureAtlas", reward.TextureAtlas);
            SetOptionalNumber(state, "posX", reward.PosX);
            SetOptionalNumber(state, "posY", reward.PosY);
            SetOptionalString(state, "description", reward.Description);
            SetOptionalInteger(state, "duration", reward.Duration);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushLandingPageItem(
        lua_State state,
        WowGarrisonLandingPageItemState item)
    {
        if (item.IsBuilding)
        {
            lua_newtable(state);
            SetInteger(state, "buildingID", item.BuildingId);
            SetOptionalString(state, "name", item.Name);
            SetInteger(state, "buildingLevel", item.BuildingLevel);
            SetOptionalString(state, "timeLeft", item.TimeLeft);
        }
        else if (item.Mission is { } mission)
        {
            PushMission(state, mission);
        }
        else
        {
            lua_newtable(state);
        }
        SetBoolean(state, "isBuilding", item.IsBuilding);
        SetBoolean(state, "isComplete", item.IsComplete);
    }

    private static void PushAutoTroop(
        lua_State state,
        WowGarrisonAutoTroopState troop)
    {
        lua_newtable(state);
        SetOptionalString(state, "name", troop.Name);
        SetIdentifier(state, "followerID", troop.FollowerId);
        SetIdentifier(state, "garrFollowerID", troop.GarrFollowerId);
        SetInteger(state, "followerTypeID", troop.FollowerTypeId);
        lua_newtable(state);
        for (var index = 0; index < troop.DisplayIds.Count; index++)
        {
            var display = troop.DisplayIds[index];
            lua_newtable(state);
            SetInteger(state, "id", display.Id);
            SetNumber(state, "followerPageScale", display.FollowerPageScale);
            SetBoolean(state, "showWeapon", display.ShowWeapon);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "displayIDs");
        SetInteger(state, "level", troop.Level);
        SetInteger(state, "quality", troop.Quality);
        SetInteger(state, "levelXP", troop.LevelXp);
        SetInteger(state, "maxXP", troop.MaxXp);
        SetNumber(state, "height", troop.Height);
        SetNumber(state, "scale", troop.Scale);
        SetOptionalNumber(state, "displayScale", troop.DisplayScale);
        SetOptionalNumber(state, "displayHeight", troop.DisplayHeight);
        SetOptionalInteger(state, "classSpec", troop.ClassSpec);
        SetOptionalString(state, "className", troop.ClassName);
        SetOptionalString(state, "flavorText", troop.FlavorText);
        SetString(state, "classAtlas", troop.ClassAtlas);
        SetInteger(state, "portraitIconID", troop.PortraitIconId);
        SetString(state, "textureKit", troop.TextureKit);
        SetBoolean(state, "isTroop", troop.IsTroop);
        SetInteger(state, "raceID", troop.RaceId);
        SetInteger(state, "health", troop.Health);
        SetInteger(state, "maxHealth", troop.MaxHealth);
        SetInteger(state, "role", troop.Role);
        SetBoolean(state, "isAutoTroop", troop.IsAutoTroop);
        SetBoolean(state, "isSoulbind", troop.IsSoulbind);
        SetBoolean(state, "isCollected", troop.IsCollected);
        PushAutoCombatStats(state, troop.AutoCombatStats);
        lua_setfield(state, -2, "autoCombatStats");
    }

    private static void PushAutoCombatStats(
        lua_State state,
        WowGarrisonAutoCombatStatsState stats)
    {
        lua_newtable(state);
        SetInteger(state, "currentHealth", stats.CurrentHealth);
        SetInteger(state, "maxHealth", stats.MaxHealth);
        SetInteger(state, "attack", stats.Attack);
        SetInteger(state, "healingTimestamp", stats.HealingTimestamp);
        SetInteger(state, "healCost", stats.HealCost);
        SetInteger(
            state,
            "minutesHealingRemaining",
            stats.MinutesHealingRemaining);
    }

    private static void PushTalentTree(
        lua_State state,
        WowGarrisonTalentTreeState tree)
    {
        lua_newtable(state);
        SetInteger(state, "treeID", tree.TreeId);
        SetOptionalString(state, "title", tree.Title);
        SetString(state, "textureKit", tree.TextureKit);
        lua_newtable(state);
        for (var index = 0; index < tree.Talents.Count; index++)
        {
            PushTalent(state, tree.Talents[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "talents");
        SetBoolean(state, "isClassAgnostic", tree.IsClassAgnostic);
        SetBoolean(state, "isThemed", tree.IsThemed);
        SetInteger(state, "featureType", tree.FeatureType);
        SetInteger(state, "featureSubtype", tree.FeatureSubtype);
    }

    private static void PushTalent(
        lua_State state,
        WowGarrisonTalentState talent)
    {
        lua_newtable(state);
        SetInteger(state, "id", talent.Id);
        PushTalentAbility(state, talent.Ability);
        lua_setfield(state, -2, "ability");
        SetString(state, "name", talent.Name);
        SetInteger(state, "icon", talent.Icon);
        SetInteger(state, "tier", talent.Tier);
        SetInteger(state, "uiOrder", talent.UiOrder);
        SetInteger(state, "type", talent.Type);
        SetOptionalInteger(
            state,
            "prerequisiteTalentID",
            talent.PrerequisiteTalentId);
        SetBoolean(state, "selected", talent.Selected);
        SetBoolean(state, "researched", talent.Researched);
        SetBoolean(state, "ignoreTalent", talent.IgnoreTalent);
        SetInteger(state, "researchDuration", talent.ResearchDuration);
        SetInteger(state, "startTime", talent.StartTime);
        SetInteger(state, "timeRemaining", talent.TimeRemaining);
        SetInteger(state, "researchGoldCost", talent.ResearchGoldCost);
        lua_newtable(state);
        for (var index = 0; index < talent.ResearchCurrencyCosts.Count; index++)
        {
            var cost = talent.ResearchCurrencyCosts[index];
            lua_newtable(state);
            SetInteger(state, "currencyType", cost.CurrencyType);
            SetInteger(state, "currencyQuantity", cost.CurrencyQuantity);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "researchCurrencyCosts");
        SetNumber(state, "talentAvailability", talent.TalentAvailability);
        SetInteger(state, "talentRank", talent.TalentRank);
        SetInteger(state, "talentMaxRank", talent.TalentMaxRank);
        SetBoolean(state, "isBeingResearched", talent.IsBeingResearched);
        SetString(state, "description", talent.Description);
        SetInteger(state, "perkSpellID", talent.PerkSpellId);
        SetOptionalString(
            state,
            "researchDescription",
            talent.ResearchDescription);
        SetOptionalString(
            state,
            "playerConditionReason",
            talent.PlayerConditionReason);
        lua_newtable(state);
        SetInteger(state, "socketType", talent.SocketInfo.SocketType);
        SetInteger(state, "socketSubtype", talent.SocketInfo.SocketSubtype);
        SetInteger(state, "misc0", talent.SocketInfo.Misc0);
        SetInteger(state, "misc1", talent.SocketInfo.Misc1);
        lua_setfield(state, -2, "socketInfo");
        SetInteger(state, "treeID", talent.TreeId);
    }

    private static void PushTalentAbility(
        lua_State state,
        WowGarrisonTalentAbilityState ability)
    {
        lua_newtable(state);
        SetInteger(state, "id", ability.Id);
        SetOptionalString(state, "name", ability.Name);
        SetString(state, "description", ability.Description);
        SetInteger(state, "icon", ability.Icon);
        SetBoolean(state, "isTrait", ability.IsTrait);
        SetBoolean(state, "isSpecialization", ability.IsSpecialization);
        SetBoolean(state, "temporary", ability.Temporary);
        SetOptionalString(state, "category", ability.Category);
        lua_newtable(state);
        for (var index = 0; index < ability.Counters.Count; index++)
        {
            var counter = ability.Counters[index];
            lua_newtable(state);
            SetOptionalString(state, "name", counter.Name);
            SetOptionalString(state, "description", counter.Description);
            SetInteger(state, "icon", counter.Icon);
            SetNumber(state, "factor", counter.Factor);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "counters");
        SetBoolean(state, "isEmptySlot", ability.IsEmptySlot);
    }

    private static void PushIntegerArray(
        lua_State state,
        IEnumerable<int> values)
    {
        lua_newtable(state);
        var index = 1;
        foreach (var value in values)
        {
            lua_pushinteger(state, value);
            lua_rawseti(state, -2, index++);
        }
    }

    private static void PushIntegerMap(
        lua_State state,
        IDictionary<int, int>? values)
    {
        lua_newtable(state);
        if (values is null)
        {
            return;
        }
        foreach (var (key, value) in values)
        {
            lua_pushinteger(state, key);
            lua_pushinteger(state, value);
            lua_settable(state, -3);
        }
    }

    private static void SetIdentifier(
        lua_State state,
        string field,
        object? value)
    {
        switch (value)
        {
            case null:
                lua_pushnil(state);
                break;
            case string text:
                lua_pushstring(state, text);
                break;
            case sbyte or byte or short or ushort or int or uint or long or ulong:
                lua_pushnumber(state, Convert.ToDouble(value));
                break;
            default:
                throw new ArgumentException(
                    "Follower identifiers must be strings, integral numbers, or null.",
                    nameof(value));
        }
        lua_setfield(state, -2, field);
    }

    private static void SetInteger(
        lua_State state,
        string field,
        long value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetNumber(
        lua_State state,
        string field,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetBoolean(
        lua_State state,
        string field,
        bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
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

    private static void SetOptionalInteger(
        lua_State state,
        string field,
        int? value)
    {
        if (value is { } integer)
        {
            lua_pushinteger(state, integer);
        }
        else
        {
            lua_pushnil(state);
        }
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalNumber(
        lua_State state,
        string field,
        float? value)
    {
        if (value is { } number)
        {
            lua_pushnumber(state, number);
        }
        else
        {
            lua_pushnil(state);
        }
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalNumber(
        lua_State state,
        string field,
        double? value)
    {
        if (value is { } number)
        {
            lua_pushnumber(state, number);
        }
        else
        {
            lua_pushnil(state);
        }
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalBoolean(
        lua_State state,
        string field,
        bool? value)
    {
        if (value is { } boolean)
        {
            lua_pushboolean(state, boolean ? 1 : 0);
        }
        else
        {
            lua_pushnil(state);
        }
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalScalar(
        lua_State state,
        string field,
        object? value)
    {
        switch (value)
        {
            case null:
                lua_pushnil(state);
                break;
            case string text:
                lua_pushstring(state, text);
                break;
            case sbyte or byte or short or ushort or int or uint or long or ulong:
                lua_pushnumber(state, Convert.ToDouble(value));
                break;
            default:
                throw new ArgumentException(
                    "Lua scalar values must be strings, integral numbers, or null.",
                    nameof(value));
        }
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        if (value is null)
        {
            lua_pushnil(state);
        }
        else
        {
            lua_pushstring(state, value);
        }
        lua_setfield(state, -2, field);
    }

    private static void ClearArray(lua_State state, int index)
    {
        var count = (int)lua_objlen(state, index);
        for (var item = 1; item <= count; item++)
        {
            lua_pushnil(state);
            lua_rawseti(state, index, item);
        }
    }
}
