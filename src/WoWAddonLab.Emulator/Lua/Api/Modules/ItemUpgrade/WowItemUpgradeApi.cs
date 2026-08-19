using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowItemUpgradeApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "CanUpgradeItem", "ClearItemUpgrade", "CloseItemUpgrade",
                     "GetItemUpgradeItemInfo"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_ItemUpgrade");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var itemUpgrade = runtime.ItemUpgrade;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "CanUpgradeItem":
            {
                var location = WowItemApi.RequiredItemLocation(
                    state,
                    "Usage: local isValid = C_ItemUpgrade.CanUpgradeItem(baseItem)");
                if (!runtime.Items.LocationItemIds.ContainsKey(location))
                {
                    return luaL_error(
                        state,
                        "Usage: local isValid = C_ItemUpgrade.CanUpgradeItem(baseItem)");
                }

                lua_pushboolean(
                    state,
                    itemUpgrade.UpgradableItemLocations.Contains(location) ? 1 : 0);
                return 1;
            }
            case "ClearItemUpgrade":
                itemUpgrade.CurrentItemInfo = null;
                itemUpgrade.ClearRequestCount++;
                return 0;
            case "CloseItemUpgrade":
                itemUpgrade.IsOpen = false;
                itemUpgrade.CloseRequestCount++;
                return 0;
            case "GetItemUpgradeItemInfo":
                if (itemUpgrade.CurrentItemInfo is not { } info)
                {
                    return 0;
                }

                PushItemInfo(state, info);
                return 1;
            default:
                return 0;
        }
    }

    private static void PushItemInfo(
        lua_State state,
        WowItemUpgradeItemInfoState info)
    {
        lua_newtable(state);
        SetInteger(state, "iconID", info.IconId);
        SetString(state, "name", info.Name);
        SetBoolean(state, "itemUpgradeable", info.ItemUpgradeable);
        SetInteger(state, "displayQuality", info.DisplayQuality);
        SetInteger(state, "highWatermarkSlot", info.HighWatermarkSlot);
        SetInteger(state, "currUpgrade", info.CurrentUpgrade);
        SetInteger(state, "maxUpgrade", info.MaximumUpgrade);
        SetInteger(state, "minItemLevel", info.MinimumItemLevel);
        SetInteger(state, "maxItemLevel", info.MaximumItemLevel);
        PushUpgradeLevelInfos(state, info.UpgradeLevelInfos);
        lua_setfield(state, -2, "upgradeLevelInfos");
        SetOptionalString(
            state,
            "customUpgradeString",
            info.CustomUpgradeString);
        PushUpgradeCostTypes(state, info.UpgradeCostTypesForSeason);
        lua_setfield(state, -2, "upgradeCostTypesForSeason");
    }

    private static void PushUpgradeLevelInfos(
        lua_State state,
        IReadOnlyList<WowItemUpgradeLevelInfoState> levelInfos)
    {
        lua_newtable(state);
        for (var index = 0; index < levelInfos.Count; index++)
        {
            var info = levelInfos[index];
            lua_newtable(state);
            SetInteger(state, "upgradeLevel", info.UpgradeLevel);
            SetInteger(state, "displayQuality", info.DisplayQuality);
            SetInteger(
                state,
                "itemLevelIncrement",
                info.ItemLevelIncrement);
            PushLevelStats(state, info.LevelStats);
            lua_setfield(state, -2, "levelStats");
            PushCurrencyCosts(state, info.CurrencyCostsToUpgrade);
            lua_setfield(state, -2, "currencyCostsToUpgrade");
            PushItemCosts(state, info.ItemCostsToUpgrade);
            lua_setfield(state, -2, "itemCostsToUpgrade");
            SetOptionalNumber(state, "moneyCost", info.MoneyCost);
            SetOptionalString(state, "failureMessage", info.FailureMessage);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushLevelStats(
        lua_State state,
        IReadOnlyList<WowItemUpgradeStatState> stats)
    {
        lua_newtable(state);
        for (var index = 0; index < stats.Count; index++)
        {
            var stat = stats[index];
            lua_newtable(state);
            SetString(state, "displayString", stat.DisplayString);
            SetInteger(state, "statValue", stat.StatValue);
            SetBoolean(state, "active", stat.Active);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushCurrencyCosts(
        lua_State state,
        IReadOnlyList<WowItemUpgradeCurrencyCostState> costs)
    {
        lua_newtable(state);
        for (var index = 0; index < costs.Count; index++)
        {
            var cost = costs[index];
            lua_newtable(state);
            SetInteger(state, "cost", cost.Cost);
            SetInteger(state, "currencyID", cost.CurrencyId);
            PushDiscountInfo(state, cost.DiscountInfo);
            lua_setfield(state, -2, "discountInfo");
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushItemCosts(
        lua_State state,
        IReadOnlyList<WowItemUpgradeItemCostState> costs)
    {
        lua_newtable(state);
        for (var index = 0; index < costs.Count; index++)
        {
            var cost = costs[index];
            lua_newtable(state);
            SetInteger(state, "cost", cost.Cost);
            SetInteger(state, "itemID", cost.ItemId);
            PushDiscountInfo(state, cost.DiscountInfo);
            lua_setfield(state, -2, "discountInfo");
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushDiscountInfo(
        lua_State state,
        WowItemUpgradeDiscountInfoState info)
    {
        lua_newtable(state);
        SetBoolean(state, "isDiscounted", info.IsDiscounted);
        SetInteger(
            state,
            "discountHighWatermark",
            info.DiscountHighWatermark);
        SetBoolean(
            state,
            "isPartialTwoHandDiscount",
            info.IsPartialTwoHandDiscount);
        SetBoolean(
            state,
            "isAccountWideDiscount",
            info.IsAccountWideDiscount);
        SetBoolean(
            state,
            "doesCurrentCharacterMeetHighWatermark",
            info.DoesCurrentCharacterMeetHighWatermark);
    }

    private static void PushUpgradeCostTypes(
        lua_State state,
        IReadOnlyList<WowItemUpgradeCostTypeForSeasonState> costTypes)
    {
        lua_newtable(state);
        for (var index = 0; index < costTypes.Count; index++)
        {
            var costType = costTypes[index];
            lua_newtable(state);
            SetOptionalInteger(state, "itemID", costType.ItemId);
            SetOptionalInteger(state, "currencyID", costType.CurrencyId);
            SetInteger(state, "orderIndex", costType.OrderIndex);
            SetOptionalString(state, "sourceString", costType.SourceString);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void SetInteger(lua_State state, string name, long value)
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

    private static void SetOptionalInteger(
        lua_State state,
        string name,
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
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalNumber(
        lua_State state,
        string name,
        long? value)
    {
        if (value is { } number)
        {
            lua_pushnumber(state, number);
        }
        else
        {
            lua_pushnil(state);
        }
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalString(
        lua_State state,
        string name,
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
        lua_setfield(state, -2, name);
    }
}
