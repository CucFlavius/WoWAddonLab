using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSpellApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CancelSpellByID", "DoesSpellExist", "EnableSpellRangeCheck", "GetAuraStatChanges",
        "GetBaseSpell", "GetDeadlyDebuffInfo", "GetItemModifiedAppearancesApplied",
        "GetMawPowerLinkBySpellID", "GetMawPowerRarityInfoBySpellID", "GetOverrideSpell",
        "GetSchoolString", "GetSpellAutoCast", "GetSpellCastCount", "GetSpellChargeDuration",
        "GetSpellCharges", "GetSpellCooldown", "GetSpellCooldownDuration", "GetSpellDescription",
        "GetSpellDisplayCount", "GetSpellIDForSpellIdentifier", "GetSpellInfo",
        "GetSpellLevelLearned", "GetSpellLink", "GetSpellLossOfControlCooldownDuration",
        "GetSpellLossOfControlCooldownInfo", "GetSpellMaxCumulativeAuraApplications",
        "GetSpellName", "GetSpellPowerCost", "GetSpellQueueWindow",
        "GetSpellSkillLineAbilityRank", "GetSpellSubtext", "GetSpellTexture",
        "GetSpellTradeSkillLink", "GetVisibilityInfo", "IsAutoAttackSpell",
        "IsAutoRepeatSpell", "IsClassTalentSpell", "IsConsumableSpell", "IsCurrentSpell",
        "IsExternalDefensive", "IsPressHoldReleaseSpell", "IsPriorityAura", "IsPvPTalentSpell",
        "IsRangedAutoAttackSpell", "IsSelfBuff", "IsSpellCrowdControl", "IsSpellDataCached",
        "IsSpellDisabled", "IsSpellHarmful", "IsSpellHelpful", "IsSpellImportant",
        "IsSpellInRange", "IsSpellPassive", "IsSpellUsable", "PickupSpell",
        "RequestLoadSpellData", "SetSpellAutoCastEnabled", "SpellHasRange",
        "TargetSpellIsEnchanting", "TargetSpellJumpsUpgradeTrack",
        "TargetSpellReplacesBonusTree", "ToggleSpellAutoCast"
    ];

    private static readonly IReadOnlyDictionary<int, (string Key, string English)>
        SchoolStrings = new Dictionary<int, (string, string)>
        {
            [127] = ("STRING_SCHOOL_ALL", "All"),
            [1] = ("STRING_SCHOOL_PHYSICAL", "Physical"),
            [2] = ("STRING_SCHOOL_HOLY", "Holy"),
            [4] = ("STRING_SCHOOL_FIRE", "Fire"),
            [8] = ("STRING_SCHOOL_NATURE", "Nature"),
            [16] = ("STRING_SCHOOL_FROST", "Frost"),
            [32] = ("STRING_SCHOOL_SHADOW", "Shadow"),
            [64] = ("STRING_SCHOOL_ARCANE", "Arcane"),
            [5] = ("STRING_SCHOOL_FLAMESTRIKE", "Flamestrike"),
            [17] = ("STRING_SCHOOL_FROSTSTRIKE", "Froststrike"),
            [65] = ("STRING_SCHOOL_SPELLSTRIKE", "Spellstrike"),
            [33] = ("STRING_SCHOOL_SHADOWSTRIKE", "Shadowstrike"),
            [9] = ("STRING_SCHOOL_STORMSTRIKE", "Stormstrike"),
            [3] = ("STRING_SCHOOL_HOLYSTRIKE", "Holystrike"),
            [20] = ("STRING_SCHOOL_FROSTFIRE", "Frostfire"),
            [68] = ("STRING_SCHOOL_SPELLFIRE", "Spellfire"),
            [12] = ("STRING_SCHOOL_FIRESTORM", "Firestorm"),
            [36] = ("STRING_SCHOOL_SHADOWFLAME", "Shadowflame"),
            [6] = ("STRING_SCHOOL_HOLYFIRE", "Holyfire"),
            [80] = ("STRING_SCHOOL_SPELLFROST", "Spellfrost"),
            [24] = ("STRING_SCHOOL_FROSTSTORM", "Froststorm"),
            [48] = ("STRING_SCHOOL_SHADOWFROST", "Shadowfrost"),
            [18] = ("STRING_SCHOOL_HOLYFROST", "Holyfrost"),
            [72] = ("STRING_SCHOOL_SPELLSTORM", "Spellstorm"),
            [96] = ("STRING_SCHOOL_SPELLSHADOW", "Spellshadow"),
            [66] = ("STRING_SCHOOL_DIVINE", "Divine"),
            [40] = ("STRING_SCHOOL_SHADOWSTORM", "Shadowstorm"),
            [10] = ("STRING_SCHOOL_HOLYSTORM", "Holystorm"),
            [34] = ("STRING_SCHOOL_SHADOWLIGHT", "Shadowlight"),
            [28] = ("STRING_SCHOOL_ELEMENTAL", "Elemental"),
            [62] = ("STRING_SCHOOL_CHROMATIC", "Chromatic"),
            [126] = ("STRING_SCHOOL_MAGIC", "Magic"),
            [124] = ("STRING_SCHOOL_CHAOS", "Chaos"),
            [106] = ("STRING_SCHOOL_COSMIC", "Cosmic")
        };

    public override void Register(lua_State state)
    {
        RegisterEnumsAndConstants(state);

        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Spell");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var spells = runtime.Spells;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "CancelSpellByID":
            {
                var spellId = RequiredInt32(
                    state,
                    1,
                    "Usage: C_Spell.CancelSpellByID(spellID)");
                if (spells.ActiveCastSpellId == spellId)
                {
                    spells.ActiveCastSpellId = null;
                    spells.LastCancelledSpellId = spellId;
                }
                return 0;
            }
            case "DoesSpellExist":
                return PushBoolean(state, spells.Find(RequiredSpellIdentifier(state, spells)) is not null);
            case "EnableSpellRangeCheck":
            {
                var spellId = RequiredSpellIdentifier(state, spells);
                var enabled = RequiredBoolean(
                    state,
                    2,
                    "Usage: C_Spell.EnableSpellRangeCheck(spellIdentifier, enable)");
                if (enabled)
                    spells.RangeCheckedSpellIds.Add(spellId);
                else
                    spells.RangeCheckedSpellIds.Remove(spellId);
                return 0;
            }
            case "GetAuraStatChanges":
            {
                var spellId = RequiredInt32(
                    state,
                    1,
                    "Usage: local healthChange, powerTypeChanges = C_Spell.GetAuraStatChanges(spellID)");
                var changes = spells.Find(spellId)?.AuraStatChanges ??
                              new WowSpellAuraStatChanges(0, []);
                lua_pushinteger(state, changes.HealthChange);
                lua_createtable(state, changes.PowerTypeChanges.Count, 0);
                for (var index = 0; index < changes.PowerTypeChanges.Count; index++)
                {
                    var change = changes.PowerTypeChanges[index];
                    lua_createtable(state, 0, 2);
                    SetInteger(state, "powerType", change.PowerType);
                    SetInteger(state, "amount", change.Amount);
                    lua_rawseti(state, -2, index + 1);
                }
                return 2;
            }
            case "GetBaseSpell":
            {
                var spellId = RequiredSpellIdentifier(state, spells);
                OptionalInt32(state, 2, 0, Usage(operation));
                lua_pushinteger(state, spells.Find(spellId)?.BaseSpellId ?? spellId);
                return 1;
            }
            case "GetDeadlyDebuffInfo":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                if (definition?.DeadlyDebuffInfo is not { } info)
                    return 0;
                lua_createtable(state, 0, 5);
                SetOptionalInteger(
                    state,
                    "criticalTimeRemainingMs",
                    info.CriticalTimeRemainingMilliseconds);
                SetOptionalInteger(state, "criticalStacks", info.CriticalStacks);
                SetInteger(state, "priority", info.Priority);
                SetString(state, "warningText", info.WarningText);
                SetOptionalInteger(state, "soundKitID", info.SoundKitId);
                return 1;
            }
            case "GetItemModifiedAppearancesApplied":
            {
                var spellId = RequiredInt32(state, 1, Usage(operation));
                return PushIntegerArray(
                    state,
                    spells.Find(spellId)?.ItemModifiedAppearancesApplied ?? []);
            }
            case "GetMawPowerLinkBySpellID":
            {
                var link = spells.Find(RequiredSpellIdentifier(state, spells))?.MawPowerLink;
                return PushOptionalStringResult(state, link);
            }
            case "GetMawPowerRarityInfoBySpellID":
            {
                var rarity = spells.Find(RequiredSpellIdentifier(state, spells))?.MawPowerRarity;
                if (rarity is null)
                    return 0;
                lua_pushinteger(state, rarity.RarityId);
                lua_pushinteger(state, rarity.AtlasId);
                return 2;
            }
            case "GetOverrideSpell":
            {
                var spellId = RequiredSpellIdentifier(state, spells);
                OptionalInt32(state, 2, 0, Usage(operation));
                OptionalBoolean(state, 3, true, Usage(operation));
                OptionalInt32(state, 4, 0, Usage(operation));
                lua_pushinteger(state, spells.Find(spellId)?.OverrideSpellId ?? spellId);
                return 1;
            }
            case "GetSchoolString":
            {
                var mask = RequiredInt32(state, 1, Usage(operation));
                var school = SchoolStrings.GetValueOrDefault(
                    mask,
                    (Key: "STRING_SCHOOL_UNKNOWN", English: "Unknown"));
                if (runtime.GlobalStringProvider?.Strings.TryGetValue(
                        school.Key,
                        out var localized) == true)
                {
                    lua_pushstring(state, localized);
                }
                else
                {
                    lua_pushstring(state, school.English);
                }
                return 1;
            }
            case "GetSpellAutoCast":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                if (definition is null)
                    return 0;
                lua_pushboolean(state, definition.AutoCastAllowed ? 1 : 0);
                lua_pushboolean(state, definition.AutoCastEnabled ? 1 : 0);
                return 2;
            }
            case "GetSpellCastCount":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                lua_pushinteger(state, definition?.CastCount ?? 0);
                return 1;
            }
            case "GetSpellChargeDuration":
                return PushDurationResult(
                    state,
                    spells.Find(RequiredSpellIdentifier(state, spells))?.ChargeDuration);
            case "GetSpellCharges":
            {
                var charges = spells.Find(RequiredSpellIdentifier(state, spells))?.Charges;
                if (charges is null)
                    return 0;
                PushChargeInfo(state, charges);
                return 1;
            }
            case "GetSpellCooldown":
            {
                var cooldown = spells.Find(RequiredSpellIdentifier(state, spells))?.Cooldown;
                if (cooldown is null)
                    return 0;
                PushCooldownInfo(state, cooldown);
                return 1;
            }
            case "GetSpellCooldownDuration":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                OptionalBoolean(state, 2, false, Usage(operation));
                return PushDurationResult(state, definition?.CooldownDuration);
            }
            case "GetSpellDescription":
                return PushOptionalStringResult(
                    state,
                    spells.Find(RequiredSpellIdentifier(state, spells))?.Description);
            case "GetSpellDisplayCount":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                var maximum = OptionalInt32(state, 2, 9999, Usage(operation));
                var replacement = OptionalString(state, 3, "*", Usage(operation)) ?? "*";
                var display = string.Empty;
                if (definition is not null)
                {
                    if (definition.IsConsumable || definition.IsStackable ||
                        definition.UseCount > 0)
                    {
                        display = definition.UseCount <= maximum
                            ? definition.UseCount.ToString(CultureInfo.InvariantCulture)
                            : replacement;
                    }
                    else if (definition.Charges?.CurrentCharges > 1)
                    {
                        display = definition.Charges.CurrentCharges
                            .ToString(CultureInfo.InvariantCulture);
                    }
                }
                lua_pushstring(state, display);
                return 1;
            }
            case "GetSpellIDForSpellIdentifier":
            {
                var spellId = RequiredSpellIdentifier(state, spells);
                if (spells.Find(spellId) is null)
                    return 0;
                lua_pushinteger(state, spellId);
                return 1;
            }
            case "GetSpellInfo":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                if (definition is null)
                    return 0;
                lua_createtable(state, 0, 7);
                SetString(state, "name", definition.Name);
                SetInteger(state, "iconID", definition.IconId);
                SetInteger(state, "originalIconID", definition.OriginalIconId);
                SetInteger(state, "castTime", definition.CastTimeMilliseconds);
                SetNumber(state, "minRange", definition.MinRange);
                SetNumber(state, "maxRange", definition.MaxRange);
                SetInteger(state, "spellID", definition.Id);
                return 1;
            }
            case "GetSpellLevelLearned":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                lua_pushinteger(state, definition?.LevelLearned ?? 0);
                return 1;
            }
            case "GetSpellLink":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                OptionalInt32(state, 2, 0, Usage(operation));
                return PushOptionalStringResult(state, definition?.Link);
            }
            case "GetSpellLossOfControlCooldownDuration":
                return PushDurationResult(
                    state,
                    spells.Find(RequiredSpellIdentifier(state, spells))
                        ?.LossOfControlCooldownDuration);
            case "GetSpellLossOfControlCooldownInfo":
            {
                var info = spells.Find(RequiredSpellIdentifier(state, spells))
                    ?.LossOfControlCooldownInfo;
                if (info is null)
                    return 0;
                PushLossOfControlInfo(state, info);
                return 1;
            }
            case "GetSpellMaxCumulativeAuraApplications":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                lua_pushinteger(state, definition?.MaxCumulativeAuraApplications ?? 0);
                return 1;
            }
            case "GetSpellName":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                return PushOptionalStringResult(
                    state,
                    string.IsNullOrEmpty(definition?.Name) ? null : definition.Name);
            }
            case "GetSpellPowerCost":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                if (definition is null)
                    return 0;
                PushPowerCosts(state, definition.PowerCosts);
                return 1;
            }
            case "GetSpellQueueWindow":
                lua_pushinteger(state, spells.QueueWindowMilliseconds);
                return 1;
            case "GetSpellSkillLineAbilityRank":
            {
                var rank = spells.Find(RequiredSpellIdentifier(state, spells))
                    ?.SkillLineAbilityRank;
                if (rank is null)
                    return 0;
                lua_pushinteger(state, rank.Value);
                return 1;
            }
            case "GetSpellSubtext":
                return PushOptionalStringResult(
                    state,
                    spells.Find(RequiredSpellIdentifier(state, spells))?.Subtext);
            case "GetSpellTexture":
            {
                var spellId = RequiredSpellIdentifier(state, spells);
                if (spellId == 0)
                    return 0;
                var definition = spells.Find(spellId);
                lua_pushinteger(state, definition?.IconId ?? 0);
                lua_pushinteger(state, definition?.OriginalIconId ?? 0);
                return 2;
            }
            case "GetSpellTradeSkillLink":
                return PushOptionalStringResult(
                    state,
                    spells.Find(RequiredSpellIdentifier(state, spells))?.TradeSkillLink);
            case "GetVisibilityInfo":
            {
                var spellId = RequiredInt32(state, 1, Usage(operation));
                var visibilityType = RequiredVisibilityType(state, 2, Usage(operation));
                var definition = spells.Find(spellId);
                if (definition is null ||
                    !definition.Visibility.TryGetValue(visibilityType, out var visibility))
                {
                    return 0;
                }
                lua_pushboolean(state, visibility.HasCustom ? 1 : 0);
                lua_pushboolean(state, visibility.AlwaysShowMine ? 1 : 0);
                lua_pushboolean(state, visibility.ShowForMySpec ? 1 : 0);
                return 3;
            }
            case "IsAutoAttackSpell":
                return PushDefinitionBoolean(state, spells, static value => value.IsAutoAttack);
            case "IsAutoRepeatSpell":
                return PushDefinitionBoolean(state, spells, static value => value.IsAutoRepeat);
            case "IsClassTalentSpell":
                return PushDefinitionBoolean(state, spells, static value => value.IsClassTalent);
            case "IsConsumableSpell":
                return PushDefinitionBoolean(state, spells, static value => value.IsConsumable);
            case "IsCurrentSpell":
                return PushDefinitionBoolean(state, spells, static value => value.IsCurrent);
            case "IsExternalDefensive":
                return PushInt32DefinitionBoolean(
                    state,
                    spells,
                    static value => value.IsExternalDefensive,
                    operation);
            case "IsPressHoldReleaseSpell":
                return PushDefinitionBoolean(state, spells, static value => value.IsPressHoldRelease);
            case "IsPriorityAura":
                return PushInt32DefinitionBoolean(
                    state,
                    spells,
                    static value => value.IsPriorityAura,
                    operation);
            case "IsPvPTalentSpell":
                return PushDefinitionBoolean(state, spells, static value => value.IsPvpTalent);
            case "IsRangedAutoAttackSpell":
            {
                var spellId = RequiredSpellIdentifier(state, spells);
                return PushBoolean(state, spells.RangedAutoAttackSpellId == spellId);
            }
            case "IsSelfBuff":
                return PushInt32DefinitionBoolean(
                    state,
                    spells,
                    static value => value.IsSelfBuff,
                    operation);
            case "IsSpellCrowdControl":
                return PushDefinitionBoolean(state, spells, static value => value.IsCrowdControl);
            case "IsSpellDataCached":
                return PushDefinitionBoolean(state, spells, static value => value.IsDataCached);
            case "IsSpellDisabled":
                return PushDefinitionBoolean(state, spells, static value => value.IsDisabled);
            case "IsSpellHarmful":
                return PushDefinitionBoolean(state, spells, static value => value.IsHarmful);
            case "IsSpellHelpful":
                return PushDefinitionBoolean(state, spells, static value => value.IsHelpful);
            case "IsSpellImportant":
                return PushDefinitionBoolean(state, spells, static value => value.IsImportant);
            case "IsSpellInRange":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                OptionalString(state, 2, null, Usage(operation));
                if (definition?.IsInRange is { } inRange)
                    lua_pushboolean(state, inRange ? 1 : 0);
                else
                    lua_pushnil(state);
                return 1;
            }
            case "IsSpellPassive":
                return PushDefinitionBoolean(state, spells, static value => value.IsPassive);
            case "IsSpellUsable":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                lua_pushboolean(state, definition?.IsUsable == true ? 1 : 0);
                lua_pushboolean(state, definition?.HasInsufficientPower == true ? 1 : 0);
                return 2;
            }
            case "PickupSpell":
                spells.PickedUpSpellId = RequiredSpellIdentifier(state, spells);
                return 0;
            case "RequestLoadSpellData":
                spells.RequestedLoadSpellIds.Add(RequiredSpellIdentifier(state, spells));
                return 0;
            case "SetSpellAutoCastEnabled":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                var enabled = RequiredBoolean(state, 2, Usage(operation));
                if (definition?.AutoCastAllowed == true)
                    definition.AutoCastEnabled = enabled;
                return 0;
            }
            case "SpellHasRange":
                return PushDefinitionBoolean(state, spells, static value => value.HasRange);
            case "TargetSpellIsEnchanting":
                return PushBoolean(state, spells.TargetSpellIsEnchanting);
            case "TargetSpellJumpsUpgradeTrack":
                return PushBoolean(state, spells.TargetSpellJumpsUpgradeTrack);
            case "TargetSpellReplacesBonusTree":
                return PushBoolean(state, spells.TargetSpellReplacesBonusTree);
            case "ToggleSpellAutoCast":
            {
                var definition = spells.Find(RequiredSpellIdentifier(state, spells));
                if (definition?.AutoCastAllowed == true)
                    definition.AutoCastEnabled = !definition.AutoCastEnabled;
                return 0;
            }
            default:
                return 0;
        }
    }

    private static int PushDefinitionBoolean(
        lua_State state,
        WowSpellState spells,
        Func<WowSpellDefinition, bool> selector)
    {
        var definition = spells.Find(RequiredSpellIdentifier(state, spells));
        return PushBoolean(state, definition is not null && selector(definition));
    }

    private static int PushInt32DefinitionBoolean(
        lua_State state,
        WowSpellState spells,
        Func<WowSpellDefinition, bool> selector,
        string operation)
    {
        var definition = spells.Find(RequiredInt32(state, 1, Usage(operation)));
        return PushBoolean(state, definition is not null && selector(definition));
    }

    private static int RequiredSpellIdentifier(lua_State state, WowSpellState spells)
    {
        if (lua_isnumber(state, 1) != 0)
            return (int)lua_tonumber(state, 1);
        var type = lua_type(state, 1);
        if (type != LUA_TSTRING)
        {
            luaL_error(state, Usage("SpellIdentifier"));
            return 0;
        }

        var text = lua_tostring(state, 1) ?? string.Empty;
        var marker = text.IndexOf("spell:", StringComparison.Ordinal);
        if (marker < 0)
            return spells.FindIdByName(text);

        return ParseSpellLinkId(text.AsSpan(marker + "spell:".Length));
    }

    private static int ParseSpellLinkId(ReadOnlySpan<char> value)
    {
        var index = 0;
        while (index < value.Length && char.IsWhiteSpace(value[index]))
            index++;

        var negative = false;
        if (index < value.Length && (value[index] == '+' || value[index] == '-'))
        {
            negative = value[index] == '-';
            index++;
        }

        if (!negative &&
            index + 2 <= value.Length &&
            value[index] == '0' &&
            (value[index + 1] == 'x' || value[index + 1] == 'X'))
        {
            index += 2;
            var start = index;
            while (index < value.Length && Uri.IsHexDigit(value[index]))
                index++;
            return uint.TryParse(
                value[start..index],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out var hexadecimal)
                ? unchecked((int)hexadecimal)
                : 0;
        }

        var decimalStart = index;
        while (index < value.Length && char.IsAsciiDigit(value[index]))
            index++;
        if (decimalStart == index ||
            !long.TryParse(
                value[decimalStart..index],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var number))
        {
            return 0;
        }

        return unchecked((int)(negative ? -number : number));
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
        return (int)value;
    }

    private static int OptionalInt32(
        lua_State state,
        int index,
        int defaultValue,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return defaultValue;
        return RequiredInt32(state, index, usage);
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TBOOLEAN)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static bool OptionalBoolean(
        lua_State state,
        int index,
        bool defaultValue,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return defaultValue;
        return RequiredBoolean(state, index, usage);
    }

    private static string? OptionalString(
        lua_State state,
        int index,
        string? defaultValue,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return defaultValue;
        var type = lua_type(state, index);
        if (type is not (LUA_TSTRING or LUA_TNUMBER))
        {
            luaL_error(state, usage);
            return defaultValue;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static WowSpellAuraVisibilityType RequiredVisibilityType(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value is < 0 or > 2)
        {
            luaL_error(state, usage);
            return WowSpellAuraVisibilityType.RaidInCombat;
        }
        return (WowSpellAuraVisibilityType)value;
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static int PushOptionalStringResult(lua_State state, string? value)
    {
        if (value is null)
            return 0;
        lua_pushstring(state, value);
        return 1;
    }

    private static int PushIntegerArray(lua_State state, IReadOnlyList<int> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int PushDurationResult(lua_State state, WowDurationState? duration)
    {
        if (duration is null)
            return 0;
        WowDurationApi.Push(state, duration);
        return 1;
    }

    private static void PushCooldownInfo(lua_State state, WowActionCooldownInfo info)
    {
        lua_createtable(state, 0, 8);
        SetNumber(state, "startTime", info.StartTime);
        SetNumber(state, "duration", info.Duration);
        SetBoolean(state, "isEnabled", info.IsEnabled);
        SetBoolean(state, "isActive", info.IsActive);
        SetNumber(state, "modRate", info.ModRate);
        SetOptionalInteger(state, "activeCategory", info.ActiveCategory);
        SetOptionalNumber(
            state,
            "timeUntilEndOfStartRecovery",
            info.TimeUntilEndOfStartRecovery);
        SetOptionalBoolean(state, "isOnGCD", info.IsOnGlobalCooldown);
    }

    private static void PushChargeInfo(lua_State state, WowActionChargeInfo info)
    {
        lua_createtable(state, 0, 6);
        SetInteger(state, "currentCharges", info.CurrentCharges);
        SetInteger(state, "maxCharges", info.MaxCharges);
        SetNumber(state, "cooldownStartTime", info.CooldownStartTime);
        SetNumber(state, "cooldownDuration", info.CooldownDuration);
        SetNumber(state, "chargeModRate", info.ChargeModRate);
        SetBoolean(state, "isActive", info.IsActive);
    }

    private static void PushLossOfControlInfo(
        lua_State state,
        WowActionLossOfControlInfo info)
    {
        lua_createtable(state, 0, 5);
        SetNumber(state, "startTime", info.StartTime);
        SetNumber(state, "duration", info.Duration);
        SetNumber(state, "modRate", info.ModRate);
        SetBoolean(state, "isActive", info.IsActive);
        SetBoolean(
            state,
            "shouldReplaceNormalCooldown",
            info.ShouldReplaceNormalCooldown);
    }

    private static void PushPowerCosts(
        lua_State state,
        IReadOnlyList<WowSpellPowerCostInfo> costs)
    {
        lua_createtable(state, costs.Count, 0);
        for (var index = 0; index < costs.Count; index++)
        {
            var cost = costs[index];
            lua_createtable(state, 0, 8);
            SetInteger(state, "type", cost.Type);
            SetString(state, "name", cost.Name);
            SetInteger(state, "cost", cost.Cost);
            SetInteger(state, "minCost", cost.MinCost);
            SetInteger(state, "costPercent", cost.CostPercent);
            SetInteger(state, "costPerSec", cost.CostPerSecond);
            SetInteger(state, "requiredAuraID", cost.RequiredAuraId);
            SetBoolean(state, "hasRequiredAura", cost.HasRequiredAura);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void RegisterEnumsAndConstants(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Enum");
        }

        lua_createtable(state, 0, 3);
        SetInteger(state, "RaidInCombat", 0);
        SetInteger(state, "RaidOutOfCombat", 1);
        SetInteger(state, "EnemyTarget", 2);
        lua_setfield(state, -2, "SpellAuraVisibilityType");

        lua_createtable(state, 0, 3);
        SetInteger(state, "NumValues", 3);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 2);
        lua_setfield(state, -2, "SpellAuraVisibilityTypeMeta");
        lua_pop(state, 1);

        lua_getglobal(state, "Constants");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Constants");
        }
        lua_createtable(state, 0, 1);
        SetInteger(state, "GLOBAL_RECOVERY_CATEGORY", 133);
        lua_setfield(state, -2, "SpellCooldownConsts");
        lua_pop(state, 1);
    }

    private static string Usage(string operation) =>
        $"Usage: C_Spell.{operation}(...)";

    private static void SetNumber(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetInteger(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBoolean(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalInteger(
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

    private static void SetOptionalNumber(
        lua_State state,
        string name,
        double? value)
    {
        if (value is { } number)
            lua_pushnumber(state, number);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalBoolean(
        lua_State state,
        string name,
        bool? value)
    {
        if (value is { } boolean)
            lua_pushboolean(state, boolean ? 1 : 0);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, name);
    }
}
