using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPlayerInteractionManagerApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "ClearInteraction", "ConfirmationInteraction", "InteractUnit",
        "IsInteractingWithNpcOfType", "IsReplacingUnit",
        "IsValidNPCInteraction", "ReopenInteraction"
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
        lua_setglobal(state, "C_PlayerInteractionManager");
    }

    private static int Dispatch(lua_State state)
    {
        var interactions = LuaBindings.GetRuntime(state).PlayerInteractions;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "ClearInteraction":
            {
                var type = OptionalInteractionType(state, operation);
                interactions.ClearInteractionRequests++;
                interactions.LastClearInteractionType = type;
                if ((type is null || type == interactions.CurrentInteractionType) &&
                    interactions.HasActiveInteraction)
                {
                    Clear(interactions);
                }
                return 0;
            }
            case "ConfirmationInteraction":
            {
                var type = OptionalInteractionType(state, operation);
                if ((type is null || type == interactions.CurrentInteractionType) &&
                    interactions.HasActiveInteraction)
                {
                    interactions.ConfirmationInteractionRequests++;
                    interactions.LastConfirmationInteractionType = type;
                }
                return 0;
            }
            case "InteractUnit":
            {
                var unit = RequiredStringValue(state, 1, operation);
                var exactMatch = OptionalTruthyBoolean(state, 2, true);
                var looseTargeting = OptionalTruthyBoolean(state, 3, true);
                interactions.InteractionRequests.Add(
                    new WowPlayerInteractionRequest(unit, exactMatch, looseTargeting));
                PushBoolean(
                    state,
                    interactions.InteractUnitResults.TryGetValue(unit, out var result) &&
                    result);
                return 1;
            }
            case "IsInteractingWithNpcOfType":
                PushBoolean(
                    state,
                    interactions.CurrentInteractionType ==
                    RequiredInteractionType(state, 1, operation));
                return 1;
            case "IsReplacingUnit":
                PushBoolean(state, interactions.IsReplacingUnit);
                return 1;
            case "IsValidNPCInteraction":
                PushBoolean(
                    state,
                    interactions.ValidNpcInteractionTypes.Contains(
                        RequiredInteractionType(state, 1, operation)));
                return 1;
            case "ReopenInteraction":
                interactions.ReopenInteractionRequests++;
                return 0;
            default:
                return 0;
        }
    }

    private static void Clear(WowPlayerInteractionManagerState interactions)
    {
        interactions.HasActiveInteraction = false;
        interactions.HasPendingInteraction = false;
        interactions.CurrentInteractionType = 0;
        interactions.PendingInteractionType = 0;
        interactions.ValidNpcInteractionTypes.Clear();
    }

    private static int? OptionalInteractionType(
        lua_State state,
        string operation)
    {
        if (lua_type(state, 1) is LUA_TNONE or LUA_TNIL)
            return null;
        return RequiredInteractionType(state, 1, operation);
    }

    private static int RequiredInteractionType(
        lua_State state,
        int index,
        string operation)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(
                state,
                $"Usage: C_PlayerInteractionManager.{operation}(...)");
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value))
            return luaL_error(
                state,
                $"Usage: C_PlayerInteractionManager.{operation}(...)");
        var integer = unchecked((int)value);
        if (integer is < 0 or > 79)
            return luaL_error(
                state,
                $"Usage: C_PlayerInteractionManager.{operation}(...)");
        return integer;
    }

    private static string RequiredStringValue(
        lua_State state,
        int index,
        string operation)
    {
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(
                state,
                $"Usage: C_PlayerInteractionManager.{operation}(...)");
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static bool OptionalTruthyBoolean(
        lua_State state,
        int index,
        bool defaultValue) =>
        lua_type(state, index) is LUA_TNONE or LUA_TNIL
            ? defaultValue
            : lua_toboolean(state, index) != 0;

    private static void PushBoolean(lua_State state, bool value) =>
        lua_pushboolean(state, value ? 1 : 0);
}
