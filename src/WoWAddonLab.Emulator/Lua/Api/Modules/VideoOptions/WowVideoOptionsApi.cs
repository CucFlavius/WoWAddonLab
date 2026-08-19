using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowVideoOptionsApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] NamespaceFunctions =
    [
        "GetCurrentGameWindowSize",
        "GetDefaultGameWindowSize",
        "GetGameWindowSizes",
        "GetGxAdapterInfo",
        "IsSpellVisualDensitySystemSupported",
        "SetGameWindowSize"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in new[]
                 {
                     "GetCurrentGraphicsAPI",
                     "GetGraphicsAPIs",
                     "GetMinRenderScale",
                     "GetMaxRenderScale",
                     "GetCameraFOVDefaults",
                     "AntiAliasingSupported",
                     "IsGraphicsSettingValueSupported",
                     "IsGraphicsCVarValueSupported"
                 })
            LuaBindings.RegisterClosureGlobal(state, function, Callback);

        lua_newtable(state);
        foreach (var function in NamespaceFunctions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_VideoOptions");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var video = runtime.VideoOptions;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetMinRenderScale":
                lua_pushnumber(state, video.MinimumRenderScale);
                return 1;
            case "GetMaxRenderScale":
                lua_pushnumber(state, video.MaximumRenderScale);
                return 1;
            case "GetCameraFOVDefaults":
                lua_pushnumber(state, video.CameraFovDefault);
                lua_pushnumber(state, video.CameraFovMinimum);
                lua_pushnumber(state, video.CameraFovMaximum);
                return 3;
            case "GetCurrentGraphicsAPI":
                lua_pushstring(state, video.CurrentGraphicsApi);
                return 1;
            case "GetGraphicsAPIs":
                foreach (var api in video.GraphicsApis)
                    lua_pushstring(state, api);
                return video.GraphicsApis.Count;
            case "IsGraphicsCVarValueSupported":
                RequireStringAndOptionIndex(
                    state,
                    "Usage: IsGraphicsCVarValueSupported(cvar, optionIndex)");
                lua_pushnumber(state, 0);
                return 1;
            case "IsGraphicsSettingValueSupported":
                RequireStringAndOptionIndex(
                    state,
                    "Usage: IsGraphicsSettingValueSupported(" +
                    "cvar, optionIndex [, isRaid])");
                lua_pushnumber(state, 0);
                return 1;
            case "AntiAliasingSupported":
                lua_pushboolean(state, 1);
                lua_pushboolean(
                    state,
                    video.AdvancedAntiAliasingAvailable ? 1 : 0);
                lua_pushboolean(
                    state,
                    video.UpscalingAntiAliasingAvailable ? 1 : 0);
                return 3;
            case "IsSpellVisualDensitySystemSupported":
                lua_pushboolean(
                    state,
                    video.SpellVisualDensitySystemSupported ? 1 : 0);
                return 1;
            case "GetCurrentGameWindowSize":
                PushSize(state, runtime.Ui.PhysicalWidth, runtime.Ui.PhysicalHeight);
                return 1;
            case "GetDefaultGameWindowSize":
                RequireUInt32(
                    state,
                    1,
                    "Usage: local size = " +
                    "C_VideoOptions.GetDefaultGameWindowSize(monitor)");
                var defaultSize = video.DefaultGameWindowSize;
                PushSize(
                    state,
                    defaultSize?.Width ?? (uint)runtime.Ui.PhysicalWidth,
                    defaultSize?.Height ?? (uint)runtime.Ui.PhysicalHeight);
                return 1;
            case "GetGameWindowSizes":
                RequireUInt32(
                    state,
                    1,
                    "Usage: local sizes = " +
                    "C_VideoOptions.GetGameWindowSizes(monitor, fullscreen)");
                RequireValue(
                    state,
                    2,
                    "Usage: local sizes = " +
                    "C_VideoOptions.GetGameWindowSizes(monitor, fullscreen)");
                var sizes = video.GameWindowSizes.Count == 0
                    ? [(Width: (uint)runtime.Ui.PhysicalWidth,
                        Height: (uint)runtime.Ui.PhysicalHeight)]
                    : video.GameWindowSizes;
                lua_createtable(state, sizes.Count, 0);
                for (var index = 0; index < sizes.Count; index++)
                {
                    PushSize(state, sizes[index].Width, sizes[index].Height);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "GetGxAdapterInfo":
                lua_createtable(state, video.Adapters.Count, 0);
                for (var index = 0; index < video.Adapters.Count; index++)
                {
                    var adapter = video.Adapters[index];
                    lua_createtable(state, 0, 3);
                    lua_pushstring(state, adapter.Name);
                    lua_setfield(state, -2, "name");
                    lua_pushboolean(state, adapter.IsLowPower ? 1 : 0);
                    lua_setfield(state, -2, "isLowPower");
                    lua_pushboolean(state, adapter.IsExternal ? 1 : 0);
                    lua_setfield(state, -2, "isExternal");
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "SetGameWindowSize":
                var width = RequireUInt32(
                    state,
                    1,
                    "Usage: C_VideoOptions.SetGameWindowSize(x, y)");
                var height = RequireUInt32(
                    state,
                    2,
                    "Usage: C_VideoOptions.SetGameWindowSize(x, y)");
                if (width == 0 && height == 0)
                {
                    SetRequestedResolution(runtime, "auto", width, height);
                }
                else if (width >= 120 && height >= 120)
                {
                    SetRequestedResolution(
                        runtime,
                        $"{width}x{height}",
                        width,
                        height);
                }
                return 0;
            default:
                return 0;
        }
    }

    private static void PushSize(lua_State state, double width, double height)
    {
        lua_createtable(state, 0, 2);
        lua_pushnumber(state, width);
        lua_setfield(state, -2, "x");
        lua_pushnumber(state, height);
        lua_setfield(state, -2, "y");
        ApplyMixinToTopTable(state, "Vector2DMixin");
    }

    private static void ApplyMixinToTopTable(lua_State state, string mixinName)
    {
        var target = lua_gettop(state);
        lua_getglobal(state, mixinName);
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

    private static void RequireStringAndOptionIndex(
        lua_State state,
        string usage)
    {
        var cvarType = lua_type(state, 1);
        if (cvarType is not (LUA_TSTRING or LUA_TNUMBER))
        {
            luaL_error(state, usage);
            return;
        }

        if (lua_isnumber(state, 2) == 0)
            luaL_error(state, usage);
    }

    private static uint RequireUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TNUMBER)
        {
            luaL_error(state, usage);
            return 0;
        }

        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }

        return (uint)value;
    }

    private static void RequireValue(
        lua_State state,
        int index,
        string usage)
    {
        if (index <= lua_gettop(state) && lua_type(state, index) != LUA_TNIL)
            return;
        luaL_error(state, usage);
    }

    private static void SetRequestedResolution(
        LuaRuntime runtime,
        string value,
        uint width,
        uint height)
    {
        runtime.VideoOptions.RequestedGameWindowSize = (width, height);
        if (!runtime.CVars.TryGet("xNewResolution", out _))
            runtime.CVars.Define("xNewResolution", "auto");
        runtime.CVars.SetValue("xNewResolution", value);
    }
}
