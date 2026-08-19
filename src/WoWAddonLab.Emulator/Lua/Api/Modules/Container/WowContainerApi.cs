using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowContainerApi : LuaApiModule
{
    private const string ContainerIdToInventoryIdUsage =
        "Usage: local inventoryID = C_Container.ContainerIDToInventoryID(containerID)";
    private const string GetBagSlotFlagUsage =
        "Usage: local isSet = C_Container.GetBagSlotFlag(bagIndex, flag)";
    private const string IsContainerFilteredUsage =
        "Usage: local isFiltered = C_Container.IsContainerFiltered(containerIndex)";
    private const string GetContainerNumSlotsUsage =
        "Usage: local numSlots = C_Container.GetContainerNumSlots(containerIndex)";
    private const string SetBagSlotFlagUsage =
        "Usage: C_Container.SetBagSlotFlag(bagIndex, flag, isSet)";
    private const string SetItemSearchUsage =
        "Usage: C_Container.SetItemSearch(searchString)";

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "ContainerIDToInventoryID",
        "GetBackpackAutosortDisabled",
        "GetBackpackSellJunkDisabled",
        "GetBagSlotFlag",
        "IsContainerFiltered",
        "CalculateTotalNumberOfFreeBagSlots",
        "GetContainerNumSlots",
        "SetItemSearch",
        "SetBagSlotFlag"
    ];

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "IsInventoryItemProfessionBag", Callback);
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Container");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var containers = LuaBindings.GetRuntime(state).Containers;
        if (operation == "IsInventoryItemProfessionBag")
        {
            if (lua_type(state, 1) != LUA_TSTRING)
                return luaL_error(state, "Usage: IsInventoryItemProfessionBag(unit, slot)");
            var unitToken = lua_tostring(state, 1) ?? string.Empty;
            if (!TryReadInventorySlot(state, 2, out var slotId) ||
                !containers.ProfessionBagSlotsByUnit.TryGetValue(
                    unitToken,
                    out var professionBagSlots) ||
                !professionBagSlots.TryGetValue(slotId, out var isProfessionBag))
            {
                return 0;
            }
            lua_pushboolean(state, isProfessionBag ? 1 : 0);
            return 1;
        }
        if (operation == "GetBackpackAutosortDisabled")
        {
            lua_pushboolean(state, containers.BackpackAutosortDisabled ? 1 : 0);
            return 1;
        }
        if (operation == "GetBackpackSellJunkDisabled")
        {
            lua_pushboolean(state, containers.BackpackSellJunkDisabled ? 1 : 0);
            return 1;
        }
        if (operation == "IsContainerFiltered")
        {
            var containerId = RequiredContainerId(
                state,
                IsContainerFilteredUsage);
            lua_pushboolean(
                state,
                containers.ItemSearch.Length > 0 &&
                containers.FilteredContainerIds.Contains(containerId)
                    ? 1
                    : 0);
            return 1;
        }
        if (operation == "CalculateTotalNumberOfFreeBagSlots")
        {
            lua_pushinteger(state, containers.TotalNumberOfFreeBagSlots);
            return 1;
        }
        if (operation == "GetContainerNumSlots")
        {
            var containerId = RequiredContainerId(
                state,
                GetContainerNumSlotsUsage);
            containers.ContainerSlotCounts.TryGetValue(
                containerId,
                out var slotCount);
            lua_pushinteger(
                state,
                slotCount);
            return 1;
        }
        if (operation == "SetItemSearch")
        {
            if (lua_isstring(state, 1) == 0)
                return luaL_error(state, SetItemSearchUsage);
            containers.ItemSearch = lua_tostring(state, 1) ?? string.Empty;
            return 0;
        }

        if (operation == "ContainerIDToInventoryID")
        {
            var zeroBasedContainerId = RequiredContainerId(
                state,
                ContainerIdToInventoryIdUsage) - 1;
            if ((uint)zeroBasedContainerId > 15)
                return 0;
            var zeroBasedInventoryId = zeroBasedContainerId switch
            {
                < 5 => zeroBasedContainerId + 30,
                < 11 => zeroBasedContainerId + 58,
                _ => zeroBasedContainerId + 89
            };
            lua_pushinteger(state, zeroBasedInventoryId + 1);
            return 1;
        }

        if (operation == "GetBagSlotFlag")
        {
            var containerId = RequiredContainerId(state, GetBagSlotFlagUsage);
            var flag = RequiredBagSlotFlag(state, 2, GetBagSlotFlagUsage);
            lua_pushboolean(
                state, containerId is >= 1 and <= 5 &&
                containers.BagSlotFlags.TryGetValue(containerId, out var flags) &&
                flags.Contains(flag)
                    ? 1
                    : 0);
            return 1;
        }
        if (operation == "SetBagSlotFlag")
        {
            var containerId = RequiredContainerId(state, SetBagSlotFlagUsage);
            var flag = RequiredBagSlotFlag(state, 2, SetBagSlotFlagUsage);
            if (lua_type(state, 3) == LUA_TNONE)
                return luaL_error(state, SetBagSlotFlagUsage);
            var enabled = lua_toboolean(state, 3) != 0;
            if (containerId is < 1 or > 5)
                return 0;
            if (!containers.BagSlotFlags.TryGetValue(containerId, out var flags))
            {
                flags = new HashSet<int>();
                containers.BagSlotFlags[containerId] = flags;
            }
            if (enabled)
                flags.Add(flag);
            else
                flags.Remove(flag);
            return 0;
        }

        return 0;
    }

    private static int RequiredContainerId(lua_State state, string usage)
    {
        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, usage);
        var number = lua_tonumber(state, 1);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            return luaL_error(state, usage);
        }
        return unchecked((int)number);
    }

    private static int RequiredBagSlotFlag(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            return luaL_error(state, usage);
        }
        var value = unchecked((int)number);
        return value is >= 0 and <= 1023
            ? value
            : luaL_error(state, usage);
    }

    private static bool TryReadInventorySlot(
        lua_State state,
        int index,
        out int slotId)
    {
        slotId = -1;
        if (lua_isnumber(state, index) != 0)
        {
            var number = lua_tonumber(state, index);
            if (!double.IsFinite(number) ||
                number < int.MinValue ||
                number > int.MaxValue)
            {
                return false;
            }
            slotId = unchecked((int)number);
        }
        else if (lua_type(state, index) == LUA_TSTRING &&
                 InventorySlotId(lua_tostring(state, index) ?? string.Empty) is
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

    private static int? InventorySlotId(string slotName) =>
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
            _ => null
        };
}
