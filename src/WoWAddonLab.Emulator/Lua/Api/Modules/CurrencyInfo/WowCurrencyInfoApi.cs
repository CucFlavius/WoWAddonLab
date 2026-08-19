using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCurrencyInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CanTransferCurrency", "DoesCurrentFilterRequireAccountCurrencyData",
        "DoesWarModeBonusApply", "ExpandCurrencyList",
        "FetchCurrencyDataFromAccountCharacters",
        "FetchCurrencyTransferTransactions", "GetAzeriteCurrencyID",
        "GetBackpackCurrencyInfo", "GetBasicCurrencyInfo", "GetCoinIcon",
        "GetCoinText", "GetCoinTextureString", "GetCostToTransferCurrency",
        "GetCurrencyContainerInfo", "GetCurrencyDescription",
        "GetCurrencyFilter", "GetCurrencyIDFromLink", "GetCurrencyInfo",
        "GetCurrencyInfoFromLink", "GetCurrencyLink", "GetCurrencyListInfo",
        "GetCurrencyListLink", "GetCurrencyListSize",
        "GetDragonIslesSuppliesCurrencyID", "GetFactionGrantedByCurrency",
        "GetMaxTransferableAmountFromQuantity",
        "GetPlayerCurrencyCategoryInfo", "GetWarResourcesCurrencyID",
        "IsAccountCharacterCurrencyDataReady",
        "IsAccountTransferableCurrency", "IsAccountWideCurrency",
        "IsCurrencyContainer", "IsCurrencyTransferInProgress",
        "IsCurrencyTransferTransactionDataReady", "PickupCurrency",
        "PlayerHasMaxQuantity", "PlayerHasMaxWeeklyQuantity",
        "RequestCurrencyDataForAccountCharacters",
        "RequestCurrencyFromAccountCharacter", "SetCurrencyBackpack",
        "SetCurrencyBackpackByID", "SetCurrencyFilter", "SetCurrencyUnused"
    ];

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
        lua_setglobal(state, "C_CurrencyInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var currency = LuaBindings.GetRuntime(state).CurrencyInfo;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "CanTransferCurrency":
            {
                var id = RequiredInt32(state, 1, Usage(operation));
                var eligibility = ResolveEligibility(currency, id);
                lua_pushboolean(state, eligibility.CanTransfer ? 1 : 0);
                if (eligibility.FailureReason is { } reason)
                    lua_pushinteger(state, (int)reason);
                else
                    lua_pushnil(state);
                return 2;
            }
            case "DoesCurrentFilterRequireAccountCurrencyData":
                return PushBoolean(
                    state,
                    currency.Filter ==
                    WowCurrencyFilterType.DiscoveredAndAllAccountTransferable);
            case "DoesWarModeBonusApply":
            {
                var definition = currency.Find(
                    RequiredInt32(state, 1, Usage(operation)));
                PushOptionalBoolean(state, definition?.WarModeBonusApplies);
                PushOptionalBoolean(
                    state,
                    definition?.LimitWarModeBonusOncePerTooltip);
                return 2;
            }
            case "ExpandCurrencyList":
            {
                var index = RequiredOneBasedIndex(state, 1, Usage(operation));
                var expanded = RequiredBoolean(state, 2, Usage(operation));
                if (TryGetListDefinition(currency, index, out var definition) &&
                    definition.IsHeader)
                {
                    definition.IsHeaderExpanded = expanded;
                }
                return 0;
            }
            case "FetchCurrencyDataFromAccountCharacters":
            {
                var id = RequiredInt32(state, 1, Usage(operation));
                if (!currency.AccountCharacterData.TryGetValue(id, out var rows))
                    return 0;
                lua_createtable(state, rows.Count, 0);
                for (var index = 0; index < rows.Count; index++)
                {
                    PushCharacterCurrencyData(state, rows[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "FetchCurrencyTransferTransactions":
                lua_createtable(
                    state,
                    currency.TransferTransactions.Count,
                    0);
                for (var index = 0;
                     index < currency.TransferTransactions.Count;
                     index++)
                {
                    PushTransferTransaction(
                        state,
                        currency.TransferTransactions[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "GetAzeriteCurrencyID":
                lua_pushinteger(state, 1553);
                return 1;
            case "GetBackpackCurrencyInfo":
            {
                var index = RequiredOneBasedIndex(state, 1, Usage(operation));
                var rows = currency.CurrencyList
                    .Select(currency.Find)
                    .Where(static row =>
                        row is { IsHeader: false, IsShowInBackpack: true })
                    .ToArray();
                if ((uint)index >= (uint)rows.Length)
                    return 0;
                var row = rows[index]!;
                PushBackpackInfo(
                    state,
                    new WowBackpackCurrencyInfo(
                        row.Name ?? string.Empty,
                        row.Quantity,
                        row.IconFileId,
                        row.CurrencyId));
                return 1;
            }
            case "GetBasicCurrencyInfo":
            {
                var id = RequiredInt32(state, 1, Usage(operation));
                var quantity = OptionalInt32(state, 2, null, Usage(operation));
                if (!TryGetBasicInfo(currency, id, quantity, out var info))
                    return 0;
                PushBasicInfo(state, info);
                return 1;
            }
            case "GetCoinIcon":
                lua_pushinteger(state, GetCoinIcon(RequiredUInt64(state, 1, Usage(operation))));
                return 1;
            case "GetCoinText":
            {
                var amount = RequiredUInt64(state, 1, Usage(operation));
                var separator = OptionalString(state, 2, ", ", Usage(operation));
                lua_pushstring(state, FormatCoinText(amount, separator));
                return 1;
            }
            case "GetCoinTextureString":
            {
                var amount = RequiredUInt64(state, 1, Usage(operation));
                var height = OptionalInt32(state, 2, 14, Usage(operation))!.Value;
                lua_pushstring(state, FormatCoinTextureString(amount, height));
                return 1;
            }
            case "GetCostToTransferCurrency":
            {
                var definition = currency.Find(
                    RequiredInt32(state, 1, Usage(operation)));
                var quantity = RequiredUInt32(state, 2, Usage(operation));
                if (definition?.TransferPercentage is not > 0)
                    return PushNil(state);
                lua_pushinteger(
                    state,
                    (int)MathF.Ceiling(
                        100f / definition.TransferPercentage.Value * quantity));
                return 1;
            }
            case "GetCurrencyContainerInfo":
            {
                var id = RequiredInt32(state, 1, Usage(operation));
                var quantity = RequiredInt32(state, 2, Usage(operation));
                if (!currency.ContainerInfo.TryGetValue(
                        (id, quantity),
                        out var info))
                {
                    return 0;
                }
                PushBasicInfo(state, info);
                return 1;
            }
            case "GetCurrencyDescription":
            {
                var definition = currency.Find(
                    RequiredInt32(state, 1, Usage(operation)));
                return PushOptionalString(state, definition?.Description);
            }
            case "GetCurrencyFilter":
                lua_pushinteger(state, (int)currency.Filter);
                return 1;
            case "GetCurrencyIDFromLink":
                lua_pushinteger(
                    state,
                    ParseCurrencyLinkId(
                        RequiredString(state, 1, Usage(operation))));
                return 1;
            case "GetCurrencyInfo":
            {
                var definition = currency.Find(
                    RequiredInt32(state, 1, Usage(operation)));
                if (definition is null)
                    return 0;
                PushCurrencyInfo(state, definition);
                return 1;
            }
            case "GetCurrencyInfoFromLink":
            {
                var id = ParseCurrencyLinkId(
                    RequiredString(state, 1, Usage(operation)));
                var definition = currency.Find(id);
                if (definition is null)
                    return 0;
                PushCurrencyInfo(state, definition);
                return 1;
            }
            case "GetCurrencyLink":
            {
                var id = RequiredInt32(state, 1, Usage(operation));
                var amount = OptionalInt32(state, 2, 0, Usage(operation))!.Value;
                return PushOptionalString(
                    state,
                    BuildCurrencyLink(currency.Find(id), amount));
            }
            case "GetCurrencyListInfo":
            {
                var index = RequiredOneBasedIndex(state, 1, Usage(operation));
                if (!TryGetListDefinition(currency, index, out var definition))
                    return 0;
                PushCurrencyInfo(state, definition);
                return 1;
            }
            case "GetCurrencyListLink":
            {
                var index = RequiredOneBasedIndex(state, 1, Usage(operation));
                return PushOptionalString(
                    state,
                    TryGetListDefinition(currency, index, out var definition)
                        ? BuildCurrencyLink(definition, 0)
                        : null);
            }
            case "GetCurrencyListSize":
                lua_pushinteger(state, currency.CurrencyList.Count);
                return 1;
            case "GetDragonIslesSuppliesCurrencyID":
                lua_pushinteger(state, 2003);
                return 1;
            case "GetFactionGrantedByCurrency":
            {
                var faction = currency.Find(
                    RequiredInt32(state, 1, Usage(operation)))?.FactionId;
                if (faction is { } id)
                    lua_pushinteger(state, id);
                else
                    lua_pushnil(state);
                return 1;
            }
            case "GetMaxTransferableAmountFromQuantity":
            {
                if (lua_isnoneornil(state, 1) != 0 || lua_isnoneornil(state, 2) != 0)
                    return PushNil(state);
                var definition = currency.Find(
                    RequiredInt32(state, 1, Usage(operation)));
                var quantity = RequiredUInt32(state, 2, Usage(operation));
                if (definition?.TransferPercentage is not > 0)
                    return PushNil(state);
                lua_pushinteger(
                    state,
                    (int)MathF.Floor(
                        definition.TransferPercentage.Value * 0.01f * quantity));
                return 1;
            }
            case "GetPlayerCurrencyCategoryInfo":
            {
                var id = RequiredInt32(state, 1, Usage(operation));
                RequiredBooleanOrDefault(state, 2, false, Usage(operation));
                currency.Categories.TryGetValue(id, out var category);
                PushCategoryInfo(
                    state,
                    category ??
                    new WowPlayerCurrencyCategoryInfo(null, [], []));
                return 1;
            }
            case "GetWarResourcesCurrencyID":
                lua_pushinteger(state, 1560);
                return 1;
            case "IsAccountCharacterCurrencyDataReady":
                return PushBoolean(
                    state,
                    currency.AccountCharacterCurrencyDataReady);
            case "IsAccountTransferableCurrency":
                return PushBoolean(
                    state,
                    currency.Find(
                        RequiredInt32(state, 1, Usage(operation)))?
                        .IsAccountTransferable == true);
            case "IsAccountWideCurrency":
                return PushBoolean(
                    state,
                    currency.Find(
                        RequiredInt32(state, 1, Usage(operation)))?
                        .IsAccountWide == true);
            case "IsCurrencyContainer":
            {
                var id = RequiredInt32(state, 1, Usage(operation));
                var quantity = RequiredInt32(state, 2, Usage(operation));
                return PushBoolean(
                    state,
                    currency.CurrencyContainers.Contains((id, quantity)) ||
                    currency.ContainerInfo.ContainsKey((id, quantity)));
            }
            case "IsCurrencyTransferInProgress":
                return PushBoolean(state, currency.CurrencyTransferInProgress);
            case "IsCurrencyTransferTransactionDataReady":
                return PushBoolean(
                    state,
                    currency.CurrencyTransferTransactionDataReady);
            case "PickupCurrency":
                currency.LastPickedUpCurrencyId =
                    RequiredInt32(state, 1, Usage(operation));
                return 0;
            case "PlayerHasMaxQuantity":
            {
                var definition = currency.Find(
                    RequiredInt32(state, 1, Usage(operation)));
                var amount = definition?.UseTotalEarnedForMaxQuantity == true
                    ? definition.TotalEarned
                    : definition?.Quantity ?? 0;
                return PushBoolean(
                    state,
                    definition is { MaxQuantity: > 0 } &&
                    amount >= definition.MaxQuantity);
            }
            case "PlayerHasMaxWeeklyQuantity":
            {
                var definition = currency.Find(
                    RequiredInt32(state, 1, Usage(operation)));
                return PushBoolean(
                    state,
                    definition is { MaxWeeklyQuantity: > 0 } &&
                    definition.QuantityEarnedThisWeek >=
                    definition.MaxWeeklyQuantity);
            }
            case "RequestCurrencyDataForAccountCharacters":
                currency.AccountCharacterCurrencyDataReady = false;
                return 0;
            case "RequestCurrencyFromAccountCharacter":
            {
                var guid = RequiredString(state, 1, Usage(operation));
                var id = RequiredInt32(state, 2, Usage(operation));
                var quantity = RequiredUInt32(state, 3, Usage(operation));
                currency.LastCurrencyTransferRequest = (guid, id, quantity);
                currency.CurrencyTransferInProgress = true;
                return 0;
            }
            case "SetCurrencyBackpack":
            {
                var index = RequiredOneBasedIndex(state, 1, Usage(operation));
                var backpack = RequiredBoolean(state, 2, Usage(operation));
                if (TryGetListDefinition(currency, index, out var definition) &&
                    !definition.IsHeader)
                {
                    definition.IsShowInBackpack = backpack;
                }
                return 0;
            }
            case "SetCurrencyBackpackByID":
            {
                var definition = currency.Find(
                    RequiredInt32(state, 1, Usage(operation)));
                var backpack = RequiredBoolean(state, 2, Usage(operation));
                if (definition is not null)
                    definition.IsShowInBackpack = backpack;
                return 0;
            }
            case "SetCurrencyFilter":
                currency.Filter = RequiredFilterType(
                    state,
                    1,
                    Usage(operation));
                return 0;
            case "SetCurrencyUnused":
            {
                var index = RequiredOneBasedIndex(state, 1, Usage(operation));
                var unused = RequiredBoolean(state, 2, Usage(operation));
                if (TryGetListDefinition(currency, index, out var definition) &&
                    !definition.IsHeader)
                {
                    definition.IsTypeUnused = unused;
                }
                return 0;
            }
            default:
                return 0;
        }
    }

    private static WowCurrencyTransferEligibility ResolveEligibility(
        WowCurrencyInfoState state,
        int currencyId)
    {
        if (state.TransferEligibility.TryGetValue(
                currencyId,
                out var eligibility))
        {
            return eligibility;
        }

        if (state.CurrencyTransferInProgress)
        {
            return new WowCurrencyTransferEligibility(
                false,
                WowAccountCurrencyTransferResult.TransactionInProgress);
        }

        var definition = state.Find(currencyId);
        return definition?.IsAccountTransferable == true
            ? new WowCurrencyTransferEligibility(true, null)
            : new WowCurrencyTransferEligibility(
                false,
                WowAccountCurrencyTransferResult.InvalidCurrency);
    }

    private static bool TryGetListDefinition(
        WowCurrencyInfoState state,
        int index,
        out WowCurrencyDefinition definition)
    {
        if ((uint)index < (uint)state.CurrencyList.Count &&
            state.Find(state.CurrencyList[index]) is { } found)
        {
            definition = found;
            return true;
        }

        definition = null!;
        return false;
    }

    private static bool TryGetBasicInfo(
        WowCurrencyInfoState state,
        int currencyId,
        int? requestedQuantity,
        out WowBasicCurrencyInfo info)
    {
        if (state.BasicInfo.TryGetValue(currencyId, out info!))
        {
            if (requestedQuantity is { } quantity)
                info = info with { DisplayAmount = quantity, ActualAmount = quantity };
            return true;
        }

        if (state.Find(currencyId) is not { } definition)
        {
            info = null!;
            return false;
        }

        var amount = requestedQuantity ?? definition.Quantity;
        info = new WowBasicCurrencyInfo(
            definition.Name ?? string.Empty,
            definition.Description ?? string.Empty,
            definition.IconFileId,
            definition.Quality,
            amount,
            amount);
        return true;
    }

    private static void PushCurrencyInfo(
        lua_State state,
        WowCurrencyDefinition definition)
    {
        lua_createtable(state, 0, 25);
        SetOptionalString(state, "name", definition.Name);
        SetOptionalString(state, "description", definition.Description);
        SetInteger(state, "currencyID", definition.CurrencyId);
        SetBoolean(state, "isHeader", definition.IsHeader);
        SetBoolean(state, "isHeaderExpanded", definition.IsHeaderExpanded);
        SetInteger(state, "currencyListDepth", definition.CurrencyListDepth);
        SetBoolean(state, "isTypeUnused", definition.IsTypeUnused);
        SetBoolean(state, "isShowInBackpack", definition.IsShowInBackpack);
        SetInteger(state, "quantity", definition.Quantity);
        SetInteger(state, "trackedQuantity", definition.TrackedQuantity);
        SetInteger(state, "iconFileID", definition.IconFileId);
        SetInteger(state, "maxQuantity", definition.MaxQuantity);
        SetBoolean(state, "canEarnPerWeek", definition.CanEarnPerWeek);
        SetInteger(
            state,
            "quantityEarnedThisWeek",
            definition.QuantityEarnedThisWeek);
        SetBoolean(state, "isTradeable", definition.IsTradeable);
        SetInteger(state, "quality", definition.Quality);
        SetInteger(
            state,
            "maxWeeklyQuantity",
            definition.MaxWeeklyQuantity);
        SetInteger(state, "totalEarned", definition.TotalEarned);
        SetBoolean(state, "discovered", definition.Discovered);
        SetBoolean(
            state,
            "useTotalEarnedForMaxQty",
            definition.UseTotalEarnedForMaxQuantity);
        SetBoolean(state, "isAccountWide", definition.IsAccountWide);
        SetBoolean(
            state,
            "isAccountTransferable",
            definition.IsAccountTransferable);
        SetOptionalNumber(
            state,
            "transferPercentage",
            definition.TransferPercentage);
        SetInteger(
            state,
            "rechargingCycleDurationMS",
            definition.RechargingCycleDurationMilliseconds);
        SetInteger(
            state,
            "rechargingAmountPerCycle",
            definition.RechargingAmountPerCycle);
    }

    private static void PushBasicInfo(
        lua_State state,
        WowBasicCurrencyInfo info)
    {
        lua_createtable(state, 0, 6);
        SetString(state, "name", info.Name);
        SetString(state, "description", info.Description);
        SetInteger(state, "icon", info.Icon);
        SetInteger(state, "quality", info.Quality);
        SetInteger(state, "displayAmount", info.DisplayAmount);
        SetInteger(state, "actualAmount", info.ActualAmount);
    }

    private static void PushBackpackInfo(
        lua_State state,
        WowBackpackCurrencyInfo info)
    {
        lua_createtable(state, 0, 4);
        SetString(state, "name", info.Name);
        SetInteger(state, "quantity", info.Quantity);
        SetInteger(state, "iconFileID", info.IconFileId);
        SetInteger(state, "currencyTypesID", info.CurrencyTypesId);
    }

    private static void PushCategoryInfo(
        lua_State state,
        WowPlayerCurrencyCategoryInfo info)
    {
        lua_createtable(state, 0, 3);
        SetOptionalString(state, "categoryName", info.CategoryName);
        PushIntegerArray(state, info.CurrencyTypes);
        lua_setfield(state, -2, "currencyTypes");
        PushIntegerArray(state, info.ChildCategories);
        lua_setfield(state, -2, "childCategories");
    }

    private static void PushCharacterCurrencyData(
        lua_State state,
        WowCharacterCurrencyData info)
    {
        lua_createtable(state, 0, 5);
        SetString(state, "characterGUID", info.CharacterGuid);
        SetString(state, "characterName", info.CharacterName);
        SetString(state, "fullCharacterName", info.FullCharacterName);
        SetInteger(state, "currencyID", info.CurrencyId);
        SetInteger(state, "quantity", info.Quantity);
    }

    private static void PushTransferTransaction(
        lua_State state,
        WowCurrencyTransferTransaction info)
    {
        lua_createtable(state, 0, 10);
        SetString(state, "sourceCharacterGUID", info.SourceCharacterGuid);
        SetString(state, "sourceCharacterName", info.SourceCharacterName);
        SetString(
            state,
            "fullSourceCharacterName",
            info.FullSourceCharacterName);
        SetString(
            state,
            "destinationCharacterGUID",
            info.DestinationCharacterGuid);
        SetString(
            state,
            "destinationCharacterName",
            info.DestinationCharacterName);
        SetString(
            state,
            "fullDestinationCharacterName",
            info.FullDestinationCharacterName);
        SetInteger(state, "currencyType", info.CurrencyType);
        SetInteger(
            state,
            "quantityTransferred",
            info.QuantityTransferred);
        SetInteger(
            state,
            "totalQuantityConsumed",
            info.TotalQuantityConsumed);
        SetNumber(state, "timestamp", info.Timestamp);
    }

    private static int GetCoinIcon(ulong amount) =>
        amount switch
        {
            < 10 => 133788,
            < 100 => 133789,
            < 1_000 => 133786,
            < 10_000 => 133787,
            < 100_000 => 133784,
            _ => 133785
        };

    private static string FormatCoinText(ulong amount, string separator)
    {
        var gold = amount / 10_000;
        var silver = amount % 10_000 / 100;
        var copper = amount % 100;
        var parts = new List<string>(3);
        if (gold != 0)
            parts.Add($"{gold} Gold");
        if (silver != 0)
            parts.Add($"{silver} Silver");
        if (copper != 0)
            parts.Add($"{copper} Copper");
        return string.Join(separator, parts);
    }

    private static string FormatCoinTextureString(ulong amount, int height)
    {
        var gold = amount / 10_000;
        var silver = amount % 10_000 / 100;
        var copper = amount % 100;
        var parts = new List<string>(3);
        if (gold != 0)
            parts.Add(CoinTexture(gold, "UI-GoldIcon", height));
        if (silver != 0)
            parts.Add(CoinTexture(silver, "UI-SilverIcon", height));
        if (copper != 0 || parts.Count == 0)
            parts.Add(CoinTexture(copper, "UI-CopperIcon", height));
        return string.Join(" ", parts);
    }

    private static string CoinTexture(
        ulong amount,
        string texture,
        int height) =>
        $"{amount}|TInterface\\MoneyFrame\\{texture}:{height}:{height}:2:0|t";

    private static string? BuildCurrencyLink(
        WowCurrencyDefinition? definition,
        int amount)
    {
        if (definition is null)
            return null;
        return $"|cnIQ{definition.Quality}:|Hcurrency:{definition.CurrencyId}:{amount}|h" +
               $"[{definition.Name ?? string.Empty}]|h|r";
    }

    private static int ParseCurrencyLinkId(string link)
    {
        var marker = link.IndexOf("currency:", StringComparison.Ordinal);
        if (marker < 0)
            return 0;
        var value = link.AsSpan(marker + "currency:".Length);
        var end = value.IndexOf(':');
        if (end >= 0)
            value = value[..end];
        var styles = NumberStyles.AllowLeadingSign;
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return uint.TryParse(
                value[2..],
                NumberStyles.HexNumber,
                CultureInfo.InvariantCulture,
                out var hexadecimal)
                ? unchecked((int)hexadecimal)
                : 0;
        }
        return int.TryParse(
            value,
            styles,
            CultureInfo.InvariantCulture,
            out var decimalValue)
            ? decimalValue
            : 0;
    }

    private static void RegisterEnumsAndConstants(lua_State state)
    {
        EnsureGlobalTable(state, "Enum");
        SetEnum(
            state,
            "AccountCurrencyTransferResult",
            [
                ("Success", 0), ("InvalidCharacter", 1),
                ("CharacterLoggedIn", 2), ("InsufficientCurrency", 3),
                ("MaxQuantity", 4), ("InvalidCurrency", 5),
                ("NoValidSourceCharacter", 6), ("ServerError", 7),
                ("CannotUseCurrency", 8), ("TransactionInProgress", 9),
                ("CurrencyTransferDisabled", 10)
            ]);
        SetEnumMeta(state, "AccountCurrencyTransferResultMeta", 11, 0, 10);
        SetEnum(
            state,
            "CurrencyFlags",
            [
                ("CurrencyTradable", 1),
                ("CurrencyAppearsInLootWindow", 2),
                ("CurrencyComputedWeeklyMaximum", 4),
                ("Currency_100_Scaler", 8),
                ("CurrencyNoLowLevelDrop", 16),
                ("CurrencyIgnoreMaxQtyOnLoad", 32),
                ("CurrencyLogOnWorldChange", 64),
                ("CurrencyTrackQuantity", 128),
                ("CurrencyResetTrackedQuantity", 256),
                ("CurrencyUpdateVersionIgnoreMax", 512),
                ("CurrencySuppressChatMessageOnVersionChange", 1024),
                ("CurrencySingleDropInLoot", 2048),
                ("CurrencyHasWeeklyCatchup", 4096),
                ("CurrencyDoNotCompressChat", 0x2000),
                ("CurrencyDoNotLogAcquisitionToBi", 0x4000),
                ("CurrencyNoRaidDrop", 0x8000),
                ("CurrencyNotPersistent", 0x10000),
                ("CurrencyDeprecated", 0x20000),
                ("CurrencyDynamicMaximum", 0x40000),
                ("CurrencySuppressChatMessages", 0x80000),
                ("CurrencyDoNotToast", 0x100000),
                ("CurrencyDestroyExtraOnLoot", 0x200000),
                ("CurrencyDontShowTotalInTooltip", 0x400000),
                ("CurrencyDontCoalesceInLootWindow", 0x800000),
                ("CurrencyAccountWide", 0x1000000),
                ("CurrencyAllowOverflowMailer", 0x2000000),
                ("CurrencyHideAsReward", 0x4000000),
                ("CurrencyHasWarmodeBonus", 0x8000000),
                ("CurrencyIsAllianceOnly", 0x10000000),
                ("CurrencyIsHordeOnly", 0x20000000),
                ("CurrencyLimitWarmodeBonusOncePerTooltip", 0x40000000),
                ("CurrencyUsesLedgerBalance", unchecked((int)0x80000000))
            ]);
        SetEnumMeta(
            state,
            "CurrencyFlagsMeta",
            32,
            1,
            unchecked((int)0x80000000));
        SetEnum(
            state,
            "CurrencyFlagsB",
            [
                ("CurrencyBUseTotalEarnedForEarned", 1),
                ("CurrencyBShowQuestXPGainInTooltip", 2),
                ("CurrencyBNoNotificationMailOnOfflineProgress", 4),
                ("CurrencyBBattlenetVirtualCurrency", 8),
                ("FutureCurrencyFlag", 16),
                ("CurrencyBDontDisplayIfZero", 32),
                ("CurrencyBScaleMaxQuantityBySeasonWeeks", 64),
                ("CurrencyBScaleMaxQuantityByWeeksSinceStart", 128),
                ("CurrencyBForceMaxQuantityOnConversion", 256),
                ("CurrencyBUnearnableBeforeMaxQuantityStart", 512),
                ("CurrencyBAllowReductionByResourcefulness", 1024),
                ("CurrencyBNoBonusXP", 2048)
            ]);
        SetEnumMeta(state, "CurrencyFlagsBMeta", 12, 1, 2048);
        SetEnum(
            state,
            "CurrencyTokenCategoryFlags",
            [
                ("FlagSortLast", 1),
                ("FlagPlayerItemAssignment", 2),
                ("Hidden", 4),
                ("Virtual", 8),
                ("StartsCollapsed", 16)
            ]);
        SetEnumMeta(state, "CurrencyTokenCategoryFlagsMeta", 5, 1, 16);
        SetEnum(
            state,
            "CurrencyFilterType",
            [
                ("None", 0), ("DiscoveredOnly", 1),
                ("DiscoveredAndAllAccountTransferable", 2)
            ]);
        SetEnumMeta(state, "CurrencyFilterTypeMeta", 3, 0, 2);
        SetEnum(
            state,
            "CurrencyConversionResult",
            [
                ("NoConversion", 0), ("Conversion", 1),
                ("SkippedAccountCurrency", 2)
            ]);
        SetEnumMeta(state, "CurrencyConversionResultMeta", 3, 0, 2);
        SetEnum(
            state,
            "CurrencyGainFlags",
            [
                ("None", 0), ("BonusAward", 1), ("DroppedFromDeath", 2),
                ("FromAccountServer", 4), ("Autotracking", 8)
            ]);
        SetEnumMeta(state, "CurrencyGainFlagsMeta", 5, 0, 8);
        SetEnum(
            state,
            "PlayerCurrencyFlagsDbFlags",
            [
                ("IgnoreMaxQtyOnload", 1), ("Reuse1", 2),
                ("InBackpack", 4), ("UnusedInUI", 8), ("Reuse2", 16)
            ]);
        SetEnumMeta(state, "PlayerCurrencyFlagsDbFlagsMeta", 5, 1, 16);
        SetEnum(
            state,
            "LinkedCurrencyFlags",
            [
                ("IgnoreAdd", 1), ("IgnoreSubtract", 2),
                ("SuppressChatLog", 4), ("AddIgnoresMax", 8)
            ]);
        SetEnumMeta(state, "LinkedCurrencyFlagsMeta", 4, 1, 8);
        SetEnum(
            state,
            "PlayerCurrencyFlags",
            [("Incremented", 1), ("Loading", 2)]);
        SetEnumMeta(state, "PlayerCurrencyFlagsMeta", 2, 1, 2);
        lua_pop(state, 1);

        EnsureGlobalTable(state, "Constants");
        RegisterConstants(state);
        lua_pop(state, 1);
    }

    internal static void RegisterConstants(lua_State state)
    {
        lua_createtable(state, 0, 28);
        SetInteger(state, "PLAYER_CURRENCY_CLIENT_FLAGS", 12);
        SetInteger(state, "MAX_CURRENCY_QUANTITY", 100_000_000);
        SetInteger(state, "CONQUEST_ARENA_AND_BG_META_CURRENCY_ID", 483);
        SetInteger(state, "CONQUEST_RATED_BG_META_CURRENCY_ID", 484);
        SetInteger(state, "CONQUEST_ASHRAN_META_CURRENCY_ID", 692);
        SetInteger(state, "ACCOUNT_WIDE_HONOR_CURRENCY_ID", 1585);
        SetInteger(state, "ACCOUNT_WIDE_HONOR_LEVEL_CURRENCY_ID", 1586);
        SetInteger(state, "CONQUEST_CURRENCY_ID", 1602);
        SetInteger(state, "CONQUEST_POINTS_CURRENCY_ID", 390);
        SetInteger(state, "CONQUEST_ARENA_META_CURRENCY_ID", 483);
        SetInteger(state, "CONQUEST_BG_META_CURRENCY_ID", 484);
        SetInteger(state, "HONOR_CURRENCY_ID", 1792);
        SetInteger(state, "CLASSIC_ARENA_POINTS_CURRENCY_ID", 1900);
        SetInteger(state, "CLASSIC_HONOR_CURRENCY_ID", 1901);
        SetInteger(state, "CLASSIC_CONQUEST_CURRENCY_ID", 390);
        SetInteger(state, "HONOR_PER_CURRENCY", 10);
        SetInteger(state, "ARTIFACT_KNOWLEDGE_CURRENCY_ID", 1171);
        SetInteger(state, "WAR_RESOURCES_CURRENCY_ID", 1560);
        SetInteger(state, "ECHOES_OF_NYALOTHA_CURRENCY_ID", 1803);
        SetInteger(state, "DRAGON_ISLES_SUPPLIES_CURRENCY_ID", 2003);
        SetInteger(state, "QUESTIONMARK_INV_ICON", 134400);
        SetInteger(
            state,
            "PVP_CURRENCY_CONQUEST_ALLIANCE_INV_ICON",
            463448);
        SetInteger(state, "PVP_CURRENCY_CONQUEST_HORDE_INV_ICON", 463449);
        SetInteger(state, "PVP_CURRENCY_HONOR_ALLIANCE_INV_ICON", 463450);
        SetInteger(state, "PVP_CURRENCY_HONOR_HORDE_INV_ICON", 463451);
        SetInteger(state, "CURRENCY_ID_RENOWN", 1822);
        SetInteger(state, "CURRENCY_ID_RENOWN_KYRIAN", 1829);
        SetInteger(state, "CURRENCY_ID_RENOWN_VENTHYR", 1830);
        SetInteger(state, "CURRENCY_ID_RENOWN_NIGHT_FAE", 1831);
        SetInteger(state, "CURRENCY_ID_RENOWN_NECROLORD", 1832);
        SetInteger(state, "CURRENCY_ID_WILLING_SOUL", 1810);
        SetInteger(state, "CURRENCY_ID_RESERVOIR_ANIMA", 1813);
        SetInteger(state, "CURRENCY_ID_PERKS_PROGRAM_DISPLAY_INFO", 2032);
        SetInteger(state, "CURRENCY_WALLET_TYPE_WOWMONEY", 0);
        lua_setfield(state, -2, "CurrencyConsts");
    }

    private static void EnsureGlobalTable(lua_State state, string name)
    {
        lua_getglobal(state, name);
        if (lua_istable(state, -1) != 0)
            return;
        lua_pop(state, 1);
        lua_newtable(state);
        lua_pushvalue(state, -1);
        lua_setglobal(state, name);
    }

    private static void SetEnum(
        lua_State state,
        string name,
        IReadOnlyList<(string Name, int Value)> members)
    {
        lua_createtable(state, 0, members.Count);
        foreach (var member in members)
            SetInteger(state, member.Name, member.Value);
        lua_setfield(state, -2, name);
    }

    private static void SetEnumMeta(
        lua_State state,
        string name,
        int count,
        int minimum,
        int maximum)
    {
        lua_createtable(state, 0, 3);
        SetInteger(state, "NumValues", count);
        SetInteger(state, "MinValue", minimum);
        SetInteger(state, "MaxValue", maximum);
        lua_setfield(state, -2, name);
    }

    private static string Usage(string operation) =>
        $"Usage: C_CurrencyInfo.{operation}(...)";

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
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (int)number;
    }

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number < 0 || number > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (uint)number;
    }

    private static ulong RequiredUInt64(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number < 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (ulong)number;
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        int? defaultValue,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return defaultValue;
        return RequiredInt32(state, index, usage);
    }

    private static int RequiredOneBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value < 1)
        {
            luaL_error(state, usage);
            return 0;
        }
        return value - 1;
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

    private static bool RequiredBooleanOrDefault(
        lua_State state,
        int index,
        bool defaultValue,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return defaultValue;
        return RequiredBoolean(state, index, usage);
    }

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is not (LUA_TSTRING or LUA_TNUMBER))
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static string OptionalString(
        lua_State state,
        int index,
        string defaultValue,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return defaultValue;
        return RequiredString(state, index, usage);
    }

    private static WowCurrencyFilterType RequiredFilterType(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value is < 0 or > 2)
        {
            luaL_error(state, usage);
            return WowCurrencyFilterType.None;
        }
        return (WowCurrencyFilterType)value;
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static int PushNil(lua_State state)
    {
        lua_pushnil(state);
        return 1;
    }

    private static int PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        return 1;
    }

    private static void PushOptionalBoolean(lua_State state, bool? value)
    {
        if (value is { } boolean)
            lua_pushboolean(state, boolean ? 1 : 0);
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
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void SetInteger(
        lua_State state,
        string field,
        int value)
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

    private static void SetOptionalNumber(
        lua_State state,
        string field,
        double? value)
    {
        if (value is { } number)
            lua_pushnumber(state, number);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }
}
