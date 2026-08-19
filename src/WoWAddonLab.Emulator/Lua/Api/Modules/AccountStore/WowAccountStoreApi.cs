using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowAccountStoreApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetCategories", "GetCategoryInfo", "GetCategoryItems",
                     "GetCurrencyAvailable", "GetCurrencyIDForStore", "GetCurrencyInfo",
                     "GetItemInfo", "GetStoreFrontState", "BeginPurchase", "RefundItem",
                     "RequestStoreFrontInfoUpdate"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_AccountStore");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var provider = runtime.AccountStoreProvider;
        var accountStore = runtime.AccountStore;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation == "GetCategories")
        {
            if (!TryReadRequiredInt32(state, 1, out var storeFrontId))
            {
                return luaL_error(
                    state,
                    "Usage: local categories = C_AccountStore.GetCategories(storeFrontID)");
            }

            PushIntegerTable(
                state,
                provider?.Categories
                    .Where(value => value.StoreFrontId == storeFrontId)
                    .OrderBy(value => value.OrderIndex)
                    .ThenBy(value => value.Id)
                    .Select(value => value.Id) ?? []);
            return 1;
        }
        if (operation == "GetCategoryItems")
        {
            if (!TryReadRequiredInt32(state, 1, out var categoryId))
            {
                return luaL_error(
                    state,
                    "Usage: local itemIDs = C_AccountStore.GetCategoryItems(categoryID)");
            }

            PushIntegerTable(
                state,
                provider?.Items
                    .Where(value => value.CategoryId == categoryId)
                    .Where(value =>
                        !accountStore.Items.TryGetValue(value.Id, out var itemState) ||
                        itemState.Mode != WowAccountStoreItemMode.Hidden)
                    .OrderBy(value => value.OrderIndex)
                    .ThenBy(value => value.Id)
                    .Select(value => value.Id) ?? []);
            return 1;
        }
        if (operation == "GetCategoryInfo")
        {
            if (!TryReadRequiredInt32(state, 1, out var categoryId))
            {
                return luaL_error(
                    state,
                    "Usage: local info = C_AccountStore.GetCategoryInfo(categoryID)");
            }

            var category = provider?.Categories.FirstOrDefault(value => value.Id == categoryId);
            lua_newtable(state);
            SetInteger(state, "id", categoryId);
            SetString(state, "name", category?.Name ?? string.Empty);
            SetInteger(state, "type", category?.Type ?? 2);
            SetInteger(state, "icon", category?.IconFileDataId ?? 0);
            return 1;
        }
        if (operation == "GetItemInfo")
        {
            if (!TryReadRequiredInt32(state, 1, out var itemId))
            {
                return luaL_error(
                    state,
                    "Usage: local info = C_AccountStore.GetItemInfo(itemID)");
            }

            var item = provider?.Items.FirstOrDefault(value => value.Id == itemId);
            if (item is null)
            {
                lua_pushnil(state);
                return 1;
            }

            var itemState = accountStore.Items.TryGetValue(item.Id, out var configuredItemState)
                ? configuredItemState
                : new WowAccountStoreItemState();
            lua_newtable(state);
            SetInteger(state, "id", item.Id);
            SetInteger(state, "status", (int)itemState.Status);
            SetInteger(state, "mode", (int)itemState.Mode);
            SetInteger(state, "currencyID", item.CurrencyId);
            SetInteger(state, "flags", item.Flags);
            SetOptionalNonZeroInteger(state, "customUIModelSceneID", item.UiModelSceneId);
            SetString(state, "name", item.Name);
            SetString(state, "description", item.Description);
            SetInteger(state, "price", item.Price);
            SetBoolean(state, "nonrefundable", item.Nonrefundable);
            SetOptionalNonZeroInteger(state, "creatureDisplayID", item.CreatureDisplayInfoId);
            SetOptionalNonZeroInteger(state, "transmogSetID", item.TransmogSetId);
            SetOptionalNonZeroInteger(state, "displayIcon", item.IconFileDataId);
            SetOptionalNumber(
                state,
                "refundSecondsRemaining",
                itemState.RefundSecondsRemaining);
            return 1;
        }
        if (operation == "GetCurrencyIDForStore")
        {
            if (!TryReadRequiredInt32(state, 1, out var storeFrontId))
            {
                return luaL_error(
                    state,
                    "Usage: local currencyID = C_AccountStore.GetCurrencyIDForStore(storeFrontID)");
            }

            int? currencyId = accountStore.CurrencyIdsByStoreFront.TryGetValue(
                storeFrontId,
                out var configuredCurrencyId)
                    ? configuredCurrencyId
                    : provider?.Items.FirstOrDefault(value =>
                        value.StoreFrontId == storeFrontId &&
                        value.CurrencyId != 0)?.CurrencyId;
            if (currencyId is { } value)
                lua_pushinteger(state, value);
            else
                lua_pushnil(state);
            return 1;
        }
        if (operation == "GetCurrencyAvailable")
        {
            if (!TryReadRequiredInt32(state, 1, out var currencyId))
            {
                return luaL_error(
                    state,
                    "Usage: local amount = C_AccountStore.GetCurrencyAvailable(currencyID)");
            }

            lua_pushinteger(
                state,
                GetOrCreateCurrency(accountStore, currencyId).Amount);
            return 1;
        }
        if (operation == "GetCurrencyInfo")
        {
            if (!TryReadRequiredInt32(state, 1, out var currencyId))
            {
                return luaL_error(
                    state,
                    "Usage: local info = C_AccountStore.GetCurrencyInfo(currencyID)");
            }

            var currency = GetOrCreateCurrency(accountStore, currencyId);
            lua_newtable(state);
            SetInteger(state, "id", currencyId);
            SetInteger(state, "amount", currency.Amount);
            SetOptionalInteger(state, "maxQuantity", currency.MaximumQuantity);
            SetString(state, "name", currency.Name);
            SetInteger(state, "icon", currency.IconFileDataId);
            return 1;
        }
        if (operation == "GetStoreFrontState")
        {
            if (!TryReadRequiredInt32(state, 1, out var storeFrontId))
            {
                return luaL_error(
                    state,
                    "Usage: local state = C_AccountStore.GetStoreFrontState(storeFrontID)");
            }

            lua_pushinteger(
                state,
                (int)(accountStore.StoreFrontStates.TryGetValue(
                    storeFrontId,
                    out var storeFrontState)
                        ? storeFrontState
                        : WowAccountStoreFrontState.Unknown));
            return 1;
        }
        if (operation == "BeginPurchase")
        {
            if (!TryReadRequiredInt32(state, 1, out var itemId))
            {
                return luaL_error(
                    state,
                    "Usage: local purchaseStarted = C_AccountStore.BeginPurchase(itemID)");
            }

            var item = provider?.Items.FirstOrDefault(value => value.Id == itemId);
            var itemState = accountStore.Items.TryGetValue(itemId, out var configuredItemState)
                ? configuredItemState
                : new WowAccountStoreItemState();
            var currencyAmount = item is not null
                ? GetOrCreateCurrency(accountStore, item.CurrencyId).Amount
                : 0;
            var purchaseStarted = item is not null &&
                                  itemState.Status == WowAccountStoreItemStatus.Unowned &&
                                  currencyAmount >= item.Price;
            if (purchaseStarted)
                accountStore.PendingPurchaseItemId = itemId;
            lua_pushboolean(state, purchaseStarted ? 1 : 0);
            return 1;
        }
        if (operation == "RefundItem")
        {
            if (!TryReadRequiredInt32(state, 1, out var itemId))
            {
                return luaL_error(
                    state,
                    "Usage: local refundStarted = C_AccountStore.RefundItem(itemID)");
            }

            var itemExists = provider?.Items.Any(value => value.Id == itemId) == true;
            var refundStarted = itemExists &&
                                accountStore.Items.TryGetValue(itemId, out var itemState) &&
                                itemState.Status == WowAccountStoreItemStatus.Refundable;
            if (refundStarted)
                accountStore.PendingRefundItemId = itemId;
            lua_pushboolean(state, refundStarted ? 1 : 0);
            return 1;
        }
        if (operation == "RequestStoreFrontInfoUpdate")
        {
            if (!TryReadRequiredInt32(state, 1, out var storeFrontId))
            {
                return luaL_error(
                    state,
                    "Usage: C_AccountStore.RequestStoreFrontInfoUpdate(storeFrontID)");
            }

            accountStore.RequestedStoreFrontId = storeFrontId;
            return 0;
        }

        return 0;
    }

    private static WowAccountStoreCurrencyState GetOrCreateCurrency(
        WowAccountStoreState accountStore,
        int currencyId)
    {
        if (accountStore.Currencies.TryGetValue(currencyId, out var currency))
            return currency;
        currency = new WowAccountStoreCurrencyState();
        accountStore.Currencies[currencyId] = currency;
        return currency;
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

    private static void PushIntegerTable(lua_State state, IEnumerable<int> values)
    {
        lua_newtable(state);
        var index = 1;
        foreach (var value in values)
        {
            lua_pushinteger(state, value);
            lua_rawseti(state, -2, index++);
        }
    }

    private static void SetInteger(lua_State state, string name, long value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalInteger(lua_State state, string name, long? value)
    {
        if (value is { } integer)
            lua_pushinteger(state, integer);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalNonZeroInteger(
        lua_State state,
        string name,
        long value)
    {
        SetOptionalInteger(state, name, value == 0 ? null : value);
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
}
