using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowMinimapApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "ClearAllTracking",
                     "CanTrackBattlePets",
                     "GetDefaultTrackingValue",
                     "GetNumTrackingTypes",
                     "GetTrackingFilter",
                     "GetTrackingInfo",
                     "IsFilteredOut",
                     "SetTracking",
                     "ShouldUseHybridMinimap"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Minimap");
    }

    private static int Dispatch(lua_State state)
    {
        var minimap = LuaBindings.GetRuntime(state).Minimap;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "ClearAllTracking":
                foreach (var trackingEntry in minimap.Tracking)
                    trackingEntry.Active = false;
                return 0;
            case "GetNumTrackingTypes":
                lua_pushinteger(state, minimap.Tracking.Count);
                return 1;
            case "ShouldUseHybridMinimap":
                lua_pushboolean(state, minimap.ShouldUseHybridMinimap ? 1 : 0);
                return 1;
            case "CanTrackBattlePets":
                lua_pushboolean(state, minimap.CanTrackBattlePets ? 1 : 0);
                return 1;
            case "IsFilteredOut":
            {
                const string usage =
                    "Usage: local isFiltered = C_Minimap.IsFilteredOut(filterType)";
                var filterMask = RequiredTrackingFilterMask(state, usage);
                var activeMask = TrackingFilterMask(minimap, useDefaults: false);
                var filtered = filterMask != 0 && (filterMask & activeMask) == 0;
                lua_pushboolean(state, filtered ? 1 : 0);
                return 1;
            }
            case "GetDefaultTrackingValue":
            {
                const string usage =
                    "Usage: local defaultValue = " +
                    "C_Minimap.GetDefaultTrackingValue(filterType)";
                var filterMask = RequiredTrackingFilterMask(state, usage);
                var defaultMask = TrackingFilterMask(minimap, useDefaults: true);
                var defaultActive = (defaultMask & filterMask) == filterMask;
                lua_pushboolean(state, defaultActive ? 1 : 0);
                return 1;
            }
            case "GetTrackingFilter":
            {
                const string usage =
                    "Usage: local trackingType = " +
                    "C_Minimap.GetTrackingFilter(spellIndex)";
                var zeroBasedIndex = RequiredZeroBasedIndex(state, usage);
                lua_createtable(state, 0, 2);
                if (Find(minimap, zeroBasedIndex) is { } filterTracking)
                {
                    if (filterTracking.SpellId is { } spellId)
                        SetNumber(state, "spellID", spellId);
                    else
                        SetNumber(state, "filterID", filterTracking.Filter);
                }
                return 1;
            }
            case "GetTrackingInfo":
            {
                const string usage =
                    "Usage: local trackingInfo = " +
                    "C_Minimap.GetTrackingInfo(spellIndex)";
                var zeroBasedIndex = RequiredZeroBasedIndex(state, usage);
                if (Find(minimap, zeroBasedIndex) is not { } tracking)
                {
                    lua_pushnil(state);
                    return 1;
                }
                lua_createtable(state, 0, 6);
                SetString(state, "name", tracking.Name);
                SetNumber(state, "texture", tracking.Texture);
                SetBoolean(state, "active", tracking.Active);
                SetString(state, "type", tracking.Type);
                SetNumber(state, "subType", tracking.SubType);
                if (tracking.SpellId is { } spellId)
                    SetNumber(state, "spellID", spellId);
                return 1;
            }
            case "SetTracking":
            {
                const string usage = "Usage: C_Minimap.SetTracking(index, on)";
                var zeroBasedIndex = RequiredZeroBasedIndex(state, usage);
                if (lua_gettop(state) < 2)
                    return luaL_error(state, usage);
                if (Find(minimap, zeroBasedIndex) is { } changed)
                    changed.Active = lua_toboolean(state, 2) != 0;
                return 0;
            }
            default:
                return 0;
        }
    }

    private static WowMinimapTrackingState? Find(
        WowMinimapState minimap,
        uint zeroBasedIndex)
    {
        return zeroBasedIndex < minimap.Tracking.Count
            ? minimap.Tracking[(int)zeroBasedIndex]
            : null;
    }

    private static uint RequiredZeroBasedIndex(lua_State state, string usage)
    {
        if (lua_gettop(state) < 1 || lua_isnumber(state, 1) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var oneBased = lua_tonumber(state, 1);
        if (!double.IsFinite(oneBased) ||
            oneBased < 0 ||
            oneBased > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }

        var shifted = oneBased - 1;
        var nativeIndex = shifted < int.MinValue || shifted > int.MaxValue
            ? int.MinValue
            : (int)shifted;
        return unchecked((uint)nativeIndex);
    }

    private static int RequiredTrackingFilterMask(
        lua_State state,
        string usage)
    {
        if (lua_gettop(state) < 1 || lua_isnumber(state, 1) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, 1);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }

        var mask = (int)number;
        if (mask is < 0 or > 0x7F_FFFF)
        {
            luaL_error(state, usage);
            return 0;
        }
        return mask;
    }

    private static int TrackingFilterMask(
        WowMinimapState minimap,
        bool useDefaults)
    {
        var mask = 0;
        foreach (var tracking in minimap.Tracking)
        {
            if (tracking.SpellId is not null ||
                !(useDefaults ? tracking.DefaultActive : tracking.Active))
            {
                continue;
            }
            mask |= tracking.Filter;
        }
        return mask;
    }

    private static void SetString(lua_State state, string key, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetNumber(lua_State state, string key, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetBoolean(lua_State state, string key, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, key);
    }
}
