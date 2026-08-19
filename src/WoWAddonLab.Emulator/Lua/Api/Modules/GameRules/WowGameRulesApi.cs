using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowGameRulesApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] FrameStrataNames =
    [
        "WORLD",
        "BACKGROUND",
        "LOW",
        "MEDIUM",
        "HIGH",
        "DIALOG",
        "FULLSCREEN",
        "FULLSCREEN_DIALOG",
        "TOOLTIP",
        "BLIZZARD"
    ];

    private static readonly string[] Functions =
    [
        "DoesGameModeHavePromo",
        "GetActiveGameMode",
        "GetCurrentGameModeDisplayInfo",
        "GetCurrentEventRealmQueues",
        "GetCurrentGameModeRecordID",
        "GetDisplayedGameModeRecordIDAtIndex",
        "GetGameModeDisplayInfoByRecordID",
        "GetGameModeGlueScreenName",
        "GetGameModePromoGlobalString",
        "GetGameRuleAsFloat",
        "GetGameRuleAsFrameStrata",
        "GetNumDisplayedGameModes",
        "IsCharacterlessLoginActive",
        "IsClassAllowedForGameMode",
        "IsGameModeEnabled",
        "IsGameRuleActive",
        "IsMultiActionBarVisibilityForced",
        "IsPersonalResourceDisplayEnabled",
        "IsPlunderstorm",
        "IsStandard",
        "IsWoWHack"
    ];

    private static readonly IReadOnlyDictionary<string, int> GameRuleValues =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["NoDebuffLimit"] = 1,
            ["CharNameReservationEnabled"] = 2,
            ["MaxCharReservationsPerRealm"] = 3,
            ["MaxAccountCharReservationsPerContentset"] = 4,
            ["EtaRealmLaunchTime"] = 5,
            ["TrivialGroupXPPercent"] = 7,
            ["CharReservationsPerRealmReopenThreshold"] = 8,
            ["DisablePct"] = 9,
            ["HardcoreRuleset"] = 10,
            ["ReplaceAbsentGmSeconds"] = 11,
            ["ReplaceGmRankLastOnlineSeconds"] = 12,
            ["GameMode"] = 13,
            ["CharacterlessLogin"] = 14,
            ["VanillaNpcKnockback"] = 16,
            ["Runecarving"] = 17,
            ["TalentRespecCostMin"] = 18,
            ["TalentRespecCostMax"] = 19,
            ["TalentRespecCostStep"] = 20,
            ["VanillaRageGenerationModifier"] = 21,
            ["SelfFoundAllowed"] = 22,
            ["DisableHonorDecay"] = 23,
            ["MaxLootDropLevel"] = 25,
            ["MicrobarScale"] = 26,
            ["MaxUnitNameDistance"] = 27,
            ["MaxNameplateDistance"] = 28,
            ["UserAddonsDisabled"] = 29,
            ["UserScriptsDisabled"] = 30,
            ["NonPlayerNameplateScale"] = 31,
            ["ForcedPartyFrameScale"] = 32,
            ["CustomActionbarOverlayHeightOffset"] = 33,
            ["ForcedChatLanguage"] = 34,
            ["LandingPageFactionID"] = 35,
            ["CollectionsPanelDisabled"] = 36,
            ["CharacterPanelDisabled"] = 37,
            ["SpellbookPanelDisabled"] = 38,
            ["TalentsPanelDisabled"] = 39,
            ["AchievementsPanelDisabled"] = 40,
            ["CommunitiesPanelDisabled"] = 41,
            ["EncounterJournalDisabled"] = 42,
            ["FinderPanelDisabled"] = 43,
            ["StoreDisabled"] = 44,
            ["HelpPanelDisabled"] = 45,
            ["GuildsDisabled"] = 46,
            ["QuestLogMicrobuttonDisabled"] = 47,
            ["MapPlunderstormCircle"] = 48,
            ["AfterDeathSpectatingUI"] = 49,
            ["FrontEndChat"] = 50,
            ["UniversalNameplateOcclusion"] = 51,
            ["FastAreaTriggerTick"] = 52,
            ["AllPlayersAreFastMovers"] = 53,
            ["IgnoreChrclassDisabledFlag"] = 54,
            ["CharacterCreateUseFixedBackgroundModel"] = 55,
            ["ForceAlteredFormsOn"] = 56,
            ["PlayerNameplateDifficultyIcon"] = 57,
            ["PlayerNameplateAlternateHealthColor"] = 58,
            ["AlwaysAllowAlliedRaces"] = 59,
            ["ActionbarIconIntroDisabled"] = 60,
            ["ReleaseSpiritGhostDisabled"] = 61,
            ["DeleteItemConfirmationDisabled"] = 62,
            ["ChatLinkLevelToastsDisabled"] = 63,
            ["BagsUIDisabled"] = 64,
            ["PetBattlesDisabled"] = 65,
            ["PerksProgramActivityTrackingDisabled"] = 66,
            ["MaximizeWorldMapDisabled"] = 67,
            ["WorldMapTrackingOptionsDisabled"] = 68,
            ["WorldMapTrackingPinDisabled"] = 69,
            ["WorldMapHelpPlateDisabled"] = 70,
            ["QuestLogPanelDisabled"] = 71,
            ["QuestLogSuperTrackingDisabled"] = 72,
            ["TutorialFrameDisabled"] = 73,
            ["IngameMailNotificationDisabled"] = 74,
            ["IngameCalendarDisabled"] = 75,
            ["IngameTrackingDisabled"] = 76,
            ["IngameWhoListDisabled"] = 77,
            ["RaceAlteredFormsDisabled"] = 78,
            ["IngameFriendsListDisabled"] = 79,
            ["MacrosDisabled"] = 80,
            ["CompactRaidFrameManagerDisabled"] = 81,
            ["EditModeDisabled"] = 82,
            ["InstanceDifficultyBannerDisabled"] = 83,
            ["FullCharacterCreateDisabled"] = 84,
            ["TargetFrameBuffsDisabled"] = 85,
            ["UnitFramePvPContextualDisabled"] = 86,
            ["ActionCombatTargetLockEnabled"] = 87,
            ["BlockWhileSheathedAllowed"] = 88,
            ["VanillaAccountMailInstant"] = 91,
            ["ClearMailOnRealmTransfer"] = 92,
            ["PremadeGroupFinderStyle"] = 93,
            ["PlunderstormAreaSelection"] = 94,
            ["GroupFinderCapabilities"] = 98,
            ["WorldMapLegendDisabled"] = 99,
            ["WorldMapFrameStrata"] = 100,
            ["MerchantFilterDisabled"] = 101,
            ["HousingDashboardDisabled"] = 102,
            ["AutoAttacksDisabled"] = 103,
            ["ObjectiveTrackerDisabled"] = 104,
            ["PlayerCastBarDisabled"] = 105,
            ["TargetCastBarDisabled"] = 106,
            ["NameplateCastBarDisabled"] = 107,
            ["SummoningStones"] = 108,
            ["TransmogEnabled"] = 109,
            ["DisableRealmSelection"] = 113,
            ["DisableCampsites"] = 114,
            ["UseSimpleCharacterSelectList"] = 115,
            ["HideFaction"] = 116,
            ["DisableVas"] = 119,
            ["PersonalResourceDisplayDisabled"] = 129,
            ["TargetFrameDisabled"] = 130,
            ["PlayerFrameDisabled"] = 131,
            ["MailGameRule"] = 132,
            ["ForcedMultiActionBarSetting"] = 133,
            ["HideAllMultiActionBars"] = 135,
            ["TimerunningAllowed"] = 137,
            ["MaxCharactersPerContentSet"] = 139,
            ["MinUndeleteLevelRequired"] = 140,
            ["DoesNotCountTowardAccountCharacterMax"] = 142,
            ["WorldMapDisabled"] = 145,
            ["MinimapDisabled"] = 146,
            ["RepairArmorDisabled"] = 147,
            ["EjSuggestedContentDisabled"] = 148,
            ["EjDungeonsDisabled"] = 149,
            ["EjRaidsDisabled"] = 150,
            ["EjItemSetsDisabled"] = 151,
            ["GdapiCharacterProfileDisabled"] = 153,
            ["HousingEnabled"] = 154,
            ["RestrictedAchievementCategoryID"] = 155,
            ["EjJourneysDisabled"] = 156,
            ["LootMethodStyle"] = 157,
            ["ExperienceBarDisabled"] = 159,
            ["HideUnavailableTransmogSlots"] = 160,
            ["HideTransmogZeroCost"] = 161,
            ["DisableQuickJoin"] = 162,
            ["DisableRaidGroups"] = 163,
            ["UseGameTableVariation"] = 164,
            ["ActionButtonTypeOverlayStrategy"] = 165,
            ["RecommendLeastPopulatedRealm"] = 169,
            ["BagSpaceOverride"] = 172,
            ["UnflaggedPlayersCanAttackPvPFlaggedPlayers"] = 173,
            ["PvPInitialRatingOverride"] = 190
        };

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
        lua_setglobal(state, "C_GameRules");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "GetGameRuleAsFloat":
            {
                if (!TryReadRequiredGameRule(state, 1, out var ruleId))
                    return luaL_error(
                        state,
                        "Usage: local value = C_GameRules.GetGameRuleAsFloat(gameRule [, decimalPlaces])");
                if (!TryReadOptionalUInt32(state, 2, out var decimalPlaces))
                    return luaL_error(
                        state,
                        "Usage: local value = C_GameRules.GetGameRuleAsFloat(gameRule [, decimalPlaces])");
                var rawValue = GetRuleValue(runtime, ruleId);
                var value = (float)rawValue;
                for (uint index = 0; index < decimalPlaces; index++)
                    value *= 0.1f;
                lua_pushnumber(state, value);
                return 1;
            }
            case "GetGameRuleAsFrameStrata":
            {
                if (!TryReadRequiredGameRule(state, 1, out var ruleId))
                    return luaL_error(
                        state,
                        "Usage: local frameStrata = C_GameRules.GetGameRuleAsFrameStrata(gameRule)");
                if (runtime.GameRules.FrameStrataOverrides.TryGetValue(ruleId, out var strata))
                    lua_pushstring(state, strata);
                else if (ruleId is >= 27 and <= 30)
                    lua_pushstring(state, FrameStrataNames[0]);
                else if (!TryGetRuleValue(runtime, ruleId, out var strataValue))
                    lua_pushnil(state);
                else if (strataValue >= 0 && strataValue < FrameStrataNames.Length)
                    lua_pushstring(state, FrameStrataNames[strataValue]);
                else
                    lua_pushstring(state, "UNKNOWN");
                return 1;
            }
            case "IsGameRuleActive":
                if (!TryReadRequiredGameRule(state, 1, out var activeRuleId))
                    return luaL_error(
                        state,
                        "Usage: local isActive = C_GameRules.IsGameRuleActive(gameRule)");
                lua_pushboolean(state, GetRuleValue(runtime, activeRuleId) != 0 ? 1 : 0);
                return 1;
            case "GetActiveGameMode":
                lua_pushinteger(state, runtime.GameRules.ActiveGameMode);
                return 1;
            case "GetCurrentEventRealmQueues":
                lua_pushinteger(state, runtime.GameRules.CurrentEventRealmQueues);
                return 1;
            case "GetCurrentGameModeRecordID":
                if (runtime.GameRules.CurrentGameModeRecordId is { } currentRecordId)
                    lua_pushinteger(state, currentRecordId);
                else
                    lua_pushnil(state);
                return 1;
            case "GetNumDisplayedGameModes":
                lua_pushinteger(state, runtime.GameRules.DisplayedGameModeRecordIds.Count);
                return 1;
            case "GetDisplayedGameModeRecordIDAtIndex":
            {
                if (!TryReadRequiredOneBasedIndex(state, 1, out var index))
                    return luaL_error(
                        state,
                        "Usage: local gameModeRecordID = C_GameRules.GetDisplayedGameModeRecordIDAtIndex(displayIndex)");
                lua_pushinteger(
                    state,
                    index < runtime.GameRules.DisplayedGameModeRecordIds.Count
                        ? runtime.GameRules.DisplayedGameModeRecordIds[index]
                        : 0);
                return 1;
            }
            case "DoesGameModeHavePromo":
                if (!TryReadRequiredInt32(state, 1, out var promoRecordId))
                    return luaL_error(
                        state,
                        "Usage: local hasPromo = C_GameRules.DoesGameModeHavePromo(gameModeRecordID)");
                lua_pushboolean(
                    state,
                    HasGameModePromo(runtime.GameRules, promoRecordId)
                        ? 1
                        : 0);
                return 1;
            case "IsGameModeEnabled":
                if (!TryReadRequiredInt32(state, 1, out var enabledRecordId))
                    return luaL_error(
                        state,
                        "Usage: local enabled = C_GameRules.IsGameModeEnabled(gameModeRecordID)");
                lua_pushboolean(
                    state,
                    runtime.GameRules.EnabledGameModeRecordIds.Contains(enabledRecordId) ||
                    !runtime.GameRules.DisabledGameModeRecordIds.Contains(enabledRecordId)
                        ? 1
                        : 0);
                return 1;
            case "IsCharacterlessLoginActive":
                lua_pushboolean(
                    state,
                    GetRuleValue(runtime, 14) != 0 ? 1 : 0);
                return 1;
            case "IsClassAllowedForGameMode":
                if (!TryReadRequiredInt32(state, 1, out var classId))
                    return luaL_error(
                        state,
                        "Usage: local valid = C_GameRules.IsClassAllowedForGameMode(classID)");
                lua_pushboolean(
                    state,
                    IsClassAllowed(
                        runtime.GameRules.ActiveGameMode,
                        unchecked((byte)classId))
                        ? 1
                        : 0);
                return 1;
            case "IsMultiActionBarVisibilityForced":
                lua_pushboolean(
                    state,
                    GetRuleValue(runtime, 133) != 0 ||
                    GetRuleValue(runtime, 135) != 0
                        ? 1
                        : 0);
                return 1;
            case "IsPersonalResourceDisplayEnabled":
                lua_pushboolean(
                    state,
                    GetRuleValue(runtime, 129) == 0 &&
                    runtime.GameRules.NameplateShowSelf
                        ? 1
                        : 0);
                return 1;
            case "IsPlunderstorm":
                lua_pushboolean(state, runtime.GameRules.ActiveGameMode == 2 ? 1 : 0);
                return 1;
            case "IsStandard":
                lua_pushboolean(state, runtime.GameRules.ActiveGameMode == 1 ? 1 : 0);
                return 1;
            case "IsWoWHack":
                lua_pushboolean(state, runtime.GameRules.ActiveGameMode == 3 ? 1 : 0);
                return 1;
            case "GetCurrentGameModeDisplayInfo":
                return PushCurrentDisplayInfo(state, runtime.GameRules);
            case "GetGameModeDisplayInfoByRecordID":
                if (!TryReadRequiredInt32(state, 1, out var displayRecordId))
                    return luaL_error(
                        state,
                        "Usage: local info = C_GameRules.GetGameModeDisplayInfoByRecordID(gameModeRecordID)");
                return PushDisplayInfo(state, runtime.GameRules, displayRecordId);
            case "GetGameModeGlueScreenName":
                return PushCurrentGameModeString(
                    state,
                    runtime.GameRules,
                    record => record.GlueScreenName);
            case "GetGameModePromoGlobalString":
                if (!TryReadRequiredInt32(state, 1, out var promoStringRecordId))
                    return luaL_error(
                        state,
                        "Usage: local promoGlobalString = C_GameRules.GetGameModePromoGlobalString(gameModeRecordID)");
                return PushGameModeString(
                    state,
                    runtime.GameRules,
                    promoStringRecordId,
                    record => record.PromoGlobalString);
            default:
                return 0;
        }
    }

    private static int GetRuleValue(LuaRuntime runtime, int ruleId)
    {
        return TryGetRuleValue(runtime, ruleId, out var value) ? value : 0;
    }

    private static bool TryGetRuleValue(
        LuaRuntime runtime,
        int ruleId,
        out int value)
    {
        if (runtime.GameRules.RuleValueOverrides.TryGetValue(ruleId, out value))
            return true;
        if (runtime.GameRules.UseProviderDefaults &&
            runtime.GameRuleProvider?.TryGetRule(ruleId, out var rule) == true)
        {
            value = rule.Value;
            return true;
        }
        value = 0;
        return false;
    }

    private static bool HasGameModePromo(
        WowGameRulesState rules,
        int recordId)
    {
        if (recordId == 0)
            return false;
        if (rules.PromotionalGameModeRecordIds.Contains(recordId))
            return true;
        return rules.GameModeRecords.TryGetValue(recordId, out var record) &&
               !string.IsNullOrEmpty(record.PromoGlobalString);
    }

    private static bool IsClassAllowed(int gameMode, int classId) => gameMode switch
    {
        2 => classId == 14,
        3 => classId == 15,
        _ => classId is >= 1 and <= 13
    };

    private static int PushCurrentDisplayInfo(
        lua_State state,
        WowGameRulesState rules)
    {
        if (rules.CurrentGameModeRecordId is not { } recordId)
        {
            lua_pushnil(state);
            return 1;
        }
        return PushDisplayInfo(state, rules, recordId);
    }

    private static int PushDisplayInfo(
        lua_State state,
        WowGameRulesState rules,
        int recordId)
    {
        if (!rules.GameModeRecords.TryGetValue(recordId, out var record) ||
            record.DisplayInfo is not { } info)
        {
            lua_pushnil(state);
            return 1;
        }

        lua_createtable(state, 0, 7);
        SetInteger(state, "logo", info.Logo);
        SetInteger(state, "logoHeight", info.LogoHeight);
        SetInteger(state, "logoVerticalOffset", info.LogoVerticalOffset);
        SetInteger(state, "logoShrunkenHeight", info.LogoShrunkenHeight);
        lua_pushboolean(state, info.LogoUsesDarkBackdrop ? 1 : 0);
        lua_setfield(state, -2, "logoUsesDarkBackdrop");
        SetInteger(
            state,
            "characterCreateExtraHeight",
            info.CharacterCreateExtraHeight);
        SetInteger(
            state,
            "characterCreateOuterBorder",
            info.CharacterCreateOuterBorder);
        return 1;
    }

    private static int PushCurrentGameModeString(
        lua_State state,
        WowGameRulesState rules,
        Func<WowGameModeRecordState, string?> selector)
    {
        if (rules.CurrentGameModeRecordId is not { } recordId)
        {
            lua_pushnil(state);
            return 1;
        }
        return PushGameModeString(state, rules, recordId, selector);
    }

    private static int PushGameModeString(
        lua_State state,
        WowGameRulesState rules,
        int recordId,
        Func<WowGameModeRecordState, string?> selector)
    {
        if (rules.GameModeRecords.TryGetValue(recordId, out var record) &&
            selector(record) is { Length: > 0 } value)
            lua_pushstring(state, value);
        else
            lua_pushnil(state);
        return 1;
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Enum");
        }

        lua_newtable(state);
        foreach (var (name, value) in GameRuleValues)
        {
            lua_pushinteger(state, value);
            lua_setfield(state, -2, name);
        }
        lua_setfield(state, -2, "GameRule");

        lua_newtable(state);
        SetInteger(state, "Standard", 1);
        SetInteger(state, "Plunderstorm", 2);
        SetInteger(state, "WoWHack", 3);
        lua_setfield(state, -2, "GameMode");
        lua_pop(state, 1);
    }

    private static void SetInteger(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static bool TryReadRequiredGameRule(
        lua_State state,
        int index,
        out int value)
    {
        return TryReadRequiredInt32(state, index, out value) &&
               GameRuleValues.Values.Contains(value);
    }

    private static bool TryReadRequiredInt32(
        lua_State state,
        int index,
        out int value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < int.MinValue or > int.MaxValue)
            return false;
        value = (int)number;
        return true;
    }

    private static bool TryReadOptionalUInt32(
        lua_State state,
        int index,
        out uint value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_type(state, index) == LUA_TNIL)
            return true;
        if (lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < uint.MinValue or > uint.MaxValue)
            return false;
        value = (uint)number;
        return true;
    }

    private static bool TryReadRequiredOneBasedIndex(
        lua_State state,
        int index,
        out int zeroBasedIndex)
    {
        zeroBasedIndex = 0;
        if (!TryReadOptionalUInt32(state, index, out var oneBasedIndex) ||
            index > lua_gettop(state) ||
            lua_type(state, index) == LUA_TNIL ||
            oneBasedIndex == 0 ||
            oneBasedIndex > int.MaxValue)
            return false;
        zeroBasedIndex = (int)oneBasedIndex - 1;
        return true;
    }
}
