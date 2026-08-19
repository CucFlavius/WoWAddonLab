using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowStoreSecureApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AckFailure",
        "ClearPreGeneratedExternalTransactionID",
        "GetBnetTransferInfo",
        "GetCharacterInfoByGUID",
        "GetCharactersForRealm",
        "GetConfirmationInfo",
        "GetCurrencyID",
        "GetCurrencyInfo",
        "GetCurrencyRegion",
        "GetEligibleRacesForVASService",
        "GetEntryInfo",
        "GetFailureInfo",
        "GetLastProductListResponseError",
        "GetProductGroupInfo",
        "GetProductGroups",
        "GetProductInfo",
        "GetProductList",
        "GetProducts",
        "GetPurchaseList",
        "GetRealmList",
        "GetUnrevokedBoostInfo",
        "GetVASCompletionInfo",
        "GetVASErrors",
        "GetVASGuildFollowInfoForCharacterByGUID",
        "GetVASGuildMasterInfoForCharacterByGUID",
        "GetVASRealmList",
        "GetVasServiceType",
        "GetWoWAccountGUIDFromName",
        "HasDistributionList",
        "HasDynamicPriceData",
        "HasProductList",
        "HasProductType",
        "HasPurchaseInProgress",
        "HasPurchaseList",
        "IsAvailable",
        "IsDynamicBundle",
        "IsRegionLocked",
        "IsVASEligibleCharacterGUID",
        "OpenNydusLink",
        "PurchaseProduct",
        "PurchaseProductConfirm",
        "PurchaseVASProduct",
        "RequestAllDynamicPriceInfo",
        "RequestCharacterGuildFollowInfo",
        "RequestRealmGuildMasterInfo",
        "SetDisconnectOnLogout",
        "SetVASProductReady",
        "ValidateBnetTransfer"
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
        lua_setglobal(state, "C_StoreSecure");
    }

    private static int Dispatch(lua_State state)
    {
        var store = LuaBindings.GetRuntime(state).StoreSecure;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        return operation switch
        {
            "AckFailure" => PushBoolean(state, store.AckFailureResult),
            "ClearPreGeneratedExternalTransactionID" =>
                ClearPreGeneratedExternalTransactionId(store),
            "GetBnetTransferInfo" => GetBnetTransferInfo(state, store),
            "GetCharacterInfoByGUID" => GetCharacterInfoByGuid(state, store),
            "GetCharactersForRealm" => GetCharactersForRealm(state, store),
            "GetConfirmationInfo" => GetConfirmationInfo(state, store),
            "GetCurrencyID" => PushInteger(state, store.CurrencyId),
            "GetCurrencyInfo" => GetCurrencyInfo(state, store),
            "GetCurrencyRegion" => PushInteger(state, store.CurrencyRegion),
            "GetEligibleRacesForVASService" =>
                GetEligibleRacesForVasService(state, store),
            "GetEntryInfo" => GetEntryInfo(state, store),
            "GetFailureInfo" => GetFailureInfo(state, store),
            "GetLastProductListResponseError" =>
                PushInteger(state, store.LastProductListResponseError),
            "GetProductGroupInfo" => GetProductGroupInfo(state, store),
            "GetProductGroups" => GetProductGroups(state, store),
            "GetProductInfo" => GetProductInfo(state, store),
            "GetProductList" => Record(store, operation),
            "GetProducts" => GetProducts(state, store),
            "GetPurchaseList" => Record(store, operation),
            "GetRealmList" => PushRealms(state, store.Realms),
            "GetUnrevokedBoostInfo" => GetUnrevokedBoostInfo(state, store),
            "GetVASCompletionInfo" => GetVasCompletionInfo(state, store),
            "GetVASErrors" => PushIntegerArray(state, store.VasErrors),
            "GetVASGuildFollowInfoForCharacterByGUID" =>
                GetVasGuildFollowInfo(state, store),
            "GetVASGuildMasterInfoForCharacterByGUID" =>
                GetVasGuildMasterInfo(state, store),
            "GetVASRealmList" => PushRealms(state, store.VasRealms),
            "GetVasServiceType" => GetVasServiceType(state, store),
            "GetWoWAccountGUIDFromName" => GetWowAccountGuidFromName(state, store),
            "HasDistributionList" => PushBoolean(state, store.HasDistributionList),
            "HasDynamicPriceData" =>
                ContainsRequiredInt32(
                    state,
                    store.DynamicPriceProductIds,
                    "Usage: local dynamicPriceDataAvailable = C_StoreSecure.HasDynamicPriceData(productID)"),
            "HasProductList" => PushBoolean(state, store.HasProductList),
            "HasProductType" =>
                ContainsRequiredInt32(
                    state,
                    store.ProductTypeIds,
                    "Usage: local isInShop = C_StoreSecure.HasProductType(productTypeID)"),
            "HasPurchaseInProgress" =>
                PushBoolean(state, store.HasPurchaseInProgress),
            "HasPurchaseList" => PushBoolean(state, store.HasPurchaseList),
            "IsAvailable" => PushBoolean(state, store.IsAvailable),
            "IsDynamicBundle" =>
                ContainsRequiredInt32(
                    state,
                    store.DynamicBundleProductIds,
                    "Usage: local isDynamicBundle = C_StoreSecure.IsDynamicBundle(productID)"),
            "IsRegionLocked" => PushBoolean(state, store.IsRegionLocked),
            "IsVASEligibleCharacterGUID" =>
                IsVasEligibleCharacterGuid(state, store),
            "OpenNydusLink" => OpenNydusLink(state, store),
            "PurchaseProduct" => PurchaseProduct(state, store),
            "PurchaseProductConfirm" => PurchaseProductConfirm(state, store),
            "PurchaseVASProduct" => PurchaseVasProduct(state, store),
            "RequestAllDynamicPriceInfo" => Record(store, operation),
            "RequestCharacterGuildFollowInfo" =>
                RequestCharacterGuildFollowInfo(state, store),
            "RequestRealmGuildMasterInfo" =>
                RequestRealmGuildMasterInfo(state, store),
            "SetDisconnectOnLogout" =>
                SetDisconnectOnLogout(state, store),
            "SetVASProductReady" => SetVasProductReady(state, store),
            "ValidateBnetTransfer" => ValidateBnetTransfer(state, store),
            _ => 0
        };
    }

    private static int ClearPreGeneratedExternalTransactionId(
        WowStoreSecureState store)
    {
        store.PreGeneratedExternalTransactionId = null;
        return 0;
    }

    private static int GetBnetTransferInfo(
        lua_State state,
        WowStoreSecureState store)
    {
        PushGuid(state, store.BnetTransferGuid);
        PushStringArray(state, store.BnetTransferInfo);
        return 2;
    }

    private static int GetCharacterInfoByGuid(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local characterInfo = C_StoreSecure.GetCharacterInfoByGUID(guid)";
        var guid = RequiredGuid(state, 1, usage);
        if (store.CharactersByGuid.TryGetValue(guid, out var character))
            PushCharacterInfo(state, character);
        else
            lua_pushnil(state);
        return 1;
    }

    private static int GetCharactersForRealm(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local characters = C_StoreSecure.GetCharactersForRealm(virtualRealmAddress, guildMastersOnly)";
        var realm = RequiredInt32(state, 1, usage);
        var guildMastersOnly = RequiredBoolean(state, 2, usage);
        var characters = store.CharactersByGuid.Values
            .Where(character =>
                character.CurrentServer == realm &&
                (!guildMastersOnly ||
                 (character.Guid is not null &&
                  store.GuildMasterGuids.Contains(character.Guid))))
            .ToArray();
        lua_createtable(state, characters.Length, 0);
        for (var index = 0; index < characters.Length; index++)
        {
            PushCharacterInfo(state, characters[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetConfirmationInfo(
        lua_State state,
        WowStoreSecureState store)
    {
        if (store.ConfirmationInfo is not { } info)
            return 0;
        lua_pushinteger(state, info.ProductId);
        lua_pushstring(state, info.ConfirmationText);
        lua_pushnumber(state, info.CurrentDollars);
        lua_pushnumber(state, info.CurrentCents);
        lua_pushnumber(state, info.NormalDollars);
        lua_pushnumber(state, info.NormalCents);
        return 6;
    }

    private static int GetCurrencyInfo(
        lua_State state,
        WowStoreSecureState store)
    {
        if (store.CurrencyInfo is not { } info)
        {
            lua_pushnil(state);
            return 1;
        }
        lua_createtable(state, 0, 2);
        SetInteger(state, "currencyID", info.CurrencyId);
        PushCurrencySharedData(state, info.SharedData);
        lua_setfield(state, -2, "sharedData");
        return 1;
    }

    private static int GetEligibleRacesForVasService(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local races = C_StoreSecure.GetEligibleRacesForVASService(guid, serviceType)";
        var guid = RequiredGuid(state, 1, usage);
        var serviceType = RequiredEnum(state, 2, 0, 9, usage);
        if (!store.EligibleRaces.TryGetValue((guid, serviceType), out var races))
        {
            lua_pushnil(state);
            return 1;
        }
        lua_createtable(state, races.Count, 0);
        for (var index = 0; index < races.Count; index++)
        {
            var race = races[index];
            lua_createtable(state, 0, 3);
            SetOptionalString(state, "raceName", race.RaceName);
            SetBoolean(state, "isAlliedRace", race.IsAlliedRace);
            SetBoolean(
                state,
                "isHeritageArmorUnlocked",
                race.IsHeritageArmorUnlocked);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetEntryInfo(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local entryInfo = C_StoreSecure.GetEntryInfo(entryID)";
        var entryId = RequiredInt32(state, 1, usage);
        if (!store.Entries.TryGetValue(entryId, out var entry))
        {
            lua_pushnil(state);
            return 1;
        }
        lua_createtable(state, 0, 5);
        SetInteger(state, "productID", entry.ProductId);
        SetInteger(state, "groupID", entry.GroupId);
        SetInteger(state, "bannerType", entry.BannerType);
        SetBoolean(state, "alreadyOwned", entry.AlreadyOwned);
        PushProductSharedData(state, entry.SharedData);
        lua_setfield(state, -2, "sharedData");
        return 1;
    }

    private static int GetFailureInfo(
        lua_State state,
        WowStoreSecureState store)
    {
        PushOptionalInteger(state, store.FailureType);
        PushOptionalInteger(state, store.FailureErrorId);
        return 2;
    }

    private static int GetProductGroupInfo(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local groupInfo = C_StoreSecure.GetProductGroupInfo(groupID)";
        var groupId = RequiredInt32(state, 1, usage);
        if (!store.ProductGroupInfos.TryGetValue(groupId, out var group))
            return 0;
        lua_createtable(state, 0, 6);
        SetOptionalString(state, "groupName", group.GroupName);
        SetFileAsset(state, "texture", group.Texture);
        SetInteger(state, "displayType", group.DisplayType);
        SetInteger(state, "flags", group.Flags);
        SetOptionalString(state, "disabledTooltip", group.DisabledTooltip);
        SetInteger(
            state,
            "parentProductGroupID",
            group.ParentProductGroupId);
        return 1;
    }

    private static int GetProductGroups(
        lua_State state,
        WowStoreSecureState store)
    {
        lua_createtable(state, store.ProductGroups.Count, 0);
        for (var index = 0; index < store.ProductGroups.Count; index++)
        {
            var group = store.ProductGroups[index];
            lua_createtable(state, 0, 2);
            SetInteger(state, "groupID", group.GroupId);
            SetInteger(state, "parentGroupID", group.ParentGroupId);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetProductInfo(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local productInfo = C_StoreSecure.GetProductInfo(productID)";
        var productId = RequiredInt32(state, 1, usage);
        if (!store.Products.TryGetValue(productId, out var product))
        {
            lua_pushnil(state);
            return 1;
        }
        lua_createtable(state, 0, 2);
        SetInteger(state, "productID", product.ProductId);
        PushProductSharedData(state, product.SharedData);
        lua_setfield(state, -2, "sharedData");
        return 1;
    }

    private static int GetProducts(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local entryIDs = C_StoreSecure.GetProducts(groupID [, includeHiddenProducts])";
        var groupId = RequiredUInt32(state, 1, usage);
        var includeHidden = OptionalBoolean(state, 2, false);
        Record(store, "GetProducts", groupId, includeHidden);
        return PushIntegerArray(
            state,
            store.ProductIdsByGroup.TryGetValue(groupId, out var ids) ? ids : []);
    }

    private static int GetUnrevokedBoostInfo(
        lua_State state,
        WowStoreSecureState store)
    {
        if (store.UnrevokedBoostInfo is not { } info)
            return 0;
        lua_pushstring(state, info.Name);
        lua_pushstring(state, info.Description);
        lua_pushstring(state, info.Icon);
        return 3;
    }

    private static int GetVasCompletionInfo(
        lua_State state,
        WowStoreSecureState store)
    {
        if (store.VasCompletionInfo is not { } info)
            return 0;
        lua_pushinteger(state, info.Result);
        PushGuid(state, info.Guid);
        lua_pushstring(state, info.Name);
        lua_pushboolean(state, info.FactionChanged ? 1 : 0);
        return 4;
    }

    private static int GetVasGuildFollowInfo(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local guildFollowInfo = C_StoreSecure.GetVASGuildFollowInfoForCharacterByGUID(guid)";
        var guid = RequiredGuid(state, 1, usage);
        if (!store.GuildFollowInfos.TryGetValue(guid, out var info))
        {
            lua_pushnil(state);
            return 1;
        }
        lua_createtable(state, 0, 2);
        SetOptionalString(state, "transferredRealm", info.TransferredRealm);
        SetBoolean(state, "factionChanged", info.FactionChanged);
        return 1;
    }

    private static int GetVasGuildMasterInfo(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local guildMasterInfo = C_StoreSecure.GetVASGuildMasterInfoForCharacterByGUID(guid)";
        var guid = RequiredGuid(state, 1, usage);
        if (!store.GuildMasterInfos.TryGetValue(guid, out var info))
        {
            lua_pushnil(state);
            return 1;
        }
        lua_createtable(state, 0, 2);
        SetString(state, "guildName", info.GuildName);
        lua_createtable(state, info.GuildMemberInfos.Count, 0);
        for (var index = 0; index < info.GuildMemberInfos.Count; index++)
        {
            var member = info.GuildMemberInfos[index];
            lua_createtable(state, 0, 2);
            PushGuid(state, member.Guid);
            lua_setfield(state, -2, "guid");
            SetString(state, "memberName", member.MemberName);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "guildMemberInfos");
        return 1;
    }

    private static int GetVasServiceType(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local serviceType = C_StoreSecure.GetVasServiceType(productID)";
        var productId = RequiredInt32(state, 1, usage);
        PushOptionalInteger(
            state,
            store.VasServiceTypesByProductId.TryGetValue(productId, out var type)
                ? type
                : null);
        return 1;
    }

    private static int GetWowAccountGuidFromName(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local guid = C_StoreSecure.GetWoWAccountGUIDFromName(accountName, isLocal)";
        var name = RequiredStringValue(state, 1, usage);
        var isLocal = RequiredBoolean(state, 2, usage);
        store.WowAccountGuids.TryGetValue((name, isLocal), out var guid);
        PushGuid(state, guid);
        return 1;
    }

    private static int IsVasEligibleCharacterGuid(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local isEligible = C_StoreSecure.IsVASEligibleCharacterGUID(guid)";
        return PushBoolean(
            state,
            store.VasEligibleCharacterGuids.Contains(
                RequiredGuid(state, 1, usage)));
    }

    private static int OpenNydusLink(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage = "Usage: C_StoreSecure.OpenNydusLink(entryID)";
        return Record(store, "OpenNydusLink", RequiredInt32(state, 1, usage));
    }

    private static int PurchaseProduct(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local success = C_StoreSecure.PurchaseProduct(productID)";
        var productId = RequiredInt32(state, 1, usage);
        Record(store, "PurchaseProduct", productId);
        return PushBoolean(
            state,
            store.ProductPurchaseResults.TryGetValue(productId, out var result) &&
            result);
    }

    private static int PurchaseProductConfirm(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local success = C_StoreSecure.PurchaseProductConfirm(confirmed [, expectedDollars, expectedCents])";
        var confirmed = RequiredBoolean(state, 1, usage);
        var dollars = OptionalFiniteNumber(state, 2, usage);
        var cents = OptionalFiniteNumber(state, 3, usage);
        Record(store, "PurchaseProductConfirm", confirmed, dollars, cents);
        return PushBoolean(state, store.ProductPurchaseConfirmResult);
    }

    private static int PurchaseVasProduct(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: local success = C_StoreSecure.PurchaseVASProduct(productID, guid [, nameChangeName, stubGuildName, stubCharacterGuid, destinationRealm, destinationWowAccount, destinationBnetAccount], isFactionBundle, isGuildFollow)";
        var arguments = new WowStoreVasPurchaseArguments(
            RequiredInt32(state, 1, usage),
            RequiredGuid(state, 2, usage),
            OptionalStringValue(state, 3, usage),
            OptionalStringValue(state, 4, usage),
            OptionalGuid(state, 5, usage),
            OptionalInt32(state, 6, usage),
            OptionalGuid(state, 7, usage),
            OptionalGuid(state, 8, usage),
            RequiredBoolean(state, 9, usage),
            RequiredBoolean(state, 10, usage));
        Record(store, "PurchaseVASProduct", arguments);
        return PushBoolean(state, store.VasPurchaseResult);
    }

    private static int RequestCharacterGuildFollowInfo(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: C_StoreSecure.RequestCharacterGuildFollowInfo(guid, realmAddress)";
        return Record(
            store,
            "RequestCharacterGuildFollowInfo",
            RequiredGuid(state, 1, usage),
            RequiredInt32(state, 2, usage));
    }

    private static int RequestRealmGuildMasterInfo(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: C_StoreSecure.RequestRealmGuildMasterInfo(realmAddress)";
        return Record(
            store,
            "RequestRealmGuildMasterInfo",
            RequiredInt32(state, 1, usage));
    }

    private static int SetDisconnectOnLogout(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: C_StoreSecure.SetDisconnectOnLogout(disconnectOnLogout)";
        store.DisconnectOnLogout = RequiredBoolean(state, 1, usage);
        return 0;
    }

    private static int SetVasProductReady(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage = "Usage: C_StoreSecure.SetVASProductReady(isReady)";
        store.VasProductReady = RequiredBoolean(state, 1, usage);
        return 0;
    }

    private static int ValidateBnetTransfer(
        lua_State state,
        WowStoreSecureState store)
    {
        const string usage =
            "Usage: C_StoreSecure.ValidateBnetTransfer(bnetAccountName)";
        return Record(
            store,
            "ValidateBnetTransfer",
            RequiredStringValue(state, 1, usage));
    }

    private static int ContainsRequiredInt32(
        lua_State state,
        ISet<int> values,
        string usage) =>
        PushBoolean(state, values.Contains(RequiredInt32(state, 1, usage)));

    private static void PushCharacterInfo(
        lua_State state,
        WowStoreCharacterInfo character)
    {
        lua_createtable(state, 0, 12);
        SetString(state, "name", character.Name);
        SetOptionalString(state, "className", character.ClassName);
        SetOptionalString(state, "raceName", character.RaceName);
        SetInteger(state, "level", character.Level);
        SetOptionalString(state, "classFileName", character.ClassFileName);
        SetOptionalString(state, "raceFileName", character.RaceFileName);
        PushGuid(state, character.Guid);
        lua_setfield(state, -2, "guid");
        PushGuid(state, character.WowAccount);
        lua_setfield(state, -2, "wowAccount");
        SetInteger(state, "currentServer", character.CurrentServer);
        SetInteger(state, "faction", character.Faction);
        SetInteger(state, "sex", character.Sex);
        SetOptionalString(
            state,
            "createScreenIconAtlas",
            character.CreateScreenIconAtlas);
    }

    private static void PushCurrencySharedData(
        lua_State state,
        WowStoreCurrencySharedData data)
    {
        lua_createtable(state, 0, 8);
        SetInteger(state, "regionID", data.RegionId);
        SetOptionalString(state, "formatShort", data.FormatShort);
        SetOptionalString(state, "formatLong", data.FormatLong);
        SetOptionalString(state, "licenseAcceptText", data.LicenseAcceptText);
        SetOptionalBoolean(
            state,
            "requireLicenseAccept",
            data.RequireLicenseAccept);
        SetOptionalBoolean(state, "browseHasStar", data.BrowseHasStar);
        SetOptionalBoolean(state, "hideBrowseNotice", data.HideBrowseNotice);
        SetOptionalBoolean(
            state,
            "hideConfirmationBrowseNotice",
            data.HideConfirmationBrowseNotice);
    }

    private static void PushProductSharedData(
        lua_State state,
        WowStoreProductSharedData data)
    {
        lua_createtable(state, 0, 28);
        SetNumber(state, "normalDollars", data.NormalDollars);
        SetNumber(state, "normalCents", data.NormalCents);
        SetNumber(state, "currentDollars", data.CurrentDollars);
        SetNumber(state, "currentCents", data.CurrentCents);
        SetBoolean(state, "buyableHere", data.BuyableHere);
        SetOptionalString(state, "name", data.Name);
        SetOptionalString(state, "description", data.Description);
        SetOptionalString(state, "tooltip", data.Tooltip);
        SetOptionalString(state, "instructions", data.Instructions);
        SetOptionalString(state, "disclaimer", data.Disclaimer);
        SetInteger(state, "flags", data.Flags);
        SetInteger(state, "eligibility", data.Eligibility);
        SetBoolean(state, "canChangeAccount", data.CanChangeAccount);
        SetBoolean(state, "canChangeBNetAccount", data.CanChangeBNetAccount);
        SetOptionalFileAsset(state, "texture", data.Texture);
        SetOptionalInteger(state, "productDecorator", data.ProductDecorator);
        SetOptionalInteger(state, "boostType", data.BoostType);
        SetOptionalInteger(state, "itemID", data.ItemId);
        SetOptionalInteger(state, "vasServiceType", data.VasServiceType);
        SetOptionalString(
            state,
            "overrideBackground",
            data.OverrideBackground);
        PushOptionalColor(state, data.OverrideTextColor);
        lua_setfield(state, -2, "overrideTextColor");
        SetOptionalString(state, "overrideTexture", data.OverrideTexture);
        SetOptionalInteger(state, "modelSceneID", data.ModelSceneId);
        PushProductCards(state, data.Cards);
        lua_setfield(state, -2, "cards");
        PushProductDeliverables(state, data.Deliverables);
        lua_setfield(state, -2, "deliverables");
        SetInteger(state, "cardType", data.CardType);
        SetInteger(state, "bannerType", data.BannerType);
        SetInteger(state, "itemQuantity", data.ItemQuantity);
    }

    private static void PushProductCards(
        lua_State state,
        IList<WowStoreProductCard> cards)
    {
        lua_createtable(state, cards.Count, 0);
        for (var index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            lua_createtable(state, 0, 5);
            SetString(state, "title", card.Title);
            SetInteger(state, "modelSceneID", card.ModelSceneId);
            SetInteger(
                state,
                "creatureDisplayInfoID",
                card.CreatureDisplayInfoId);
            PushIntegerArray(state, card.ItemModifiedAppearanceIds);
            lua_setfield(state, -2, "itemModifiedAppearanceIDs");
            SetBoolean(
                state,
                "displayTransmogItemsIndividually",
                card.DisplayTransmogItemsIndividually);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushProductDeliverables(
        lua_State state,
        IList<WowStoreProductDeliverable> deliverables)
    {
        lua_createtable(state, deliverables.Count, 0);
        for (var index = 0; index < deliverables.Count; index++)
        {
            var deliverable = deliverables[index];
            lua_createtable(state, 0, 2);
            SetString(state, "name", deliverable.Name);
            SetBoolean(state, "owned", deliverable.Owned);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static int PushRealms(
        lua_State state,
        IList<WowStoreRealmInfo> realms)
    {
        lua_createtable(state, realms.Count, 0);
        for (var index = 0; index < realms.Count; index++)
        {
            var realm = realms[index];
            lua_createtable(state, 0, 5);
            SetInteger(
                state,
                "virtualRealmAddress",
                realm.VirtualRealmAddress);
            SetString(state, "realmName", realm.RealmName);
            SetInteger(state, "characterCount", realm.CharacterCount);
            SetOptionalBoolean(state, "pvp", realm.Pvp);
            SetOptionalBoolean(state, "rp", realm.Rp);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int RequiredInt32(lua_State state, int index, string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return (int)value;
    }

    private static uint RequiredUInt32(lua_State state, int index, string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < uint.MinValue || value > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (uint)value;
    }

    private static int RequiredEnum(
        lua_State state,
        int index,
        int minimum,
        int maximum,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value < minimum || value > maximum)
            return luaL_error(state, usage);
        return value;
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state))
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static bool OptionalBoolean(
        lua_State state,
        int index,
        bool defaultValue) =>
        index > lua_gettop(state)
            ? defaultValue
            : lua_toboolean(state, index) != 0;

    private static string RequiredGuid(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_type(state, index) != LUA_TSTRING)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static string? OptionalGuid(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        return RequiredGuid(state, index, usage);
    }

    private static string RequiredStringValue(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static string? OptionalStringValue(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        return RequiredStringValue(state, index, usage);
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        return RequiredInt32(state, index, usage);
    }

    private static double? OptionalFiniteNumber(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return null;
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value))
        {
            luaL_error(state, usage);
            return null;
        }
        return value;
    }

    private static int PushIntegerArray(
        lua_State state,
        IEnumerable<int> source)
    {
        var values = source as int[] ?? source.ToArray();
        lua_createtable(state, values.Length, 0);
        for (var index = 0; index < values.Length; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static void PushStringArray(
        lua_State state,
        IList<string> values)
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

    private static int PushInteger(lua_State state, long value)
    {
        lua_pushinteger(state, value);
        return 1;
    }

    private static void PushGuid(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value.HasValue)
            lua_pushinteger(state, value.Value);
        else
            lua_pushnil(state);
    }

    private static void PushOptionalColor(
        lua_State state,
        WowStoreColor? color)
    {
        if (color is null)
        {
            lua_pushnil(state);
            return;
        }
        lua_createtable(state, 0, 4);
        SetNumber(state, "r", color.R);
        SetNumber(state, "g", color.G);
        SetNumber(state, "b", color.B);
        SetNumber(state, "a", color.A);
    }

    private static void SetInteger(lua_State state, string name, long value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetNumber(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalString(
        lua_State state,
        string name,
        string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBoolean(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalBoolean(
        lua_State state,
        string name,
        bool? value)
    {
        if (value.HasValue)
            lua_pushboolean(state, value.Value ? 1 : 0);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string name,
        int? value)
    {
        PushOptionalInteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetFileAsset(
        lua_State state,
        string name,
        int value)
    {
        if (value == 0)
            lua_pushnil(state);
        else
            lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalFileAsset(
        lua_State state,
        string name,
        int? value)
    {
        if (!value.HasValue || value.Value == 0)
            lua_pushnil(state);
        else
            lua_pushnumber(state, value.Value);
        lua_setfield(state, -2, name);
    }

    private static int Record(
        WowStoreSecureState store,
        string operation,
        params object?[] arguments)
    {
        store.Requests.Add(new WowStoreSecureRequest(operation, arguments));
        return 0;
    }
}
