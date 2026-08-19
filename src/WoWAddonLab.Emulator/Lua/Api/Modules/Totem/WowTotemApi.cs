using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTotemApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "DestroyTotem",
        "GetNumTotemSlots",
        "GetTotemCannotDismiss",
        "GetTotemDuration",
        "GetTotemInfo",
        "GetTotemTimeLeft",
        "TargetTotem"
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

        if (operation == "GetNumTotemSlots")
        {
            lua_pushinteger(state, runtime.Totems.SlotCount);
            return 1;
        }

        var slotIndex = RequiredOneBasedSlotIndex(
            state,
            operation switch
            {
                "DestroyTotem" => "Usage: DestroyTotem(slot)",
                "GetTotemCannotDismiss" =>
                    "Usage: local cannotDismiss = GetTotemCannotDismiss(slot)",
                "GetTotemDuration" =>
                    "Usage: local duration = GetTotemDuration(slot)",
                "GetTotemInfo" =>
                    "Usage: local haveTotem, totemName, startTime, duration, " +
                    "icon, modRate, spellID = GetTotemInfo(slot)",
                "GetTotemTimeLeft" =>
                    "Usage: local timeLeft = GetTotemTimeLeft(slot)",
                "TargetTotem" => "Usage: TargetTotem(slot)",
                _ => string.Empty
            });
        var validSlot = slotIndex < runtime.Totems.SlotCount;
        var slot = validSlot ? (int)slotIndex + 1 : 0;

        switch (operation)
        {
            case "GetTotemInfo":
                if (!validSlot)
                    return 0;
                if (runtime.Totems.Find(slot) is not { } value)
                {
                    lua_pushboolean(state, 0);
                    lua_pushstring(state, string.Empty);
                    lua_pushnumber(state, 0);
                    lua_pushnumber(state, 0);
                    lua_pushinteger(state, 0);
                    lua_pushnumber(state, 0);
                    lua_pushinteger(state, 0);
                    return 7;
                }
                lua_pushboolean(state, 1);
                lua_pushstring(state, value.Name);
                lua_pushnumber(state, value.StartTime);
                lua_pushnumber(state, value.Duration);
                lua_pushnumber(state, value.IconFileId);
                lua_pushnumber(state, value.ModRate);
                lua_pushinteger(state, value.SpellId);
                return 7;
            case "GetTotemDuration":
                if (!validSlot || runtime.Totems.Find(slot) is not { } duration)
                {
                    lua_pushnil(state);
                    return 1;
                }
                PushDuration(state, duration);
                return 1;
            case "GetTotemTimeLeft":
                if (!validSlot)
                {
                    lua_pushnil(state);
                    return 1;
                }
                if (runtime.Totems.Find(slot) is not { } timed)
                {
                    lua_pushnumber(state, 0);
                    return 1;
                }
                lua_pushnumber(
                    state,
                    Math.Max(
                        0,
                        (timed.Duration - (runtime.Time - timed.StartTime)) /
                        timed.ModRate));
                return 1;
            case "GetTotemCannotDismiss":
                if (!validSlot)
                    lua_pushnil(state);
                else
                    lua_pushboolean(
                        state,
                        runtime.Totems.Find(slot)?.CannotDismiss == true ? 1 : 0);
                return 1;
            case "DestroyTotem":
                if (validSlot && runtime.Totems.Remove(slot))
                    runtime.TriggerEvent("PLAYER_TOTEM_UPDATE", slot);
                return 0;
            case "TargetTotem":
                if (validSlot && runtime.Totems.Find(slot) is not null)
                    runtime.Totems.TargetedSlot = slot;
                return 0;
            default:
                return 0;
        }
    }

    private static uint RequiredOneBasedSlotIndex(
        lua_State state,
        string usage)
    {
        if (lua_isnumber(state, 1) == 0)
            luaL_error(state, usage);

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
            luaL_error(state, usage);

        return unchecked((uint)value - 1);
    }

    private static void PushDuration(lua_State state, WowTotemSlotState duration)
        => WowDurationApi.Push(
            state,
            new WowDurationState(
                duration.StartTime,
                duration.Duration,
                duration.ModRate));

    private static void SetNumber(lua_State state, string key, double value)
    {
        lua_pushstring(state, key);
        lua_pushnumber(state, value);
        lua_settable(state, -3);
    }
}
