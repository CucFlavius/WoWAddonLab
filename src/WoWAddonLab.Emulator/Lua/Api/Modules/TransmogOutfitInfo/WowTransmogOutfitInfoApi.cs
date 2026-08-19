using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTransmogOutfitInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly SlotInfo[] AppearanceSlots =
    [
        new(0, 0, 1, "HEADSLOT", false),
        new(1, 0, 2, "SHOULDERSLOT", false),
        new(2, 0, 2, "SHOULDERSLOT", true),
        new(3, 0, 3, "BACKSLOT", false),
        new(4, 0, 4, "CHESTSLOT", false),
        new(5, 0, 6, "TABARDSLOT", false),
        new(6, 0, 5, "SHIRTSLOT", false),
        new(7, 0, 7, "WRISTSLOT", false),
        new(8, 0, 8, "HANDSSLOT", false),
        new(9, 0, 9, "WAISTSLOT", false),
        new(10, 0, 10, "LEGSSLOT", false),
        new(11, 0, 11, "FEETSLOT", false),
        new(12, 0, 0, "MAINHANDSLOT", false),
        new(13, 0, 0, "SECONDARYHANDSLOT", false),
        new(14, 0, 0, "RANGEDSLOT", false)
    ];

    private static readonly SlotInfo[] IllusionSlots =
    [
        new(12, 1, 0, "MAINHANDSLOT", false),
        new(13, 1, 0, "SECONDARYHANDSLOT", false),
        new(14, 1, 0, "RANGEDSLOT", false)
    ];

    private static readonly string[] Functions =
    [
        "ChangeViewedOutfit",
        "ClearAllPendingSituations",
        "ClearAllPendingTransmogs",
        "GetActiveOutfitID",
        "GetAllSlotLocationInfo",
        "GetCurrentlyViewedOutfitID",
        "GetLinkedSlotInfo",
        "GetMaxNumberOfTotalOutfitsForSource",
        "GetMaxNumberOfUsableOutfits",
        "GetNumberOfOutfitsUnlockedForSource",
        "GetOutfitSituationsEnabled",
        "GetOutfitsInfo",
        "GetPendingTransmogCost",
        "GetUISituationCategoriesAndOptions",
        "HasPendingOutfitSituations",
        "InTransmogEvent",
        "IsEquippedGearOutfitDisplayed",
        "IsEquippedGearOutfitLocked",
        "IsUsableDiscountAvailable",
        "TransmogEventActive",
        "GetTransmogOutfitSlotFromInventorySlot"
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
        lua_setglobal(state, "C_TransmogOutfitInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "ChangeViewedOutfit":
            case "ClearAllPendingSituations":
            case "ClearAllPendingTransmogs":
            case "GetPendingTransmogCost":
                return 0;
            case "GetActiveOutfitID":
            case "GetCurrentlyViewedOutfitID":
            case "GetMaxNumberOfTotalOutfitsForSource":
            case "GetMaxNumberOfUsableOutfits":
            case "GetNumberOfOutfitsUnlockedForSource":
                lua_pushinteger(state, 0);
                return 1;
            case "GetAllSlotLocationInfo":
                PushSlotArray(state, AppearanceSlots);
                PushSlotArray(state, IllusionSlots);
                return 2;
            case "GetOutfitSituationsEnabled":
            case "HasPendingOutfitSituations":
            case "InTransmogEvent":
            case "IsEquippedGearOutfitDisplayed":
            case "IsEquippedGearOutfitLocked":
            case "IsUsableDiscountAvailable":
            case "TransmogEventActive":
                lua_pushboolean(state, 0);
                return 1;
            case "GetOutfitsInfo":
            case "GetUISituationCategoriesAndOptions":
                lua_newtable(state);
                return 1;
            case "GetTransmogOutfitSlotFromInventorySlot":
            {
                var inventorySlot = lua_type(state, 1) == LUA_TNUMBER
                    ? (int)lua_tonumber(state, 1)
                    : -1;
                var slot = inventorySlot switch
                {
                    0 => 0,
                    2 => 1,
                    3 => 6,
                    4 => 4,
                    6 => 9,
                    7 => 10,
                    8 => 11,
                    9 => 7,
                    14 => 3,
                    15 => 12,
                    16 => 13,
                    17 => 14,
                    18 => 5,
                    _ => -1
                };
                if (slot < 0)
                    return 0;
                lua_pushinteger(state, slot);
                return 1;
            }
            case "GetLinkedSlotInfo":
            {
                var slot = lua_type(state, 1) == LUA_TNUMBER
                    ? (int)lua_tonumber(state, 1)
                    : -1;
                if (slot is not (1 or 2))
                    return 0;
                lua_newtable(state);
                PushSlot(state, AppearanceSlots[1]);
                lua_setfield(state, -2, "primarySlotInfo");
                PushSlot(state, AppearanceSlots[2]);
                lua_setfield(state, -2, "secondarySlotInfo");
                return 1;
            }
            default:
                return 0;
        }
    }

    private static void PushSlotArray(lua_State state, IReadOnlyList<SlotInfo> slots)
    {
        lua_newtable(state);
        for (var index = 0; index < slots.Count; index++)
        {
            PushSlot(state, slots[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushSlot(lua_State state, SlotInfo slot)
    {
        lua_newtable(state);
        SetNumber(state, "slot", slot.Slot);
        SetNumber(state, "type", slot.Type);
        SetNumber(state, "collectionType", slot.CollectionType);
        lua_pushstring(state, slot.SlotName);
        lua_setfield(state, -2, "slotName");
        lua_pushboolean(state, slot.IsSecondary ? 1 : 0);
        lua_setfield(state, -2, "isSecondary");
    }

    private static void SetNumber(lua_State state, string field, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private sealed record SlotInfo(
        int Slot,
        int Type,
        int CollectionType,
        string SlotName,
        bool IsSecondary);
}
