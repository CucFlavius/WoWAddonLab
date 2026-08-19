using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowAzeriteEmpoweredItemApi : LuaApiModule
{
    private const string Namespace = "C_AzeriteEmpoweredItem";

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CanSelectPower",
        "ConfirmAzeriteEmpoweredItemRespec",
        "GetAllTierInfo",
        "GetAllTierInfoByItemID",
        "GetAzeriteEmpoweredItemRespecCost",
        "GetPowerInfo",
        "GetPowerText",
        "GetSpecsForPower",
        "HasAnyUnselectedPowers",
        "HasBeenViewed",
        "IsAzeriteEmpoweredItem",
        "IsAzeriteEmpoweredItemByID",
        "IsAzeritePreviewSourceDisplayable",
        "IsHeartOfAzerothEquipped",
        "IsPowerAvailableForSpec",
        "IsPowerSelected",
        "SelectPower",
        "SetHasBeenViewed"
    ];

    public override void Register(lua_State state)
    {
        RegisterEnums(state);

        lua_createtable(state, 0, Functions.Length);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, Namespace);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var azerite = runtime.AzeriteEmpoweredItem;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ??
            string.Empty;

        switch (operation)
        {
            case "CanSelectPower":
            {
                const string usage =
                    "Usage: local canSelect = " +
                    "C_AzeriteEmpoweredItem.CanSelectPower(" +
                    "azeriteEmpoweredItemLocation, powerID)";
                var (location, item) = RequiredAzeriteItem(
                    state,
                    azerite,
                    usage);
                _ = location;
                var powerId = RequiredInt32(state, 2, usage);
                return PushBoolean(
                    state,
                    item.SelectablePowerIds.Contains(powerId));
            }
            case "ConfirmAzeriteEmpoweredItemRespec":
            {
                const string usage =
                    "Usage: C_AzeriteEmpoweredItem." +
                    "ConfirmAzeriteEmpoweredItemRespec(" +
                    "azeriteEmpoweredItemLocation)";
                var (location, _) = RequiredAzeriteItem(
                    state,
                    azerite,
                    usage);
                azerite.ConfirmedRespecLocations.Add(location);
                return 0;
            }
            case "GetAllTierInfo":
            {
                const string usage =
                    "Usage: local tierInfo = " +
                    "C_AzeriteEmpoweredItem.GetAllTierInfo(" +
                    "azeriteEmpoweredItemLocation)";
                var (_, item) = RequiredAzeriteItem(
                    state,
                    azerite,
                    usage);
                PushTierInfo(state, item.Tiers);
                return 1;
            }
            case "GetAllTierInfoByItemID":
            {
                const string usage =
                    "Usage: local tierInfo = " +
                    "C_AzeriteEmpoweredItem.GetAllTierInfoByItemID(" +
                    "itemInfo [, classID])";
                var itemId = WowItemApi.RequiredItemId(
                    state,
                    runtime.Items,
                    usage);
                var classId = OptionalInt32(state, 2, usage);
                IReadOnlyList<WowAzeriteEmpoweredItemTierInfo> tiers;
                if (itemId is not { } resolvedItemId)
                {
                    tiers = [];
                }
                else if (!azerite.TierInfoByItem.TryGetValue(
                             (resolvedItemId, classId),
                             out var classTiers) &&
                         !azerite.TierInfoByItem.TryGetValue(
                             (resolvedItemId, null),
                             out classTiers))
                {
                    tiers = [];
                }
                else
                {
                    tiers = classTiers;
                }
                PushTierInfo(state, tiers);
                return 1;
            }
            case "GetAzeriteEmpoweredItemRespecCost":
                lua_pushnumber(state, azerite.RespecCost);
                return 1;
            case "GetPowerInfo":
            {
                const string usage =
                    "Usage: local powerInfo = " +
                    "C_AzeriteEmpoweredItem.GetPowerInfo(powerID)";
                var powerId = RequiredInt32(state, 1, usage);
                if (!azerite.Powers.TryGetValue(powerId, out var power))
                    return 0;
                lua_createtable(state, 0, 2);
                SetInteger(state, "azeritePowerID", power.AzeritePowerId);
                SetInteger(state, "spellID", power.SpellId);
                return 1;
            }
            case "GetPowerText":
            {
                const string usage =
                    "Usage: local powerText = " +
                    "C_AzeriteEmpoweredItem.GetPowerText(" +
                    "azeriteEmpoweredItemLocation, powerID, level)";
                var (location, _) = RequiredAzeriteItem(
                    state,
                    azerite,
                    usage);
                var powerId = RequiredInt32(state, 2, usage);
                var level = RequiredPowerLevel(state, 3, usage);
                if (!azerite.PowerText.TryGetValue(
                        (location, powerId, level),
                        out var powerText))
                {
                    return 0;
                }
                lua_createtable(state, 0, 2);
                SetString(state, "name", powerText.Name);
                SetString(state, "description", powerText.Description);
                return 1;
            }
            case "GetSpecsForPower":
            {
                const string usage =
                    "Usage: local specInfo = " +
                    "C_AzeriteEmpoweredItem.GetSpecsForPower(powerID)";
                var powerId = RequiredInt32(state, 1, usage);
                if (!azerite.SpecsByPowerId.TryGetValue(powerId, out var specs) ||
                    specs.Count == 0)
                {
                    return 0;
                }
                lua_createtable(state, specs.Count, 0);
                for (var index = 0; index < specs.Count; index++)
                {
                    var spec = specs[index];
                    lua_createtable(state, 0, 2);
                    SetInteger(state, "classID", spec.ClassId);
                    SetInteger(state, "specID", spec.SpecId);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "HasAnyUnselectedPowers":
            {
                const string usage =
                    "Usage: local hasAnyUnselectedPowers = " +
                    "C_AzeriteEmpoweredItem.HasAnyUnselectedPowers(" +
                    "azeriteEmpoweredItemLocation)";
                var (_, item) = RequiredAzeriteItem(
                    state,
                    azerite,
                    usage);
                return PushBoolean(state, item.HasAnyUnselectedPowers);
            }
            case "HasBeenViewed":
            {
                const string usage =
                    "Usage: local hasBeenViewed = " +
                    "C_AzeriteEmpoweredItem.HasBeenViewed(" +
                    "azeriteEmpoweredItemLocation)";
                var (_, item) = RequiredAzeriteItem(
                    state,
                    azerite,
                    usage);
                return PushBoolean(state, item.HasBeenViewed);
            }
            case "IsAzeriteEmpoweredItem":
            {
                const string usage =
                    "Usage: local isAzeriteEmpoweredItem = " +
                    "C_AzeriteEmpoweredItem.IsAzeriteEmpoweredItem(" +
                    "itemLocation)";
                var location = WowItemApi.RequiredItemLocation(state, usage);
                return PushBoolean(
                    state,
                    azerite.ItemsByLocation.ContainsKey(location));
            }
            case "IsAzeriteEmpoweredItemByID":
            {
                const string usage =
                    "Usage: local isAzeriteEmpoweredItem = " +
                    "C_AzeriteEmpoweredItem.IsAzeriteEmpoweredItemByID(" +
                    "itemInfo)";
                var itemId = WowItemApi.RequiredItemId(
                    state,
                    runtime.Items,
                    usage);
                return PushBoolean(
                    state,
                    itemId is { } id &&
                    azerite.EmpoweredItemIds.Contains(id));
            }
            case "IsAzeritePreviewSourceDisplayable":
            {
                const string usage =
                    "Usage: local isAzeritePreviewSourceDisplayable = " +
                    "C_AzeriteEmpoweredItem." +
                    "IsAzeritePreviewSourceDisplayable(itemInfo [, classID])";
                var itemId = WowItemApi.RequiredItemId(
                    state,
                    runtime.Items,
                    usage);
                var classId = OptionalInt32(state, 2, usage);
                return PushBoolean(
                    state,
                    itemId is { } id &&
                    (azerite.DisplayablePreviewSources.Contains((id, classId)) ||
                     azerite.DisplayablePreviewSources.Contains((id, null))));
            }
            case "IsHeartOfAzerothEquipped":
                return PushBoolean(state, azerite.IsHeartOfAzerothEquipped);
            case "IsPowerAvailableForSpec":
            {
                const string usage =
                    "Usage: local isPowerAvailableForSpec = " +
                    "C_AzeriteEmpoweredItem.IsPowerAvailableForSpec(" +
                    "powerID, specID)";
                var powerId = RequiredInt32(state, 1, usage);
                var specId = RequiredInt32(state, 2, usage);
                return PushBoolean(
                    state,
                    azerite.AvailablePowersBySpec.Contains((powerId, specId)));
            }
            case "IsPowerSelected":
            {
                const string usage =
                    "Usage: local isSelected = " +
                    "C_AzeriteEmpoweredItem.IsPowerSelected(" +
                    "azeriteEmpoweredItemLocation, powerID)";
                var (_, item) = RequiredAzeriteItem(
                    state,
                    azerite,
                    usage);
                var powerId = RequiredInt32(state, 2, usage);
                return PushBoolean(
                    state,
                    item.SelectedPowerIds.Contains(powerId));
            }
            case "SelectPower":
            {
                const string usage =
                    "Usage: local success = " +
                    "C_AzeriteEmpoweredItem.SelectPower(" +
                    "azeriteEmpoweredItemLocation, powerID)";
                var (location, item) = RequiredAzeriteItem(
                    state,
                    azerite,
                    usage);
                var powerId = RequiredInt32(state, 2, usage);
                if (!item.SelectablePowerIds.Contains(powerId) ||
                    !item.TryGetTierIndex(powerId, out var tierIndex))
                {
                    return PushBoolean(state, false);
                }
                azerite.SelectRequests.Add(
                    new WowAzeriteEmpoweredItemSelectRequest(
                        location,
                        tierIndex,
                        powerId));
                return PushBoolean(state, true);
            }
            case "SetHasBeenViewed":
            {
                const string usage =
                    "Usage: C_AzeriteEmpoweredItem.SetHasBeenViewed(" +
                    "azeriteEmpoweredItemLocation)";
                var (location, item) = RequiredAzeriteItem(
                    state,
                    azerite,
                    usage);
                if (!item.HasBeenViewedFlag)
                    azerite.SetHasBeenViewedRequests.Add(location);
                return 0;
            }
            default:
                return 0;
        }
    }

    private static (WowItemLocation Location,
        WowAzeriteEmpoweredItemData Item) RequiredAzeriteItem(
        lua_State state,
        WowAzeriteEmpoweredItemState azerite,
        string usage)
    {
        var location = WowItemApi.RequiredItemLocation(state, usage);
        if (azerite.ItemsByLocation.TryGetValue(location, out var item))
            return (location, item);
        luaL_error(state, usage);
        return default;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_gettop(state) < index || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value is < int.MinValue or > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (int)value;
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_gettop(state) < index || lua_isnil(state, index) != 0)
            return null;
        return RequiredInt32(state, index, usage);
    }

    private static int RequiredPowerLevel(
        lua_State state,
        int index,
        string usage)
    {
        var level = RequiredInt32(state, index, usage);
        if (level is < 0 or > 2)
        {
            luaL_error(state, usage);
            return 0;
        }
        return level;
    }

    private static void PushTierInfo(
        lua_State state,
        IEnumerable<WowAzeriteEmpoweredItemTierInfo> tiers)
    {
        var tierValues = tiers as
            IReadOnlyList<WowAzeriteEmpoweredItemTierInfo> ??
            tiers.ToArray();
        lua_createtable(state, tierValues.Count, 0);
        for (var tierIndex = 0; tierIndex < tierValues.Count; tierIndex++)
        {
            var tier = tierValues[tierIndex];
            lua_createtable(state, 0, 2);
            lua_createtable(state, tier.AzeritePowerIds.Count, 0);
            for (var powerIndex = 0;
                 powerIndex < tier.AzeritePowerIds.Count;
                 powerIndex++)
            {
                lua_pushnumber(state, tier.AzeritePowerIds[powerIndex]);
                lua_rawseti(state, -2, powerIndex + 1);
            }
            lua_setfield(state, -2, "azeritePowerIDs");
            SetInteger(state, "unlockLevel", tier.UnlockLevel);
            lua_rawseti(state, -2, tierIndex + 1);
        }
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static void SetInteger(
        lua_State state,
        string field,
        int value)
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

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
        }

        lua_createtable(state, 0, 3);
        SetInteger(state, "Base", 0);
        SetInteger(state, "Upgraded", 1);
        SetInteger(state, "Downgraded", 2);
        lua_setfield(state, -2, "AzeritePowerLevel");

        lua_createtable(state, 0, 3);
        SetInteger(state, "NumValues", 3);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 2);
        lua_setfield(state, -2, "AzeritePowerLevelMeta");

        lua_setglobal(state, "Enum");
    }
}
