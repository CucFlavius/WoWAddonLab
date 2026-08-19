using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCatalogShopApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "BulkPurchaseProducts",
        "BulkRefundDecors",
        "CloseCatalogShopInteraction",
        "ConfirmHousingPurchase",
        "FindBestCurrencyProductForNeededAmount",
        "GetAvailableCategoryIDs",
        "GetAvailableTransmogRaceInfos",
        "GetCatalogShopProductDisplayInfo",
        "GetCategoryInfo",
        "GetCategorySectionInfo",
        "GetFailureInfo",
        "GetFirstCategoryByProductID",
        "GetNewProducts",
        "GetProductAvailabilityTimeRemainingSecs",
        "GetProductIDsForBundle",
        "GetProductIDsForCategory",
        "GetProductIDsForCategorySection",
        "GetProductInfo",
        "GetProductSortOrder",
        "GetRefundableDecors",
        "GetSectionIDsForCategory",
        "GetSpellVisualInfoForMount",
        "GetVCProductInfos",
        "GetVirtualCurrencyBalance",
        "HasNewProducts",
        "IsProductIncludedInAnyBundle",
        "IsShop2Enabled",
        "OnLegalDisclaimerClicked",
        "OnLegalPersonalizedOptOutClicked",
        "OpenCatalogShopInteractionFromHouse",
        "OpenCatalogShopInteractionFromShop",
        "ProductDisplayedTelemetry",
        "ProductSelectedTelemetry",
        "PurchaseProduct",
        "RefreshRefundableDecors",
        "RefreshVirtualCurrencyBalance",
        "ShouldShowHousingWarning",
        "StartHousingVCPurchaseConfirmation"
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
        lua_setglobal(state, "C_CatalogShop");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var shop = runtime.CatalogShop;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        return operation switch
        {
            "BulkPurchaseProducts" => BulkPurchaseProducts(state, shop),
            "BulkRefundDecors" => BulkRefundDecors(state, shop),
            "CloseCatalogShopInteraction" => CloseCatalogShopInteraction(shop),
            "ConfirmHousingPurchase" => ConfirmHousingPurchase(state, shop),
            "FindBestCurrencyProductForNeededAmount" =>
                FindBestCurrencyProductForNeededAmount(state, shop),
            "GetAvailableCategoryIDs" => PushIntegerArray(state, shop.AvailableCategoryIds),
            "GetAvailableTransmogRaceInfos" =>
                GetAvailableTransmogRaceInfos(state, shop),
            "GetCatalogShopProductDisplayInfo" =>
                GetCatalogShopProductDisplayInfo(state, shop),
            "GetCategoryInfo" => GetCategoryInfo(state, shop),
            "GetCategorySectionInfo" => GetCategorySectionInfo(state, shop),
            "GetFailureInfo" => GetFailureInfo(state, shop),
            "GetFirstCategoryByProductID" =>
                GetFirstCategoryByProductId(state, shop),
            "GetNewProducts" => PushIntegerArray(state, shop.NewProductIds),
            "GetProductAvailabilityTimeRemainingSecs" =>
                GetProductAvailabilityTimeRemainingSecs(state, shop),
            "GetProductIDsForBundle" => GetProductIdsForBundle(state, shop),
            "GetProductIDsForCategory" => GetProductIdsForCategory(state, shop),
            "GetProductIDsForCategorySection" =>
                GetProductIdsForCategorySection(state, shop),
            "GetProductInfo" => GetProductInfo(state, shop),
            "GetProductSortOrder" => GetProductSortOrder(state, shop),
            "GetRefundableDecors" => GetRefundableDecors(state, shop),
            "GetSectionIDsForCategory" => GetSectionIdsForCategory(state, shop),
            "GetSpellVisualInfoForMount" =>
                GetSpellVisualInfoForMount(state, shop),
            "GetVCProductInfos" => GetVcProductInfos(state, shop),
            "GetVirtualCurrencyBalance" => GetVirtualCurrencyBalance(state, shop),
            "HasNewProducts" => PushBoolean(state, shop.NewProductIds.Count > 0),
            "IsProductIncludedInAnyBundle" =>
                IsProductIncludedInAnyBundle(state, shop),
            "IsShop2Enabled" => IsShop2Enabled(state, shop),
            "OnLegalDisclaimerClicked" => OnLegalDisclaimerClicked(state, shop),
            "OnLegalPersonalizedOptOutClicked" =>
                Record(shop, operation),
            "OpenCatalogShopInteractionFromHouse" =>
                OpenCatalogShopInteraction(state, shop, WowCatalogShopInteractionSource.House),
            "OpenCatalogShopInteractionFromShop" =>
                OpenCatalogShopInteraction(state, shop, WowCatalogShopInteractionSource.Shop),
            "ProductDisplayedTelemetry" =>
                ProductDisplayedTelemetry(state, shop),
            "ProductSelectedTelemetry" =>
                ProductSelectedTelemetry(state, shop),
            "PurchaseProduct" => PurchaseProduct(state, shop),
            "RefreshRefundableDecors" => RefreshRefundableDecors(shop),
            "RefreshVirtualCurrencyBalance" =>
                RefreshVirtualCurrencyBalance(state, shop),
            "ShouldShowHousingWarning" =>
                PushBoolean(state, shop.ShouldShowHousingWarning),
            "StartHousingVCPurchaseConfirmation" =>
                StartHousingVcPurchaseConfirmation(state, shop),
            _ => 0
        };
    }

    private static int BulkPurchaseProducts(lua_State state, WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local canPurchaseProducts = C_CatalogShop.BulkPurchaseProducts(productIDs)";
        var productIds = RequiredInt32Array(state, 1, usage);
        Record(shop, "BulkPurchaseProducts", productIds.Cast<object?>().ToArray());
        return PushBoolean(state, shop.BulkPurchaseResult);
    }

    private static int BulkRefundDecors(lua_State state, WowCatalogShopState shop)
    {
        const string usage =
            "Usage: C_CatalogShop.BulkRefundDecors(decorGUIDs)";
        var guids = RequiredGuidArray(state, 1, usage);
        Record(shop, "BulkRefundDecors", guids.Cast<object?>().ToArray());
        return 0;
    }

    private static int CloseCatalogShopInteraction(WowCatalogShopState shop)
    {
        shop.InteractionSource = WowCatalogShopInteractionSource.None;
        shop.ShoppingSessionUuid = null;
        Record(shop, "CloseCatalogShopInteraction");
        return 0;
    }

    private static int ConfirmHousingPurchase(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: C_CatalogShop.ConfirmHousingPurchase(productIDs)";
        var productIds = RequiredInt32Array(state, 1, usage);
        Record(shop, "ConfirmHousingPurchase", productIds.Cast<object?>().ToArray());
        return 0;
    }

    private static int FindBestCurrencyProductForNeededAmount(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local vcProductID = C_CatalogShop.FindBestCurrencyProductForNeededAmount(vcCurrencyCode, amountNeeded)";
        var currencyCode = RequiredStringValue(state, 1, usage);
        var amountNeeded = RequiredUInt32(state, 2, usage);
        PushOptionalInteger(
            state,
            shop.BestCurrencyProducts.TryGetValue(
                (currencyCode, amountNeeded),
                out var productId)
                    ? productId
                    : null);
        return 1;
    }

    private static int GetAvailableTransmogRaceInfos(
        lua_State state,
        WowCatalogShopState shop)
    {
        lua_createtable(state, shop.AvailableTransmogRaceInfos.Count, 0);
        for (var index = 0; index < shop.AvailableTransmogRaceInfos.Count; index++)
        {
            var info = shop.AvailableTransmogRaceInfos[index];
            lua_createtable(state, 0, 2);
            SetInteger(state, "raceID", info.RaceId);
            SetString(state, "displayName", info.DisplayName);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetCatalogShopProductDisplayInfo(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local item = C_CatalogShop.GetCatalogShopProductDisplayInfo(catalogShopProductID)";
        var productId = RequiredInt32(state, 1, usage);
        var info = shop.ProductDisplayInfos.TryGetValue(productId, out var configured)
            ? configured
            : new WowCatalogShopProductDisplayInfo();
        PushProductDisplayInfo(state, info);
        return 1;
    }

    private static int GetCategoryInfo(lua_State state, WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local categoryInfo = C_CatalogShop.GetCategoryInfo(categoryID)";
        var categoryId = RequiredInt32(state, 1, usage);
        PushCategoryInfo(
            state,
            shop.Categories.TryGetValue(categoryId, out var category)
                ? category
                : new WowCatalogShopCategoryInfo(categoryId));
        return 1;
    }

    private static int GetCategorySectionInfo(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local sectionInfo = C_CatalogShop.GetCategorySectionInfo(categoryID, sectionID)";
        var categoryId = RequiredInt32(state, 1, usage);
        var sectionId = RequiredInt32(state, 2, usage);
        var section = shop.CategorySections.TryGetValue(
            (categoryId, sectionId),
            out var configured)
                ? configured
                : new WowCatalogShopCategorySectionInfo(sectionId);
        PushCategorySectionInfo(state, section);
        return 1;
    }

    private static int GetFailureInfo(lua_State state, WowCatalogShopState shop)
    {
        lua_pushinteger(state, unchecked((byte)shop.FailureType));
        lua_pushinteger(state, unchecked((byte)shop.FailureType));
        return 2;
    }

    private static int GetFirstCategoryByProductId(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local categoryInfo = C_CatalogShop.GetFirstCategoryByProductID(productID)";
        var productId = RequiredInt32(state, 1, usage);
        if (!shop.FirstCategoryIdsByProductId.TryGetValue(productId, out var categoryId))
        {
            lua_pushnil(state);
            return 1;
        }
        PushCategoryInfo(
            state,
            shop.Categories.TryGetValue(categoryId, out var category)
                ? category
                : new WowCatalogShopCategoryInfo(categoryId));
        return 1;
    }

    private static int GetProductAvailabilityTimeRemainingSecs(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local timeRemainingSecs = C_CatalogShop.GetProductAvailabilityTimeRemainingSecs(catalogShopProductID)";
        var productId = RequiredInt32(state, 1, usage);
        PushOptionalInteger(
            state,
            shop.ProductAvailabilitySeconds.TryGetValue(productId, out var seconds)
                ? Math.Max(0, seconds)
                : null);
        return 1;
    }

    private static int GetProductIdsForBundle(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local productIDs = C_CatalogShop.GetProductIDsForBundle(bundleProductID)";
        var productId = RequiredInt32(state, 1, usage);
        var children = shop.BundleChildren.TryGetValue(productId, out var configured)
            ? configured
            : [];
        lua_createtable(state, children.Count, 0);
        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            lua_createtable(state, 0, 3);
            SetInteger(state, "childProductID", child.ChildProductId);
            SetInteger(state, "displayOrder", child.DisplayOrder);
            SetInteger(state, "quantityInBundle", child.QuantityInBundle);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetProductIdsForCategory(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local productIDs = C_CatalogShop.GetProductIDsForCategory(categoryID)";
        var categoryId = RequiredInt32(state, 1, usage);
        return PushIntegerArray(
            state,
            shop.ProductIdsByCategory.TryGetValue(categoryId, out var products)
                ? products
                : []);
    }

    private static int GetProductIdsForCategorySection(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local productIDs = C_CatalogShop.GetProductIDsForCategorySection(categoryID, sectionID)";
        var categoryId = RequiredInt32(state, 1, usage);
        var sectionId = RequiredInt32(state, 2, usage);
        return PushIntegerArray(
            state,
            shop.ProductIdsByCategorySection.TryGetValue(
                (categoryId, sectionId),
                out var products)
                    ? products
                    : []);
    }

    private static int GetProductInfo(lua_State state, WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local productInfo = C_CatalogShop.GetProductInfo(productID)";
        var productId = RequiredInt32(state, 1, usage);
        if (!shop.Products.TryGetValue(productId, out var product))
        {
            lua_pushnil(state);
            return 1;
        }
        PushProductInfo(state, product);
        return 1;
    }

    private static int GetProductSortOrder(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local sortOrder = C_CatalogShop.GetProductSortOrder(categoryID, sectionID, productID)";
        var categoryId = RequiredInt32(state, 1, usage);
        var sectionId = RequiredInt32(state, 2, usage);
        var productId = RequiredInt32(state, 3, usage);
        PushOptionalInteger(
            state,
            shop.ProductSortOrders.TryGetValue(
                (categoryId, sectionId, productId),
                out var order)
                    ? order
                    : null);
        return 1;
    }

    private static int GetRefundableDecors(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local refundableDecors, minTimeRemaining = C_CatalogShop.GetRefundableDecors([productID])";
        uint? filter = lua_type(state, 1) is LUA_TNONE or LUA_TNIL
            ? null
            : RequiredUInt32(state, 1, usage);
        var rows = filter.HasValue
            ? shop.RefundableDecors.Where(row => row.ProductId == filter).ToArray()
            : shop.RefundableDecors.ToArray();
        lua_createtable(state, rows.Length, 0);
        for (var index = 0; index < rows.Length; index++)
        {
            var row = rows[index];
            lua_createtable(state, 0, 4);
            SetString(state, "decorGUID", row.DecorGuid);
            SetInteger(state, "timeRemainingSeconds", row.TimeRemainingSeconds);
            SetString(state, "name", row.Name);
            SetString(state, "price", row.Price);
            lua_rawseti(state, -2, index + 1);
        }
        lua_pushinteger(state, shop.MinimumRefundableDecorTimeRemainingSeconds);
        return 2;
    }

    private static int GetSectionIdsForCategory(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local sectionIDs = C_CatalogShop.GetSectionIDsForCategory(categoryID)";
        var categoryId = RequiredInt32(state, 1, usage);
        return PushIntegerArray(
            state,
            shop.SectionIdsByCategory.TryGetValue(categoryId, out var sections)
                ? sections
                : []);
    }

    private static int GetSpellVisualInfoForMount(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local spellVisualInfo = C_CatalogShop.GetSpellVisualInfoForMount(spellVisualID)";
        var spellVisualId = RequiredUInt32(state, 1, usage);
        var info = shop.MountSpellVisualInfos.TryGetValue(
            spellVisualId,
            out var configured)
                ? configured
                : new WowCatalogShopSpellVisualInfo();
        lua_createtable(state, 0, 2);
        SetInteger(state, "animID", info.AnimId);
        SetInteger(state, "spellVisualKitID", info.SpellVisualKitId);
        return 1;
    }

    private static int GetVcProductInfos(lua_State state, WowCatalogShopState shop)
    {
        lua_createtable(state, shop.VcProductInfos.Count, 0);
        for (var index = 0; index < shop.VcProductInfos.Count; index++)
        {
            lua_createtable(state, 0, 1);
            SetInteger(
                state,
                "vcProductID",
                shop.VcProductInfos[index].VcProductId);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetVirtualCurrencyBalance(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local balance = C_CatalogShop.GetVirtualCurrencyBalance(currencyCode)";
        var currencyCode = RequiredStringValue(state, 1, usage);
        if (shop.VirtualCurrencyBalances.TryGetValue(currencyCode, out var balance))
            lua_pushstring(state, balance);
        else
            lua_pushnil(state);
        return 1;
    }

    private static int IsProductIncludedInAnyBundle(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local isIncluded = C_CatalogShop.IsProductIncludedInAnyBundle(productID)";
        var productId = RequiredInt32(state, 1, usage);
        var included = shop.ProductsIncludedInBundles.Contains(productId) ||
                       shop.BundleChildren.Values.Any(children =>
                           children.Any(child => child.ChildProductId == productId));
        return PushBoolean(state, included);
    }

    private static int IsShop2Enabled(lua_State state, WowCatalogShopState shop)
    {
        if (shop.IsShop2Enabled is { } enabled)
            lua_pushboolean(state, enabled ? 1 : 0);
        else
            lua_pushnil(state);
        return 1;
    }

    private static int OnLegalDisclaimerClicked(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: C_CatalogShop.OnLegalDisclaimerClicked(catalogShopProductID)";
        Record(
            shop,
            "OnLegalDisclaimerClicked",
            RequiredInt32(state, 1, usage));
        return 0;
    }

    private static int OpenCatalogShopInteraction(
        lua_State state,
        WowCatalogShopState shop,
        WowCatalogShopInteractionSource source)
    {
        shop.InteractionSource = source;
        shop.ShoppingSessionUuid ??= Guid.NewGuid().ToString();
        Record(
            shop,
            source == WowCatalogShopInteractionSource.House
                ? "OpenCatalogShopInteractionFromHouse"
                : "OpenCatalogShopInteractionFromShop");
        lua_pushstring(state, shop.ShoppingSessionUuid);
        return 1;
    }

    private static int ProductDisplayedTelemetry(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: C_CatalogShop.ProductDisplayedTelemetry(categoryId, sectionId, catalogShopProductID)";
        shop.Telemetry.Add(
            new WowCatalogShopTelemetryEntry(
                "ProductDisplayedTelemetry",
                RequiredInt32(state, 1, usage),
                RequiredInt32(state, 2, usage),
                RequiredInt32(state, 3, usage)));
        return 0;
    }

    private static int ProductSelectedTelemetry(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: C_CatalogShop.ProductSelectedTelemetry(categoryId, sectionId, catalogShopProductID, wasCodeSelection)";
        shop.Telemetry.Add(
            new WowCatalogShopTelemetryEntry(
                "ProductSelectedTelemetry",
                RequiredInt32(state, 1, usage),
                RequiredInt32(state, 2, usage),
                RequiredInt32(state, 3, usage),
                RequiredBoolean(state, 4, usage)));
        return 0;
    }

    private static int PurchaseProduct(lua_State state, WowCatalogShopState shop)
    {
        const string usage =
            "Usage: local canPurchase = C_CatalogShop.PurchaseProduct(productID)";
        var productId = RequiredInt32(state, 1, usage);
        Record(shop, "PurchaseProduct", productId);
        return PushBoolean(
            state,
            shop.PurchaseResults.TryGetValue(productId, out var result) && result);
    }

    private static int RefreshRefundableDecors(WowCatalogShopState shop)
    {
        shop.RefreshRefundableDecorsCount++;
        Record(shop, "RefreshRefundableDecors");
        return 0;
    }

    private static int RefreshVirtualCurrencyBalance(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: C_CatalogShop.RefreshVirtualCurrencyBalance(currencyCode)";
        var currencyCode = RequiredStringValue(state, 1, usage);
        shop.LastVirtualCurrencyRefreshCode = currencyCode;
        Record(shop, "RefreshVirtualCurrencyBalance", currencyCode);
        return 0;
    }

    private static int StartHousingVcPurchaseConfirmation(
        lua_State state,
        WowCatalogShopState shop)
    {
        const string usage =
            "Usage: C_CatalogShop.StartHousingVCPurchaseConfirmation(productID)";
        var productId = RequiredInt32(state, 1, usage);
        shop.PendingHousingVcProductId = productId;
        Record(shop, "StartHousingVCPurchaseConfirmation", productId);
        return 0;
    }

    private static void PushCategoryInfo(
        lua_State state,
        WowCatalogShopCategoryInfo category)
    {
        lua_createtable(state, 0, 6);
        SetInteger(state, "ID", category.Id);
        SetString(state, "displayName", category.DisplayName);
        SetString(state, "iconTexture", category.IconTexture);
        SetString(state, "linkTag", category.LinkTag);
        SetBoolean(state, "isDisabled", category.IsDisabled);
        SetBoolean(
            state,
            "showPersistentRefundButton",
            category.ShowPersistentRefundButton);
    }

    private static void PushCategorySectionInfo(
        lua_State state,
        WowCatalogShopCategorySectionInfo section)
    {
        lua_createtable(state, 0, 6);
        SetInteger(state, "ID", section.Id);
        SetString(state, "displayName", section.DisplayName);
        SetOptionalInteger(
            state,
            "parentCatalogShopCategoryInfoID",
            section.ParentCatalogShopCategoryInfoId);
        SetOptionalString(state, "cardType", section.CardType);
        SetOptionalInteger(state, "scrollGridSize", section.ScrollGridSize);
        SetBoolean(
            state,
            "shouldShowRecommendationOptOutDisclaimer",
            section.ShouldShowRecommendationOptOutDisclaimer);
    }

    private static void PushProductDisplayInfo(
        lua_State state,
        WowCatalogShopProductDisplayInfo info)
    {
        lua_createtable(state, 0, 34);
        SetInteger(
            state,
            "defaultPreviewModelSceneID",
            info.DefaultPreviewModelSceneId);
        SetInteger(
            state,
            "defaultCardModelSceneID",
            info.DefaultCardModelSceneId);
        SetInteger(
            state,
            "defaultWideCardModelSceneID",
            info.DefaultWideCardModelSceneId);
        SetInteger(state, "itemID", info.ItemId);
        SetOptionalInteger(
            state,
            "overridePreviewModelSceneID",
            info.OverridePreviewModelSceneId);
        SetOptionalInteger(
            state,
            "overrideCardModelSceneID",
            info.OverrideCardModelSceneId);
        SetOptionalInteger(
            state,
            "overrideWideCardModelSceneID",
            info.OverrideWideCardModelSceneId);
        SetIntegerArray(state, "creatureDisplayInfoIDs", info.CreatureDisplayInfoIds);
        SetIntegerArray(state, "spellVisualIDs", info.SpellVisualIds);
        SetOptionalInteger(
            state,
            "mainHandItemModifiedAppearanceID",
            info.MainHandItemModifiedAppearanceId);
        SetOptionalInteger(
            state,
            "offHandItemModifiedAppearanceID",
            info.OffHandItemModifiedAppearanceId);
        SetIntegerArray(
            state,
            "itemModifiedAppearanceIDs",
            info.ItemModifiedAppearanceIds);
        SetOptionalInteger(state, "iconFileDataID", info.IconFileDataId);
        SetOptionalString(state, "iconTextureKit", info.IconTextureKit);
        SetOptionalString(state, "productType", info.ProductType);
        SetOptionalString(state, "itemDescription", info.ItemDescription);
        SetBoolean(state, "hasUnknownLicense", info.HasUnknownLicense);
        SetOptionalString(state, "productPMTURL", info.ProductPmtUrl);
        SetStringArray(
            state,
            "additionalProductPMTURLs",
            info.AdditionalProductPmtUrls);
        SetOptionalString(
            state,
            "otherProductImageAtlasName",
            info.OtherProductImageAtlasName);
        SetOptionalString(
            state,
            "otherProductGameTitleBaseTag",
            info.OtherProductGameTitleBaseTag);
        SetOptionalString(
            state,
            "otherProductGameType",
            info.OtherProductGameType);
        SetOptionalInteger(
            state,
            "customLoopingSoundStart",
            info.CustomLoopingSoundStart);
        SetOptionalInteger(
            state,
            "customLoopingSoundMiddle",
            info.CustomLoopingSoundMiddle);
        SetOptionalInteger(
            state,
            "customLoopingSoundEnd",
            info.CustomLoopingSoundEnd);
        SetOptionalString(state, "specialActorID_1", info.SpecialActorId1);
        SetOptionalString(state, "specialActorID_2", info.SpecialActorId2);
        SetOptionalString(state, "specialActorID_3", info.SpecialActorId3);
        SetOptionalString(state, "specialActorID_4", info.SpecialActorId4);
        SetOptionalString(state, "specialActorID_5", info.SpecialActorId5);
        SetOptionalInteger(state, "gameFlavorID", info.GameFlavorId);
        SetOptionalInteger(state, "decorFileDataID", info.DecorFileDataId);
        SetOptionalInteger(state, "quantity", info.Quantity);
        SetOptionalString(state, "houseTextureAtlas", info.HouseTextureAtlas);
    }

    private static void PushProductInfo(
        lua_State state,
        WowCatalogShopProductInfo product)
    {
        lua_createtable(state, 0, 44);
        SetInteger(state, "catalogShopProductID", product.CatalogShopProductId);
        SetString(state, "name", product.Name);
        SetOptionalString(state, "type", product.Type);
        SetString(state, "description", product.Description);
        SetString(state, "iconTexture", product.IconTexture);
        SetBoolean(state, "isFullyOwned", product.IsFullyOwned);
        SetBoolean(state, "isPurchasePending", product.IsPurchasePending);
        SetBoolean(state, "refundable", product.Refundable);
        SetString(state, "price", product.Price);
        SetString(state, "originalPrice", product.OriginalPrice);
        SetInteger(state, "discountPercentage", product.DiscountPercentage);
        SetInteger(state, "itemID", product.ItemId);
        SetInteger(state, "mountID", product.MountId);
        SetString(state, "mountTypeName", product.MountTypeName);
        SetInteger(state, "speciesID", product.SpeciesId);
        SetInteger(state, "transmogSetID", product.TransmogSetId);
        SetInteger(
            state,
            "itemModifiedAppearanceID",
            product.ItemModifiedAppearanceId);
        PushProductSubItems(state, product.SubItems);
        lua_setfield(state, -2, "subItems");
        SetBoolean(state, "subItemsLoaded", product.SubItemsLoaded);
        SetString(state, "backgroundTexture", product.BackgroundTexture);
        SetOptionalString(state, "foregroundTexture", product.ForegroundTexture);
        SetOptionalString(state, "smallCardBGTexture", product.SmallCardBgTexture);
        SetOptionalString(state, "smallCardFGTexture", product.SmallCardFgTexture);
        SetOptionalString(state, "wideCardBGTexture", product.WideCardBgTexture);
        SetOptionalString(state, "wideCardFGTexture", product.WideCardFgTexture);
        SetOptionalString(state, "previewIconTexture", product.PreviewIconTexture);
        SetOptionalString(
            state,
            "optionalWideCardBackgroundTexture",
            product.OptionalWideCardBackgroundTexture);
        SetBoolean(state, "isBundle", product.IsBundle);
        SetInteger(state, "bundleChildrenSize", product.BundleChildrenSize);
        SetInteger(state, "licenseTermType", product.LicenseTermType);
        SetInteger(state, "licenseTermDuration", product.LicenseTermDuration);
        PushVirtualCurrencies(state, product.VirtualCurrencies);
        lua_setfield(state, -2, "virtualCurrencies");
        SetBoolean(state, "isHidden", product.IsHidden);
        SetBoolean(state, "isMystery", product.IsMystery);
        SetBoolean(state, "hasPendingOrders", product.HasPendingOrders);
        SetInteger(state, "numBundleDetailCards", product.NumBundleDetailCards);
        SetBoolean(
            state,
            "isDynamicallyDiscounted",
            product.IsDynamicallyDiscounted);
        SetBoolean(
            state,
            "shouldShowOriginalPrice",
            product.ShouldShowOriginalPrice);
        SetOptionalString(
            state,
            "wideCardBGOverrideProductURL",
            product.WideCardBgOverrideProductUrl);
        SetOptionalString(
            state,
            "previewBGOverrideProductURL",
            product.PreviewBgOverrideProductUrl);
        SetOptionalString(
            state,
            "previewSmallBGOverrideProductURL",
            product.PreviewSmallBgOverrideProductUrl);
        PushDecorQuantity(state, product.DecorQuantity);
        lua_setfield(state, -2, "decorQuantity");
        SetBoolean(state, "isVCProduct", product.IsVcProduct);
        SetBoolean(state, "containsHousingItem", product.ContainsHousingItem);
    }

    private static void PushProductSubItems(
        lua_State state,
        IList<WowCatalogShopProductSubItem> items)
    {
        lua_createtable(state, items.Count, 0);
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            lua_createtable(state, 0, 5);
            SetString(state, "name", item.Name);
            SetInteger(state, "itemID", item.ItemId);
            SetInteger(state, "itemAppearanceID", item.ItemAppearanceId);
            SetString(state, "invType", item.InvType);
            SetInteger(state, "quality", item.Quality);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushVirtualCurrencies(
        lua_State state,
        IList<WowCatalogShopVirtualCurrencyGrant> currencies)
    {
        lua_createtable(state, currencies.Count, 0);
        for (var index = 0; index < currencies.Count; index++)
        {
            var currency = currencies[index];
            lua_createtable(state, 0, 2);
            SetInteger(state, "amount", currency.Amount);
            SetString(state, "currencyCode", currency.CurrencyCode);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushDecorQuantity(
        lua_State state,
        WowCatalogShopDecorQuantity? quantity)
    {
        if (quantity is null)
        {
            lua_pushnil(state);
            return;
        }
        lua_createtable(state, 0, 2);
        SetInteger(state, "placedQuantity", quantity.PlacedQuantity);
        SetInteger(state, "storedQuantity", quantity.StoredQuantity);
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
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (uint)value;
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

    private static IReadOnlyList<int> RequiredInt32Array(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_type(state, index) != LUA_TTABLE)
        {
            luaL_error(state, usage);
            return [];
        }
        var absolute = AbsoluteIndex(state, index);
        var count = checked((int)lua_objlen(state, absolute));
        var values = new List<int>(count);
        for (var item = 1; item <= count; item++)
        {
            lua_rawgeti(state, absolute, item);
            values.Add(RequiredInt32(state, -1, usage));
            lua_pop(state, 1);
        }
        return values;
    }

    private static IReadOnlyList<string> RequiredGuidArray(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_type(state, index) != LUA_TTABLE)
        {
            luaL_error(state, usage);
            return [];
        }
        var absolute = AbsoluteIndex(state, index);
        var count = checked((int)lua_objlen(state, absolute));
        var values = new List<string>(count);
        for (var item = 1; item <= count; item++)
        {
            lua_rawgeti(state, absolute, item);
            if (lua_type(state, -1) != LUA_TSTRING)
            {
                lua_pop(state, 1);
                luaL_error(state, usage);
                return [];
            }
            values.Add(lua_tostring(state, -1) ?? string.Empty);
            lua_pop(state, 1);
        }
        return values;
    }

    private static int AbsoluteIndex(lua_State state, int index) =>
        index > 0 || index <= LUA_REGISTRYINDEX
            ? index
            : lua_gettop(state) + index + 1;

    private static int PushIntegerArray(lua_State state, IList<int> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value.HasValue)
            lua_pushinteger(state, value.Value);
        else
            lua_pushnil(state);
    }

    private static void SetInteger(lua_State state, string name, long value)
    {
        lua_pushinteger(state, value);
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

    private static void SetIntegerArray(
        lua_State state,
        string name,
        IList<int> values)
    {
        PushIntegerArray(state, values);
        lua_setfield(state, -2, name);
    }

    private static void SetStringArray(
        lua_State state,
        string name,
        IList<string> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushstring(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, name);
    }

    private static int Record(
        WowCatalogShopState shop,
        string operation,
        params object?[] arguments)
    {
        shop.Requests.Add(new WowCatalogShopRequest(operation, arguments));
        return 0;
    }
}
