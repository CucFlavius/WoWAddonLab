using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowMountJournalApi : LuaApiModule
{
    private const int DynamicFlightModeSpellId = 436854;
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetDynamicFlightModeSpellID",
        "GetNumMountsNeedingFanfare",
        "GetNumDisplayedMounts",
        "ClearRecentFanfares",
        "GetNumMounts",
        "GetMountIDs",
        "GetMountInfoByID",
        "GetMountInfoExtraByID",
        "GetDisplayedMountInfo",
        "IsDragonridingUnlocked",
        "GetCollectedFilterSetting",
        "SetCollectedFilterSetting",
        "SetAllSourceFilters",
        "SetDefaultFilters",
        "IsSourceChecked",
        "SetSourceFilter",
        "IsTypeChecked",
        "SetTypeFilter",
        "SetSearch",
        "GetMountEquipmentUnlockLevel",
        "GetAppliedMountEquipmentID",
        "AreMountEquipmentEffectsSuppressed",
        "IsItemMountEquipment",
        "SummonByID",
        "IsUsingDefaultFilters",
        "IsValidSourceFilter",
        "IsValidTypeFilter"
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
        lua_setglobal(state, "C_MountJournal");
    }

    private static int Dispatch(lua_State state)
    {
        var journal = LuaBindings.GetRuntime(state).MountJournal;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetDynamicFlightModeSpellID":
                lua_pushinteger(state, DynamicFlightModeSpellId);
                return 1;
            case "GetNumMountsNeedingFanfare":
                lua_pushinteger(state, journal.MountsNeedingFanfare);
                return 1;
            case "GetNumDisplayedMounts":
                lua_pushinteger(state, journal.DisplayedMounts.Count);
                return 1;
            case "ClearRecentFanfares":
                journal.MountsNeedingFanfare = 0;
                return 0;
            case "GetNumMounts":
                lua_pushinteger(state, journal.TotalMountCount);
                return 1;
            case "GetMountIDs":
                return GetMountIds(state, journal);
            case "GetMountInfoByID":
                return GetMountInfoById(state, journal);
            case "GetMountInfoExtraByID":
                return GetMountInfoExtraById(state, journal);
            case "GetDisplayedMountInfo":
                return GetDisplayedMountInfo(state, journal);
            case "IsDragonridingUnlocked":
                lua_pushboolean(state, journal.IsDragonridingUnlocked ? 1 : 0);
                return 1;
            case "GetCollectedFilterSetting":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    "Usage: local isChecked = C_MountJournal.GetCollectedFilterSetting(filterIndex)");
                lua_pushboolean(
                    state,
                    journal.CollectedFilterSettings.TryGetValue(index, out var isChecked)
                    && isChecked
                        ? 1
                        : 0);
                return 1;
            }
            case "SetCollectedFilterSetting":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    "Usage: C_MountJournal.SetCollectedFilterSetting(filterIndex, isChecked)");
                if (lua_isnoneornil(state, 2) != 0)
                    return luaL_error(
                        state,
                        "Usage: C_MountJournal.SetCollectedFilterSetting(filterIndex, isChecked)");
                journal.CollectedFilterSettings[index] = lua_toboolean(state, 2) != 0;
                journal.IsUsingDefaultFilters = false;
                return 0;
            }
            case "SetAllSourceFilters":
            {
                var enabled = RequiredBoolean(
                    state,
                    "Usage: C_MountJournal.SetAllSourceFilters(isChecked)");
                foreach (var index in journal.ValidSourceFilters)
                    journal.SourceFilterSettings[index] = enabled;
                journal.IsUsingDefaultFilters = false;
                return 0;
            }
            case "SetDefaultFilters":
                ResetFilters(journal);
                return 0;
            case "IsSourceChecked":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    "Usage: local isChecked = C_MountJournal.IsSourceChecked(filterIndex)");
                lua_pushboolean(
                    state,
                    journal.SourceFilterSettings.TryGetValue(index, out var sourceChecked) &&
                    sourceChecked
                        ? 1
                        : 0);
                return 1;
            }
            case "SetSourceFilter":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    "Usage: C_MountJournal.SetSourceFilter(filterIndex, isChecked)");
                journal.SourceFilterSettings[index] = RequiredBoolean(
                    state,
                    "Usage: C_MountJournal.SetSourceFilter(filterIndex, isChecked)",
                    2);
                journal.IsUsingDefaultFilters = false;
                return 0;
            }
            case "IsTypeChecked":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    "Usage: local isChecked = C_MountJournal.IsTypeChecked(filterIndex)");
                lua_pushboolean(
                    state,
                    journal.TypeFilterSettings.TryGetValue(index, out var typeChecked) &&
                    typeChecked
                        ? 1
                        : 0);
                return 1;
            }
            case "SetTypeFilter":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    "Usage: C_MountJournal.SetTypeFilter(filterIndex, isChecked)");
                journal.TypeFilterSettings[index] = RequiredBoolean(
                    state,
                    "Usage: C_MountJournal.SetTypeFilter(filterIndex, isChecked)",
                    2);
                journal.IsUsingDefaultFilters = false;
                return 0;
            }
            case "SetSearch":
                if (lua_isstring(state, 1) == 0)
                    return luaL_error(state, "Usage: C_MountJournal.SetSearch(searchValue)");
                journal.SearchText = lua_tostring(state, 1) ?? string.Empty;
                return 0;
            case "GetMountEquipmentUnlockLevel":
                lua_pushinteger(state, journal.MountEquipmentUnlockLevel);
                return 1;
            case "GetAppliedMountEquipmentID":
                if (journal.AppliedMountEquipmentId is { } equipmentId)
                {
                    lua_pushinteger(state, equipmentId);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            case "AreMountEquipmentEffectsSuppressed":
                lua_pushboolean(
                    state,
                    journal.AreMountEquipmentEffectsSuppressed ? 1 : 0);
                return 1;
            case "IsItemMountEquipment":
            {
                var location = WowItemApi.RequiredItemLocation(
                    state,
                    "Usage: local isMountEquipment = C_MountJournal.IsItemMountEquipment(itemLocation)");
                lua_pushboolean(
                    state,
                    journal.MountEquipmentItemLocations.Contains(location) ? 1 : 0);
                return 1;
            }
            case "SummonByID":
            {
                var mountId = RequiredInt32(
                    state,
                    "Usage: C_MountJournal.SummonByID(mountID)");
                journal.SummonedMountId = mountId == 0 ? null : mountId;
                return 0;
            }
            case "IsUsingDefaultFilters":
                lua_pushboolean(state, journal.IsUsingDefaultFilters ? 1 : 0);
                return 1;
            case "IsValidSourceFilter":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    "Usage: local isValid = C_MountJournal.IsValidSourceFilter(filterIndex)");
                lua_pushboolean(
                    state,
                    journal.ValidSourceFilters.Contains(index) ? 1 : 0);
                return 1;
            }
            case "IsValidTypeFilter":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    "Usage: local isValid = C_MountJournal.IsValidTypeFilter(filterIndex)");
                lua_pushboolean(
                    state,
                    index is >= 1 and <= 32 and not 4 ? 1 : 0);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static int GetDisplayedMountInfo(
        lua_State state,
        WowMountJournalState journal)
    {
        var index = RequiredOneBasedIndex(
            state,
            "Usage: local name, spellID, icon, isActive, isUsable, sourceType, " +
            "isFavorite, isFactionSpecific, faction, shouldHideOnChar, isCollected, " +
            "mountID, isSteadyFlight = C_MountJournal.GetDisplayedMountInfo(displayIndex)");
        if (index < 1 || index > journal.DisplayedMounts.Count)
        {
            return 0;
        }

        return PushMountInfo(state, journal.DisplayedMounts[index - 1]);
    }

    private static int GetMountIds(
        lua_State state,
        WowMountJournalState journal)
    {
        var mountIds = journal.MountIds.Count > 0
            ? journal.MountIds
            : journal.DisplayedMounts.Select(mount => mount.MountId).ToArray();
        lua_newtable(state);
        for (var index = 0; index < mountIds.Count; index++)
        {
            lua_pushinteger(state, mountIds[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetMountInfoById(
        lua_State state,
        WowMountJournalState journal)
    {
        var mountId = RequiredInt32(
            state,
            "Usage: local name, spellID, icon, isActive, isUsable, sourceType, " +
            "isFavorite, isFactionSpecific, faction, shouldHideOnChar, isCollected, " +
            "mountID, isSteadyFlight = C_MountJournal.GetMountInfoByID(mountID)");
        var mount = journal.DisplayedMounts.FirstOrDefault(
            value => value.MountId == mountId);
        return mount is null ? 0 : PushMountInfo(state, mount);
    }

    private static int GetMountInfoExtraById(
        lua_State state,
        WowMountJournalState journal)
    {
        var mountId = RequiredInt32(
            state,
            "Usage: local creatureDisplayInfoID, description, source, isSelfMount, " +
            "mountTypeID, uiModelSceneID, animID, spellVisualKitID, " +
            "disablePlayerMountPreview = C_MountJournal.GetMountInfoExtraByID(mountID)");
        if (!journal.ExtraInfoByMountId.TryGetValue(mountId, out var info))
            return 0;

        if (info.CreatureDisplayInfoId is { } creatureDisplayInfoId)
            lua_pushinteger(state, creatureDisplayInfoId);
        else
            lua_pushnil(state);
        PushOptionalString(state, info.Description);
        PushOptionalString(state, info.Source);
        lua_pushboolean(state, info.IsSelfMount ? 1 : 0);
        lua_pushnumber(state, info.MountTypeId);
        lua_pushnumber(state, info.UiModelSceneId);
        lua_pushnumber(state, info.AnimationId);
        lua_pushnumber(state, info.SpellVisualKitId);
        lua_pushboolean(state, info.DisablePlayerMountPreview ? 1 : 0);
        return 9;
    }

    private static int PushMountInfo(
        lua_State state,
        WowDisplayedMountInfoState mount)
    {
        PushOptionalString(state, mount.Name);
        lua_pushinteger(state, mount.SpellId);
        lua_pushinteger(state, mount.IconFileId);
        lua_pushboolean(state, mount.IsActive ? 1 : 0);
        lua_pushboolean(state, mount.IsUsable ? 1 : 0);
        lua_pushinteger(state, mount.SourceType);
        lua_pushboolean(state, mount.IsFavorite ? 1 : 0);
        lua_pushboolean(state, mount.IsFactionSpecific ? 1 : 0);
        if (mount.Faction is { } faction)
        {
            lua_pushinteger(state, faction);
        }
        else
        {
            lua_pushnil(state);
        }
        lua_pushboolean(state, mount.ShouldHideOnCharacter ? 1 : 0);
        lua_pushboolean(state, mount.IsCollected ? 1 : 0);
        lua_pushinteger(state, mount.MountId);
        lua_pushboolean(state, mount.IsSteadyFlight ? 1 : 0);
        return 13;
    }

    private static int RequiredOneBasedIndex(lua_State state, string usage)
    {
        if (lua_isnumber(state, 1) == 0)
        {
            return luaL_error(state, usage);
        }

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
        {
            return luaL_error(state, usage);
        }

        return unchecked((int)(uint)(long)(value - 1d)) + 1;
    }

    private static int RequiredInt32(lua_State state, string usage)
    {
        if (lua_isnumber(state, 1) == 0)
        {
            return luaL_error(state, usage);
        }

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
        {
            return luaL_error(state, usage);
        }

        return (int)value;
    }

    private static bool RequiredBoolean(lua_State state, string usage, int index = 1)
    {
        if (lua_isnoneornil(state, index) != 0)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static void ResetFilters(WowMountJournalState journal)
    {
        journal.CollectedFilterSettings[1] = true;
        journal.CollectedFilterSettings[2] = true;
        journal.CollectedFilterSettings[3] = false;
        foreach (var index in journal.ValidSourceFilters)
            journal.SourceFilterSettings[index] = true;
        foreach (var index in journal.TypeFilterSettings.Keys.ToArray())
            journal.TypeFilterSettings[index] = true;
        journal.IsUsingDefaultFilters = true;
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
        {
            lua_pushnil(state);
        }
        else
        {
            lua_pushstring(state, value);
        }
    }
}
