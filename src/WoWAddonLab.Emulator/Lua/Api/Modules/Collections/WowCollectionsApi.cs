using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCollectionsApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        RegisterNamespace(
            state,
            "C_TransmogCollection",
            "ClearNewAppearance", "ClearSearch", "EndSearch", "GetAllFactionsShown",
            "GetAllRacesShown", "GetAllAppearanceSources", "GetAppearanceCameraID", "GetAppearanceCameraIDBySource",
            "GetAppearanceSourceInfo", "GetAppearanceSources", "GetCategoryAppearances", "GetCategoryCollectedCount",
            "GetCategoryForItem", "GetCategoryInfo", "GetCategoryTotal", "GetItemInfo",
            "GetClassFilter", "GetCollectedShown", "GetFallbackWeaponAppearance",
            "GetFilteredCategoryCollectedCount", "GetFilteredCategoryTotal",
            "GetCustomSets", "GetIllusionInfo", "GetIllusions", "GetIsAppearanceFavorite",
            "GetLatestAppearance", "GetNumMaxCustomSets", "GetNumTransmogSources", "GetSourceIcon", "GetSourceInfo",
            "GetValidAppearanceSourcesForClass",
            "GetUncollectedShown", "IsNewAppearance", "IsSearchDBLoading",
            "IsSearchInProgress", "IsSourceTypeFilterChecked", "IsUsingDefaultFilters",
            "IsValidTransmogSource", "PlayerCanCollectSource", "SearchProgress", "SearchSize", "SetAllFactionsShown",
            "SetAllRacesShown", "SetAllSourceTypeFilters", "SetClassFilter",
            "SetCollectedShown", "SetDefaultFilters", "SetIsAppearanceFavorite", "SetSearch",
            "SetSearchAndFilterCategory", "SetSourceTypeFilter", "SetUncollectedShown",
            "UpdateUsableAppearances");
        RegisterNamespace(
            state,
            "C_TransmogSets",
            "ClearLatestSource", "ClearSetNewSourcesForSlot", "GetAllSets",
            "GetAllSourceIDs", "GetBaseSetID",
            "GetAvailableSets", "GetBaseSetsFilter", "GetCameraIDs", "GetFilteredBaseSetsCounts",
            "GetIsFavorite", "GetLatestSource", "GetSetInfo", "GetSetNewSources", "GetSetsContainingSourceID",
            "GetSetsFilter", "GetSourcesForSlot", "GetTransmogSetsClassFilter",
            "GetValidClassForSet", "GetVariantSets", "HasAvailableSets", "IsSetVisible",
            "IsUsingDefaultBaseSetsFilters", "IsUsingDefaultSetsFilters",
            "SetBaseSetsFilter", "SetDefaultBaseSetsFilters", "SetHasNewSources",
            "SetHasNewSourcesForSlot", "SetIsFavorite", "SetSetsFilter",
            "SetTransmogSetsClassFilter");
        RegisterNamespace(
            state,
            "C_Heirloom",
            "CanHeirloomUpgradeFromPending", "CreateHeirloom", "GetClassAndSpecFilters",
            "GetCollectedHeirloomFilter", "GetHeirloomInfo",
            "GetHeirloomItemIDs", "GetHeirloomItemIDFromDisplayedIndex", "GetHeirloomLink",
            "GetHeirloomMaxUpgradeLevel", "GetHeirloomSourceFilter",
            "GetNumDisplayedHeirlooms", "GetUncollectedHeirloomFilter",
            "IsPendingHeirloomUpgrade", "PlayerHasHeirloom", "SetClassAndSpecFilters",
            "SetCollectedHeirloomFilter", "SetHeirloomSourceFilter", "SetSearch",
            "SetUncollectedHeirloomFilter", "UpgradeHeirloom");
        RegisterNamespace(
            state,
            "C_WarbandScene",
            "GetRandomEntryID", "GetWarbandSceneEntry", "HasWarbandScene",
            "IsFavorite", "SearchWarbandSceneEntries", "SetFavorite");
        RegisterNamespace(
            state,
            "C_HeirloomInfo",
            "IsUsingDefaultFilters", "SetDefaultFilters");
        RegisterNamespace(
            state,
            "C_SpellBook",
            "GetNumSpellBookSkillLines", "GetSpellBookSkillLineInfo", "HasPetSpells",
            "IsSpellInSpellBook", "IsSpellKnown");
    }

    private static void RegisterNamespace(lua_State state, string name, params string[] functions)
    {
        lua_newtable(state);
        foreach (var function in functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, name);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetLatestAppearance":
            case "GetAppearanceCameraID":
            case "GetAppearanceCameraIDBySource":
            case "GetIllusionInfo":
            case "GetCameraIDs":
            case "GetValidClassForSet":
            case "GetHeirloomInfo":
            case "GetHeirloomItemIDFromDisplayedIndex":
            case "GetHeirloomLink":
            case "GetSpellBookSkillLineInfo":
            case "HasPetSpells":
            case "GetWarbandSceneEntry":
                return 0;
            case "GetSourceInfo":
            {
                var sourceId = RequiredInt32(
                    state,
                    1,
                    "Usage: local sourceInfo = C_TransmogCollection.GetSourceInfo(sourceID)");
                if (runtime.TransmogAppearanceProvider?.TryGetSource(sourceId, out var definition) != true)
                    return 0;
                PushAppearanceSourceInfo(state, runtime, definition);
                return 1;
            }
            case "GetAppearanceSourceInfo":
            {
                var sourceId = RequiredInt32(
                    state,
                    1,
                    "Usage: local info = C_TransmogCollection.GetAppearanceSourceInfo(itemModifiedAppearanceID)");
                if (runtime.TransmogAppearanceProvider?.TryGetSource(sourceId, out var definition) != true)
                    return 0;
                PushAppearanceSourceData(state, runtime, definition);
                return 1;
            }
            case "GetAllAppearanceSources":
            {
                var visualId = RequiredInt32(
                    state,
                    1,
                    "Usage: local itemModifiedAppearanceIDs = C_TransmogCollection.GetAllAppearanceSources(itemAppearanceID)");
                var sources = runtime.TransmogAppearanceProvider?.GetSourcesByVisual(visualId) ?? [];
                return PushIntegerList(state, sources.Select(value => value.SourceId).ToArray());
            }
            case "GetAppearanceSources":
            {
                var visualId = RequiredInt32(
                    state,
                    1,
                    "Usage: local sources = C_TransmogCollection.GetAppearanceSources(appearanceID [, categoryType, transmogLocation])");
                var provider = runtime.TransmogAppearanceProvider;
                if (provider is null)
                    return 0;
                var sources = provider.GetSourcesByVisual(visualId);
                lua_newtable(state);
                for (var index = 0; index < sources.Count; index++)
                {
                    PushAppearanceSourceInfo(state, runtime, sources[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "GetValidAppearanceSourcesForClass":
            {
                const string usage =
                    "Usage: local sources = C_TransmogCollection.GetValidAppearanceSourcesForClass(appearanceID, classID [, categoryType, transmogLocation])";
                var visualId = RequiredInt32(state, 1, usage);
                var classId = RequiredInt32(state, 2, usage);
                var provider = runtime.TransmogAppearanceProvider;
                if (provider is null)
                    return 0;
                var sources = provider.GetSourcesByVisual(visualId)
                    .Where(value => IsClassAllowed(value, classId))
                    .ToArray();
                lua_newtable(state);
                for (var index = 0; index < sources.Length; index++)
                {
                    PushAppearanceSourceInfo(state, runtime, sources[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "GetCategoryAppearances":
            {
                var categoryId = RequiredInt32(
                    state,
                    1,
                    "Usage: local appearances = C_TransmogCollection.GetCategoryAppearances(category [, transmogLocation])");
                if (categoryId is < 0 or > 29)
                    return luaL_error(
                        state,
                        "Usage: local appearances = C_TransmogCollection.GetCategoryAppearances(category [, transmogLocation])");
                var sources = runtime.TransmogAppearanceProvider?.GetSourcesByCategory(categoryId) ?? [];
                var appearances = sources
                    .GroupBy(value => value.VisualId)
                    .Select(group => group.ToArray())
                    .ToArray();
                lua_newtable(state);
                for (var index = 0; index < appearances.Length; index++)
                {
                    PushCategoryAppearanceInfo(state, runtime, appearances[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "GetCategoryForItem":
            {
                var sourceId = RequiredInt32(
                    state,
                    1,
                    "Usage: local collectionCategory = C_TransmogCollection.GetCategoryForItem(itemModifiedAppearanceID)");
                if (runtime.TransmogAppearanceProvider?.TryGetSource(sourceId, out var definition) != true)
                    return 0;
                lua_pushinteger(state, definition.CategoryId);
                return 1;
            }
            case "GetCategoryTotal":
            case "GetCategoryCollectedCount":
            {
                var categoryId = RequiredInt32(
                    state,
                    1,
                    $"Usage: local count = C_TransmogCollection.{operation}(category)");
                var sources = runtime.TransmogAppearanceProvider?.GetSourcesByCategory(categoryId) ?? [];
                var count = operation == "GetCategoryCollectedCount"
                    ? sources.Where(value => runtime.TransmogSets.CollectedSourceIds.Contains(value.SourceId))
                        .Select(value => value.VisualId)
                        .Distinct()
                        .Count()
                    : sources.Select(value => value.VisualId).Distinct().Count();
                lua_pushinteger(state, count);
                return 1;
            }
            case "GetItemInfo":
            {
                const string usage =
                    "Usage: local itemAppearanceID, itemModifiedAppearanceID = C_TransmogCollection.GetItemInfo(itemInfo)";
                if (!TryReadItemId(state, 1, out var itemId))
                    return luaL_error(state, usage);
                int? itemModId = null;
                if (lua_isnumber(state, 2) != 0)
                    itemModId = RequiredInt32(state, 2, usage);
                if (runtime.TransmogAppearanceProvider?.TryGetSourceForItem(
                        itemId,
                        itemModId,
                        out var definition) != true)
                    return 0;
                lua_pushnumber(state, definition.VisualId);
                lua_pushnumber(state, definition.SourceId);
                return 2;
            }
            case "PlayerCanCollectSource":
            {
                var sourceId = RequiredInt32(
                    state,
                    1,
                    "Usage: local hasItemData, canCollect = C_TransmogCollection.PlayerCanCollectSource(sourceID)");
                WowAppearanceSourceDefinition? definition = null;
                var found = runtime.TransmogAppearanceProvider?.TryGetSource(
                    sourceId,
                    out definition!) == true;
                lua_pushboolean(state, found ? 1 : 0);
                lua_pushboolean(
                    state,
                    found && definition is not null && IsClassAllowed(runtime, definition) ? 1 : 0);
                return 2;
            }
            case "GetAllSets":
                return PushSetList(
                    state,
                    runtime,
                    runtime.TransmogSetProvider?.Sets ?? []);
            case "GetAllSourceIDs":
            {
                var setId = RequiredInt32(
                    state,
                    1,
                    "Usage: local sources = C_TransmogSets.GetAllSourceIDs(transmogSetID)");
                var provider = runtime.TransmogSetProvider;
                if (provider is null || !provider.TryGetSet(setId, out _))
                    return 0;
                return PushIntegerList(state, provider.GetSourceIds(setId));
            }
            case "GetBaseSetID":
            {
                var setId = RequiredInt32(
                    state,
                    1,
                    "Usage: local baseTransmogSetID = C_TransmogSets.GetBaseSetID(transmogSetID)");
                if (runtime.TransmogSetProvider?.TryGetSet(setId, out var definition) != true)
                    return 0;
                lua_pushnumber(state, definition.BaseSetId ?? definition.SetId);
                return 1;
            }
            case "GetSetInfo":
            {
                var setId = RequiredInt32(
                    state,
                    1,
                    "Usage: local set = C_TransmogSets.GetSetInfo(transmogSetID)");
                if (runtime.TransmogSetProvider?.TryGetSet(setId, out var definition) != true)
                    return 0;
                PushSetInfo(state, runtime, definition);
                return 1;
            }
            case "GetVariantSets":
            {
                var setId = RequiredInt32(
                    state,
                    1,
                    "Usage: local sets = C_TransmogSets.GetVariantSets(transmogSetID)");
                var provider = runtime.TransmogSetProvider;
                if (provider is null || !provider.TryGetSet(setId, out _))
                    return 0;
                return PushSetList(state, runtime, provider.GetVariantSets(setId));
            }
            case "GetSetsContainingSourceID":
            {
                var sourceId = RequiredInt32(
                    state,
                    1,
                    "Usage: local setIDs = C_TransmogSets.GetSetsContainingSourceID(sourceID)");
                var provider = runtime.TransmogSetProvider;
                if (provider is null)
                    return 0;
                return PushIntegerList(state, provider.GetSetIdsContainingSource(sourceId));
            }
            case "GetIsFavorite":
            {
                var setId = RequiredInt32(
                    state,
                    1,
                    "Usage: local isFavorite, isGroupFavorite = C_TransmogSets.GetIsFavorite(transmogSetID)");
                if (runtime.TransmogSetProvider?.TryGetSet(setId, out var definition) != true)
                    return 0;
                lua_pushboolean(state, runtime.TransmogSets.FavoriteSetIds.Contains(setId) ? 1 : 0);
                var baseSetId = definition.BaseSetId ?? definition.SetId;
                var group = runtime.TransmogSetProvider.GetVariantSets(baseSetId);
                var groupFavorite = runtime.TransmogSets.FavoriteSetIds.Contains(baseSetId) &&
                                    group.All(value => runtime.TransmogSets.FavoriteSetIds.Contains(value.SetId));
                lua_pushboolean(state, groupFavorite ? 1 : 0);
                return 2;
            }
            case "SetIsFavorite":
            {
                var setId = RequiredInt32(
                    state,
                    1,
                    "Usage: C_TransmogSets.SetIsFavorite(transmogSetID, isFavorite)");
                if (lua_type(state, 2) != LUA_TBOOLEAN)
                    return luaL_error(state, "Usage: C_TransmogSets.SetIsFavorite(transmogSetID, isFavorite)");
                if (lua_toboolean(state, 2) != 0)
                    runtime.TransmogSets.FavoriteSetIds.Add(setId);
                else
                    runtime.TransmogSets.FavoriteSetIds.Remove(setId);
                return 0;
            }
            case "GetNumTransmogSources":
            case "GetNumSpellBookSkillLines":
            case "GetNumDisplayedHeirlooms":
            case "GetLatestSource":
            case "GetRandomEntryID":
            case "GetFallbackWeaponAppearance":
            case "GetFilteredCategoryCollectedCount":
            case "GetFilteredCategoryTotal":
            case "SearchProgress":
            case "SearchSize":
            case "GetHeirloomMaxUpgradeLevel":
                lua_pushinteger(state, 0);
                return 1;
            case "GetSourceIcon":
            {
                var sourceId = RequiredInt32(
                    state,
                    1,
                    "Usage: local icon = C_TransmogCollection.GetSourceIcon(itemModifiedAppearanceID)");
                if (runtime.TransmogAppearanceProvider?.TryGetSource(sourceId, out var definition) != true)
                    return 0;
                lua_pushnumber(state, definition.IconFileDataId);
                return 1;
            }
            case "GetNumMaxCustomSets":
                lua_pushinteger(state, 25);
                return 1;
            case "GetClassFilter":
                lua_pushinteger(state, 1);
                return 1;
            case "SearchWarbandSceneEntries":
            case "GetAvailableSets":
            case "GetCustomSets":
            case "GetHeirloomItemIDs":
            case "GetIllusions":
            case "GetSetNewSources":
            case "GetSourcesForSlot":
                lua_newtable(state);
                return 1;
            case "GetFilteredBaseSetsCounts":
                lua_pushinteger(state, 0);
                lua_pushinteger(state, 0);
                return 2;
            case "ClearSearch":
                lua_pushboolean(state, 1);
                return 1;
            case "GetCategoryInfo":
            {
                var category = lua_type(state, 1) == LUA_TNUMBER
                    ? (int)lua_tonumber(state, 1)
                    : 0;
                if (category < 1 || category > 29)
                    return 0;
                var names = new[]
                {
                    string.Empty, "Head", "Shoulder", "Back", "Chest", "Shirt",
                    "Tabard", "Wrist", "Hands", "Waist", "Legs", "Feet", "Wand",
                    "One-Handed Axes", "One-Handed Swords", "One-Handed Maces",
                    "Daggers", "Fist Weapons", "Shields", "Held In Off-hand",
                    "Two-Handed Axes", "Two-Handed Swords", "Two-Handed Maces",
                    "Staves", "Polearms", "Bows", "Guns", "Crossbows",
                    "Warglaives", "Paired"
                };
                var isWeapon = category >= 12;
                lua_pushstring(state, names[category]);
                lua_pushboolean(state, isWeapon ? 1 : 0);
                lua_pushboolean(state, isWeapon ? 1 : 0);
                lua_pushboolean(state, isWeapon ? 1 : 0);
                lua_pushboolean(state, isWeapon ? 1 : 0);
                lua_pushboolean(state, category is >= 25 and <= 27 ? 1 : 0);
                return 6;
            }
            case "GetClassAndSpecFilters":
                lua_pushinteger(state, 0);
                lua_pushinteger(state, 0);
                return 2;
            case "GetTransmogSetsClassFilter":
                lua_pushinteger(state, 1);
                return 1;
            case "GetCollectedShown":
            case "GetUncollectedShown":
            case "GetAllFactionsShown":
            case "GetAllRacesShown":
            case "GetBaseSetsFilter":
            case "GetSetsFilter":
            case "GetCollectedHeirloomFilter":
            case "GetHeirloomSourceFilter":
            case "GetUncollectedHeirloomFilter":
            case "IsSourceTypeFilterChecked":
            case "IsUsingDefaultFilters":
            case "IsUsingDefaultBaseSetsFilters":
            case "IsUsingDefaultSetsFilters":
                lua_pushboolean(state, 1);
                return 1;
            case "IsValidTransmogSource":
            case "IsNewAppearance":
            case "IsSearchDBLoading":
            case "IsSearchInProgress":
            case "HasAvailableSets":
            case "IsSetVisible":
            case "CanHeirloomUpgradeFromPending":
            case "IsPendingHeirloomUpgrade":
            case "PlayerHasHeirloom":
            case "HasWarbandScene":
            case "IsFavorite":
                lua_pushboolean(state, 0);
                return 1;
            case "GetIsAppearanceFavorite":
            {
                var visualId = RequiredInt32(
                    state,
                    1,
                    "Usage: local isFavorite = C_TransmogCollection.GetIsAppearanceFavorite(itemAppearanceID)");
                lua_pushboolean(
                    state,
                    runtime.TransmogSets.FavoriteVisualIds.Contains(visualId) ? 1 : 0);
                return 1;
            }
            case "SetIsAppearanceFavorite":
            {
                var visualId = RequiredInt32(
                    state,
                    1,
                    "Usage: C_TransmogCollection.SetIsAppearanceFavorite(itemAppearanceID, isFavorite)");
                if (lua_type(state, 2) != LUA_TBOOLEAN)
                    return luaL_error(
                        state,
                        "Usage: C_TransmogCollection.SetIsAppearanceFavorite(itemAppearanceID, isFavorite)");
                if (lua_toboolean(state, 2) != 0)
                    runtime.TransmogSets.FavoriteVisualIds.Add(visualId);
                else
                    runtime.TransmogSets.FavoriteVisualIds.Remove(visualId);
                return 0;
            }
            case "IsSpellInSpellBook":
            case "IsSpellKnown":
            {
                const string usage =
                    "Usage: local isKnown = C_SpellBook.IsSpellKnown(spellID [, spellBank])";
                if (lua_isnumber(state, 1) == 0)
                    return luaL_error(state, usage);
                var value = lua_tonumber(state, 1);
                if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
                    return luaL_error(state, usage);
                lua_pushboolean(
                    state,
                    runtime.Spells.KnownSpellIds.Contains((int)value) ? 1 : 0);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static int PushSetList(
        lua_State state,
        LuaRuntime runtime,
        IReadOnlyList<WowTransmogSetDefinition> definitions)
    {
        lua_newtable(state);
        for (var index = 0; index < definitions.Count; index++)
        {
            PushSetInfo(state, runtime, definitions[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int PushIntegerList(
        lua_State state,
        IReadOnlyList<int> values)
    {
        lua_newtable(state);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushnumber(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static void PushSetInfo(
        lua_State state,
        LuaRuntime runtime,
        WowTransmogSetDefinition definition)
    {
        lua_newtable(state);
        SetNumber(state, "setID", definition.SetId);
        SetString(state, "name", definition.Name);
        SetOptionalNumber(state, "baseSetID", definition.BaseSetId);
        SetOptionalString(state, "description", definition.Description);
        SetOptionalString(state, "label", definition.Label);
        SetNumber(state, "expansionID", definition.ExpansionId);
        SetNumber(state, "patchID", definition.PatchId);
        SetNumber(state, "uiOrder", definition.UiOrder);
        SetNumber(state, "classMask", definition.ClassMask);
        SetBoolean(state, "hiddenUntilCollected", definition.HiddenUntilCollected);
        SetOptionalString(state, "requiredFaction", definition.RequiredFaction);
        SetBoolean(state, "collected", runtime.TransmogSets.CollectedSetIds.Contains(definition.SetId));
        SetBoolean(state, "favorite", runtime.TransmogSets.FavoriteSetIds.Contains(definition.SetId));
        SetBoolean(state, "limitedTimeSet", definition.LimitedTimeSet);
        var classId = runtime.Units.Player.ClassId;
        var validForCharacter = definition.ClassMask == 0 ||
                                classId is > 0 and <= 32 &&
                                (definition.ClassMask & (1 << (classId - 1))) != 0;
        SetBoolean(state, "validForCharacter", validForCharacter);
        SetBoolean(state, "grantAsPrecedingVariant", definition.GrantAsPrecedingVariant);
    }

    private static void PushAppearanceSourceInfo(
        lua_State state,
        LuaRuntime runtime,
        WowAppearanceSourceDefinition definition)
    {
        var classAllowed = IsClassAllowed(runtime, definition);
        var canDisplay = definition.CategoryId != 0 && classAllowed;

        lua_newtable(state);
        SetNumber(state, "visualID", definition.VisualId);
        SetNumber(state, "sourceID", definition.SourceId);
        SetBoolean(
            state,
            "isCollected",
            runtime.TransmogSets.CollectedSourceIds.Contains(definition.SourceId));
        SetNumber(state, "itemID", definition.ItemId);
        SetNumber(state, "itemModID", definition.ItemModId);
        SetNumber(state, "invType", definition.InventoryType);
        SetNumber(state, "categoryID", definition.CategoryId);
        SetBoolean(state, "playerCanCollect", classAllowed);
        SetBoolean(state, "isValidSourceForPlayer", classAllowed);
        SetBoolean(state, "canDisplayOnPlayer", canDisplay);
        SetOptionalNumber(state, "inventorySlot", definition.InventorySlot);
        SetOptionalNumber(state, "sourceType", definition.SourceType);
        SetOptionalString(state, "name", definition.Name);
        SetOptionalNumber(state, "quality", definition.Quality);
        SetOptionalString(state, "useError", null);
        SetOptionalNumber(state, "useErrorType", null);
        SetOptionalBoolean(
            state,
            "meetsTransmogPlayerCondition",
            definition.MeetsTransmogPlayerCondition);
        SetOptionalBoolean(state, "isHideVisual", definition.IsHideVisual);
    }

    private static void PushAppearanceSourceData(
        lua_State state,
        LuaRuntime runtime,
        WowAppearanceSourceDefinition definition)
    {
        var itemLink = ItemLink(definition);
        lua_newtable(state);
        SetNumber(state, "category", definition.CategoryId);
        SetNumber(state, "itemAppearanceID", definition.VisualId);
        SetBoolean(state, "canHaveIllusion", definition.CategoryId >= 12);
        SetNumber(state, "icon", definition.IconFileDataId);
        SetBoolean(
            state,
            "isCollected",
            runtime.TransmogSets.CollectedSourceIds.Contains(definition.SourceId));
        SetString(state, "itemLink", itemLink);
        SetString(state, "transmoglink", itemLink);
        SetOptionalNumber(state, "sourceType", definition.SourceType);
        SetNumber(state, "itemSubclass", definition.ItemSubclass);
        SetBoolean(state, "ignoreModelAttachmentChecksForIllusion", false);
    }

    private static void PushCategoryAppearanceInfo(
        lua_State state,
        LuaRuntime runtime,
        IReadOnlyList<WowAppearanceSourceDefinition> sources)
    {
        var definition = sources.First();
        var classAllowed = sources.Any(value => IsClassAllowed(runtime, value));
        var collected = sources.Any(value =>
            runtime.TransmogSets.CollectedSourceIds.Contains(value.SourceId));
        var hasRequiredHoliday = sources.Any(value => value.RequiredTransmogHolidayId > 0);

        lua_newtable(state);
        SetNumber(state, "visualID", definition.VisualId);
        SetBoolean(state, "isCollected", collected);
        SetBoolean(
            state,
            "isFavorite",
            runtime.TransmogSets.FavoriteVisualIds.Contains(definition.VisualId));
        SetBoolean(state, "isHideVisual", sources.Any(value => value.IsHideVisual == true));
        SetBoolean(state, "canDisplayOnPlayer", classAllowed);
        SetNumber(state, "uiOrder", definition.UiOrder);
        SetNumber(state, "exclusions", 0);
        SetBoolean(state, "isUsable", classAllowed);
        SetBoolean(state, "hasRequiredHoliday", hasRequiredHoliday);
        SetBoolean(state, "hasActiveRequiredHoliday", false);
        SetOptionalBoolean(state, "alwaysShowItem", null);
    }

    private static bool IsClassAllowed(
        LuaRuntime runtime,
        WowAppearanceSourceDefinition definition) =>
        IsClassAllowed(definition, runtime.Units.Player.ClassId);

    private static bool IsClassAllowed(
        WowAppearanceSourceDefinition definition,
        int classId)
    {
        var classMask = definition.AllowableClassMask;
        return classMask is 0 or -1 ||
               classId is > 0 and <= 32 &&
               (classMask & (1 << (classId - 1))) != 0;
    }

    private static string ItemLink(WowAppearanceSourceDefinition definition) =>
        $"|Hitem:{definition.ItemId}::::::::|h[{definition.Name ?? string.Empty}]|h";

    private static bool TryReadItemId(lua_State state, int index, out int itemId)
    {
        itemId = 0;
        if (lua_isnumber(state, index) != 0)
        {
            var value = lua_tonumber(state, index);
            if (!double.IsFinite(value) || value < 0 || value > int.MaxValue)
                return false;
            itemId = (int)value;
            return true;
        }
        if (lua_type(state, index) != LUA_TSTRING)
            return false;

        var text = lua_tostring(state, index) ?? string.Empty;
        if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out itemId))
            return itemId >= 0;
        var marker = text.IndexOf("item:", StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
            return false;
        marker += 5;
        var end = marker;
        while (end < text.Length && char.IsDigit(text[end]))
            end++;
        return end > marker &&
               int.TryParse(
                   text.AsSpan(marker, end - marker),
                   NumberStyles.None,
                   CultureInfo.InvariantCulture,
                   out itemId);
    }

    private static int RequiredInt32(lua_State state, int index, string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return RaiseArgumentError(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return RaiseArgumentError(state, usage);
        return unchecked((int)value);
    }

    private static int RaiseArgumentError(lua_State state, string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static void SetNumber(lua_State state, string field, int value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalNumber(lua_State state, string field, int? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushnumber(state, value.Value);
        lua_setfield(state, -2, field);
    }

    private static void SetString(lua_State state, string field, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalString(lua_State state, string field, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetBoolean(lua_State state, string field, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalBoolean(lua_State state, string field, bool? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushboolean(state, value.Value ? 1 : 0);
        lua_setfield(state, -2, field);
    }
}
