using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowGameUiApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "SetUIVisibility", Callback);
        LuaBindings.RegisterClosureGlobal(state, "CreateWindow", Callback);
        LuaBindings.RegisterClosureGlobal(state, "ConsoleSetFontHeight", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetMonitorCount", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetMonitorName", Callback);
        LuaBindings.RegisterClosureGlobal(state, "MultiSampleAntiAliasingSupported", Callback);
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "DoesAnyDisplayHaveNotch",
                     "GetTopLeftNotchSafeRegion",
                     "GetTopRightNotchSafeRegion",
                     "GetUIParent",
                     "GetWorldFrame",
                     "Reload",
                     "ShouldUIParentAvoidNotch"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_UI");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "SetUIVisibility":
                RequireArgument(state, 1, "Usage: SetUIVisibility(visible)");
                runtime.Client.UiVisible = lua_toboolean(state, 1) != 0;
                return 0;
            case "CreateWindow":
                lua_pushnil(state);
                return 1;
            case "ConsoleSetFontHeight":
                if (lua_isnumber(state, 1) == 0)
                    return luaL_error(
                        state,
                        "Usage: ConsoleSetFontHeight(fontHeightInPixels)");
                runtime.GameUi.ConsoleFontHeightPixels = (float)lua_tonumber(state, 1);
                return 0;
            case "GetMonitorCount":
                lua_pushinteger(state, runtime.GameUi.Monitors.Count);
                return 1;
            case "GetMonitorName":
                if (lua_isnumber(state, 1) == 0)
                    return 0;

                var monitorIndex = (int)lua_tonumber(state, 1) - 1;
                if (monitorIndex >= 0 && monitorIndex < runtime.GameUi.Monitors.Count)
                {
                    var monitor = runtime.GameUi.Monitors[monitorIndex];
                    if (monitor.Name is null)
                        lua_pushnil(state);
                    else
                        lua_pushstring(state, monitor.Name);
                    lua_pushboolean(state, monitor.IsPrimary ? 1 : 0);
                    return 2;
                }

                lua_pushnil(state);
                lua_pushboolean(state, 0);
                return 2;
            case "MultiSampleAntiAliasingSupported":
                foreach (var option in runtime.GameUi.MultisampleOptions)
                {
                    lua_pushstring(state, $"{option.QualityIndex},0");
                    lua_pushinteger(state, option.SampleCount);
                    lua_pushinteger(state, option.SampleCount);
                }
                return runtime.GameUi.MultisampleOptions.Count * 3;
            case "DoesAnyDisplayHaveNotch":
                lua_pushboolean(state, runtime.GameUi.DoesAnyDisplayHaveNotch ? 1 : 0);
                return 1;
            case "GetTopLeftNotchSafeRegion":
                return PushSafeRegion(state, runtime.GameUi.TopLeftNotchSafeRegion);
            case "GetTopRightNotchSafeRegion":
                return PushSafeRegion(state, runtime.GameUi.TopRightNotchSafeRegion);
            case "GetUIParent":
                runtime.PushObject(runtime.Ui.Find(runtime.Ui.UiParentId));
                return 1;
            case "GetWorldFrame":
                runtime.PushObject(runtime.Ui.Find("WorldFrame"));
                return 1;
            case "Reload":
                runtime.GameUi.ReloadRequestCount++;
                return 0;
            case "ShouldUIParentAvoidNotch":
                lua_pushboolean(state, runtime.GameUi.ShouldUiParentAvoidNotch ? 1 : 0);
                return 1;
            default:
                return 0;
        }
    }

    private static int PushSafeRegion(lua_State state, WowNotchSafeRegion region)
    {
        lua_pushnumber(state, region.X);
        lua_pushnumber(state, region.X + region.Width);
        lua_pushnumber(state, region.Y);
        lua_pushnumber(state, region.Y + region.Height);
        return 4;
    }

    private static void RequireArgument(lua_State state, int index, string usage)
    {
        if (lua_gettop(state) >= index)
            return;
        luaL_error(state, usage);
    }
}
