using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowItemApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] InventorySlotGlobalNames =
    [
        "INVTYPE_NON_EQUIP_IGNORE",
        "INVTYPE_HEAD",
        "INVTYPE_NECK",
        "INVTYPE_SHOULDER",
        "INVTYPE_BODY",
        "INVTYPE_CHEST",
        "INVTYPE_WAIST",
        "INVTYPE_LEGS",
        "INVTYPE_FEET",
        "INVTYPE_WRIST",
        "INVTYPE_HAND",
        "INVTYPE_FINGER",
        "INVTYPE_TRINKET",
        "INVTYPE_WEAPON",
        "INVTYPE_SHIELD",
        "INVTYPE_RANGED",
        "INVTYPE_CLOAK",
        "INVTYPE_2HWEAPON",
        "INVTYPE_BAG",
        "INVTYPE_TABARD",
        "INVTYPE_ROBE",
        "INVTYPE_WEAPONMAINHAND",
        "INVTYPE_WEAPONOFFHAND",
        "INVTYPE_HOLDABLE",
        "INVTYPE_AMMO",
        "INVTYPE_THROWN",
        "INVTYPE_RANGEDRIGHT",
        "INVTYPE_QUIVER",
        "INVTYPE_RELIC",
        "INVTYPE_PROFESSION_TOOL",
        "INVTYPE_PROFESSION_GEAR",
        "INDEX_EQUIPABLESPELL_OFFENSIVE_TYPE",
        "INDEX_EQUIPABLESPELL_UTILITY_TYPE",
        "INDEX_EQUIPABLESPELL_DEFENSIVE_TYPE",
        "INDEX_EQUIPABLESPELL_WEAPON_TYPE"
    ];

    private static readonly string[] Functions =
    [
        "DoesItemExist",
        "DoesItemExistByID",
        "GetAppliedItemTransmogInfo",
        "GetDetailedItemLevelInfo",
        "GetItemCooldown",
        "GetItemClassInfo",
        "GetItemCount",
        "GetItemFamily",
        "GetItemIconByID",
        "GetItemID",
        "GetItemIDForItemInfo",
        "GetItemInfo",
        "GetItemInfoInstant",
        "GetItemInventorySlotInfo",
        "GetItemLink",
        "GetItemLocation",
        "GetItemNameByID",
        "GetItemSpecInfo",
        "GetItemSubClassInfo"
    ];

    public override void Register(lua_State state)
    {
        RegisterItemQualityEnums(state);
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Item");
    }

    private static void RegisterItemQualityEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");

        lua_newtable(state);
        SetInteger(state, "Poor", 0);
        SetInteger(state, "Common", 1);
        SetInteger(state, "Uncommon", 2);
        SetInteger(state, "Rare", 3);
        SetInteger(state, "Epic", 4);
        SetInteger(state, "Legendary", 5);
        SetInteger(state, "Artifact", 6);
        SetInteger(state, "Heirloom", 7);
        SetInteger(state, "WoWToken", 8);
        lua_setfield(state, -2, "ItemQuality");

        lua_newtable(state);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 8);
        SetInteger(state, "NumValues", 9);
        lua_setfield(state, -2, "ItemQualityMeta");

        lua_pop(state, 1);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "DoesItemExist":
            {
                var location = RequiredItemLocation(
                    state,
                    "Usage: local itemExists = " +
                    "C_Item.DoesItemExist(emptiableItemLocation)");
                lua_pushboolean(
                    state,
                    DoesItemExist(runtime, location) ? 1 : 0);
                return 1;
            }
            case "GetAppliedItemTransmogInfo":
            {
                var location = RequiredItemLocation(
                    state,
                    "Usage: local info = " +
                    "C_Item.GetAppliedItemTransmogInfo(itemLoc)");
                if (runtime.Items.AppliedTransmogByLocation.TryGetValue(
                        location,
                        out var transmog))
                {
                    LuaBindings.PushItemTransmogInfo(state, transmog);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            case "GetDetailedItemLevelInfo":
            {
                var item = RequiredItem(
                    state,
                    runtime.Items,
                    "Usage: local actualItemLevel, previewLevel, " +
                    "sparseItemLevel = C_Item." +
                    "GetDetailedItemLevelInfo(itemInfo)");
                if (item is null)
                    return 0;
                lua_pushinteger(state, item.ItemLevel);
                PushOptionalInteger(state, item.PreviewItemLevel);
                lua_pushinteger(
                    state,
                    item.SparseItemLevel ?? item.ItemLevel);
                return 3;
            }
            case "GetItemCooldown":
            {
                var item = RequiredItem(
                    state,
                    runtime.Items,
                    "Usage: local startTimeSeconds, durationSeconds, " +
                    "enableCooldownTimer = C_Item.GetItemCooldown(itemInfo)");
                var cooldown = item?.Cooldown ??
                    new WowItemCooldownData(0, 0, false);
                lua_pushnumber(state, cooldown.StartTimeSeconds);
                lua_pushnumber(state, cooldown.DurationSeconds);
                lua_pushboolean(state, cooldown.EnableCooldownTimer ? 1 : 0);
                return 3;
            }
            case "GetItemClassInfo":
            {
                const string usage = "Usage: local result = C_Item.GetItemClassInfo(itemClassID)";
                var classId = RequiredInt32(state, 1, usage);
                runtime.Items.Classes.TryGetValue(classId, out var className);
                PushOptionalString(state, className);
                return 1;
            }
            case "GetItemFamily":
            {
                var item = RequiredItem(
                    state,
                    runtime.Items,
                    "Usage: local result = C_Item.GetItemFamily(itemInfo)");
                if (item is null)
                    return 0;
                lua_pushnumber(state, item.Family);
                return 1;
            }
            case "GetItemID":
            {
                var location = RequiredItemLocation(
                    state,
                    "Usage: local itemID = C_Item.GetItemID(itemLocation)");
                if (runtime.Items.LocationItemIds.TryGetValue(
                        location,
                        out var itemId))
                {
                    lua_pushinteger(state, itemId);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            case "DoesItemExistByID":
            {
                const string usage =
                    "Usage: local itemExists = " +
                    "C_Item.DoesItemExistByID(itemInfo)";
                var itemId = RequiredItemId(state, runtime.Items, usage);
                lua_pushboolean(state, itemId is > 0 ? 1 : 0);
                return 1;
            }
            case "GetItemCount":
                return GetItemCount(state, runtime);
            case "GetItemIconByID":
            {
                var item = RequiredItem(
                    state,
                    runtime.Items,
                    "Usage: local icon = C_Item.GetItemIconByID(itemInfo)");
                if (item is null)
                    lua_pushnil(state);
                else
                    lua_pushnumber(state, item.TextureFileId);
                return 1;
            }
            case "GetItemIDForItemInfo":
            {
                const string usage = "Usage: local itemID = C_Item.GetItemIDForItemInfo(itemInfo)";
                var itemId = RequiredItemId(state, runtime.Items, usage);
                lua_pushnumber(state, itemId ?? 0);
                return 1;
            }
            case "GetItemInfo":
            {
                var item = RequiredItem(
                    state,
                    runtime.Items,
                    "Usage: local itemName, itemLink, itemQuality, itemLevel, " +
                    "itemMinLevel, itemType, itemSubType, itemStackCount, " +
                    "itemEquipLoc, itemTexture, sellPrice, classID, subclassID, " +
                    "bindType, expansionID, setID, isCraftingReagent, " +
                    "itemDescription = C_Item.GetItemInfo(itemInfo)");
                if (item is null)
                    return 0;
                return PushItemInfo(state, item);
            }
            case "GetItemInfoInstant":
            {
                var item = RequiredItem(
                    state,
                    runtime.Items,
                    "Usage: local itemID, itemType, itemSubType, itemEquipLoc, " +
                    "icon, classID, subClassID = " +
                    "C_Item.GetItemInfoInstant(itemInfo)");
                if (item is null)
                    return 0;
                return PushItemInfoInstant(state, item);
            }
            case "GetItemInventorySlotInfo":
            {
                var slot = RequiredInventorySlot(
                    state,
                    "Usage: local result = " +
                    "C_Item.GetItemInventorySlotInfo(inventorySlot)");
                if (runtime.Items.InventorySlotNames.TryGetValue(slot, out var name))
                {
                    PushOptionalString(state, name);
                }
                else
                {
                    lua_getglobal(state, InventorySlotGlobalNames[slot]);
                    if (lua_type(state, -1) != LUA_TSTRING)
                    {
                        lua_pop(state, 1);
                        lua_pushnil(state);
                    }
                }
                return 1;
            }
            case "GetItemLink":
            {
                var location = RequiredItemLocation(
                    state,
                    "Usage: local itemLink = C_Item.GetItemLink(itemLocation)");
                if (runtime.Items.LocationItemIds.TryGetValue(
                        location,
                        out var itemId) &&
                    runtime.Items.TryGetItem(itemId, out var item))
                {
                    PushOptionalString(state, item.Link);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            case "GetItemLocation":
            {
                const string usage =
                    "Usage: local itemLocation = C_Item.GetItemLocation(itemGUID)";
                if (lua_type(state, 1) != LUA_TSTRING)
                    return luaL_error(state, usage);
                var guid = lua_tostring(state, 1) ?? string.Empty;
                if (runtime.Items.LocationsByGuid.TryGetValue(
                        guid,
                        out var location))
                {
                    PushItemLocation(state, location);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            case "GetItemNameByID":
            {
                var item = RequiredItem(
                    state,
                    runtime.Items,
                    "Usage: local itemName = C_Item.GetItemNameByID(itemInfo)");
                PushOptionalString(state, item?.Name);
                return 1;
            }
            case "GetItemSpecInfo":
            {
                const string usage = "Usage: local specTable = C_Item.GetItemSpecInfo(itemInfo)";
                var itemId = RequiredItemId(state, runtime.Items, usage);
                if (itemId is null ||
                    !runtime.Items.SpecializationIds.TryGetValue(itemId.Value, out var specializationIds))
                    return 0;
                lua_newtable(state);
                for (var index = 0; index < specializationIds.Count; index++)
                {
                    lua_pushnumber(state, specializationIds[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "GetItemSubClassInfo":
                return GetItemSubClassInfo(state, runtime);
            default:
                return 0;
        }
    }

    private static int GetItemCount(lua_State state, LuaRuntime runtime)
    {
        const string usage =
            "Usage: local count = C_Item.GetItemCount(itemInfo [, includeBank, includeUses, " +
            "includeReagentBank, includeAccountBank])";
        var itemId = RequiredItemId(state, runtime.Items, usage);
        var includeBank = OptionalBoolean(state, 2, usage);
        var includeUses = OptionalBoolean(state, 3, usage);
        var includeReagentBank = OptionalBoolean(state, 4, usage);
        var includeAccountBank = OptionalBoolean(state, 5, usage);
        if (itemId is null || !runtime.Items.Counts.TryGetValue(itemId.Value, out var counts))
        {
            lua_pushnumber(state, 0);
            return 1;
        }

        var count = counts.Backpack;
        if (includeBank)
            count += counts.Bank;
        if (includeReagentBank)
            count += counts.ReagentBank;
        if (includeAccountBank)
            count += counts.AccountBank;
        if (includeUses && counts.Uses is { } uses)
            count = uses;
        lua_pushnumber(state, count);
        return 1;
    }

    private static int GetItemSubClassInfo(
        lua_State state,
        LuaRuntime runtime)
    {
        const string usage =
            "Usage: local subClassName, subClassUsesInvType = " +
            "C_Item.GetItemSubClassInfo(itemClassID, itemSubClassID)";
        var classId = RequiredInt32(state, 1, usage);
        var subClassId = RequiredInt32(state, 2, usage);

        if (classId == 17)
        {
            var globalName = $"BATTLE_PET_NAME_{subClassId + 1}";
            lua_getglobal(state, globalName);
            if (lua_type(state, -1) != LUA_TSTRING)
            {
                lua_pop(state, 1);
                lua_pushnil(state);
            }
            lua_pushboolean(state, 0);
            return 2;
        }

        if (!runtime.Items.SubClasses.TryGetValue(
                (classId, subClassId),
                out var subClass))
        {
            return 0;
        }

        PushOptionalString(state, subClass.Name);
        lua_pushboolean(state, subClass.UsesInventoryType ? 1 : 0);
        return 2;
    }

    private static int PushItemInfo(lua_State state, WowItemData item)
    {
        PushOptionalString(state, item.Name);
        PushOptionalString(state, item.Link);
        lua_pushnumber(state, item.Quality);
        lua_pushnumber(state, item.ItemLevel);
        lua_pushnumber(state, item.MinimumLevel);
        PushOptionalString(state, item.ItemType);
        PushOptionalString(state, item.ItemSubType);
        lua_pushnumber(state, item.StackCount);
        PushOptionalString(state, item.EquipLocation);
        lua_pushnumber(state, item.TextureFileId);
        lua_pushnumber(state, item.SellPrice);
        lua_pushnumber(state, item.ClassId);
        lua_pushnumber(state, item.SubClassId);
        lua_pushnumber(state, item.BindType);
        lua_pushnumber(state, item.ExpansionId);
        PushOptionalInteger(state, item.SetId);
        lua_pushboolean(state, item.IsCraftingReagent ? 1 : 0);
        PushOptionalString(state, item.Description);
        return 18;
    }

    private static int PushItemInfoInstant(lua_State state, WowItemData item)
    {
        lua_pushnumber(state, item.ItemId);
        PushOptionalString(state, item.ItemType);
        PushOptionalString(state, item.ItemSubType);
        PushOptionalString(state, item.EquipLocation);
        lua_pushnumber(state, item.TextureFileId);
        lua_pushnumber(state, item.ClassId);
        lua_pushnumber(state, item.SubClassId);
        return 7;
    }

    private static WowItemData? RequiredItem(
        lua_State state,
        WowItemState items,
        string usage)
    {
        var itemId = RequiredItemId(state, items, usage);
        if (itemId is null)
            return null;
        items.TryGetItem(itemId.Value, out var item);
        return item;
    }

    internal static int? RequiredItemId(
        lua_State state,
        WowItemState items,
        string usage)
    {
        if (lua_gettop(state) < 1)
            return RaiseItemInfoError(state, usage);

        if (lua_isnumber(state, 1) != 0)
        {
            var number = lua_tonumber(state, 1);
            if (!double.IsFinite(number) ||
                number is < int.MinValue or > int.MaxValue)
            {
                return RaiseItemInfoError(state, usage);
            }
            return (int)number;
        }

        if (lua_istable(state, 1) != 0)
        {
            var location = RequiredItemLocation(state, usage);
            return items.LocationItemIds.TryGetValue(location, out var itemId)
                ? itemId
                : null;
        }

        if (lua_type(state, 1) != LUA_TSTRING)
            return RaiseItemInfoError(state, usage);

        var text = lua_tostring(state, 1) ?? string.Empty;
        if (TryParseItemId(text, out var parsedItemId))
            return parsedItemId;

        return items.Items.Values.FirstOrDefault(item =>
            string.Equals(item.Name, text, StringComparison.Ordinal) ||
            string.Equals(item.Link, text, StringComparison.Ordinal))?.ItemId;
    }

    private static int? RaiseItemInfoError(
        lua_State state,
        string usage)
    {
        luaL_error(state, usage);
        return null;
    }

    private static bool TryParseItemId(string text, out int itemId)
    {
        if (int.TryParse(
                text,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out itemId))
        {
            return true;
        }

        var marker = text.IndexOf(
            "item:",
            StringComparison.OrdinalIgnoreCase);
        if (marker < 0)
            return false;
        marker += "item:".Length;
        var end = marker;
        while (end < text.Length && char.IsAsciiDigit(text[end]))
            end++;
        return end > marker &&
            int.TryParse(
                text.AsSpan(marker, end - marker),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out itemId);
    }

    private static bool DoesItemExist(
        LuaRuntime runtime,
        WowItemLocation location)
    {
        if (runtime.Items.LocationItemIds.ContainsKey(location))
            return true;
        if (location.Kind != WowItemLocationKind.Equipment)
            return false;

        return runtime.Equipment.InventoryItems.Any(pair =>
            pair.Key.SlotId == location.SlotIndex &&
            pair.Key.UnitToken.Equals(
                "player",
                StringComparison.OrdinalIgnoreCase));
    }

    internal static WowItemLocation RequiredItemLocation(
        lua_State state,
        string usage) =>
        RequiredItemLocation(state, 1, usage);

    internal static WowItemLocation RequiredItemLocation(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_gettop(state) < index || lua_istable(state, index) == 0)
        {
            luaL_error(state, usage);
            return default;
        }

        var hasEquipmentSlot = HasTableField(
            state,
            index,
            "equipmentSlotIndex");
        if (hasEquipmentSlot)
        {
            var slot = RequiredTableNumber(
                state,
                index,
                "equipmentSlotIndex",
                usage);
            return WowItemLocation.Equipment(slot);
        }

        if (!HasTableField(state, index, "bagID") ||
            !HasTableField(state, index, "slotIndex"))
        {
            luaL_error(state, usage);
            return default;
        }

        var bagId = RequiredTableNumber(state, index, "bagID", usage);
        var slotIndex = RequiredTableNumber(state, index, "slotIndex", usage);
        return WowItemLocation.Bag(bagId, slotIndex);
    }

    internal static void PushItemLocation(
        lua_State state,
        WowItemLocation location)
    {
        lua_newtable(state);
        var tableIndex = lua_gettop(state);
        if (location.Kind == WowItemLocationKind.Equipment)
        {
            lua_pushnumber(state, location.SlotIndex);
            lua_setfield(state, tableIndex, "equipmentSlotIndex");
        }
        else
        {
            lua_pushnumber(state, location.BagId);
            lua_setfield(state, tableIndex, "bagID");
            lua_pushnumber(state, location.SlotIndex);
            lua_setfield(state, tableIndex, "slotIndex");
        }

        lua_getglobal(state, "Mixin");
        if (lua_isfunction(state, -1) == 0)
        {
            lua_pop(state, 1);
            return;
        }

        lua_pushvalue(state, tableIndex);
        lua_getglobal(state, "ItemLocationMixin");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 3);
            return;
        }

        if (lua_pcall(state, 2, 1, 0) == 0)
        {
            lua_remove(state, tableIndex);
            return;
        }

        lua_pop(state, 1);
    }

    private static bool HasTableField(
        lua_State state,
        int tableIndex,
        string field)
    {
        lua_getfield(state, tableIndex, field);
        var exists = lua_type(state, -1) != LUA_TNIL;
        lua_pop(state, 1);
        return exists;
    }

    private static int RequiredTableNumber(
        lua_State state,
        int tableIndex,
        string field,
        string usage)
    {
        lua_getfield(state, tableIndex, field);
        if (lua_isnumber(state, -1) == 0)
        {
            lua_pop(state, 1);
            luaL_error(state, usage);
            return 0;
        }
        var number = lua_tonumber(state, -1);
        lua_pop(state, 1);
        if (!double.IsFinite(number) ||
            number is < int.MinValue or > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (int)number;
    }

    private static int RequiredInventorySlot(
        lua_State state,
        string usage)
    {
        var raw = RequiredInt32(state, 1, usage);
        var slot = unchecked((byte)raw);
        if (slot > 34)
            luaL_error(state, usage);
        return slot;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number is < int.MinValue or > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (int)number;
    }

    private static bool OptionalBoolean(lua_State state, int index, string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return false;
        if (lua_isboolean(state, index) == 0)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value is { } number)
            lua_pushnumber(state, number);
        else
            lua_pushnil(state);
    }

    private static void SetInteger(lua_State state, string field, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }
}
