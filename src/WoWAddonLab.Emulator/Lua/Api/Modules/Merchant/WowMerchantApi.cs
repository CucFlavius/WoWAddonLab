using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowMerchantApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] NamespaceFunctions =
    [
        "GetBuybackItemID",
        "GetItemInfo",
        "GetItemLink",
        "GetMerchantCurrencies",
        "GetNumItems",
        "GetNumJunkItems",
        "IsMerchantItemRefundable",
        "IsSellAllJunkEnabled",
        "SellAllJunkItems"
    ];

    private static readonly string[] GlobalFunctions =
    [
        "BuybackItem",
        "BuyMerchantItem",
        "CanAffordMerchantItem",
        "CanMerchantRepair",
        "CloseMerchant",
        "GetBuybackItemInfo",
        "GetBuybackItemLink",
        "GetMerchantItemCostInfo",
        "GetMerchantItemCostItem",
        "GetMerchantItemID",
        "GetMerchantItemLink",
        "GetMerchantItemMaxStack",
        "GetMerchantFilter",
        "GetMerchantNumItems",
        "GetNumBuybackItems",
        "GetRepairAllCost",
        "HideRepairCursor",
        "InRepairMode",
        "PickupMerchantItem",
        "RepairAllItems",
        "ResetSetMerchantFilter",
        "SetMerchantFilter",
        "ShowBuybackSellCursor",
        "ShowRepairCursor"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in NamespaceFunctions)
        {
            lua_pushstring(state, $"C_MerchantFrame.{function}");
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_MerchantFrame");

        foreach (var function in GlobalFunctions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setglobal(state, function);
        }
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var merchant = runtime.Merchant;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "C_MerchantFrame.GetBuybackItemID":
            {
                var index = RequiredOneBasedIndex(state, 1, operation);
                var item = BuybackItem(merchant, index);
                if (item is null)
                    return 0;
                lua_pushnumber(state, item.ItemId);
                return 1;
            }
            case "C_MerchantFrame.GetItemInfo":
            {
                var index = RequiredOneBasedIndex(state, 1, operation);
                var item = MerchantItem(merchant, index);
                if (item is null)
                    return 0;
                PushMerchantItemInfo(state, item);
                return 1;
            }
            case "C_MerchantFrame.GetMerchantCurrencies":
                PushIntegerArray(state, CollectMerchantCurrencyIds(merchant));
                return 1;
            case "C_MerchantFrame.GetNumJunkItems":
                lua_pushnumber(
                    state,
                    merchant.IsOpen && merchant.SellAllJunkEnabled
                        ? Math.Max(0, merchant.NumJunkItems)
                        : 0);
                return 1;
            case "C_MerchantFrame.IsMerchantItemRefundable":
            {
                var index = RequiredOneBasedIndex(state, 1, operation);
                lua_pushboolean(
                    state,
                    MerchantItem(merchant, index)?.IsRefundable == true ? 1 : 0);
                return 1;
            }
            case "C_MerchantFrame.IsSellAllJunkEnabled":
                lua_pushboolean(
                    state,
                    merchant.IsOpen && merchant.SellAllJunkEnabled ? 1 : 0);
                return 1;
            case "C_MerchantFrame.SellAllJunkItems":
                if (merchant.IsOpen && merchant.SellAllJunkEnabled)
                    merchant.SellAllJunkRequestCount++;
                return 0;

            case "CloseMerchant":
                merchant.CloseRequestCount++;
                merchant.IsOpen = false;
                merchant.IsInRepairMode = false;
                merchant.BuybackSellCursorMode = null;
                return 0;
            case "GetMerchantNumItems":
            case "C_MerchantFrame.GetNumItems":
                lua_pushnumber(state, merchant.IsOpen ? merchant.Items.Count : 0);
                return 1;
            case "GetMerchantFilter":
                lua_pushnumber(state, merchant.Filter);
                return 1;
            case "GetNumBuybackItems":
                lua_pushnumber(
                    state,
                    merchant.IsOpen ? merchant.BuybackItems.Count : 0);
                return 1;
            case "GetMerchantItemCostInfo":
            {
                var index = RequiredLegacyIndex(state, 1, operation);
                lua_pushnumber(
                    state,
                    MerchantItem(merchant, index)?.Costs.Count ?? 0);
                return 1;
            }
            case "GetMerchantItemCostItem":
            {
                var itemIndex = RequiredLegacyIndex(state, 1, operation);
                var costIndex = RequiredLegacyIndex(state, 2, operation);
                var item = MerchantItem(merchant, itemIndex);
                var cost = item is not null &&
                           costIndex <= item.Costs.Count
                    ? item.Costs[costIndex - 1]
                    : null;
                if (cost is null)
                {
                    lua_pushnil(state);
                    lua_pushnumber(state, 0);
                    return 2;
                }

                PushOptionalNumber(state, cost.TextureFileId);
                lua_pushnumber(state, cost.Quantity);
                PushOptionalString(state, cost.Link);
                if (!cost.IsCurrency)
                    return 3;
                PushOptionalString(state, cost.Name);
                return 4;
            }
            case "GetMerchantItemID":
            {
                var index = RequiredLegacyIndex(state, 1, operation);
                var item = MerchantItem(merchant, index);
                var id = item?.Kind == WowMerchantItemKind.Item
                    ? item.ItemId
                    : null;
                if (id is null or 0)
                    return 0;
                lua_pushnumber(state, id.Value);
                return 1;
            }
            case "GetMerchantItemLink":
            case "C_MerchantFrame.GetItemLink":
            {
                var index = RequiredLegacyIndex(state, 1, operation);
                var item = MerchantItem(merchant, index);
                if (item is null)
                    return 0;
                PushOptionalString(state, item.Link);
                return 1;
            }
            case "GetMerchantItemMaxStack":
            {
                var index = RequiredLegacyIndex(state, 1, operation);
                var item = MerchantItem(merchant, index);
                lua_pushnumber(
                    state,
                    item?.Kind == WowMerchantItemKind.Item
                        ? Math.Max(1, item.MaxStack)
                        : 1);
                return 1;
            }
            case "GetBuybackItemInfo":
            {
                var index = RequiredLegacyIndex(state, 1, operation);
                PushBuybackItemInfo(state, BuybackItem(merchant, index));
                return 7;
            }
            case "GetBuybackItemLink":
            {
                var index = RequiredLegacyIndex(state, 1, operation);
                var item = BuybackItem(merchant, index);
                if (item is null)
                    return 0;
                PushOptionalString(state, item.Link);
                return 1;
            }
            case "CanAffordMerchantItem":
            {
                var index = RequiredLegacyIndex(state, 1, operation);
                var canAfford = MerchantItem(merchant, index)?.CanAfford;
                if (!canAfford.HasValue)
                    return 0;
                lua_pushboolean(state, canAfford.Value ? 1 : 0);
                return 1;
            }
            case "PickupMerchantItem":
            {
                var index = OptionalLegacyIndex(state, 1);
                var item = index.HasValue
                    ? MerchantItem(merchant, index.Value)
                    : null;
                if (item is
                    {
                        Kind: WowMerchantItemKind.Item,
                        IsPurchasable: true
                    })
                {
                    merchant.LastPickedItemIndex = index;
                }
                return 0;
            }
            case "BuyMerchantItem":
            {
                var index = RequiredLegacyIndex(state, 1, operation);
                var item = MerchantItem(merchant, index);
                if (item is null || !item.IsPurchasable)
                    return 0;
                var quantity = OptionalLegacyInteger(state, 2) ??
                               Math.Max(1, item.StackCount);
                quantity = Math.Max(1, quantity);
                if (item.Kind == WowMerchantItemKind.Item)
                    quantity = Math.Min(quantity, 5000);
                merchant.LastPurchaseRequest =
                    new WowMerchantPurchaseRequest(index, quantity);
                return 0;
            }
            case "BuybackItem":
            {
                var index = RequiredLegacyIndex(state, 1, operation);
                if (BuybackItem(merchant, index) is not null)
                    merchant.LastBuybackItemIndex = index;
                return 0;
            }
            case "ShowBuybackSellCursor":
            {
                var index = RequiredLegacyIndex(state, 1, operation);
                var item = BuybackItem(merchant, index);
                if (item is not null)
                {
                    merchant.IsInRepairMode = false;
                    merchant.BuybackSellCursorMode =
                        item.Price > Math.Max(0, runtime.Client.Money) ? 49 : 3;
                }
                return 0;
            }
            case "CanMerchantRepair":
                lua_pushboolean(state, merchant.CanRepair ? 1 : 0);
                return 1;
            case "ShowRepairCursor":
                if (merchant.CanRepair)
                {
                    merchant.BuybackSellCursorMode = null;
                    merchant.IsInRepairMode = true;
                }
                return 0;
            case "HideRepairCursor":
                if (merchant.IsInRepairMode)
                    merchant.IsInRepairMode = false;
                return 0;
            case "InRepairMode":
                lua_pushboolean(state, merchant.IsInRepairMode ? 1 : 0);
                return 1;
            case "GetRepairAllCost":
                lua_pushnumber(
                    state,
                    merchant.CanRepair ? merchant.RepairAllCost : 0);
                if (!merchant.CanRepair)
                    return 1;
                lua_pushboolean(state, merchant.RepairAllCost != 0 ? 1 : 0);
                return 2;
            case "RepairAllItems":
            {
                var useGuildBank = lua_toboolean(state, 1) != 0;
                if (merchant.CanRepair &&
                    (useGuildBank ||
                     runtime.Client.Money >= merchant.RepairAllCost))
                {
                    merchant.RepairAllRequestCount++;
                    merchant.LastRepairAllUsedGuildBank = useGuildBank;
                }
                return 0;
            }
            case "ResetSetMerchantFilter":
                merchant.Filter = 2;
                merchant.ResetFilterRequestCount++;
                return 0;
            case "SetMerchantFilter":
            {
                var filter = OptionalLegacyIndex(state, 1);
                if (filter.HasValue)
                {
                    merchant.Filter = filter.Value;
                    merchant.ResetFilterRequestCount++;
                }
                return 0;
            }
        }

        return 0;
    }

    private static WowMerchantItemData? MerchantItem(
        WowMerchantState merchant,
        int oneBasedIndex) =>
        merchant.IsOpen &&
        oneBasedIndex > 0 &&
        oneBasedIndex <= merchant.Items.Count
            ? merchant.Items[oneBasedIndex - 1]
            : null;

    private static WowBuybackItemData? BuybackItem(
        WowMerchantState merchant,
        int oneBasedIndex) =>
        merchant.IsOpen &&
        oneBasedIndex > 0 &&
        oneBasedIndex <= merchant.BuybackItems.Count
            ? merchant.BuybackItems[oneBasedIndex - 1]
            : null;

    private static IReadOnlyList<int> CollectMerchantCurrencyIds(
        WowMerchantState merchant)
    {
        if (!merchant.IsOpen)
            return [];

        var values = new List<int>();
        var seen = new HashSet<int>();

        static void Add(
            ICollection<int> destination,
            ISet<int> observed,
            int? value)
        {
            if (value is > 0 && observed.Add(value.Value))
                destination.Add(value.Value);
        }

        foreach (var currencyId in merchant.CurrencyIds)
            Add(values, seen, currencyId);

        foreach (var item in merchant.Items)
        {
            if (item.Kind == WowMerchantItemKind.Currency)
                Add(values, seen, item.CurrencyId);
            foreach (var cost in item.Costs)
            {
                if (cost.IsCurrency)
                    Add(values, seen, cost.CurrencyId);
            }
        }

        return values;
    }

    private static int RequiredOneBasedIndex(
        lua_State state,
        int argument,
        string operation)
    {
        var index = RequiredLegacyIndex(state, argument, operation);
        if (index < 1)
            return luaL_error(state, Usage(operation));
        return index;
    }

    private static int RequiredLegacyIndex(
        lua_State state,
        int argument,
        string operation)
    {
        if (lua_isnumber(state, argument) == 0)
            return luaL_error(state, Usage(operation));
        var value = lua_tonumber(state, argument);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return luaL_error(state, Usage(operation));
        }
        return unchecked((int)value);
    }

    private static int? OptionalLegacyIndex(lua_State state, int argument)
    {
        if (lua_isnumber(state, argument) == 0)
            return null;
        var value = lua_tonumber(state, argument);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return null;
        }
        return unchecked((int)value);
    }

    private static int? OptionalLegacyInteger(lua_State state, int argument) =>
        lua_isnumber(state, argument) != 0
            ? OptionalLegacyIndex(state, argument)
            : null;

    private static string Usage(string operation) => operation switch
    {
        "C_MerchantFrame.GetBuybackItemID" =>
            "Usage: local buybackItemID = C_MerchantFrame.GetBuybackItemID(buybackSlotIndex)",
        "C_MerchantFrame.GetItemInfo" =>
            "Usage: local info = C_MerchantFrame.GetItemInfo(index)",
        "C_MerchantFrame.IsMerchantItemRefundable" =>
            "Usage: local isRefundable = C_MerchantFrame.IsMerchantItemRefundable(index)",
        _ => $"Usage: {operation}(index)"
    };

    private static void PushMerchantItemInfo(
        lua_State state,
        WowMerchantItemData item)
    {
        lua_newtable(state);
        PushOptionalStringField(state, "name", item.Name);
        PushOptionalNumberField(state, "texture", item.TextureFileId);
        PushNumberField(state, "price", item.Price);
        PushNumberField(state, "stackCount", item.StackCount);
        PushNumberField(state, "numAvailable", item.NumAvailable);
        PushBooleanField(state, "isPurchasable", item.IsPurchasable);
        PushBooleanField(state, "isUsable", item.IsUsable);
        PushBooleanField(state, "hasExtendedCost", item.HasExtendedCost);
        PushOptionalNumberField(
            state,
            "currencyID",
            item.Kind == WowMerchantItemKind.Currency
                ? item.CurrencyId
                : null);
        PushOptionalNumberField(
            state,
            "spellID",
            item.Kind is WowMerchantItemKind.Spell or WowMerchantItemKind.Type4
                ? item.SpellId
                : null);
        PushBooleanField(state, "isQuestStartItem", item.IsQuestStartItem);
    }

    private static void PushBuybackItemInfo(
        lua_State state,
        WowBuybackItemData? item)
    {
        if (item is null)
        {
            lua_pushnil(state);
            lua_pushnil(state);
            lua_pushnumber(state, 0);
            lua_pushnumber(state, 1);
            lua_pushnumber(state, 0);
            lua_pushboolean(state, 1);
            lua_pushnil(state);
            return;
        }

        PushOptionalString(state, item.Name);
        PushOptionalNumber(state, item.TextureFileId);
        lua_pushnumber(state, item.Price);
        lua_pushnumber(state, Math.Max(1, item.StackCount));
        lua_pushnumber(state, 0);
        lua_pushboolean(state, item.IsUsable ? 1 : 0);
        if (item.AdditionalFlag.HasValue)
            lua_pushboolean(state, item.AdditionalFlag.Value ? 1 : 0);
        else
            lua_pushnil(state);
    }

    private static void PushIntegerArray(
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

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }

    private static void PushOptionalNumber(lua_State state, int? value)
    {
        if (value.HasValue)
            lua_pushnumber(state, value.Value);
        else
            lua_pushnil(state);
    }

    private static void PushOptionalStringField(
        lua_State state,
        string field,
        string? value)
    {
        PushOptionalString(state, value);
        lua_setfield(state, -2, field);
    }

    private static void PushOptionalNumberField(
        lua_State state,
        string field,
        int? value)
    {
        PushOptionalNumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void PushNumberField(
        lua_State state,
        string field,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void PushBooleanField(
        lua_State state,
        string field,
        bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, field);
    }
}
