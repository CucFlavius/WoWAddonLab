using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowHousingCatalogApi : LuaApiModule
{
    private static readonly lua_CFunction NamespaceCallback = DispatchNamespace;
    private static readonly lua_CFunction SearcherCallback = DispatchSearcher;

    private static readonly string[] SearcherMethods =
    [
        "GetAllSearchItems", "GetCatalogSearchResults", "GetEditorModeContext",
        "GetFilterTagStatus", "GetFilteredCategoryID", "GetFilteredSubcategoryID",
        "GetNumSearchItems", "GetSearchCount", "GetSearchText", "GetSortType",
        "IsAllowedIndoorsActive", "IsAllowedOutdoorsActive", "IsBaseVariantOnlyActive",
        "IsCollectedActive", "IsCustomizableOnlyActive",
        "IsFirstAcquisitionBonusOnlyActive", "IsSearchInProgress", "IsStoredOnlyActive",
        "IsUncollectedActive", "RunSearch", "SetAllInFilterTagGroup",
        "SetAllowedIndoors", "SetAllowedOutdoors", "SetAutoUpdateOnParamChanges",
        "SetBaseVariantOnly", "SetCollected", "SetCustomizableOnly",
        "SetEditorModeContext", "SetFilterTagStatus", "SetFilteredCategoryID",
        "SetFilteredSubcategoryID", "SetFirstAcquisitionBonusOnly",
        "SetResultsUpdatedCallback", "SetSearchText", "SetSortType", "SetStoredOnly",
        "SetUncollected", "ToggleAllowedIndoors", "ToggleAllowedOutdoors",
        "ToggleBaseVariantOnly", "ToggleCollected", "ToggleCustomizableOnly",
        "ToggleFilterTag", "ToggleFirstAcquisitionBonusOnly", "ToggleStoredOnly",
        "ToggleUncollected"
    ];

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "CreateCatalogSearcher", "DeletePreviewCartDecor", "DestroyEntry",
                     "GetAllFilterTagGroups", "GetAllVariantInfosForEntry", "GetBundleInfo",
                     "GetCartSizeLimit", "GetCatalogCategoryAndSubcategoryNames",
                     "GetCatalogCategoryInfo", "GetCatalogEntryInfo", "GetCatalogEntryInfoByItem",
                     "GetCatalogEntryInfoByRecordID", "GetCatalogEntryRefundTimeStampByRecordID",
                     "GetCatalogEntryVariantInfo", "GetCatalogSubcategoryInfo",
                     "GetDecorMaxOwnedCount", "GetDecorTotalOwnedCount",
                     "GetDestroyableInstanceCount", "GetFeaturedBundles",
                     "GetFeaturedSmallProducts", "GetMarketInfoForDecor", "GetMaxHouseLevel",
                     "HasFeaturedEntries", "HousingMarketActionAddToCart",
                     "HousingMarketActionClearCart", "HousingMarketActionRemoveFromCart",
                     "HousingMarketActionViewBundle", "HousingMarketActionViewInStore",
                     "IsPreviewCartItemShown", "PromotePreviewDecor",
                     "RequestHousingMarketInfoRefresh", "RequestHousingMarketRefundInfo",
                     "SearchCatalogCategories", "SearchCatalogSubcategories",
                     "SetPreviewCartItemShown"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, NamespaceCallback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_HousingCatalog");
    }

    private static int DispatchNamespace(lua_State state)
    {
        var housing = LuaBindings.GetRuntime(state).Housing;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "CreateCatalogSearcher":
                break;
            case "GetAllFilterTagGroups":
            case "GetAllVariantInfosForEntry":
            case "GetFeaturedBundles":
            case "GetFeaturedSmallProducts":
            case "SearchCatalogSubcategories":
                lua_newtable(state);
                return 1;
            case "SearchCatalogCategories":
            {
                const string usage =
                    "Usage: local categoryIDs = " +
                    "C_HousingCatalog.SearchCatalogCategories(searchParams)";
                if (lua_type(state, 1) != LUA_TTABLE)
                    return luaL_error(state, usage);
                var categories = housing.CatalogCategories.Values
                    .OrderBy(category => category.OrderIndex)
                    .ThenBy(category => category.Id)
                    .ToArray();
                lua_createtable(state, categories.Length, 0);
                for (var index = 0; index < categories.Length; index++)
                {
                    lua_pushinteger(state, categories[index].Id);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "GetCartSizeLimit":
            case "GetDecorMaxOwnedCount":
            case "GetDestroyableInstanceCount":
            case "GetMaxHouseLevel":
                lua_pushinteger(state, 0);
                return 1;
            case "GetDecorTotalOwnedCount":
                lua_pushinteger(state, 0);
                lua_pushinteger(state, 0);
                return 2;
            case "GetCatalogCategoryAndSubcategoryNames":
                lua_pushstring(state, string.Empty);
                lua_pushstring(state, string.Empty);
                return 2;
            case "HasFeaturedEntries":
            case "IsPreviewCartItemShown":
            case "PromotePreviewDecor":
                lua_pushboolean(state, 0);
                return 1;
            case "GetBundleInfo":
            case "GetCatalogEntryInfo":
            case "GetCatalogEntryInfoByItem":
            case "GetCatalogEntryInfoByRecordID":
            case "GetCatalogEntryRefundTimeStampByRecordID":
            case "GetCatalogEntryVariantInfo":
            case "GetCatalogSubcategoryInfo":
            case "GetMarketInfoForDecor":
                return 0;
            case "GetCatalogCategoryInfo":
            {
                const string usage =
                    "Usage: local info = " +
                    "C_HousingCatalog.GetCatalogCategoryInfo(categoryID)";
                if (!TryReadRequiredInt32(state, 1, out var categoryId))
                    return luaL_error(state, usage);
                if (!housing.CatalogCategories.TryGetValue(categoryId, out var category))
                    return 0;
                PushCategoryInfo(state, category);
                return 1;
            }
            default:
                return 0;
        }

        lua_newtable(state);
        foreach (var method in SearcherMethods)
        {
            lua_pushstring(state, method);
            lua_pushcclosure(state, SearcherCallback, 1);
            lua_setfield(state, -2, method);
        }

        SetBooleanField(state, -1, "__allowedIndoors", true);
        SetBooleanField(state, -1, "__allowedOutdoors", true);
        return 1;
    }

    private static void PushCategoryInfo(
        lua_State state,
        WowHousingCatalogCategoryState category)
    {
        lua_createtable(state, 0, 6);
        SetIntegerField(state, "ID", category.Id);
        SetIntegerField(state, "orderIndex", category.OrderIndex);
        SetOptionalStringField(state, "name", category.Name);
        SetOptionalStringField(state, "icon", category.Icon);
        lua_createtable(state, category.SubcategoryIds.Count, 0);
        for (var index = 0; index < category.SubcategoryIds.Count; index++)
        {
            lua_pushinteger(state, category.SubcategoryIds[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "subcategoryIDs");
        SetBooleanField(state, -1, "anyStoredEntries", category.AnyStoredEntries);
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

    private static void SetIntegerField(lua_State state, string field, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalStringField(
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

    private static int DispatchSearcher(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetAllSearchItems":
            case "GetCatalogSearchResults":
                lua_newtable(state);
                return 1;
            case "GetNumSearchItems":
            case "GetSearchCount":
                lua_pushinteger(state, 0);
                return 1;
            case "GetSortType":
                return PushFieldOrInteger(state, "__sortType", 0);
            case "GetEditorModeContext":
                return PushField(state, "__editorMode");
            case "GetFilteredCategoryID":
                return PushField(state, "__categoryID");
            case "GetFilteredSubcategoryID":
                return PushField(state, "__subcategoryID");
            case "GetSearchText":
                return PushField(state, "__searchText");
            case "GetFilterTagStatus":
            case "IsSearchInProgress":
                lua_pushboolean(state, 0);
                return 1;
            case "IsAllowedIndoorsActive":
                return PushBooleanField(state, "__allowedIndoors");
            case "IsAllowedOutdoorsActive":
                return PushBooleanField(state, "__allowedOutdoors");
            case "IsBaseVariantOnlyActive":
                return PushBooleanField(state, "__baseVariantOnly");
            case "IsCollectedActive":
                return PushBooleanField(state, "__collected");
            case "IsCustomizableOnlyActive":
                return PushBooleanField(state, "__customizableOnly");
            case "IsFirstAcquisitionBonusOnlyActive":
                return PushBooleanField(state, "__firstAcquisitionBonusOnly");
            case "IsStoredOnlyActive":
                return PushBooleanField(state, "__storedOnly");
            case "IsUncollectedActive":
                return PushBooleanField(state, "__uncollected");
            case "SetAllowedIndoors":
                return SetBooleanArgument(state, "__allowedIndoors");
            case "SetAllowedOutdoors":
                return SetBooleanArgument(state, "__allowedOutdoors");
            case "SetAutoUpdateOnParamChanges":
                return SetBooleanArgument(state, "__autoUpdate");
            case "SetBaseVariantOnly":
                return SetBooleanArgument(state, "__baseVariantOnly");
            case "SetCollected":
                return SetBooleanArgument(state, "__collected");
            case "SetCustomizableOnly":
                return SetBooleanArgument(state, "__customizableOnly");
            case "SetFirstAcquisitionBonusOnly":
                return SetBooleanArgument(state, "__firstAcquisitionBonusOnly");
            case "SetStoredOnly":
                return SetBooleanArgument(state, "__storedOnly");
            case "SetUncollected":
                return SetBooleanArgument(state, "__uncollected");
            case "SetEditorModeContext":
                return SetArgument(state, "__editorMode");
            case "SetFilteredCategoryID":
                return SetArgument(state, "__categoryID");
            case "SetFilteredSubcategoryID":
                return SetArgument(state, "__subcategoryID");
            case "SetResultsUpdatedCallback":
                return SetArgument(state, "__resultsUpdatedCallback");
            case "SetSearchText":
                return SetArgument(state, "__searchText");
            case "SetSortType":
                return SetArgument(state, "__sortType");
            case "ToggleAllowedIndoors":
                return ToggleBooleanField(state, "__allowedIndoors");
            case "ToggleAllowedOutdoors":
                return ToggleBooleanField(state, "__allowedOutdoors");
            case "ToggleBaseVariantOnly":
                return ToggleBooleanField(state, "__baseVariantOnly");
            case "ToggleCollected":
                return ToggleBooleanField(state, "__collected");
            case "ToggleCustomizableOnly":
                return ToggleBooleanField(state, "__customizableOnly");
            case "ToggleFirstAcquisitionBonusOnly":
                return ToggleBooleanField(state, "__firstAcquisitionBonusOnly");
            case "ToggleStoredOnly":
                return ToggleBooleanField(state, "__storedOnly");
            case "ToggleUncollected":
                return ToggleBooleanField(state, "__uncollected");
            case "RunSearch":
                InvokeResultsUpdatedCallback(state);
                return 0;
            default:
                return 0;
        }
    }

    private static int SetArgument(lua_State state, string field)
    {
        if (lua_gettop(state) >= 2)
        {
            lua_pushvalue(state, 2);
            lua_setfield(state, 1, field);
        }
        return 0;
    }

    private static int SetBooleanArgument(lua_State state, string field)
    {
        SetBooleanField(state, 1, field, lua_toboolean(state, 2) != 0);
        return 0;
    }

    private static int ToggleBooleanField(lua_State state, string field)
    {
        lua_getfield(state, 1, field);
        var value = lua_toboolean(state, -1) == 0;
        lua_pop(state, 1);
        SetBooleanField(state, 1, field, value);
        return 0;
    }

    private static int PushBooleanField(lua_State state, string field)
    {
        lua_getfield(state, 1, field);
        var value = lua_toboolean(state, -1) != 0;
        lua_pop(state, 1);
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static int PushField(lua_State state, string field)
    {
        lua_getfield(state, 1, field);
        return 1;
    }

    private static int PushFieldOrInteger(lua_State state, string field, int fallback)
    {
        lua_getfield(state, 1, field);
        if (lua_type(state, -1) != LUA_TNIL)
            return 1;
        lua_pop(state, 1);
        lua_pushinteger(state, fallback);
        return 1;
    }

    private static void SetBooleanField(lua_State state, int tableIndex, string field, bool value)
    {
        var targetIndex = tableIndex < 0 ? tableIndex - 1 : tableIndex;
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, targetIndex, field);
    }

    private static void InvokeResultsUpdatedCallback(lua_State state)
    {
        lua_getfield(state, 1, "__resultsUpdatedCallback");
        if (lua_type(state, -1) == LUA_TFUNCTION)
        {
            if (lua_pcall(state, 0, 0, 0) != 0)
                lua_pop(state, 1);
        }
        else
        {
            lua_pop(state, 1);
        }
    }
}
