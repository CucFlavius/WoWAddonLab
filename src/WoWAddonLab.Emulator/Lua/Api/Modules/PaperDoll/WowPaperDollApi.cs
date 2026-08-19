using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPaperDollApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] GlobalFunctions =
    [
        "GetAttackPowerForStat", "GetDodgeChance", "GetBlockChance", "GetShieldBlock",
        "GetParryChance",
        "GetMeleeHaste", "GetRangedCritChance", "GetCritChance",
        "GetSpellCritChance", "GetPowerRegen", "GetHaste", "GetManaRegen",
        "GetMasteryEffect", "GetLifesteal", "GetAvoidance", "GetCombatRating",
        "GetCombatRatingBonus", "GetCombatRatingBonusForCombatRatingValue",
        "GetModResilienceDamageReduction", "GetSpeed", "GetVersatilityBonus",
        "GetAverageItemLevel", "IsRangedWeapon",
        "GetSpellBonusDamage", "GetSpellBonusHealing",
        "GetPVPGearStatRules", "GetOverrideAPBySpellPower",
        "GetOverrideSpellPowerByAP", "HasAPEffectsSpellPower",
        "HasSPEffectsAttackPower", "GetPetSpellBonusDamage", "GetExpertise",
        "GetCritChanceProvidesParryEffect", "GetDodgeChanceFromAttribute",
        "GetParryChanceFromAttribute"
    ];

    private static readonly string[] NamespaceFunctions =
    [
        "CanAutoEquipCursorItem", "CanCursorCanGoInSlot", "GetArmorEffectiveness",
        "GetArmorEffectivenessAgainstTarget", "GetInspectAzeriteItemEmpoweredChoices",
        "GetInspectGuildInfo", "GetInspectItemLevel", "GetInspectRatedBGBlitzData",
        "GetInspectRatedBGData", "GetInspectRatedSoloShuffleData", "GetMinItemLevel",
        "GetStaggerPercentage", "IsInventorySlotEnabled", "IsRangedSlotShown",
        "OffhandHasShield", "OffhandHasWeapon"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in GlobalFunctions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);

        lua_newtable(state);
        foreach (var function in NamespaceFunctions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_PaperDollInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var paperDoll = runtime.PaperDoll;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetAttackPowerForStat":
            {
                const string usage =
                    "Usage: local result = GetAttackPowerForStat(stat, value)";
                var stat = RequiredOneBasedIndex(state, 1, usage);
                var value = RequiredInt32(state, 2, usage);
                lua_pushinteger(
                    state,
                    Math.Max(value, 0) *
                    (stat <= 1
                        ? paperDoll.AttackPowerPerStatPoint.GetValueOrDefault(stat)
                        : 0));
                return 1;
            }
            case "GetDodgeChance":
                return PushNumber(state, paperDoll.DodgeChance);
            case "GetBlockChance":
                return PushNumber(state, paperDoll.BlockChance);
            case "GetShieldBlock":
                lua_pushinteger(state, paperDoll.ShieldBlock);
                return 1;
            case "GetParryChance":
                return PushNumber(state, paperDoll.ParryChance);
            case "GetMeleeHaste":
                return PushNumber(state, paperDoll.MeleeHaste);
            case "GetRangedCritChance":
                return PushNumber(state, paperDoll.RangedCriticalStrikeChance);
            case "GetCritChance":
                return PushNumber(state, paperDoll.CriticalStrikeChance);
            case "GetSpellCritChance":
                return PushNumber(state, paperDoll.SpellCriticalStrikeChance);
            case "GetPowerRegen":
                lua_pushnumber(state, paperDoll.PowerRegeneration);
                lua_pushnumber(state, paperDoll.PowerRegenerationWhileCasting);
                return 2;
            case "GetHaste":
                return PushNumber(state, paperDoll.Haste);
            case "GetManaRegen":
                lua_pushnumber(state, paperDoll.ManaRegeneration);
                lua_pushnumber(state, paperDoll.ManaRegenerationInCombat);
                return 2;
            case "GetMasteryEffect":
                lua_pushnumber(state, paperDoll.MasteryEffect);
                lua_pushnumber(state, paperDoll.MasteryBonusCoefficient);
                return 2;
            case "GetLifesteal":
                return PushNumber(state, paperDoll.Lifesteal);
            case "GetAvoidance":
                return PushNumber(state, paperDoll.Avoidance);
            case "GetSpeed":
                return PushNumber(state, paperDoll.Speed);
            case "GetModResilienceDamageReduction":
                return PushNumber(state, paperDoll.ModifiedResilienceDamageReduction);
            case "GetCombatRating":
            {
                var rating = RequiredCombatRatingIndex(
                    state,
                    "Usage: local result = GetCombatRating(ratingIndex)");
                if (rating is null)
                    return PushOptionalInteger(state, null);
                lua_pushinteger(
                    state,
                    paperDoll.CombatRatings.GetValueOrDefault(rating.Value));
                return 1;
            }
            case "GetCombatRatingBonus":
            {
                var rating = RequiredCombatRatingIndex(
                    state,
                    "Usage: local result = GetCombatRatingBonus(ratingIndex)");
                return PushOptionalNumber(
                    state,
                    rating is { } value
                        ? paperDoll.CombatRatingBonuses.GetValueOrDefault(value)
                        : null);
            }
            case "GetCombatRatingBonusForCombatRatingValue":
            {
                const string usage =
                    "Usage: local result = " +
                    "GetCombatRatingBonusForCombatRatingValue(ratingIndex, value)";
                var rating = RequiredOneBasedIndex(state, 1, usage);
                var value = RequiredInt32(state, 2, usage);
                if (rating >= 32)
                    return PushOptionalNumber(state, null);
                return PushOptionalNumber(
                    state,
                    value *
                    paperDoll.CombatRatingBonusPerPoint.GetValueOrDefault(rating));
            }
            case "GetVersatilityBonus":
            {
                var rating = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: local result = GetVersatilityBonus(combatRating)");
                return PushNumber(
                    state,
                    paperDoll.VersatilityBonuses.GetValueOrDefault(rating));
            }
            case "GetAverageItemLevel":
                lua_pushnumber(state, paperDoll.AverageItemLevel);
                lua_pushnumber(state, paperDoll.EquippedItemLevel);
                lua_pushnumber(state, paperDoll.PvpItemLevel);
                return 3;
            case "IsRangedWeapon":
                return PushBoolean(state, paperDoll.HasRangedWeapon);
            case "GetSpellBonusDamage":
            {
                var school = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: local result = GetSpellBonusDamage(school)");
                if (school > 6)
                    return PushOptionalInteger(state, null);
                return PushOptionalInteger(
                    state,
                    paperDoll.SpellBonusDamageBySchool.GetValueOrDefault(school));
            }
            case "GetSpellBonusHealing":
                lua_pushinteger(state, paperDoll.SpellBonusHealing);
                return 1;
            case "GetPVPGearStatRules":
                return PushBoolean(state, paperDoll.UsesPvpGearStatRules);
            case "GetOverrideAPBySpellPower":
                return PushNumber(state, paperDoll.AttackPowerFromSpellPowerMultiplier);
            case "GetOverrideSpellPowerByAP":
                return PushNumber(state, paperDoll.SpellPowerFromAttackPowerMultiplier);
            case "HasAPEffectsSpellPower":
                return PushBoolean(state, paperDoll.AttackPowerAffectsSpellPower);
            case "HasSPEffectsAttackPower":
                return PushBoolean(state, paperDoll.SpellPowerAffectsAttackPower);
            case "GetPetSpellBonusDamage":
                lua_pushinteger(state, paperDoll.PetSpellBonusDamage);
                return 1;
            case "GetExpertise":
                lua_pushnumber(state, paperDoll.Expertise);
                lua_pushnumber(state, paperDoll.OffHandExpertise);
                lua_pushnumber(state, paperDoll.RangedExpertise);
                return 3;
            case "GetCritChanceProvidesParryEffect":
                return PushBoolean(state, paperDoll.CriticalStrikeProvidesParry);
            case "GetDodgeChanceFromAttribute":
                return PushNumber(state, paperDoll.DodgeChanceFromAttribute);
            case "GetParryChanceFromAttribute":
                return PushNumber(state, paperDoll.ParryChanceFromAttribute);
            case "CanAutoEquipCursorItem":
                return PushBoolean(state, paperDoll.CanAutoEquipCursorItem);
            case "CanCursorCanGoInSlot":
            {
                var slot = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: local canOccupySlot = " +
                    "C_PaperDollInfo.CanCursorCanGoInSlot(slotIndex)");
                return PushBoolean(
                    state,
                    paperDoll.CursorCompatibleSlots.Contains(slot));
            }
            case "GetArmorEffectiveness":
            {
                const string usage =
                    "Usage: local effectiveness = " +
                    "C_PaperDollInfo.GetArmorEffectiveness(armor, attackerLevel)";
                var armor = RequiredFloat(state, 1, usage);
                var attackerLevel = RequiredInt32(state, 2, usage);
                return PushNumber(
                    state,
                    CalculateArmorEffectiveness(paperDoll, armor, attackerLevel));
            }
            case "GetArmorEffectivenessAgainstTarget":
                RequiredFloat(
                    state,
                    1,
                    "Usage: local effectiveness = " +
                    "C_PaperDollInfo.GetArmorEffectivenessAgainstTarget(armor)");
                return PushOptionalNumber(state, paperDoll.ArmorEffectivenessAgainstTarget);
            case "GetInspectAzeriteItemEmpoweredChoices":
                return PushAzeriteChoices(state, paperDoll);
            case "GetInspectGuildInfo":
            {
                var unit = RequiredString(
                    state,
                    1,
                    "Usage: local achievementPoints, numMembers, guildName, " +
                    "realmName = C_PaperDollInfo.GetInspectGuildInfo(unitString)");
                if (!paperDoll.InspectGuildByUnit.TryGetValue(unit, out var guild))
                    return 0;
                lua_pushinteger(state, guild.AchievementPoints);
                lua_pushinteger(state, guild.MemberCount);
                lua_pushstring(state, guild.GuildName);
                lua_pushstring(state, guild.RealmName);
                return 4;
            }
            case "GetInspectItemLevel":
            {
                var unit = RequiredUnitToken(
                    state,
                    1,
                    "Usage: local equippedItemLevel = " +
                    "C_PaperDollInfo.GetInspectItemLevel(unit)");
                var itemLevel = paperDoll.InspectItemLevels.TryGetValue(unit, out var inspected)
                    ? inspected
                    : 0;
                lua_pushinteger(state, itemLevel);
                return 1;
            }
            case "GetInspectRatedBGBlitzData":
                return PushInspectPvp(state, paperDoll.InspectRatedBgBlitz);
            case "GetInspectRatedBGData":
                return PushInspectRatedBg(state, paperDoll.InspectRatedBg);
            case "GetInspectRatedSoloShuffleData":
                return PushInspectPvp(state, paperDoll.InspectRatedSoloShuffle);
            case "GetMinItemLevel":
                return PushOptionalInteger(state, paperDoll.MinimumItemLevel);
            case "GetStaggerPercentage":
            {
                var unit = RequiredUnitToken(
                    state,
                    1,
                    "Usage: local stagger, staggerAgainstTarget = " +
                    "C_PaperDollInfo.GetStaggerPercentage(unit)");
                var stagger = paperDoll.StaggerByUnit.TryGetValue(unit, out var configured)
                    ? configured
                    : new WowStaggerState();
                lua_pushnumber(state, stagger.Percentage);
                if (stagger.PercentageAgainstTarget is { } targetStagger)
                    lua_pushnumber(state, targetStagger);
                else
                    lua_pushnil(state);
                return 2;
            }
            case "IsInventorySlotEnabled":
            {
                var slotName = RequiredString(
                    state,
                    1,
                    "Usage: local isEnabled = " +
                    "C_PaperDollInfo.IsInventorySlotEnabled(slotName)");
                return PushBoolean(
                    state,
                    paperDoll.KnownInventorySlots.Contains(slotName) &&
                    !paperDoll.DisabledInventorySlots.Contains(slotName));
            }
            case "IsRangedSlotShown":
                return PushBoolean(state, false);
            case "OffhandHasShield":
                return PushBoolean(state, paperDoll.OffHandHasShield);
            case "OffhandHasWeapon":
                return PushBoolean(state, paperDoll.OffHandHasWeapon);
            default:
                return 0;
        }
    }


    private static int PushAzeriteChoices(lua_State state, WowPaperDollState paperDoll)
    {
        const string usage =
            "Usage: local azeritePowerIDs = " +
            "C_PaperDollInfo.GetInspectAzeriteItemEmpoweredChoices(" +
            "unit, equipmentSlotIndex)";
        var unit = RequiredUnitToken(state, 1, usage);
        var slot = RequiredOneBasedIndex(state, 2, usage);
        if (slot > 18)
            return 0;

        IReadOnlyList<int>? choices = null;
        foreach (var entry in paperDoll.InspectAzeritePowerChoices)
        {
            if (entry.Key.Slot == slot &&
                entry.Key.Unit.Equals(unit, StringComparison.OrdinalIgnoreCase))
            {
                choices = entry.Value;
                break;
            }
        }
        if (choices is null)
            return 0;

        lua_createtable(state, 5, 0);
        for (var index = 0; index < 5; index++)
        {
            lua_pushinteger(state, index < choices.Count ? choices[index] : 0);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int PushInspectPvp(lua_State state, WowInspectPvpState value)
    {
        lua_createtable(state, 0, 5);
        SetInteger(state, "rating", value.Rating);
        SetInteger(state, "gamesWon", value.GamesWon);
        SetInteger(state, "gamesPlayed", value.GamesPlayed);
        SetInteger(state, "roundsWon", value.RoundsWon);
        SetInteger(state, "roundsPlayed", value.RoundsPlayed);
        return 1;
    }

    private static int PushInspectRatedBg(lua_State state, WowInspectRatedBgState value)
    {
        lua_createtable(state, 0, 3);
        SetInteger(state, "rating", value.Rating);
        SetInteger(state, "played", value.Played);
        SetInteger(state, "won", value.Won);
        return 1;
    }

    private static void SetInteger(lua_State state, string key, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, key);
    }

    private static double CalculateArmorEffectiveness(
        WowPaperDollState paperDoll,
        double armor,
        int attackerLevel)
    {
        if (!paperDoll.ArmorMitigationConstantsByAttackerLevel.TryGetValue(
                attackerLevel,
                out var mitigationConstant))
        {
            return paperDoll.ArmorEffectivenessFallback;
        }

        var scale = paperDoll.ArmorMitigationScalesByAttackerLevel.GetValueOrDefault(
            attackerLevel,
            1);
        var denominator = armor + mitigationConstant * scale;
        var effectiveness = denominator == 0 ? 0 : armor / denominator;
        return Math.Min(
            paperDoll.ArmorEffectivenessCap,
            Math.Max(effectiveness, 0));
    }

    private static double RequiredFloat(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < -float.MaxValue || value > float.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (float)value;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return unchecked((int)value);
    }

    private static uint RequiredOneBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var zeroBased = lua_tonumber(state, index) - 1;
        if (!double.IsFinite(zeroBased) ||
            zeroBased < 0 ||
            zeroBased > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return unchecked((uint)zeroBased);
    }

    private static uint? RequiredCombatRatingIndex(
        lua_State state,
        string usage)
    {
        var rating = RequiredOneBasedIndex(state, 1, usage);
        return rating < 32 ? rating : null;
    }

    private static string RequiredUnitToken(
        lua_State state,
        int index,
        string usage) =>
        RequiredString(state, index, usage);

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static int PushNumber(lua_State state, double value)
    {
        lua_pushnumber(state, value);
        return 1;
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static int PushOptionalNumber(lua_State state, double? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushnumber(state, value.Value);
        return 1;
    }

    private static int PushOptionalInteger(lua_State state, int? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushinteger(state, value.Value);
        return 1;
    }

}
