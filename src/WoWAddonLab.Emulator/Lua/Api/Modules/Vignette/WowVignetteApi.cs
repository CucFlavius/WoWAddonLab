using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowVignetteApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] Functions =
    [
        "FindBestUniqueVignette",
        "GetHealthPercent",
        "GetRecommendedGroupSize",
        "GetVignetteInfo",
        "GetVignettePosition",
        "GetVignettes"
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
        lua_setglobal(state, "C_VignetteInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        var vignettes = LuaBindings.GetRuntime(state).Vignettes;
        switch (operation)
        {
            case "GetVignettes":
                RequireArgumentCount(state, 0, "Usage: local vignetteGUIDs = C_VignetteInfo.GetVignettes()");
                PushGuidArray(state, vignettes.Guids);
                return 1;
            case "FindBestUniqueVignette":
                return FindBestUniqueVignette(state, vignettes);
            case "GetHealthPercent":
            {
                var guid = RequiredGuid(
                    state,
                    1,
                    "Usage: local healthPct = C_VignetteInfo.GetHealthPercent(vignetteGUID)");
                if (vignettes.HealthPercentByGuid.TryGetValue(guid, out var health))
                    lua_pushnumber(state, health);
                else
                    lua_pushnil(state);
                return 1;
            }
            case "GetRecommendedGroupSize":
            {
                var guid = RequiredGuid(
                    state,
                    1,
                    "Usage: local minGroupSize, maxGroupSize = " +
                    "C_VignetteInfo.GetRecommendedGroupSize(vignetteGUID)");
                if (!vignettes.RecommendedGroupSizeByGuid.TryGetValue(guid, out var sizes))
                    return 0;
                lua_pushinteger(state, sizes.Minimum);
                lua_pushinteger(state, sizes.Maximum);
                return 2;
            }
            case "GetVignetteInfo":
            {
                var guid = RequiredGuid(
                    state,
                    1,
                    "Usage: local vignetteInfo = C_VignetteInfo.GetVignetteInfo(vignetteGUID)");
                if (!vignettes.InfoByGuid.TryGetValue(guid, out var info))
                {
                    lua_pushnil(state);
                    return 1;
                }
                PushVignetteInfo(state, info);
                return 1;
            }
            case "GetVignettePosition":
                return GetVignettePosition(state, vignettes);
            default:
                return 0;
        }
    }

    private static int FindBestUniqueVignette(
        lua_State state,
        WowVignetteState vignettes)
    {
        const string usage =
            "Usage: local bestUniqueVignetteIndex = " +
            "C_VignetteInfo.FindBestUniqueVignette(vignetteGUIDs)";
        RequireArgumentCount(state, 1, usage);
        if (lua_istable(state, 1) == 0)
            return luaL_error(state, usage);

        var count = (int)lua_objlen(state, 1);
        var guids = new string[count];
        for (var index = 0; index < count; index++)
        {
            lua_rawgeti(state, 1, index + 1);
            if (lua_type(state, -1) != LUA_TSTRING)
            {
                lua_pop(state, 1);
                return luaL_error(state, usage);
            }
            guids[index] = lua_tostring(state, -1) ?? string.Empty;
            lua_pop(state, 1);
        }

        var preferred = vignettes.BestUniqueGuid;
        var resultIndex = preferred is null
            ? Array.FindIndex(
                guids,
                guid => vignettes.InfoByGuid.TryGetValue(guid, out var info) &&
                    info.IsUnique)
            : Array.FindIndex(
                guids,
                guid => guid.Equals(preferred, StringComparison.OrdinalIgnoreCase));
        if (resultIndex < 0)
            lua_pushnil(state);
        else
            lua_pushinteger(state, resultIndex + 1);
        return 1;
    }

    private static int GetVignettePosition(
        lua_State state,
        WowVignetteState vignettes)
    {
        const string usage =
            "Usage: local vignettePosition, vignetteFacing = " +
            "C_VignetteInfo.GetVignettePosition(vignetteGUID, uiMapID)";
        RequireArgumentCount(state, 2, usage);
        var guid = ReadGuid(state, 1, usage);
        var mapId = ReadInt32(state, 2, usage);
        if (!vignettes.PositionsByGuid.TryGetValue(guid, out var positions) ||
            !positions.TryGetValue(mapId, out var position))
        {
            return 0;
        }

        lua_createtable(state, 0, 2);
        SetNumberField(state, "x", position.X);
        SetNumberField(state, "y", position.Y);
        ApplyVector2Mixin(state);
        if (position.Facing is { } facing)
            lua_pushnumber(state, facing);
        else
            lua_pushnil(state);
        return 2;
    }

    private static void ApplyVector2Mixin(lua_State state)
    {
        var target = lua_gettop(state);
        lua_getglobal(state, "Vector2DMixin");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            return;
        }

        var mixin = lua_gettop(state);
        lua_pushnil(state);
        while (lua_next(state, mixin) != 0)
        {
            lua_pushvalue(state, -2);
            lua_pushvalue(state, -2);
            lua_settable(state, target);
            lua_pop(state, 1);
        }
        lua_pop(state, 1);
    }

    private static void PushGuidArray(lua_State state, IReadOnlyList<string> guids)
    {
        lua_createtable(state, guids.Count, 0);
        for (var index = 0; index < guids.Count; index++)
        {
            lua_pushstring(state, guids[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushVignetteInfo(lua_State state, WowVignetteInfo info)
    {
        lua_createtable(state, 0, 19);
        SetStringField(state, "vignetteGUID", info.VignetteGuid);
        SetStringField(state, "objectGUID", info.ObjectGuid);
        SetOptionalStringField(state, "name", info.Name);
        SetBooleanField(state, "isDead", info.IsDead);
        SetBooleanField(state, "onWorldMap", info.OnWorldMap);
        SetBooleanField(state, "zoneInfiniteAOI", info.ZoneInfiniteAoi);
        SetBooleanField(state, "onMinimap", info.OnMinimap);
        SetBooleanField(state, "isUnique", info.IsUnique);
        SetBooleanField(state, "inFogOfWar", info.InFogOfWar);
        SetOptionalStringField(state, "atlasName", info.AtlasName);
        SetBooleanField(state, "hasTooltip", info.HasTooltip);
        SetIntegerField(state, "vignetteID", info.VignetteId);
        SetIntegerField(state, "type", info.Type);
        SetIntegerField(state, "rewardQuestID", info.RewardQuestId);
        SetOptionalIntegerField(state, "tooltipWidgetSet", info.TooltipWidgetSet);
        SetOptionalIntegerField(state, "iconWidgetSet", info.IconWidgetSet);
        SetOptionalBooleanField(
            state,
            "addPaddingAboveTooltipWidgets",
            info.AddPaddingAboveTooltipWidgets);
        if (info.MapPin is not null)
        {
            PushMapPin(state, info.MapPin);
            lua_setfield(state, -2, "mapPin");
        }
        SetOptionalIntegerField(state, "objectiveType", info.ObjectiveType);
    }

    private static void PushMapPin(lua_State state, WowVignetteMapPin mapPin)
    {
        lua_createtable(state, 0, 4);
        PushMapPinButton(state, mapPin.Button);
        lua_setfield(state, -2, "button");
        PushMapPinButton(state, mapPin.ButtonSelected);
        lua_setfield(state, -2, "buttonSelected");
        SetOptionalStringField(state, "underlay", mapPin.Underlay);
        SetOptionalStringField(state, "outerGlow", mapPin.OuterGlow);
    }

    private static void PushMapPinButton(
        lua_State state,
        WowVignetteMapPinButton button)
    {
        lua_createtable(state, 0, 5);
        SetOptionalStringField(state, "normal", button.Normal);
        SetOptionalStringField(state, "pressed", button.Pressed);
        SetOptionalStringField(state, "highlight", button.Highlight);
        SetOptionalStringField(state, "icon", button.Icon);
        SetBooleanField(
            state,
            "useNormalAsHiglight",
            button.UseNormalAsHiglight);
    }

    private static string RequiredGuid(lua_State state, int expected, string usage)
    {
        RequireArgumentCount(state, expected, usage);
        return ReadGuid(state, 1, usage);
    }

    private static string ReadGuid(lua_State state, int index, string usage)
    {
        if (index > lua_gettop(state) || lua_type(state, index) != LUA_TSTRING)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static int ReadInt32(lua_State state, int index, string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return (int)value;
    }

    private static void RequireArgumentCount(
        lua_State state,
        int expected,
        string usage)
    {
        if (lua_gettop(state) != expected)
            luaL_error(state, usage);
    }

    private static void SetStringField(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalStringField(
        lua_State state,
        string name,
        string? value)
    {
        if (value is null)
            return;
        SetStringField(state, name, value);
    }

    private static void SetIntegerField(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalIntegerField(
        lua_State state,
        string name,
        int? value)
    {
        if (value is null)
            return;
        SetIntegerField(state, name, value.Value);
    }

    private static void SetNumberField(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBooleanField(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalBooleanField(
        lua_State state,
        string name,
        bool? value)
    {
        if (value is null)
            return;
        SetBooleanField(state, name, value.Value);
    }
}
