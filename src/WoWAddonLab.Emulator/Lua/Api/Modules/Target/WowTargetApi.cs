using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTargetApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] Functions =
    [
        "ClearFocus",
        "ClearTarget",
        "FocusUnit",
        "IsTargetLoose",
        "TargetUnit"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "ClearFocus":
                if (runtime.Client.InCombatLockdown)
                    return 0;
                if (!runtime.Target.HasFocus)
                    return 0;
                runtime.Target.HasFocus = false;
                runtime.Target.FocusGuid = null;
                runtime.Units.ClearAlias("focus");
                runtime.TriggerEvent("PLAYER_FOCUS_CHANGED");
                return 0;
            case "ClearTarget":
            {
                if (runtime.Client.InCombatLockdown)
                {
                    lua_pushboolean(state, 0);
                    return 1;
                }
                var changed = runtime.Target.HasTarget;
                runtime.Target.HasTarget = false;
                runtime.Target.TargetGuid = null;
                runtime.Units.ClearAlias("target");
                if (changed)
                    runtime.TriggerEvent("PLAYER_TARGET_CHANGED");
                lua_pushboolean(state, changed ? 1 : 0);
                return 1;
            }
            case "FocusUnit":
                return FocusUnit(state, runtime);
            case "IsTargetLoose":
                lua_pushboolean(state, runtime.Target.IsLoose ? 1 : 0);
                return 1;
            case "TargetUnit":
                return TargetUnit(state, runtime);
            default:
                return 0;
        }
    }

    private static int FocusUnit(
        lua_State state,
        LuaRuntime runtime)
    {
        if (runtime.Client.InCombatLockdown)
            return 0;
        if (!TryReadOptionalString(state, 1, out var unitToken))
            return luaL_error(state, "Usage: FocusUnit([name])");

        var unit = unitToken.Length == 0
            ? runtime.Units.Find("target")
            : runtime.Units.Find(unitToken);
        if (unit is null)
            return 0;

        var previousGuid = runtime.Target.FocusGuid;
        runtime.Units.AssignAlias("focus", unit);
        runtime.Target.HasFocus = true;
        runtime.Target.FocusGuid = unit.Guid;
        if (!unit.Guid.Equals(previousGuid, StringComparison.OrdinalIgnoreCase))
            runtime.TriggerEvent("PLAYER_FOCUS_CHANGED");
        return 0;
    }

    private static int TargetUnit(
        lua_State state,
        LuaRuntime runtime)
    {
        if (runtime.Client.InCombatLockdown)
            return 0;
        if (!TryReadOptionalString(state, 1, out var name))
            return luaL_error(state, "Usage: TargetUnit([name, exactMatch])");

        var exactMatch = lua_gettop(state) >= 2 &&
                         lua_type(state, 2) != LUA_TNIL &&
                         lua_toboolean(state, 2) != 0;
        var unit = runtime.Units.ResolveTarget(name, exactMatch);
        if (unit is null)
            return 0;

        var previousGuid = runtime.Target.TargetGuid;
        runtime.Units.AssignAlias("target", unit);
        runtime.Target.HasTarget = true;
        runtime.Target.TargetGuid = unit.Guid;
        if (!unit.Guid.Equals(previousGuid, StringComparison.OrdinalIgnoreCase))
            runtime.TriggerEvent("PLAYER_TARGET_CHANGED");
        return 0;
    }

    private static bool TryReadOptionalString(
        lua_State state,
        int index,
        out string value)
    {
        if (lua_gettop(state) < index || lua_type(state, index) == LUA_TNIL)
        {
            value = string.Empty;
            return true;
        }
        if (lua_type(state, index) != LUA_TSTRING)
        {
            value = string.Empty;
            return false;
        }
        value = lua_tostring(state, index) ?? string.Empty;
        return true;
    }
}
