using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowEquipmentApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "GetWeaponEnchantInfo", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetCorruption", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetInventoryItemTexture", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetInventoryItemQuality", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetInventoryItemID", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetInventoryItemLink", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetInventoryItemDurability", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetInventorySlotInfo", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetInventoryAlertStatus", Callback);
        LuaBindings.RegisterClosureGlobal(state, "IsInventoryItemLocked", Callback);
        LuaBindings.RegisterClosureGlobal(state, "PickupInventoryItem", Callback);
        RegisterEquipmentSetNamespace(state);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var equipment = runtime.Equipment;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation == "GetCorruption")
        {
            lua_pushnumber(state, equipment.Corruption);
            return 1;
        }
        if (operation is "GetInventoryItemTexture" or "GetInventoryItemQuality" or
            "GetInventoryItemID" or "GetInventoryItemLink")
        {
            var lookup = GetUnitInventoryItem(
                state,
                runtime,
                equipment,
                out var item);
            if (lookup != InventoryLookupResult.Found)
            {
                if ((lookup == InventoryLookupResult.MissingItem &&
                     operation is "GetInventoryItemTexture" or
                         "GetInventoryItemQuality") ||
                    (lookup == InventoryLookupResult.MissingUnit &&
                     operation == "GetInventoryItemTexture"))
                {
                    lua_pushnil(state);
                    return 1;
                }
                return 0;
            }

            switch (operation)
            {
                case "GetInventoryItemTexture":
                    PushOptionalInteger(state, item.TextureFileId);
                    return 1;
                case "GetInventoryItemQuality":
                    PushOptionalInteger(state, item.Quality);
                    return 1;
                case "GetInventoryItemID":
                    lua_pushinteger(state, item.ItemId);
                    return 1;
                default:
                    if (item.Link is null)
                    {
                        lua_pushnil(state);
                        return 1;
                    }
                    lua_pushstring(state, item.Link);
                    return 1;
            }
        }
        if (operation == "GetInventoryItemDurability")
        {
            if (lua_isnumber(state, 1) == 0)
            {
                return luaL_error(
                    state,
                    "Usage: GetInventoryItemDurability(slot)");
            }

            var number = lua_tonumber(state, 1);
            if (!double.IsFinite(number) ||
                number < int.MinValue ||
                number > int.MaxValue)
            {
                return 0;
            }

            var item = FindInventoryItem(equipment, "player", (int)number);
            if (item?.MaxDurability is not > 0)
                return 0;
            lua_pushnumber(state, item.CurrentDurability.GetValueOrDefault());
            lua_pushnumber(state, item.MaxDurability.Value);
            return 2;
        }
        if (operation == "GetInventorySlotInfo")
        {
            var slotName = lua_isstring(state, 1) != 0
                ? lua_tostring(state, 1) ?? string.Empty
                : string.Empty;
            var slot = ResolveInventorySlot(runtime, slotName);
            if (slot is null)
            {
                return luaL_error(
                    state,
                    "Invalid inventory slot in GetInventorySlotInfo");
            }

            lua_pushinteger(state, slot.SlotId);
            equipment.InventorySlotTextureFileIds.TryGetValue(
                slot.SlotId,
                out var textureFileId);
            PushOptionalInteger(
                state,
                equipment.InventorySlotTextureFileIds.ContainsKey(slot.SlotId)
                    ? textureFileId
                    : slot.TextureFileId);
            lua_pushboolean(state, 0);
            return 3;
        }
        if (operation == "GetInventoryAlertStatus")
        {
            if (lua_isnumber(state, 1) == 0)
                return luaL_error(state, "Usage: GetInventoryAlertStatus(index)");
            var index = lua_tonumber(state, 1);
            var zeroBasedIndex = double.IsFinite(index)
                ? unchecked((int)index) - 1
                : -1;
            lua_pushinteger(
                state,
                zeroBasedIndex >= 0 &&
                zeroBasedIndex < equipment.InventoryAlertStatuses.Length
                    ? equipment.InventoryAlertStatuses[zeroBasedIndex]
                    : 0);
            return 1;
        }
        if (operation == "PickupInventoryItem")
        {
            if (TryParseInventorySlot(state, 1, out var slotId))
                equipment.LastPickedInventorySlot = slotId;
            return 0;
        }
        if (operation == "IsInventoryItemLocked")
        {
            if (!TryParseInventorySlot(state, 1, out var slotId))
                return 0;
            var isLocked = FindInventoryItem(equipment, "player", slotId)
                ?.IsLocked == true;
            lua_pushboolean(state, isLocked ? 1 : 0);
            return 1;
        }
        if (operation != "GetWeaponEnchantInfo")
            return DispatchEquipmentSet(state, equipment, operation);

        PushEnchant(state, equipment.MainHandEnchant);
        PushEnchant(state, equipment.OffHandEnchant);
        return 8;
    }

    private static int DispatchEquipmentSet(
        lua_State state,
        WowEquipmentState equipment,
        string operation)
    {
        switch (operation)
        {
            case "CanUseEquipmentSets":
                PushBoolean(state, equipment.CanUseEquipmentSets);
                return 1;
            case "GetNumEquipmentSets":
                lua_pushinteger(
                    state,
                    equipment.EquipmentSets.Count > 0
                        ? equipment.EquipmentSets.Count
                        : equipment.EquipmentSetCount);
                return 1;
            case "GetEquipmentSetIDs":
                lua_newtable(state);
                for (var index = 0; index < equipment.EquipmentSets.Count; index++)
                {
                    lua_pushinteger(state, equipment.EquipmentSets[index].Id);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "GetEquipmentSetAssignedSpec":
            {
                var set = FindSet(
                    equipment,
                    RequiredInt32(state, 1, operation));
                if (set?.AssignedSpecIndex is not { } specIndex)
                    return 0;
                lua_pushinteger(state, specIndex);
                return 1;
            }
            case "GetEquipmentSetForSpec":
            {
                var specIndex = checked(
                    (int)RequiredOneBasedIndex(state, 1, operation) + 1);
                var set = equipment.EquipmentSets.FirstOrDefault(
                    value => value.AssignedSpecIndex == specIndex);
                if (set is null)
                    return 0;
                lua_pushinteger(state, set.Id);
                return 1;
            }
            case "GetEquipmentSetID":
            {
                var name = RequiredString(state, 1, operation);
                var set = equipment.EquipmentSets.FirstOrDefault(
                    value => string.Equals(
                        value.Name,
                        name,
                        StringComparison.Ordinal));
                if (set is null)
                    return 0;
                lua_pushinteger(state, set.Id);
                return 1;
            }
            case "GetEquipmentSetInfo":
            {
                var set = FindSet(
                    equipment,
                    RequiredInt32(state, 1, operation));
                if (set is null)
                    return 0;
                lua_pushstring(state, set.Name);
                lua_pushinteger(state, set.IconFileId);
                lua_pushinteger(state, set.Id);
                PushBoolean(state, set.IsEquipped);
                lua_pushinteger(state, set.NumItems);
                lua_pushinteger(state, set.NumEquipped);
                lua_pushinteger(state, set.NumInInventory);
                lua_pushinteger(state, set.NumLost);
                lua_pushinteger(state, set.NumIgnored);
                return 9;
            }
            case "GetIgnoredSlots":
            {
                var set = FindSet(
                    equipment,
                    RequiredInt32(state, 1, operation));
                if (set is null)
                    return 0;
                PushOptionalBooleanArray(state, set.IgnoredSlots);
                return 1;
            }
            case "GetItemIDs":
            {
                var set = FindSet(
                    equipment,
                    RequiredInt32(state, 1, operation));
                if (set is null)
                    return 0;
                PushOptionalIntegerArray(state, set.ItemIds);
                return 1;
            }
            case "GetItemLocations":
            {
                var set = FindSet(
                    equipment,
                    RequiredInt32(state, 1, operation));
                if (set is null)
                    return 0;
                PushOptionalIntegerArray(state, set.ItemLocations);
                return 1;
            }
            case "EquipmentSetContainsLockedItems":
            {
                var set = FindSet(
                    equipment,
                    RequiredInt32(state, 1, operation));
                PushBoolean(state, set?.ContainsLockedItems == true);
                return 1;
            }
            case "IsSlotIgnoredForSave":
            {
                var slot = RequiredOneBasedIndex(state, 1, operation);
                PushBoolean(
                    state,
                    slot < equipment.IgnoredSlotsForSave.Length &&
                    equipment.IgnoredSlotsForSave[slot]);
                return 1;
            }
            case "ClearIgnoredSlotsForSave":
                Array.Clear(equipment.IgnoredSlotsForSave);
                return 0;
            case "IgnoreSlotForSave":
            {
                var slot = RequiredOneBasedIndex(state, 1, operation);
                if (slot < equipment.IgnoredSlotsForSave.Length)
                    equipment.IgnoredSlotsForSave[slot] = true;
                return 0;
            }
            case "UnignoreSlotForSave":
            {
                var slot = RequiredOneBasedIndex(state, 1, operation);
                if (slot < equipment.IgnoredSlotsForSave.Length)
                    equipment.IgnoredSlotsForSave[slot] = false;
                return 0;
            }
            case "AssignSpecToEquipmentSet":
            {
                var setId = RequiredInt32(state, 1, operation);
                var zeroBasedSpec = RequiredOneBasedIndex(state, 2, operation);
                if (zeroBasedSpec > 4)
                    return 0;
                var specIndex = checked((int)zeroBasedSpec + 1);
                var set = FindSet(equipment, setId);
                if (set is null)
                    return 0;
                foreach (var candidate in equipment.EquipmentSets)
                {
                    if (candidate.AssignedSpecIndex == specIndex)
                        candidate.AssignedSpecIndex = null;
                }
                set.AssignedSpecIndex = specIndex;
                return 0;
            }
            case "UnassignEquipmentSetSpec":
            {
                var set = FindSet(
                    equipment,
                    RequiredInt32(state, 1, operation));
                if (set is not null)
                    set.AssignedSpecIndex = null;
                return 0;
            }
            case "CreateEquipmentSet":
            {
                var name = RequiredString(state, 1, operation);
                var icon = OptionalString(state, 2, operation);
                var id = equipment.NextEquipmentSetId;
                while (FindSet(equipment, id) is not null)
                    id++;
                equipment.NextEquipmentSetId = checked(id + 1);
                equipment.EquipmentSets.Add(
                    new WowEquipmentSetState
                    {
                        Id = id,
                        Name = name,
                        IconAsset = icon
                    });
                return 0;
            }
            case "DeleteEquipmentSet":
            {
                var set = FindSet(
                    equipment,
                    RequiredInt32(state, 1, operation));
                if (set is not null)
                    equipment.EquipmentSets.Remove(set);
                return 0;
            }
            case "ModifyEquipmentSet":
            {
                var setId = RequiredInt32(state, 1, operation);
                var name = RequiredString(state, 2, operation);
                var icon = OptionalString(state, 3, operation);
                var set = FindSet(equipment, setId);
                if (set is not null)
                {
                    set.Name = name;
                    if (icon is not null)
                        set.IconAsset = icon;
                }
                return 0;
            }
            case "PickupEquipmentSet":
            {
                var set = FindSet(
                    equipment,
                    RequiredInt32(state, 1, operation));
                if (set is not null)
                    set.PickupCount++;
                return 0;
            }
            case "SaveEquipmentSet":
            {
                var setId = RequiredInt32(state, 1, operation);
                var icon = OptionalString(state, 2, operation);
                var set = FindSet(equipment, setId);
                if (set is not null)
                {
                    set.SaveCount++;
                    if (icon is not null)
                        set.IconAsset = icon;
                }
                return 0;
            }
            case "UseEquipmentSet":
            {
                var set = FindSet(
                    equipment,
                    RequiredInt32(state, 1, operation));
                var used = set?.CanEquip == true;
                if (used)
                {
                    foreach (var candidate in equipment.EquipmentSets)
                        candidate.IsEquipped = ReferenceEquals(candidate, set);
                }
                PushBoolean(state, used);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static void RegisterEquipmentSetNamespace(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "AssignSpecToEquipmentSet", "CanUseEquipmentSets",
                     "ClearIgnoredSlotsForSave", "CreateEquipmentSet", "DeleteEquipmentSet",
                     "EquipmentSetContainsLockedItems", "GetEquipmentSetAssignedSpec",
                     "GetEquipmentSetForSpec", "GetEquipmentSetID", "GetEquipmentSetIDs",
                     "GetEquipmentSetInfo", "GetIgnoredSlots", "GetItemIDs",
                     "GetItemLocations", "GetNumEquipmentSets", "IgnoreSlotForSave",
                     "IsSlotIgnoredForSave", "ModifyEquipmentSet",
                     "PickupEquipmentSet", "SaveEquipmentSet", "UnassignEquipmentSetSpec",
                     "UnignoreSlotForSave", "UseEquipmentSet"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_EquipmentSet");
    }

    private static WowEquipmentSetState? FindSet(
        WowEquipmentState equipment,
        int setId) =>
        equipment.EquipmentSets.FirstOrDefault(value => value.Id == setId);

    private static int RequiredInt32(
        lua_State state,
        int index,
        string operation)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, Usage(operation));
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return luaL_error(state, Usage(operation));
        }
        return unchecked((int)value);
    }

    private static uint RequiredOneBasedIndex(
        lua_State state,
        int index,
        string operation)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, Usage(operation));
            return 0;
        }
        var zeroBased = lua_tonumber(state, index) - 1;
        if (!double.IsFinite(zeroBased) ||
            zeroBased < 0 ||
            zeroBased > uint.MaxValue)
        {
            luaL_error(state, Usage(operation));
            return 0;
        }
        return unchecked((uint)zeroBased);
    }

    private static string RequiredString(
        lua_State state,
        int index,
        string operation)
    {
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, Usage(operation));
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static string? OptionalString(
        lua_State state,
        int index,
        string operation)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return null;
        return RequiredString(state, index, operation);
    }

    private static string Usage(string operation) =>
        $"Usage: C_EquipmentSet.{operation}(...)";

    private static void PushOptionalBooleanArray(
        lua_State state,
        IList<bool?> values)
    {
        lua_newtable(state);
        for (var index = 0; index < 19; index++)
        {
            var value = index < values.Count ? values[index] : null;
            if (value is { } boolean)
                PushBoolean(state, boolean);
            else
                lua_pushnil(state);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushOptionalIntegerArray(
        lua_State state,
        IList<int?> values)
    {
        lua_newtable(state);
        for (var index = 0; index < 19; index++)
        {
            var value = index < values.Count ? values[index] : null;
            if (value is { } integer)
                lua_pushinteger(state, integer);
            else
                lua_pushnil(state);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushBoolean(lua_State state, bool value) =>
        lua_pushboolean(state, value ? 1 : 0);

    private static void PushEnchant(lua_State state, WowWeaponEnchantState enchant)
    {
        lua_pushboolean(state, enchant.HasEnchant ? 1 : 0);
        if (!enchant.HasEnchant)
        {
            lua_pushnil(state);
            lua_pushnil(state);
            lua_pushnil(state);
            return;
        }
        lua_pushnumber(state, enchant.ExpirationMilliseconds);
        lua_pushinteger(state, enchant.Charges);
        lua_pushinteger(state, enchant.EnchantId);
    }

    private static InventoryLookupResult GetUnitInventoryItem(
        lua_State state,
        LuaRuntime runtime,
        WowEquipmentState equipment,
        out WowInventoryItemState item)
    {
        item = null!;
        if (lua_isstring(state, 1) == 0 ||
            !TryParseInventorySlot(state, 2, out var slotId))
        {
            return InventoryLookupResult.InvalidArguments;
        }

        var unitToken = lua_tostring(state, 1);
        if (runtime.Units.Find(unitToken) is null)
            return InventoryLookupResult.MissingUnit;
        item = FindInventoryItem(equipment, unitToken!, slotId)!;
        return item is null
            ? InventoryLookupResult.MissingItem
            : InventoryLookupResult.Found;
    }

    private static WowInventoryItemState? FindInventoryItem(
        WowEquipmentState equipment,
        string unitToken,
        int slotId)
    {
        if (equipment.InventoryItems.TryGetValue(
                (unitToken.ToLowerInvariant(), slotId),
                out var exact))
        {
            return exact;
        }
        return equipment.InventoryItems.FirstOrDefault(
            entry =>
                entry.Key.SlotId == slotId &&
                entry.Key.UnitToken.Equals(
                    unitToken,
                    StringComparison.OrdinalIgnoreCase)).Value;
    }

    private static bool TryParseInventorySlot(
        lua_State state,
        int index,
        out int slotId)
    {
        slotId = -1;
        if (lua_isnumber(state, index) != 0)
        {
            var numericSlot = lua_tonumber(state, index);
            if (!double.IsFinite(numericSlot) ||
                numericSlot < int.MinValue ||
                numericSlot > int.MaxValue)
            {
                return false;
            }
            slotId = unchecked((int)numericSlot);
        }
        else if (lua_isstring(state, index) != 0 &&
                 FallbackInventorySlotId(
                     lua_tostring(state, index) ?? string.Empty) is
                     { } namedSlot)
        {
            slotId = namedSlot;
        }
        else
        {
            return false;
        }

        return slotId is >= 1 and <= 35 or >= 64 and <= 69 or
            >= 85 and <= 105;
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value is { } integer)
            lua_pushinteger(state, integer);
        else
            lua_pushnil(state);
    }

    private static WowInventorySlotInfo? ResolveInventorySlot(
        LuaRuntime runtime,
        string slotName)
    {
        if (runtime.Equipment.TryGetInventorySlot(slotName, out var slot))
            return slot;
        return FallbackInventorySlotId(slotName) is { } slotId
            ? new WowInventorySlotInfo(slotId, null)
            : null;
    }

    private static int? FallbackInventorySlotId(string slotName) =>
        slotName.ToUpperInvariant() switch
        {
            "AMMOSLOT" => 0,
            "HEADSLOT" => 1,
            "NECKSLOT" => 2,
            "SHOULDERSLOT" => 3,
            "SHIRTSLOT" or "BODYSLOT" => 4,
            "CHESTSLOT" => 5,
            "WAISTSLOT" => 6,
            "LEGSSLOT" => 7,
            "FEETSLOT" => 8,
            "WRISTSLOT" => 9,
            "HANDSSLOT" => 10,
            "FINGER0SLOT" => 11,
            "FINGER1SLOT" => 12,
            "TRINKET0SLOT" => 13,
            "TRINKET1SLOT" => 14,
            "BACKSLOT" => 15,
            "MAINHANDSLOT" => 16,
            "SECONDARYHANDSLOT" or "OFFHANDSLOT" => 17,
            "RANGEDSLOT" => 18,
            "TABARDSLOT" => 19,
            "BAG0SLOT" => 20,
            "BAG1SLOT" => 21,
            "BAG2SLOT" => 22,
            "BAG3SLOT" => 23,
            "REAGENTBAG0SLOT" => 24,
            _ => null
        };

    private enum InventoryLookupResult
    {
        InvalidArguments,
        MissingUnit,
        MissingItem,
        Found
    }
}
