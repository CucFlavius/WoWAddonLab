using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowLegendaryCraftingApi : LuaApiModule
{
    private const int PlayerInteractionType = 48;
    private const int RuneforgeCraftSpellId = 288097;
    private const string Namespace = "C_LegendaryCrafting";

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CloseRuneforgeInteraction",
        "CraftRuneforgeLegendary",
        "GetRuneforgeItemPreviewInfo",
        "GetRuneforgeLegendaryComponentInfo",
        "GetRuneforgeLegendaryCost",
        "GetRuneforgeLegendaryCraftSpellID",
        "GetRuneforgeLegendaryCurrencies",
        "GetRuneforgeLegendaryUpgradeCost",
        "GetRuneforgeModifierInfo",
        "GetRuneforgeModifiers",
        "GetRuneforgePowerInfo",
        "GetRuneforgePowerSlots",
        "GetRuneforgePowers",
        "GetRuneforgePowersByClassSpecAndCovenant",
        "IsRuneforgeLegendary",
        "IsRuneforgeLegendaryMaxLevel",
        "IsUpgradeItemValidForRuneforgeLegendary",
        "IsValidRuneforgeBaseItem",
        "MakeRuneforgeCraftDescription",
        "UpgradeRuneforgeLegendary"
    ];

    public override void Register(lua_State state)
    {
        RegisterEnums(state);

        lua_createtable(state, 0, Functions.Length);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, Namespace);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var crafting = runtime.LegendaryCrafting;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "CloseRuneforgeInteraction":
                ClearInteraction(runtime.PlayerInteractions);
                runtime.TriggerEvent(
                    "RUNEFORGE_LEGENDARY_CRAFTING_CLOSED");
                return 0;
            case "CraftRuneforgeLegendary":
            {
                const string usage =
                    "Usage: C_LegendaryCrafting." +
                    "CraftRuneforgeLegendary(description)";
                RequiredTable(state, 1, usage);
                var baseItem = RequiredItemLocationField(
                    state,
                    1,
                    "baseItem",
                    usage);
                var powerId = RequiredInt32Field(
                    state,
                    1,
                    "runeforgePowerID",
                    usage);
                var modifiers = RequiredInt32ArrayField(
                    state,
                    1,
                    "modifiers",
                    usage);
                crafting.CraftRequests.Add(
                    new WowRuneforgeCraftRequest(
                        baseItem,
                        powerId,
                        modifiers));
                return 0;
            }
            case "GetRuneforgeItemPreviewInfo":
            {
                const string usage =
                    "Usage: local info = C_LegendaryCrafting." +
                    "GetRuneforgeItemPreviewInfo(baseItem " +
                    "[, runeforgePowerID, modifiers])";
                var baseItem =
                    WowItemApi.RequiredItemLocation(state, usage);
                var powerId = OptionalInt32(state, 2, usage);
                var modifiers = OptionalInt32Array(state, 3, usage);
                var rule = crafting.PreviewRules.FirstOrDefault(candidate =>
                    candidate.BaseItem == baseItem &&
                    candidate.PowerId == powerId &&
                    candidate.Modifiers.SequenceEqual(modifiers));
                if (rule is null)
                {
                    lua_pushnil(state);
                    return 1;
                }
                PushPreviewInfo(state, rule.Info);
                return 1;
            }
            case "GetRuneforgeLegendaryComponentInfo":
            {
                const string usage =
                    "Usage: local componentInfo = C_LegendaryCrafting." +
                    "GetRuneforgeLegendaryComponentInfo(" +
                    "runeforgeLegendary)";
                var item =
                    WowItemApi.RequiredItemLocation(state, usage);
                if (!crafting.ComponentsByItem.TryGetValue(
                        item,
                        out var info))
                {
                    info = new WowRuneforgeLegendaryComponentInfo(0, []);
                }
                lua_createtable(state, 0, 2);
                SetNumber(state, "powerID", info.PowerId);
                PushInt32Array(state, info.Modifiers);
                lua_setfield(state, -2, "modifiers");
                return 1;
            }
            case "GetRuneforgeLegendaryCost":
            {
                const string usage =
                    "Usage: local cost = C_LegendaryCrafting." +
                    "GetRuneforgeLegendaryCost(baseItem)";
                var item =
                    WowItemApi.RequiredItemLocation(state, usage);
                PushCurrencyCosts(
                    state,
                    GetCosts(crafting, item));
                return 1;
            }
            case "GetRuneforgeLegendaryCraftSpellID":
                lua_pushnumber(state, RuneforgeCraftSpellId);
                return 1;
            case "GetRuneforgeLegendaryCurrencies":
                PushInt32Array(state, crafting.Currencies);
                return 1;
            case "GetRuneforgeLegendaryUpgradeCost":
            {
                const string usage =
                    "Usage: local cost = C_LegendaryCrafting." +
                    "GetRuneforgeLegendaryUpgradeCost(" +
                    "runeforgeLegendary, upgradeItem)";
                var legendary =
                    WowItemApi.RequiredItemLocation(state, 1, usage);
                var upgrade =
                    WowItemApi.RequiredItemLocation(state, 2, usage);
                PushCurrencyCosts(
                    state,
                    SubtractCosts(
                        GetCosts(crafting, upgrade),
                        GetCosts(crafting, legendary)));
                return 1;
            }
            case "GetRuneforgeModifierInfo":
            {
                const string usage =
                    "Usage: local name, description = " +
                    "C_LegendaryCrafting.GetRuneforgeModifierInfo(" +
                    "baseItem [, powerID], addedModifierIndex, modifiers)";
                var baseItem =
                    WowItemApi.RequiredItemLocation(state, usage);
                var powerId = OptionalInt32(state, 2, usage);
                var modifierIndex =
                    RequiredOneBasedIndex(state, 3, usage);
                var modifiers = RequiredInt32Array(state, 4, usage);
                var rule =
                    crafting.ModifierInfoRules.FirstOrDefault(candidate =>
                        candidate.BaseItem == baseItem &&
                        candidate.PowerId == powerId &&
                        candidate.AddedModifierIndex == modifierIndex &&
                        candidate.Modifiers.SequenceEqual(modifiers));
                lua_pushstring(state, rule?.Name ?? string.Empty);
                PushStringArray(state, rule?.Description ?? []);
                return 2;
            }
            case "GetRuneforgeModifiers":
                PushInt32Array(state, crafting.Modifiers);
                return 1;
            case "GetRuneforgePowerInfo":
            {
                const string usage =
                    "Usage: local power = C_LegendaryCrafting." +
                    "GetRuneforgePowerInfo(runeforgePowerID)";
                var powerId = RequiredInt32(state, 1, usage);
                if (!crafting.Powers.TryGetValue(powerId, out var info))
                {
                    info = new WowRuneforgePowerInfo(
                        powerId,
                        1,
                        null,
                        0,
                        string.Empty,
                        null,
                        0,
                        null,
                        false,
                        false,
                        null,
                        []);
                }
                PushPowerInfo(state, info);
                return 1;
            }
            case "GetRuneforgePowerSlots":
            {
                const string usage =
                    "Usage: local slotNames = C_LegendaryCrafting." +
                    "GetRuneforgePowerSlots(runeforgePowerID)";
                var powerId = RequiredInt32(state, 1, usage);
                PushStringArray(
                    state,
                    crafting.Powers.TryGetValue(powerId, out var info)
                        ? info.Slots
                        : []);
                return 1;
            }
            case "GetRuneforgePowers":
            {
                const string usage =
                    "Usage: local primaryRuneforgePowerIDs, " +
                    "otherRuneforgePowerIDs = C_LegendaryCrafting." +
                    "GetRuneforgePowers([baseItem, filter])";
                WowItemLocation? baseItem = null;
                if (lua_gettop(state) >= 1 && lua_isnil(state, 1) == 0)
                {
                    baseItem =
                        WowItemApi.RequiredItemLocation(state, usage);
                }
                var filter = OptionalPowerFilter(state, 2, usage);
                if (!crafting.PowerLists.TryGetValue(
                        (baseItem, filter),
                        out var powers))
                {
                    powers = new WowRuneforgePowerLists([], []);
                }
                PushInt32Array(state, powers.PrimaryPowerIds);
                PushInt32Array(state, powers.OtherPowerIds);
                return 2;
            }
            case "GetRuneforgePowersByClassSpecAndCovenant":
            {
                const string usage =
                    "Usage: local runeforgePowerIDs = " +
                    "C_LegendaryCrafting." +
                    "GetRuneforgePowersByClassSpecAndCovenant(" +
                    "[classID, specID, covenantID, filter])";
                var classId = OptionalInt32(state, 1, usage);
                var specId = OptionalInt32(state, 2, usage);
                var covenantId = OptionalInt32(state, 3, usage);
                var filter = OptionalPowerFilter(state, 4, usage);
                crafting.PowerListsByClassSpecAndCovenant.TryGetValue(
                    (classId, specId, covenantId, filter),
                    out var powers);
                PushInt32Array(state, powers ?? []);
                return 1;
            }
            case "IsRuneforgeLegendary":
            {
                const string usage =
                    "Usage: local isRuneforgeLegendary = " +
                    "C_LegendaryCrafting.IsRuneforgeLegendary(item)";
                var item =
                    WowItemApi.RequiredItemLocation(state, usage);
                return PushBoolean(
                    state,
                    crafting.RuneforgeLegendaryItems.Contains(item));
            }
            case "IsRuneforgeLegendaryMaxLevel":
            {
                const string usage =
                    "Usage: local isMaxLevel = C_LegendaryCrafting." +
                    "IsRuneforgeLegendaryMaxLevel(runeforgeLegendary)";
                var item =
                    WowItemApi.RequiredItemLocation(state, usage);
                return PushBoolean(
                    state,
                    crafting.MaxLevelRuneforgeLegendaryItems.Contains(item));
            }
            case "IsUpgradeItemValidForRuneforgeLegendary":
            {
                const string usage =
                    "Usage: local isValid = C_LegendaryCrafting." +
                    "IsUpgradeItemValidForRuneforgeLegendary(" +
                    "runeforgeLegendary, upgradeItem)";
                var legendary =
                    WowItemApi.RequiredItemLocation(state, 1, usage);
                var upgrade =
                    WowItemApi.RequiredItemLocation(state, 2, usage);
                return PushBoolean(
                    state,
                    crafting.ValidUpgradePairs.Contains(
                        (legendary, upgrade)));
            }
            case "IsValidRuneforgeBaseItem":
            {
                const string usage =
                    "Usage: local isValid = C_LegendaryCrafting." +
                    "IsValidRuneforgeBaseItem(baseItem)";
                var item =
                    WowItemApi.RequiredItemLocation(state, usage);
                return PushBoolean(
                    state,
                    crafting.ValidBaseItems.Contains(item));
            }
            case "MakeRuneforgeCraftDescription":
            {
                const string usage =
                    "Usage: local description = C_LegendaryCrafting." +
                    "MakeRuneforgeCraftDescription(" +
                    "baseItem, runeforgePowerID, modifiers)";
                var baseItem =
                    WowItemApi.RequiredItemLocation(state, usage);
                var powerId = RequiredInt32(state, 2, usage);
                var modifiers = RequiredInt32Array(state, 3, usage);
                lua_createtable(state, 0, 3);
                WowItemApi.PushItemLocation(state, baseItem);
                lua_setfield(state, -2, "baseItem");
                SetNumber(state, "runeforgePowerID", powerId);
                PushInt32Array(state, modifiers);
                lua_setfield(state, -2, "modifiers");
                return 1;
            }
            case "UpgradeRuneforgeLegendary":
            {
                const string usage =
                    "Usage: C_LegendaryCrafting." +
                    "UpgradeRuneforgeLegendary(" +
                    "runeforgeLegendary, upgradeItem)";
                var legendary =
                    WowItemApi.RequiredItemLocation(state, 1, usage);
                var upgrade =
                    WowItemApi.RequiredItemLocation(state, 2, usage);
                crafting.UpgradeRequests.Add(
                    new WowRuneforgeUpgradeRequest(
                        legendary,
                        upgrade));
                return 0;
            }
            default:
                return 0;
        }
    }

    private static IReadOnlyList<WowRuneforgeCurrencyCost> GetCosts(
        WowLegendaryCraftingState state,
        WowItemLocation item) =>
        state.CostsByItem.TryGetValue(item, out var costs)
            ? costs
            : [];

    private static IReadOnlyList<WowRuneforgeCurrencyCost> SubtractCosts(
        IReadOnlyList<WowRuneforgeCurrencyCost> upgrade,
        IReadOnlyList<WowRuneforgeCurrencyCost> current)
    {
        var result = upgrade
            .Select(cost => new WowRuneforgeCurrencyCost(
                cost.CurrencyId,
                cost.Amount))
            .ToList();
        foreach (var existing in current)
        {
            var index = result.FindIndex(
                cost => cost.CurrencyId == existing.CurrencyId);
            if (index < 0)
                continue;
            var remaining = result[index].Amount - existing.Amount;
            if (remaining > 0)
                result[index] = result[index] with { Amount = remaining };
            else
                result.RemoveAt(index);
        }
        return result;
    }

    private static WowItemLocation RequiredItemLocationField(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        var absolute = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, absolute, field);
        var result =
            WowItemApi.RequiredItemLocation(state, -1, usage);
        lua_pop(state, 1);
        return result;
    }

    private static int RequiredInt32Field(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        var absolute = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, absolute, field);
        var result = RequiredInt32(state, -1, usage);
        lua_pop(state, 1);
        return result;
    }

    private static IReadOnlyList<int> RequiredInt32ArrayField(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        var absolute = AbsoluteIndex(state, tableIndex);
        lua_getfield(state, absolute, field);
        var result = RequiredInt32Array(state, -1, usage);
        lua_pop(state, 1);
        return result;
    }

    private static IReadOnlyList<int> OptionalInt32Array(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_gettop(state) < index || lua_isnil(state, index) != 0)
            return [];
        return RequiredInt32Array(state, index, usage);
    }

    private static IReadOnlyList<int> RequiredInt32Array(
        lua_State state,
        int index,
        string usage)
    {
        RequiredTable(state, index, usage);
        var absolute = AbsoluteIndex(state, index);
        var count = checked((int)lua_objlen(state, absolute));
        var result = new List<int>(count);
        for (var item = 1; item <= count; item++)
        {
            lua_rawgeti(state, absolute, item);
            result.Add(RequiredInt32(state, -1, usage));
            lua_pop(state, 1);
        }
        return result;
    }

    private static void RequiredTable(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_gettop(state) < index || lua_istable(state, index) == 0)
            luaL_error(state, usage);
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
        if (!double.IsFinite(value) ||
            value is < int.MinValue or > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (int)value;
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_gettop(state) < index || lua_isnil(state, index) != 0)
            return null;
        return RequiredInt32(state, index, usage);
    }

    private static int OptionalPowerFilter(
        lua_State state,
        int index,
        string usage)
    {
        var value = OptionalInt32(state, index, usage) ?? 0;
        if (value is < 0 or > 3)
        {
            luaL_error(state, usage);
            return 0;
        }
        return value;
    }

    private static int RequiredOneBasedIndex(
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
        if (!double.IsFinite(value) ||
            value is < uint.MinValue or > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return unchecked((int)((uint)value - 1));
    }

    private static int AbsoluteIndex(lua_State state, int index) =>
        index > 0 || index <= LUA_REGISTRYINDEX
            ? index
            : lua_gettop(state) + index + 1;

    private static void PushPreviewInfo(
        lua_State state,
        WowRuneforgeItemPreviewInfo info)
    {
        lua_createtable(state, 0, 3);
        SetString(state, "itemGUID", info.ItemGuid);
        SetNumber(state, "itemLevel", info.ItemLevel);
        SetString(state, "itemName", info.ItemName);
    }

    private static void PushPowerInfo(
        lua_State state,
        WowRuneforgePowerInfo info)
    {
        lua_createtable(state, 0, 12);
        SetNumber(state, "runeforgePowerID", info.RuneforgePowerId);
        SetNumber(state, "state", info.State);
        SetOptionalString(state, "name", info.Name);
        SetNumber(
            state,
            "descriptionSpellID",
            info.DescriptionSpellId);
        SetString(state, "description", info.Description);
        SetOptionalString(state, "source", info.Source);
        SetNumber(state, "iconFileID", info.IconFileId);
        SetOptionalString(state, "specName", info.SpecName);
        SetBoolean(state, "matchesSpec", info.MatchesSpec);
        SetBoolean(
            state,
            "matchesCovenant",
            info.MatchesCovenant);
        SetOptionalNumber(state, "covenantID", info.CovenantId);
        PushStringArray(state, info.Slots);
        lua_setfield(state, -2, "slots");
    }

    private static void PushCurrencyCosts(
        lua_State state,
        IReadOnlyList<WowRuneforgeCurrencyCost> costs)
    {
        lua_createtable(state, costs.Count, 0);
        for (var index = 0; index < costs.Count; index++)
        {
            var cost = costs[index];
            lua_createtable(state, 0, 2);
            SetNumber(state, "currencyID", cost.CurrencyId);
            SetNumber(state, "amount", cost.Amount);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushInt32Array(
        lua_State state,
        IReadOnlyList<int> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushnumber(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushStringArray(
        lua_State state,
        IReadOnlyList<string> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushstring(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static void SetNumber(
        lua_State state,
        string field,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalNumber(
        lua_State state,
        string field,
        int? value)
    {
        if (value is { } number)
            lua_pushnumber(state, number);
        else
            lua_pushnil(state);
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
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
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

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
        }

        SetEnum(
            state,
            "RuneforgePowerState",
            ("Available", 0),
            ("Unavailable", 1),
            ("Invalid", 2));
        SetEnumMeta(state, "RuneforgePowerStateMeta", 0, 2, 3);
        SetEnum(
            state,
            "RuneforgePowerFilter",
            ("All", 0),
            ("Relevant", 1),
            ("Available", 2),
            ("Unavailable", 3));
        SetEnumMeta(state, "RuneforgePowerFilterMeta", 0, 3, 4);
        lua_setglobal(state, "Enum");
    }

    private static void SetEnum(
        lua_State state,
        string name,
        params (string Name, int Value)[] values)
    {
        lua_createtable(state, 0, values.Length);
        foreach (var value in values)
            SetNumber(state, value.Name, value.Value);
        lua_setfield(state, -2, name);
    }

    private static void SetEnumMeta(
        lua_State state,
        string name,
        int minimum,
        int maximum,
        int count) =>
        SetEnum(
            state,
            name,
            ("MinValue", minimum),
            ("MaxValue", maximum),
            ("NumValues", count));

    private static void ClearInteraction(
        WowPlayerInteractionManagerState interactions)
    {
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType = PlayerInteractionType;
        if (!interactions.HasActiveInteraction ||
            interactions.CurrentInteractionType != PlayerInteractionType)
        {
            return;
        }

        interactions.HasActiveInteraction = false;
        interactions.HasPendingInteraction = false;
        interactions.CurrentInteractionType = 0;
        interactions.PendingInteractionType = 0;
        interactions.ValidNpcInteractionTypes.Remove(PlayerInteractionType);
    }
}
