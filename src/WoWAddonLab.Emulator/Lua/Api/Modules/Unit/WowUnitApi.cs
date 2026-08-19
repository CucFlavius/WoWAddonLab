using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowUnitApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "UnitAffectingCombat",
        "UnitArmor",
        "UnitAttackPower",
        "UnitAttackSpeed",
        "UnitBattlePetLevel",
        "UnitBattlePetType",
        "UnitCanAssist",
        "UnitCanAttack",
        "UnitCanCooperate",
        "UnitCastingInfo",
        "UnitChannelInfo",
        "UnitClass",
        "UnitClassBase",
        "UnitClassification",
        "UnitDamage",
        "UnitEffectiveLevel",
        "UnitExists",
        "UnitFactionGroup",
        "UnitFullName",
        "UnitGetIncomingHeals",
        "UnitGetTotalAbsorbs",
        "UnitGetTotalHealAbsorbs",
        "UnitGetAvailableRoles",
        "UnitGroupRolesAssigned",
        "UnitGUID",
        "UnitHPPerStamina",
        "UnitHonor",
        "UnitHonorLevel",
        "UnitHonorMax",
        "IsUnitModelReadyForUI",
        "GetUnitPowerBarInfo",
        "GetUnitTotalModifiedMaxHealthPercent",
        "GetReadyCheckStatus",
        "GetRaidTargetIndex",
        "GetNegativeCorruptionEffectInfo",
        "IsFalling",
        "IsFlying",
        "CanBeRaidTarget",
        "UnitDistanceSquared",
        "UnitHasIncomingResurrection",
        "UnitHasVehicleUI",
        "UnitHasVehiclePlayerFrameUI",
        "UnitHealth",
        "UnitHealthMax",
        "UnitInParty",
        "UnitInOtherParty",
        "UnitInVehicle",
        "UnitInPartyIsAI",
        "UnitInRange",
        "UnitInRaid",
        "UnitIsConnected",
        "UnitIsBattlePet",
        "UnitIsBattlePetCompanion",
        "UnitIsBossMob",
        "UnitIsCorpse",
        "UnitIsDead",
        "UnitIsDeadOrGhost",
        "UnitIsGhost",
        "UnitIsGameObject",
        "UnitIsEnemy",
        "UnitIsFriend",
        "UnitIsGroupAssistant",
        "UnitIsGroupLeader",
        "UnitIsHumanPlayer",
        "UnitIsPlayer",
        "UnitIsPVP",
        "PlayerIsPVPInactive",
        "UnitIsPVPFreeForAll",
        "UnitIsPossessed",
        "UnitIsQuestBoss",
        "UnitIsTapDenied",
        "UnitIsUnconscious",
        "UnitIsUnit",
        "UnitIsVisible",
        "UnitIsWildBattlePet",
        "UnitLeadsAnyGroup",
        "UnitLevel",
        "UnitName",
        "UnitNameUnmodified",
        "UnitOnTaxi",
        "UnitPVPName",
        "UnitPower",
        "UnitPowerMax",
        "UnitPowerType",
        "UnitPowerBarTimerInfo",
        "UnitPosition",
        "UnitPhaseReason",
        "UnitRealmRelationship",
        "UnitRangedAttackPower",
        "UnitRangedDamage",
        "UnitSex",
        "UnitSelectionColor",
        "UnitStat",
        "UnitStagger",
        "UnitThreatSituation",
        "UnitTrialBankedLevels",
        "UnitTrialXP",
        "UnitPlayerControlled",
        "UnitXP",
        "UnitXPMax",
        "WorldLootObjectExists",
        "GetUnitMaxHealthModifier",
        "GetUnitSpeed"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var unit = runtime.Units.Find(OptionalString(state, 1));

        switch (operation)
        {
            case "IsFalling":
            {
                const string usage = "Usage: local result = IsFalling([unit])";
                string unitToken;
                if (lua_isnoneornil(state, 1) != 0)
                {
                    unitToken = "player";
                }
                else
                {
                    if (lua_isstring(state, 1) == 0)
                        return luaL_error(state, usage);
                    unitToken = lua_tostring(state, 1) ?? string.Empty;
                }

                lua_pushboolean(
                    state,
                    runtime.Units.Find(unitToken)?.IsFalling == true ? 1 : 0);
                return 1;
            }
            case "UnitHPPerStamina":
                RequireUnitToken(
                    state,
                    "Usage: local result = UnitHPPerStamina(unit)");
                lua_pushnumber(state, unit?.HealthPerStamina ?? 0);
                return 1;
            case "GetUnitMaxHealthModifier":
                RequireUnitToken(
                    state,
                    "Usage: local result = GetUnitMaxHealthModifier(unit)");
                lua_pushnumber(state, unit?.MaximumHealthModifier ?? 0);
                return 1;
            case "UnitArmor":
            {
                RequireUnitToken(
                    state,
                    "Usage: local base, effective, real, bonus = UnitArmor(unit)");
                lua_pushinteger(state, unit?.BaselineArmor ?? 0);
                lua_pushinteger(state, unit?.EffectiveArmor ?? 0);
                lua_pushinteger(state, unit?.Armor ?? 0);
                lua_pushinteger(state, unit?.BonusArmor ?? 0);
                return 4;
            }
            case "UnitDamage":
            {
                RequireUnitToken(
                    state,
                    "Usage: local minDamage, maxDamage, offhandMinDamage, " +
                    "offhandMaxDamage, posBuff, negBuff, percent = UnitDamage(unit)");
                var damage = unit?.Damage ?? new WowDamageState();
                lua_pushnumber(state, damage.Minimum);
                lua_pushnumber(state, damage.Maximum);
                lua_pushnumber(state, damage.MinimumOffHand);
                lua_pushnumber(state, damage.MaximumOffHand);
                lua_pushinteger(state, damage.PositiveBonus);
                lua_pushinteger(state, damage.NegativeBonus);
                lua_pushnumber(state, damage.PercentModifier);
                return 7;
            }
            case "UnitAttackSpeed":
                RequireUnitToken(
                    state,
                    "Usage: local attackSpeed, offhandAttackSpeed = " +
                    "UnitAttackSpeed(unit)");
                lua_pushnumber(state, unit?.MainHandAttackSpeed ?? 0);
                if (unit?.OffHandAttackSpeed is { } offHandSpeed)
                {
                    lua_pushnumber(state, offHandSpeed);
                    return 2;
                }
                lua_pushnil(state);
                return 2;
            case "UnitAttackPower":
                RequireUnitToken(
                    state,
                    "Usage: local attackPower, posBuff, negBuff = " +
                    "UnitAttackPower(unit)");
                return PushAttackPower(state, unit?.AttackPower);
            case "GetUnitSpeed":
            {
                RequireUnitToken(
                    state,
                    "Usage: local currentSpeed, runSpeed, flightSpeed, " +
                    "swimSpeed = GetUnitSpeed(unit)");
                var speed = unit?.MovementSpeed ?? new WowMovementSpeedState();
                lua_pushnumber(state, speed.Current);
                lua_pushnumber(state, speed.Run);
                lua_pushnumber(state, speed.Flight);
                lua_pushnumber(state, speed.Swim);
                return 4;
            }
            case "UnitRangedDamage":
            {
                RequireUnitToken(
                    state,
                    "Usage: local speed, minDamage, maxDamage, posBuff, " +
                    "negBuff, percent = UnitRangedDamage(unit)");
                var damage = unit?.RangedDamage ?? new WowRangedDamageState();
                lua_pushnumber(state, damage.AttackTime);
                lua_pushnumber(state, damage.Minimum);
                lua_pushnumber(state, damage.Maximum);
                lua_pushinteger(state, damage.PositiveBonus);
                lua_pushinteger(state, damage.NegativeBonus);
                lua_pushnumber(state, damage.PercentModifier);
                return 6;
            }
            case "UnitRangedAttackPower":
                RequireUnitToken(
                    state,
                    "Usage: local attackPower, posBuff, negBuff = " +
                    "UnitRangedAttackPower(unit)");
                return PushAttackPower(state, unit?.RangedAttackPower);
            case "IsUnitModelReadyForUI":
                lua_pushboolean(state, unit?.IsModelReadyForUi == true ? 1 : 0);
                return 1;
            case "UnitPosition":
                if (unit?.Position is { } position)
                {
                    lua_pushnumber(state, position.X);
                    lua_pushnumber(state, position.Y);
                    lua_pushnumber(state, position.Z);
                    lua_pushinteger(state, position.MapId);
                }
                else
                {
                    lua_pushnil(state);
                    lua_pushnil(state);
                    lua_pushnil(state);
                    lua_pushinteger(state, 0);
                }
                return 4;
            case "UnitPhaseReason":
                if (unit?.PhaseReason is { } phaseReason)
                    lua_pushinteger(state, phaseReason);
                else
                    lua_pushnil(state);
                return 1;
            case "UnitGetAvailableRoles":
                if (unit?.AvailableRoles is not { } roles)
                    return 0;
                lua_pushboolean(state, roles.Tank ? 1 : 0);
                lua_pushboolean(state, roles.Healer ? 1 : 0);
                lua_pushboolean(state, roles.Damage ? 1 : 0);
                return 3;
            case "UnitCastingInfo":
                if (unit?.Cast is not { } cast)
                    return 0;
                lua_pushstring(state, cast.Name);
                lua_pushstring(state, cast.DisplayName);
                lua_pushinteger(state, cast.TextureId);
                lua_pushnumber(state, cast.StartTimeMilliseconds);
                lua_pushnumber(state, cast.EndTimeMilliseconds);
                lua_pushboolean(state, cast.IsTradeSkill ? 1 : 0);
                lua_pushstring(state, cast.CastId);
                lua_pushboolean(state, cast.NotInterruptible ? 1 : 0);
                lua_pushinteger(state, cast.SpellId);
                lua_pushstring(state, cast.CastBarId);
                lua_pushnumber(state, cast.DelayTimeMilliseconds);
                return 11;
            case "UnitChannelInfo":
                if (unit?.Channel is not { } channel)
                    return 0;
                lua_pushstring(state, channel.Name);
                lua_pushstring(state, channel.DisplayName);
                lua_pushinteger(state, channel.TextureId);
                lua_pushnumber(state, channel.StartTimeMilliseconds);
                lua_pushnumber(state, channel.EndTimeMilliseconds);
                lua_pushboolean(state, channel.IsTradeSkill ? 1 : 0);
                lua_pushboolean(state, channel.NotInterruptible ? 1 : 0);
                lua_pushinteger(state, channel.SpellId);
                lua_pushboolean(state, channel.IsEmpowered ? 1 : 0);
                lua_pushinteger(state, channel.NumEmpowerStages);
                lua_pushstring(state, channel.CastBarId);
                return 11;
            case "UnitExists":
                lua_pushboolean(state, unit is null ? 0 : 1);
                return 1;
            case "WorldLootObjectExists":
                lua_pushboolean(state, unit?.HasWorldLootObject == true ? 1 : 0);
                return 1;
            case "UnitIsVisible":
                lua_pushboolean(state, unit?.IsVisible == true ? 1 : 0);
                return 1;
            case "UnitGroupRolesAssigned":
                lua_pushstring(state, unit?.GroupRole ?? "NONE");
                return 1;
            case "GetUnitPowerBarInfo":
                if (unit?.AlternatePowerBar is not { } powerBar)
                    return 0;
                PushPowerBarInfo(state, powerBar);
                return 1;
            case "GetReadyCheckStatus":
                PushOptionalString(state, unit?.ReadyCheckStatus);
                return 1;
            case "GetRaidTargetIndex":
                if (unit?.RaidTargetIndex is { } raidTargetIndex)
                    lua_pushinteger(state, raidTargetIndex);
                else
                    lua_pushnil(state);
                return 1;
            case "CanBeRaidTarget":
                lua_pushboolean(state, unit?.CanBeRaidTarget == true ? 1 : 0);
                return 1;
            case "GetNegativeCorruptionEffectInfo":
                lua_newtable(state);
                return 1;
            case "UnitPowerBarTimerInfo":
            {
                var index = ReadOptionalOneBasedIndex(
                    state,
                    2,
                    "Usage: local duration, expiration, barID, auraID = " +
                    "UnitPowerBarTimerInfo(unit [, index])");
                if (unit is null || !unit.PowerBarTimers.TryGetValue(index, out var timer))
                    return 0;
                lua_pushnumber(state, timer.Duration);
                lua_pushnumber(state, timer.Expiration);
                lua_pushinteger(state, timer.BarId);
                lua_pushinteger(state, timer.AuraId);
                return 4;
            }
            case "UnitLevel":
                lua_pushinteger(state, unit?.Level ?? 0);
                return 1;
            case "UnitXP":
                lua_pushnumber(state, unit?.Experience ?? 0);
                return 1;
            case "UnitXPMax":
                lua_pushnumber(state, unit?.MaximumExperience ?? 0);
                return 1;
            case "UnitTrialXP":
                lua_pushnumber(state, unit?.TrialExperience ?? 0);
                return 1;
            case "UnitTrialBankedLevels":
                lua_pushinteger(state, unit?.TrialBankedLevels ?? 0);
                lua_pushnumber(state, unit?.TrialXpIntoCurrentLevel ?? 0);
                lua_pushnumber(state, unit?.TrialXpForNextLevel ?? 0);
                return 3;
            case "UnitHonor":
                lua_pushnumber(state, unit?.Honor ?? 0);
                return 1;
            case "UnitHonorMax":
                lua_pushnumber(state, unit?.MaximumHonor ?? 0);
                return 1;
            case "UnitHonorLevel":
                lua_pushinteger(state, unit?.HonorLevel ?? 0);
                return 1;
            case "UnitEffectiveLevel":
                lua_pushinteger(state, unit?.Level ?? 0);
                return 1;
            case "UnitFactionGroup":
            {
                var useDisplayRace = lua_gettop(state) >= 2 && lua_toboolean(state, 2) != 0;
                var tag = useDisplayRace
                    ? unit?.DisplayFactionGroupTag ?? unit?.FactionGroupTag
                    : unit?.FactionGroupTag;
                var localized = useDisplayRace
                    ? unit?.LocalizedDisplayFactionGroup ?? unit?.LocalizedFactionGroup
                    : unit?.LocalizedFactionGroup;

                PushOptionalString(state, tag);
                PushOptionalString(state, localized);
                return 2;
            }
            case "UnitHealth":
            {
                var usePredicted =
                    lua_gettop(state) < 2 || lua_toboolean(state, 2) != 0;
                var health = usePredicted
                    ? unit?.PredictedHealth ?? unit?.Health ?? 0
                    : unit?.Health ?? 0;
                lua_pushnumber(state, health);
                return 1;
            }
            case "UnitHealthMax":
                lua_pushnumber(state, unit?.MaximumHealth ?? 0);
                return 1;
            case "GetUnitTotalModifiedMaxHealthPercent":
                lua_pushnumber(state, unit?.TotalModifiedMaximumHealthPercent ?? 0);
                return 1;
            case "UnitGetIncomingHeals":
                if (unit is null)
                {
                    lua_pushnil(state);
                    return 1;
                }

                var healerGuid = OptionalString(state, 2);
                lua_pushnumber(
                    state,
                    healerGuid is null
                        ? unit.IncomingHeals
                        : unit.IncomingHealsByHealerGuid.GetValueOrDefault(healerGuid));
                return 1;
            case "UnitGetTotalAbsorbs":
                lua_pushnumber(state, unit?.TotalAbsorbs ?? 0);
                return 1;
            case "UnitGetTotalHealAbsorbs":
                lua_pushnumber(state, unit?.TotalHealAbsorbs ?? 0);
                return 1;
            case "UnitStagger":
                lua_pushnumber(state, unit?.Stagger ?? 0);
                return 1;
            case "UnitPower":
            {
                var powerType = OptionalInteger(state, 2);
                var unmodified = lua_gettop(state) >= 3 && lua_toboolean(state, 3) != 0;
                lua_pushnumber(state, ResolvePower(unit, powerType, unmodified, maximum: false));
                return 1;
            }
            case "UnitPowerMax":
            {
                var powerType = OptionalInteger(state, 2);
                var unmodified = lua_gettop(state) >= 3 && lua_toboolean(state, 3) != 0;
                lua_pushnumber(state, ResolvePower(unit, powerType, unmodified, maximum: true));
                return 1;
            }
            case "UnitPowerType":
            {
                var index = OptionalInteger(state, 2) ?? 0;
                if (unit is null || !unit.PowerTypes.TryGetValue(index, out var powerType))
                    return 0;

                lua_pushinteger(state, powerType.Id);
                lua_pushstring(state, powerType.Token);
                lua_pushnumber(state, powerType.Red);
                lua_pushnumber(state, powerType.Green);
                lua_pushnumber(state, powerType.Blue);
                return 5;
            }
            case "UnitStat":
            {
                var statIndex = OptionalInteger(state, 2) ?? 0;
                if (statIndex is < 1 or > 5)
                    return luaL_error(state, "Invalid stat index in UnitStat");
                var stat = unit?.Stats.GetValueOrDefault(statIndex) ??
                           new WowUnitStatState(0, 0, 0, 0);
                lua_pushnumber(state, stat.Current);
                lua_pushnumber(state, stat.Effective);
                lua_pushnumber(state, stat.PositiveBuff);
                lua_pushnumber(state, stat.NegativeBuff);
                return 4;
            }
            case "UnitRealmRelationship":
                if (unit is null)
                    lua_pushnil(state);
                else
                    lua_pushinteger(state, unit.RealmRelationship);
                return 1;
            case "UnitSex":
                if (unit is null)
                    lua_pushnil(state);
                else
                    lua_pushinteger(state, unit.Sex);
                return 1;
            case "UnitBattlePetLevel":
                if (unit?.BattlePetLevel is { } battlePetLevel)
                    lua_pushinteger(state, battlePetLevel);
                else
                    lua_pushnil(state);
                return 1;
            case "UnitBattlePetType":
                if (unit?.BattlePetType is { } battlePetType)
                    lua_pushinteger(state, battlePetType);
                else
                    lua_pushnil(state);
                return 1;
            case "UnitClassification":
                lua_pushstring(state, unit?.Classification ?? "normal");
                return 1;
            case "UnitSelectionColor":
                lua_pushnumber(state, unit?.SelectionRed ?? 0);
                lua_pushnumber(state, unit?.SelectionGreen ?? 0);
                lua_pushnumber(state, unit?.SelectionBlue ?? 0);
                lua_pushnumber(state, unit?.SelectionAlpha ?? 1);
                return 4;
            case "UnitIsPlayer":
                lua_pushboolean(state, unit?.IsPlayer == true ? 1 : 0);
                return 1;
            case "UnitIsHumanPlayer":
                lua_pushboolean(state, unit?.IsHumanPlayer == true ? 1 : 0);
                return 1;
            case "UnitPlayerControlled":
                lua_pushboolean(state, unit?.IsPlayerControlled == true ? 1 : 0);
                return 1;
            case "UnitIsBattlePet":
                if (unit is null)
                    lua_pushnil(state);
                else
                    lua_pushboolean(state, unit.IsBattlePet ? 1 : 0);
                return 1;
            case "UnitIsBattlePetCompanion":
                lua_pushboolean(state, unit?.IsBattlePetCompanion == true ? 1 : 0);
                return 1;
            case "UnitIsWildBattlePet":
                lua_pushboolean(state, unit?.IsWildBattlePet == true ? 1 : 0);
                return 1;
            case "UnitIsBossMob":
                lua_pushboolean(state, unit?.IsBossMob == true ? 1 : 0);
                return 1;
            case "UnitIsQuestBoss":
                lua_pushboolean(state, unit?.IsQuestBoss == true ? 1 : 0);
                return 1;
            case "UnitIsTapDenied":
                lua_pushboolean(state, unit?.IsTapDenied == true ? 1 : 0);
                return 1;
            case "UnitIsUnconscious":
                lua_pushboolean(state, unit?.IsUnconscious == true ? 1 : 0);
                return 1;
            case "UnitLeadsAnyGroup":
                lua_pushboolean(state, unit?.IsGroupLeader == true ? 1 : 0);
                return 1;
            case "UnitIsUnit":
            {
                var other = runtime.Units.Find(OptionalString(state, 2));
                lua_pushboolean(
                    state,
                    unit is not null &&
                    other is not null &&
                    unit.Guid.Equals(other.Guid, StringComparison.OrdinalIgnoreCase)
                        ? 1
                        : 0);
                return 1;
            }
            case "UnitGUID":
                PushOptionalString(state, unit?.Guid);
                return 1;
            case "UnitThreatSituation":
            {
                var mobGuid = OptionalString(state, 2);
                var threat = mobGuid is null
                    ? unit?.ThreatSituation
                    : unit?.ThreatSituationByMobGuid.GetValueOrDefault(mobGuid);
                if (threat is { } value)
                    lua_pushinteger(state, value);
                else
                    lua_pushnil(state);
                return 1;
            }
            case "UnitName":
            case "UnitFullName":
                PushOptionalString(state, unit?.Name);
                PushOptionalString(state, unit?.Realm);
                return 2;
            case "UnitNameUnmodified":
                PushOptionalString(state, unit?.Name);
                PushOptionalString(state, unit?.Realm);
                return 2;
            case "UnitPVPName":
                lua_pushstring(state, unit?.PvpName ?? unit?.Name ?? string.Empty);
                return 1;
            case "UnitClass":
                if (unit is null)
                    return 0;
                PushOptionalString(state, unit.ClassName);
                PushOptionalString(state, unit.ClassFile);
                lua_pushinteger(state, unit.ClassId);
                return 3;
            case "UnitClassBase":
                RequireUnitToken(
                    state,
                    "Usage: local classFilename, classID = UnitClassBase(unit)");
                if (unit is null)
                    return 0;
                PushOptionalString(state, unit.ClassFile);
                lua_pushinteger(state, unit.ClassId);
                return 2;
            case "UnitAffectingCombat":
                lua_pushboolean(state, unit?.IsAffectingCombat == true ? 1 : 0);
                return 1;
            case "UnitHasVehicleUI":
                lua_pushboolean(state, unit?.HasVehicleUi == true ? 1 : 0);
                return 1;
            case "UnitHasVehiclePlayerFrameUI":
                lua_pushboolean(state, unit?.HasVehiclePlayerFrameUi == true ? 1 : 0);
                return 1;
            case "UnitHasIncomingResurrection":
                if (lua_type(state, 1) != LUA_TSTRING)
                    return luaL_error(
                        state,
                        "Usage: UnitHasIncomingResurrection(\"unit\")");
                lua_pushboolean(state, unit?.HasIncomingResurrection == true ? 1 : 0);
                return 1;
            case "UnitInParty":
            {
                var partyCategory = OptionalInteger(state, 2);
                var isInParty = partyCategory switch
                {
                    null => unit?.IsInParty == true,
                    0 or 1 => unit?.InPartyByPartyCategory
                        .GetValueOrDefault(partyCategory.Value) == true,
                    _ => false
                };
                lua_pushboolean(state, isInParty ? 1 : 0);
                return 1;
            }
            case "UnitInVehicle":
                lua_pushboolean(state, unit?.IsInVehicle == true ? 1 : 0);
                return 1;
            case "UnitOnTaxi":
                if (lua_isnoneornil(state, 1) != 0)
                {
                    return luaL_error(
                        state,
                        "Usage: local result = UnitOnTaxi(unit)");
                }
                lua_pushboolean(state, unit?.IsOnTaxi == true ? 1 : 0);
                return 1;
            case "UnitInPartyIsAI":
                lua_pushboolean(state, unit?.IsPartyAi == true ? 1 : 0);
                return 1;
            case "UnitInRange":
                lua_pushboolean(state, unit?.IsInRange == true ? 1 : 0);
                lua_pushboolean(state, unit?.IsInRange is not null ? 1 : 0);
                return 2;
            case "UnitDistanceSquared":
                lua_pushnumber(state, unit?.DistanceSquared ?? 0);
                lua_pushboolean(state, unit?.HasCheckedDistance == true ? 1 : 0);
                return 2;
            case "UnitInOtherParty":
                lua_pushboolean(state, unit?.IsInOtherParty == true ? 1 : 0);
                return 1;
            case "UnitInRaid":
            {
                var partyCategory = OptionalInteger(state, 2);
                var raidIndex = partyCategory switch
                {
                    null => unit?.RaidIndex,
                    0 or 1 => unit?.RaidIndexByPartyCategory
                        .GetValueOrDefault(partyCategory.Value),
                    _ => null
                };
                if (raidIndex is { } value)
                    lua_pushinteger(state, value);
                else
                    lua_pushnil(state);
                return 1;
            }
            case "UnitIsConnected":
                lua_pushboolean(state, unit?.IsConnected == true ? 1 : 0);
                return 1;
            case "UnitIsCorpse":
                lua_pushboolean(state, unit?.IsCorpse == true ? 1 : 0);
                return 1;
            case "UnitIsDead":
                lua_pushboolean(state, unit?.IsDead == true ? 1 : 0);
                return 1;
            case "UnitIsDeadOrGhost":
                lua_pushboolean(
                    state,
                    unit?.IsDead == true || unit?.IsGhost == true ? 1 : 0);
                return 1;
            case "UnitIsGhost":
                lua_pushboolean(state, unit?.IsGhost == true ? 1 : 0);
                return 1;
            case "UnitIsGameObject":
                lua_pushboolean(state, unit?.IsGameObject == true ? 1 : 0);
                return 1;
            case "UnitIsFriend":
            {
                var other = runtime.Units.Find(OptionalString(state, 2));
                var friendly = AreFriendly(unit, other);
                lua_pushboolean(state, friendly ? 1 : 0);
                return 1;
            }
            case "IsFlying":
            {
                const string usage = "Usage: local result = IsFlying([unit])";
                string unitToken;
                if (lua_isnoneornil(state, 1) != 0)
                {
                    unitToken = "player";
                }
                else
                {
                    if (lua_isstring(state, 1) == 0)
                        return luaL_error(state, usage);
                    unitToken = lua_tostring(state, 1) ?? string.Empty;
                }

                lua_pushboolean(
                    state,
                    runtime.Units.Find(unitToken)?.IsFlying == true ? 1 : 0);
                return 1;
            }
            case "UnitIsEnemy":
            {
                var other = runtime.Units.Find(OptionalString(state, 2));
                lua_pushboolean(
                    state,
                    unit is not null && other is not null && !AreFriendly(unit, other) ? 1 : 0);
                return 1;
            }
            case "UnitCanAssist":
            {
                var other = runtime.Units.Find(OptionalString(state, 2));
                lua_pushboolean(state, AreFriendly(unit, other) ? 1 : 0);
                return 1;
            }
            case "UnitCanCooperate":
            {
                var other = runtime.Units.Find(OptionalString(state, 2));
                lua_pushboolean(state, AreFriendly(unit, other) ? 1 : 0);
                return 1;
            }
            case "UnitCanAttack":
            {
                var other = runtime.Units.Find(OptionalString(state, 2));
                lua_pushboolean(
                    state,
                    unit is not null && other is not null && !AreFriendly(unit, other) ? 1 : 0);
                return 1;
            }
            case "UnitIsGroupAssistant":
                lua_pushboolean(state, unit?.IsGroupAssistant == true ? 1 : 0);
                return 1;
            case "UnitIsPVP":
                lua_pushboolean(state, unit?.IsPvp == true ? 1 : 0);
                return 1;
            case "PlayerIsPVPInactive":
                lua_pushboolean(state, unit?.IsPvpInactive == true ? 1 : 0);
                return 1;
            case "UnitIsPVPFreeForAll":
                lua_pushboolean(state, unit?.IsPvpFreeForAll == true ? 1 : 0);
                return 1;
            case "UnitIsPossessed":
                lua_pushboolean(state, unit?.IsPossessed == true ? 1 : 0);
                return 1;
            case "UnitIsGroupLeader":
            {
                var partyCategory = OptionalInteger(state, 2);
                var isLeader = partyCategory is 0 or 1
                    ? unit?.GroupLeaderByPartyCategory
                        .GetValueOrDefault(partyCategory.Value) == true
                    : unit?.IsGroupLeader == true;
                lua_pushboolean(state, isLeader ? 1 : 0);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static string? OptionalString(lua_State state, int index) =>
        lua_type(state, index) == LUA_TSTRING ? lua_tostring(state, index) : null;

    private static bool AreFriendly(WowUnitState? unit, WowUnitState? other) =>
        unit is not null &&
        other is not null &&
        (unit.Guid.Equals(other.Guid, StringComparison.OrdinalIgnoreCase) ||
         (!string.IsNullOrEmpty(unit.FactionGroupTag) &&
          unit.FactionGroupTag.Equals(other.FactionGroupTag, StringComparison.OrdinalIgnoreCase)));

    private static void RequireUnitToken(lua_State state, string usage)
    {
        if (lua_type(state, 1) != LUA_TSTRING)
            luaL_error(state, usage);
    }

    private static int PushAttackPower(
        lua_State state,
        WowAttackPowerState? value)
    {
        lua_pushinteger(state, value?.Base ?? 0);
        lua_pushinteger(state, value?.PositiveBonus ?? 0);
        lua_pushinteger(state, value?.NegativeBonus ?? 0);
        return 3;
    }

    private static void PushPowerBarInfo(lua_State state, WowUnitPowerBarInfoState value)
    {
        lua_newtable(state);
        SetNumber(state, "ID", value.Id);
        SetNumber(state, "barType", value.BarType);
        SetNumber(state, "minPower", value.MinimumPower);
        SetNumber(state, "startInset", value.StartInset);
        SetNumber(state, "endInset", value.EndInset);
        SetBoolean(state, "smooth", value.Smooth);
        SetBoolean(state, "hideFromOthers", value.HideFromOthers);
        SetBoolean(state, "showOnRaid", value.ShowOnRaid);
        SetBoolean(state, "opaqueSpark", value.OpaqueSpark);
        SetBoolean(state, "opaqueFlash", value.OpaqueFlash);
        SetBoolean(state, "anchorTop", value.AnchorTop);
        SetBoolean(state, "forcePercentage", value.ForcePercentage);
        SetBoolean(state, "sparkUnderFrame", value.SparkUnderFrame);
        SetBoolean(state, "flashAtMinPower", value.FlashAtMinimumPower);
        SetBoolean(state, "fractionalCounter", value.FractionalCounter);
        SetBoolean(state, "animateNumbers", value.AnimateNumbers);
        SetBoolean(state, "attachTooltipToBar", value.AttachTooltipToBar);
    }

    private static void SetNumber(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBoolean(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

    private static int? OptionalInteger(lua_State state, int index) =>
        lua_type(state, index) == LUA_TNUMBER ? (int)lua_tonumber(state, index) : null;

    private static int ReadOptionalOneBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_type(state, index) == LUA_TNIL)
            return 0;
        if (lua_type(state, index) != LUA_TNUMBER)
            luaL_error(state, usage);

        var value = lua_tonumber(state, index);
        if (value < 0 || value > uint.MaxValue)
            luaL_error(state, usage);

        return value > int.MaxValue ? int.MaxValue : (int)value - 1;
    }

    private static long ResolvePower(
        WowUnitState? unit,
        int? requestedPowerType,
        bool unmodified,
        bool maximum)
    {
        if (unit is null)
            return 0;

        if (requestedPowerType is null || requestedPowerType == unit.CurrentPowerTypeId)
        {
            if (unit.PowerValues.TryGetValue(unit.CurrentPowerTypeId, out var currentType))
            {
                return maximum
                    ? unmodified
                        ? currentType.UnmodifiedMaximum ?? currentType.Maximum
                        : currentType.Maximum
                    : unmodified
                        ? currentType.UnmodifiedCurrent ?? currentType.Current
                        : currentType.Current;
            }

            return maximum ? unit.MaximumPower : unit.Power;
        }

        if (!unit.PowerValues.TryGetValue(requestedPowerType.Value, out var value))
            return 0;

        return maximum
            ? unmodified
                ? value.UnmodifiedMaximum ?? value.Maximum
                : value.Maximum
            : unmodified
                ? value.UnmodifiedCurrent ?? value.Current
                : value.Current;
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }
}
