using System.Globalization;
using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowUnitAuraApi : LuaApiModule
{
    private const string AddBlockedAuraUsage = "Usage: C_UnitAuras.AddBlockedAura(unitToken, auraInstanceID)";
    private const string AddPrivateAuraAnchorUsage = "Usage: C_UnitAuras.AddPrivateAuraAnchor(privateAuraAnchorInfo)";
    private const string ClearBlockedAurasUsage = "Usage: C_UnitAuras.ClearBlockedAuras(unitToken)";
    private const string AuraByInstanceUsage = "Usage: C_UnitAuras.GetAuraDataByAuraInstanceID(unitToken, auraInstanceID)";
    private const string AuraByIndexUsage = "Usage: C_UnitAuras.GetAuraDataByIndex(unitToken, index [, filter])";
    private const string AuraBySlotUsage = "Usage: C_UnitAuras.GetAuraDataBySlot(unitToken, slot)";
    private const string AuraBySpellNameUsage = "Usage: C_UnitAuras.GetAuraDataBySpellName(unitToken, spellName [, filter])";
    private const string AuraSlotsUsage = "Usage: C_UnitAuras.GetAuraSlots(unitToken [, filter, maxSlots, continuationToken])";
    private const string AuraIsBigDefensiveUsage =
        "Usage: local isBigDefensive = C_UnitAuras.AuraIsBigDefensive(spellID)";
    private const string BuffByIndexUsage = "Usage: C_UnitAuras.GetBuffDataByIndex(unitToken, index [, filter])";
    private const string DebuffByIndexUsage = "Usage: C_UnitAuras.GetDebuffDataByIndex(unitToken, index [, filter])";
    private const string PlayerAuraBySpellUsage = "Usage: C_UnitAuras.GetPlayerAuraBySpellID(spellIdentifier)";
    private const string UnitAuraBySpellUsage = "Usage: C_UnitAuras.GetUnitAuraBySpellID(unitToken, spellIdentifier)";
    private const string UnitAuraListUsage = "Usage: C_UnitAuras.GetUnitAuras(unitToken, filter [, maxCount, sortRule, sortDirection])";
    private const string UnitAuraIdsUsage = "Usage: C_UnitAuras.GetUnitAuraInstanceIDs(unitToken, filter [, maxCount, sortRule, sortDirection])";
    private const string WantsAlteredFormUsage =
        "Usage: local wantsAlteredForm = C_UnitAuras.WantsAlteredForm(unit)";
    private const string RemoveAnchorUsage = "Usage: C_UnitAuras.RemovePrivateAuraAnchor(anchorID)";
    private const string WarningAnchorUsage = "Usage: C_UnitAuras.SetPrivateWarningTextAnchor(parent [, anchor])";
    private const string ShowDispelTypeUsage = "Usage: C_UnitAuras.TriggerPrivateAuraShowDispelType(showDispelType)";

    private static readonly HashSet<string> FramePoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "TOPLEFT", "TOP", "TOPRIGHT", "LEFT", "CENTER", "RIGHT",
        "BOTTOMLEFT", "BOTTOM", "BOTTOMRIGHT"
    };

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AddBlockedAura",
        "AddPrivateAuraAnchor",
        "AuraIsBigDefensive",
        "ClearBlockedAuras",
        "GetAuraDataByAuraInstanceID",
        "GetAuraDataByIndex",
        "GetAuraDataBySlot",
        "GetAuraDataBySpellName",
        "GetAuraSlots",
        "GetBuffDataByIndex",
        "GetDebuffDataByIndex",
        "GetPlayerAuraBySpellID",
        "GetUnitAuraBySpellID",
        "GetUnitAuraInstanceIDs",
        "GetUnitAuras",
        "RemovePrivateAuraAnchor",
        "ResetAuraDataProvider",
        "SetPrivateWarningTextAnchor",
        "SwitchAuraDataProvider",
        "TriggerPrivateAuraShowDispelType",
        "WantsAlteredForm"
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
        lua_setglobal(state, "C_UnitAuras");
        RegisterEnums(state);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "WantsAlteredForm":
                if (!TryReadRequiredString(state, 1, out var alteredFormUnit) ||
                    !LuaBindings.IsRecognizedUnitToken(alteredFormUnit))
                    return luaL_error(state, WantsAlteredFormUsage);
                lua_pushboolean(
                    state,
                    runtime.UnitAuras.AlteredFormByUnitToken.TryGetValue(
                        alteredFormUnit,
                        out var wantsAlteredForm) && wantsAlteredForm
                        ? 1
                        : 0);
                return 1;
            case "AuraIsBigDefensive":
                if (!TryReadSpellIdentifier(
                        runtime,
                        state,
                        1,
                        "player",
                        out var defensiveSpellId))
                {
                    return luaL_error(state, AuraIsBigDefensiveUsage);
                }
                lua_pushboolean(
                    state,
                    runtime.Spells.Find(defensiveSpellId)?.IsBigDefensive == true
                        ? 1
                        : 0);
                return 1;
            case "ResetAuraDataProvider":
                runtime.TriggerEvent("AURA_DATA_PROVIDER_SWITCH", true);
                return 0;
            case "SwitchAuraDataProvider":
                runtime.TriggerEvent("AURA_DATA_PROVIDER_SWITCH", false);
                return 0;
            case "TriggerPrivateAuraShowDispelType":
                if (lua_gettop(state) < 1 || lua_isnil(state, 1) != 0)
                    return luaL_error(state, ShowDispelTypeUsage);
                runtime.UnitAuras.PrivateAuraShowDispelType = lua_toboolean(state, 1) != 0;
                if (runtime.UnitAuras.ShowDispelTypeCallbackReference > 0)
                {
                    runtime.InvokeReference(
                        runtime.UnitAuras.ShowDispelTypeCallbackReference,
                        null,
                        runtime.UnitAuras.PrivateAuraShowDispelType);
                }
                return 0;
            case "AddBlockedAura":
                if (!TryReadRequiredString(state, 1, out var blockedUnit) ||
                    !TryReadRequiredInt32(state, 2, out var blockedAuraId))
                    return luaL_error(state, AddBlockedAuraUsage);
                runtime.UnitAuras.AddBlockedAura(blockedUnit, blockedAuraId);
                return 0;
            case "ClearBlockedAuras":
                if (!TryReadRequiredString(state, 1, out var clearedUnit))
                    return luaL_error(state, ClearBlockedAurasUsage);
                runtime.UnitAuras.ClearBlockedAuras(clearedUnit);
                return 0;
            case "SetPrivateWarningTextAnchor":
                return SetPrivateWarningTextAnchor(runtime, state);
            case "RemovePrivateAuraAnchor":
                if (!TryReadRequiredUInt32(state, 1, out var anchorId))
                    return luaL_error(state, RemoveAnchorUsage);
                if (runtime.UnitAuras.RemovePrivateAuraAnchor(anchorId))
                    WowUnitAurasPrivateApi.NotifyAnchorRemoved(runtime, anchorId);
                return 0;
            case "AddPrivateAuraAnchor":
                return AddPrivateAuraAnchor(runtime, state);
            case "GetAuraSlots":
                return GetAuraSlots(runtime, state);
            case "GetAuraDataBySlot":
                if (!TryReadRequiredString(state, 1, out var slotUnit) ||
                    !TryReadRequiredUInt32(state, 2, out var slot))
                    return luaL_error(state, AuraBySlotUsage);
                return PushOptionalAura(state, FindBySlot(runtime, slotUnit, slot));
            case "GetAuraDataByAuraInstanceID":
                if (!TryReadRequiredString(state, 1, out var instanceUnit) ||
                    !TryReadRequiredUInt32(state, 2, out var instanceId))
                    return luaL_error(state, AuraByInstanceUsage);
                return PushOptionalAura(
                    state,
                    runtime.UnitAuras.Find(instanceUnit)
                        .FirstOrDefault(aura => aura.AuraInstanceId == instanceId));
            case "GetAuraDataBySpellName":
                if (!TryReadRequiredString(state, 1, out var spellNameUnit) ||
                    !TryReadRequiredString(state, 2, out var spellName) ||
                    !TryReadOptionalString(state, 3, out var spellNameFilter))
                    return luaL_error(state, AuraBySpellNameUsage);
                return PushOptionalAura(
                    state,
                    Filter(runtime, spellNameUnit, spellNameFilter ?? "HELPFUL")
                        .Select(entry => entry.Aura)
                        .FirstOrDefault(aura =>
                            aura.Name.Equals(spellName, StringComparison.OrdinalIgnoreCase)));
            case "GetPlayerAuraBySpellID":
                if (!TryReadSpellIdentifier(runtime, state, 1, "player", out var playerSpellId))
                    return luaL_error(state, PlayerAuraBySpellUsage);
                return PushOptionalAura(
                    state,
                    runtime.UnitAuras.Find("player")
                        .FirstOrDefault(aura => aura.SpellId == playerSpellId));
            case "GetUnitAuraBySpellID":
                if (!TryReadRequiredString(state, 1, out var spellUnit) ||
                    !TryReadSpellIdentifier(runtime, state, 2, spellUnit, out var unitSpellId))
                    return luaL_error(state, UnitAuraBySpellUsage);
                return PushOptionalAura(
                    state,
                    runtime.UnitAuras.Find(spellUnit)
                        .FirstOrDefault(aura => aura.SpellId == unitSpellId));
            case "GetAuraDataByIndex":
                return GetAuraByIndex(runtime, state, AuraByIndexUsage, null);
            case "GetBuffDataByIndex":
                return GetAuraByIndex(runtime, state, BuffByIndexUsage, "HELPFUL");
            case "GetDebuffDataByIndex":
                return GetAuraByIndex(runtime, state, DebuffByIndexUsage, "HARMFUL");
            case "GetUnitAuraInstanceIDs":
                return GetUnitAuraList(runtime, state, idsOnly: true);
            case "GetUnitAuras":
                return GetUnitAuraList(runtime, state, idsOnly: false);
            default:
                return 0;
        }
    }

    private static int SetPrivateWarningTextAnchor(LuaRuntime runtime, lua_State state)
    {
        var parent = LuaBindings.GetObject(runtime, 1);
        if (parent is null || !WowWidgetApi.IsFrameWidget(parent.ObjectType))
            return luaL_error(state, WarningAnchorUsage);

        WowAuraAnchorPointState? anchor = null;
        if (lua_gettop(state) >= 2 && lua_isnil(state, 2) == 0)
        {
            if (lua_type(state, 2) != LUA_TTABLE ||
                !TryReadAnchor(runtime, state, 2, out anchor))
                return luaL_error(state, WarningAnchorUsage);
        }

        runtime.UnitAuras.PrivateWarningTextAnchor = new WowPrivateWarningTextAnchorState(
            parent.Id,
            anchor?.Point ?? "CENTER",
            anchor?.RelativeToObjectId,
            anchor?.RelativePoint ?? "CENTER",
            anchor?.OffsetX ?? 0,
            anchor?.OffsetY ?? 0);
        return 0;
    }

    private static int AddPrivateAuraAnchor(LuaRuntime runtime, lua_State state)
    {
        if (lua_type(state, 1) != LUA_TTABLE)
            return luaL_error(state, AddPrivateAuraAnchorUsage);

        if (!TryReadRequiredStringField(state, 1, "unitToken", out var unitToken) ||
            !TryReadRequiredUInt32Field(state, 1, "auraIndex", out var auraIndex) ||
            !TryReadRequiredObjectField(runtime, state, 1, "parent", out var parent) ||
            !WowWidgetApi.IsFrameWidget(parent.ObjectType))
            return luaL_error(state, AddPrivateAuraAnchorUsage);

        var showCountdownFrame =
            ReadOptionalBooleanField(state, 1, "showCooldownFrame") ||
            ReadOptionalBooleanField(state, 1, "showCountdownFrame");
        var showCountdownNumbers = ReadOptionalBooleanField(state, 1, "showCountdownNumbers");
        var isContainer = ReadOptionalBooleanField(state, 1, "isContainer");

        WowAuraAnchorPointState? iconAnchor = null;
        double? iconWidth = null;
        double? iconHeight = null;
        double? borderScale = null;
        if (!TryReadOptionalTableField(state, 1, "iconInfo", out var hasIconInfo))
            return luaL_error(state, AddPrivateAuraAnchorUsage);
        if (hasIconInfo)
        {
            var iconInfoIndex = lua_gettop(state);
            var width = 0.0;
            var height = 0.0;
            var valid =
                TryReadRequiredTableField(state, iconInfoIndex, "iconAnchor", out var iconAnchorIndex) &&
                TryReadAnchor(runtime, state, iconAnchorIndex, out iconAnchor) &&
                TryReadRequiredNumberField(state, iconInfoIndex, "iconWidth", out width) &&
                TryReadRequiredNumberField(state, iconInfoIndex, "iconHeight", out height) &&
                TryReadOptionalNumberField(state, iconInfoIndex, "borderScale", out borderScale);
            if (valid)
            {
                iconWidth = width;
                iconHeight = height;
            }
            lua_settop(state, iconInfoIndex - 1);
            if (!valid)
                return luaL_error(state, AddPrivateAuraAnchorUsage);
        }

        WowAuraAnchorPointState? durationAnchor = null;
        if (!TryReadOptionalTableField(state, 1, "durationAnchor", out var hasDurationAnchor))
            return luaL_error(state, AddPrivateAuraAnchorUsage);
        if (hasDurationAnchor)
        {
            var durationIndex = lua_gettop(state);
            var valid = TryReadAnchor(runtime, state, durationIndex, out durationAnchor);
            lua_pop(state, 1);
            if (!valid)
                return luaL_error(state, AddPrivateAuraAnchorUsage);
        }

        var anchor = runtime.UnitAuras.AddPrivateAuraAnchor(
            unitToken,
            auraIndex,
            parent.Id,
            showCountdownFrame,
            showCountdownNumbers,
            isContainer,
            iconAnchor,
            iconWidth,
            iconHeight,
            borderScale,
            durationAnchor);
        WowUnitAurasPrivateApi.NotifyAnchorAdded(runtime, anchor);
        lua_pushnumber(state, anchor.Id);
        return 1;
    }

    private static int GetAuraSlots(LuaRuntime runtime, lua_State state)
    {
        if (!TryReadRequiredString(state, 1, out var unit) ||
            !TryReadOptionalString(state, 2, out var filter) ||
            !TryReadOptionalUInt32(state, 3, out var maximum) ||
            !TryReadOptionalUInt32(state, 4, out var continuation))
            return luaL_error(state, AuraSlotsUsage);

        var source = runtime.UnitAuras.Find(unit);
        var offset = continuation.HasValue
            ? Math.Min((long)continuation.Value, source.Count)
            : 0;
        var slots = new List<int>();
        var cursor = offset;
        while (cursor < source.Count &&
               (!maximum.HasValue || slots.Count != maximum.Value))
        {
            if (WowUnitAuraState.MatchesAura(source[(int)cursor], filter))
                slots.Add((int)cursor + 1);
            cursor++;
        }
        if (cursor < source.Count)
            lua_pushnumber(state, cursor);
        else
            lua_pushnil(state);
        foreach (var slot in slots)
            lua_pushinteger(state, slot);
        return slots.Count + 1;
    }

    private static int GetAuraByIndex(
        LuaRuntime runtime,
        lua_State state,
        string usage,
        string? forcedFilter)
    {
        if (!TryReadRequiredString(state, 1, out var unit) ||
            !TryReadRequiredUInt32(state, 2, out var index) ||
            !TryReadOptionalString(state, 3, out var optionalFilter))
            return luaL_error(state, usage);
        var filter = forcedFilter is null
            ? optionalFilter ?? "HELPFUL"
            : CombineFilter(forcedFilter, optionalFilter);
        return PushOptionalAura(state, FindByIndex(runtime, unit, index, filter));
    }

    private static int GetUnitAuraList(
        LuaRuntime runtime,
        lua_State state,
        bool idsOnly)
    {
        var usage = idsOnly ? UnitAuraIdsUsage : UnitAuraListUsage;
        if (!TryReadRequiredString(state, 1, out var unit) ||
            !TryReadRequiredString(state, 2, out var filter) ||
            !TryReadOptionalUInt32(state, 3, out var maximum) ||
            !TryReadOptionalEnum(state, 4, 0, 6, out var sortRule) ||
            !TryReadOptionalEnum(state, 5, 0, 1, out var sortDirection))
            return luaL_error(state, usage);

        var auras = SortAndLimit(
            Filter(runtime, unit, filter),
            maximum,
            sortRule ?? 0,
            sortDirection ?? 0);
        return idsOnly
            ? PushAuraInstanceIds(state, auras)
            : PushAuras(state, auras);
    }

    private static WowUnitAuraInfoState? FindBySlot(
        LuaRuntime runtime,
        string unit,
        uint slot)
    {
        var auras = runtime.UnitAuras.Find(unit);
        return slot > 0 && slot <= auras.Count ? auras[(int)slot - 1] : null;
    }

    private static WowUnitAuraInfoState? FindByIndex(
        LuaRuntime runtime,
        string unit,
        uint index,
        string? filter)
    {
        var auras = Filter(runtime, unit, filter);
        return index > 0 && index <= auras.Count ? auras[(int)index - 1].Aura : null;
    }

    private static IReadOnlyList<(int Slot, WowUnitAuraInfoState Aura)> Filter(
        LuaRuntime runtime,
        string unit,
        string? filter) =>
        runtime.UnitAuras.Filter(unit, filter);

    private static IReadOnlyList<(int Slot, WowUnitAuraInfoState Aura)> SortAndLimit(
        IReadOnlyList<(int Slot, WowUnitAuraInfoState Aura)> source,
        uint? maximum,
        int sortRule,
        int sortDirection)
    {
        IEnumerable<(int Slot, WowUnitAuraInfoState Aura)> result = source;
        result = sortRule switch
        {
            1 => result
                .OrderByDescending(entry => entry.Aura.IsFromPlayerOrPlayerPet)
                .ThenByDescending(entry => entry.Aura.CanApplyAura)
                .ThenBy(entry => entry.Aura.AuraInstanceId),
            2 => result
                .OrderBy(entry => entry.Aura.IsFromPlayerOrPlayerPet)
                .ThenByDescending(entry => entry.Aura.ExpirationTime == 0)
                .ThenByDescending(entry => entry.Aura.ExpirationTime)
                .ThenBy(entry => entry.Aura.AuraInstanceId),
            3 => result
                .OrderByDescending(entry => entry.Aura.IsFromPlayerOrPlayerPet)
                .ThenByDescending(entry => entry.Aura.CanApplyAura)
                .ThenBy(entry => entry.Aura.ExpirationTime == 0)
                .ThenBy(entry => entry.Aura.ExpirationTime)
                .ThenBy(entry => entry.Aura.AuraInstanceId),
            4 => result
                .OrderBy(entry => entry.Aura.ExpirationTime == 0)
                .ThenBy(entry => entry.Aura.ExpirationTime)
                .ThenBy(entry => entry.Aura.AuraInstanceId),
            5 => result
                .OrderByDescending(entry => entry.Aura.IsFromPlayerOrPlayerPet)
                .ThenByDescending(entry => entry.Aura.CanApplyAura)
                .ThenBy(entry => entry.Aura.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Aura.AuraInstanceId),
            6 => result
                .OrderBy(entry => entry.Aura.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Aura.AuraInstanceId),
            _ => result
        };
        var array = result.ToArray();
        if (sortDirection == 1)
            Array.Reverse(array);
        return maximum.HasValue
            ? array.Take(checked((int)Math.Min(maximum.Value, int.MaxValue))).ToArray()
            : array;
    }

    private static int PushAuraInstanceIds(
        lua_State state,
        IReadOnlyList<(int Slot, WowUnitAuraInfoState Aura)> auras)
    {
        lua_createtable(state, auras.Count, 0);
        for (var index = 0; index < auras.Count; index++)
        {
            lua_pushnumber(state, auras[index].Aura.AuraInstanceId);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int PushAuras(
        lua_State state,
        IReadOnlyList<(int Slot, WowUnitAuraInfoState Aura)> auras)
    {
        lua_createtable(state, auras.Count, 0);
        for (var index = 0; index < auras.Count; index++)
        {
            PushAura(state, auras[index].Aura);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    internal static int PushOptionalAura(lua_State state, WowUnitAuraInfoState? aura)
    {
        if (aura is null)
            lua_pushnil(state);
        else
            PushAura(state, aura);
        return 1;
    }

    internal static void PushAura(lua_State state, WowUnitAuraInfoState aura)
    {
        lua_createtable(state, 0, 26);
        SetString(state, "name", aura.Name);
        SetNumber(state, "icon", aura.Icon);
        SetNumber(state, "applications", aura.Applications);
        SetOptionalString(state, "dispelName", aura.DispelName);
        SetNumber(state, "duration", aura.Duration);
        SetNumber(state, "expirationTime", aura.ExpirationTime);
        SetOptionalString(state, "sourceUnit", aura.SourceUnit);
        SetBoolean(state, "isStealable", aura.IsStealable);
        SetBoolean(state, "nameplateShowPersonal", aura.NameplateShowPersonal);
        SetNumber(state, "spellId", aura.SpellId);
        SetBoolean(state, "canApplyAura", aura.CanApplyAura);
        SetBoolean(state, "isBossAura", aura.IsBossAura);
        SetBoolean(state, "nameplateShowAll", aura.NameplateShowAll);
        SetNumber(state, "timeMod", aura.TimeMod);
        SetBoolean(state, "isHelpful", aura.IsHelpful);
        SetBoolean(state, "isHarmful", aura.IsHarmful);
        SetBoolean(state, "isFromPlayerOrPlayerPet", aura.IsFromPlayerOrPlayerPet);
        SetBoolean(state, "isNameplateOnly", aura.IsNameplateOnly);
        SetBoolean(state, "isRaid", aura.IsRaid);
        SetBoolean(state, "canActivePlayerDispel", aura.CanActivePlayerDispel);
        SetNumber(state, "auraInstanceID", aura.AuraInstanceId);
        lua_createtable(state, aura.Points.Count, 0);
        for (var index = 0; index < aura.Points.Count; index++)
        {
            lua_pushnumber(state, aura.Points[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "points");
        SetBoolean(state, "isTankRoleAura", aura.IsTankRoleAura);
        SetBoolean(state, "isHealerRoleAura", aura.IsHealerRoleAura);
        SetBoolean(state, "isDPSRoleAura", aura.IsDpsRoleAura);
        SetBoolean(state, "hideOnPartyFrames", aura.HideOnPartyFrames);
    }

    private static bool TryReadRequiredString(
        lua_State state,
        int index,
        out string value)
    {
        value = string.Empty;
        if (index > lua_gettop(state) || lua_isstring(state, index) == 0)
            return false;
        value = lua_tostring(state, index) ?? string.Empty;
        return true;
    }

    private static bool TryReadOptionalString(
        lua_State state,
        int index,
        out string? value)
    {
        value = null;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (lua_isstring(state, index) == 0)
            return false;
        value = lua_tostring(state, index);
        return true;
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

    private static bool TryReadRequiredUInt32(
        lua_State state,
        int index,
        out uint value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < uint.MinValue or > uint.MaxValue)
            return false;
        value = (uint)number;
        return true;
    }

    private static bool TryReadOptionalUInt32(
        lua_State state,
        int index,
        out uint? value)
    {
        value = null;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (!TryReadRequiredUInt32(state, index, out var parsed))
            return false;
        value = parsed;
        return true;
    }

    private static bool TryReadOptionalEnum(
        lua_State state,
        int index,
        int minimum,
        int maximum,
        out int? value)
    {
        value = null;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return true;
        if (!TryReadRequiredInt32(state, index, out var parsed) ||
            parsed < minimum ||
            parsed > maximum)
            return false;
        value = parsed;
        return true;
    }

    private static bool TryReadSpellIdentifier(
        LuaRuntime runtime,
        lua_State state,
        int index,
        string unit,
        out int spellId)
    {
        spellId = 0;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return false;
        if (lua_isnumber(state, index) != 0)
        {
            var number = lua_tonumber(state, index);
            if (!double.IsFinite(number) || number is < int.MinValue or > int.MaxValue)
                return false;
            spellId = (int)number;
            return true;
        }
        if (lua_type(state, index) != LUA_TSTRING)
            return false;

        var identifier = lua_tostring(state, index) ?? string.Empty;
        var marker = identifier.IndexOf("spell:", StringComparison.OrdinalIgnoreCase);
        if (marker >= 0)
        {
            var text = identifier[(marker + 6)..];
            var terminator = text.IndexOfAny(['|', ']', ' ', ':']);
            if (terminator >= 0)
                text = text[..terminator];
            var style = NumberStyles.Integer;
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                text = text[2..];
                style = NumberStyles.HexNumber;
            }
            return int.TryParse(text, style, CultureInfo.InvariantCulture, out spellId);
        }

        spellId = runtime.Spells.FindIdByName(identifier);
        if (spellId != 0)
            return true;
        spellId = runtime.UnitAuras.Find(unit)
            .FirstOrDefault(aura =>
                aura.Name.Equals(identifier, StringComparison.OrdinalIgnoreCase))
            ?.SpellId ?? 0;
        return true;
    }

    private static bool TryReadRequiredStringField(
        lua_State state,
        int tableIndex,
        string name,
        out string value)
    {
        tableIndex = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, tableIndex, name);
        var valid = TryReadRequiredString(state, lua_gettop(state), out value);
        lua_pop(state, 1);
        return valid;
    }

    private static bool TryReadRequiredUInt32Field(
        lua_State state,
        int tableIndex,
        string name,
        out uint value)
    {
        tableIndex = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, tableIndex, name);
        var valid = TryReadRequiredUInt32(state, lua_gettop(state), out value);
        lua_pop(state, 1);
        return valid;
    }

    private static bool ReadOptionalBooleanField(lua_State state, int tableIndex, string name) =>
        TryReadRequiredBooleanField(state, tableIndex, name, out var value) && value;

    private static bool TryReadRequiredBooleanField(
        lua_State state,
        int tableIndex,
        string name,
        out bool value)
    {
        tableIndex = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, tableIndex, name);
        var top = lua_gettop(state);
        var valid = lua_isnil(state, top) == 0;
        value = valid && lua_toboolean(state, top) != 0;
        lua_pop(state, 1);
        return valid;
    }

    private static bool TryReadRequiredNumberField(
        lua_State state,
        int tableIndex,
        string name,
        out double value)
    {
        tableIndex = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, tableIndex, name);
        var top = lua_gettop(state);
        var valid = lua_isnumber(state, top) != 0;
        value = valid ? lua_tonumber(state, top) : 0;
        valid &= double.IsFinite(value);
        lua_pop(state, 1);
        return valid;
    }

    private static bool TryReadOptionalNumberField(
        lua_State state,
        int tableIndex,
        string name,
        out double? value)
    {
        tableIndex = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, tableIndex, name);
        var top = lua_gettop(state);
        if (lua_isnil(state, top) != 0)
        {
            value = null;
            lua_pop(state, 1);
            return true;
        }
        var valid = lua_isnumber(state, top) != 0;
        var number = valid ? lua_tonumber(state, top) : 0;
        valid &= double.IsFinite(number);
        value = valid ? number : null;
        lua_pop(state, 1);
        return valid;
    }

    private static bool TryReadRequiredObjectField(
        LuaRuntime runtime,
        lua_State state,
        int tableIndex,
        string name,
        out UiObject value)
    {
        tableIndex = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, tableIndex, name);
        value = LuaBindings.GetObject(runtime, -1)!;
        lua_pop(state, 1);
        return value is not null;
    }

    private static bool TryReadRequiredTableField(
        lua_State state,
        int tableIndex,
        string name,
        out int fieldIndex)
    {
        tableIndex = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, tableIndex, name);
        fieldIndex = lua_gettop(state);
        if (lua_type(state, fieldIndex) == LUA_TTABLE)
            return true;
        lua_pop(state, 1);
        fieldIndex = 0;
        return false;
    }

    private static bool TryReadOptionalTableField(
        lua_State state,
        int tableIndex,
        string name,
        out bool present)
    {
        tableIndex = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, tableIndex, name);
        if (lua_isnil(state, -1) != 0)
        {
            lua_pop(state, 1);
            present = false;
            return true;
        }
        present = true;
        if (lua_type(state, -1) == LUA_TTABLE)
            return true;
        lua_pop(state, 1);
        return false;
    }

    private static bool TryReadAnchor(
        LuaRuntime runtime,
        lua_State state,
        int tableIndex,
        out WowAuraAnchorPointState? anchor)
    {
        anchor = null;
        tableIndex = AbsoluteIndex(state, tableIndex);
        if (lua_type(state, tableIndex) != LUA_TTABLE ||
            !TryReadRequiredFramePointField(state, tableIndex, "point", out var point) ||
            !TryReadRequiredObjectField(runtime, state, tableIndex, "relativeTo", out var relativeTo) ||
            !(relativeTo.IsRegion || WowWidgetApi.IsFrameWidget(relativeTo.ObjectType)) ||
            !TryReadRequiredFramePointField(state, tableIndex, "relativePoint", out var relativePoint) ||
            !TryReadRequiredNumberField(state, tableIndex, "offsetX", out var offsetX) ||
            !TryReadRequiredNumberField(state, tableIndex, "offsetY", out var offsetY))
            return false;
        anchor = new WowAuraAnchorPointState(
            point,
            relativeTo.Id,
            relativePoint,
            offsetX,
            offsetY);
        return true;
    }

    private static bool TryReadRequiredFramePointField(
        lua_State state,
        int tableIndex,
        string name,
        out string value)
    {
        tableIndex = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, tableIndex, name);
        var valid = lua_type(state, -1) == LUA_TSTRING;
        value = valid ? lua_tostring(state, -1) ?? string.Empty : string.Empty;
        valid &= FramePoints.Contains(value);
        lua_pop(state, 1);
        return valid;
    }

    private static int AbsoluteIndex(lua_State state, int index) =>
        index < 0 ? lua_gettop(state) + index + 1 : index;

    private static string CombineFilter(string required, string? filter) =>
        string.IsNullOrWhiteSpace(filter) ? required : $"{required}|{filter}";

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_type(state, -1) != LUA_TTABLE)
        {
            lua_pop(state, 1);
            lua_newtable(state);
        }

        lua_newtable(state);
        SetInteger(state, "Normal", 0);
        SetInteger(state, "Reverse", 1);
        lua_setfield(state, -2, "UnitAuraSortDirection");

        lua_newtable(state);
        SetInteger(state, "NumValues", 2);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 1);
        lua_setfield(state, -2, "UnitAuraSortDirectionMeta");

        lua_newtable(state);
        SetInteger(state, "Unsorted", 0);
        SetInteger(state, "Default", 1);
        SetInteger(state, "BigDefensive", 2);
        SetInteger(state, "Expiration", 3);
        SetInteger(state, "ExpirationOnly", 4);
        SetInteger(state, "Name", 5);
        SetInteger(state, "NameOnly", 6);
        lua_setfield(state, -2, "UnitAuraSortRule");

        lua_newtable(state);
        SetInteger(state, "NumValues", 7);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 6);
        lua_setfield(state, -2, "UnitAuraSortRuleMeta");
        lua_setglobal(state, "Enum");
    }

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

    private static void SetBoolean(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalString(lua_State state, string name, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }
}
