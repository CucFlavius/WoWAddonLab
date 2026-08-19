using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTrainerApi : LuaApiModule
{
    private const int TrainerInteractionType = 7;

    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        foreach (var function in new[]
                 {
                     "CloseTrainer",
                     "GetNumTrainerServices",
                     "GetTrainerServiceStepIndex",
                     "GetTrainerServiceTypeFilter",
                     "GetTrainerTradeskillRankValues",
                     "IsTradeskillTrainer",
                     "SetTrainerServiceTypeFilter"
                 })
        {
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
        }
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var trainer = runtime.Trainer;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation == "CloseTrainer")
        {
            CloseTrainer(runtime);
            return 0;
        }
        if (operation == "GetNumTrainerServices")
        {
            lua_pushnumber(state, trainer.ServiceCount);
            return 1;
        }
        if (operation == "IsTradeskillTrainer")
        {
            lua_pushboolean(state, trainer.IsTradeSkillTrainer ? 1 : 0);
            return 1;
        }
        if (operation == "GetTrainerServiceStepIndex")
        {
            if (trainer.ServiceStepIndex is { } stepIndex)
                lua_pushnumber(state, stepIndex + 1);
            else
                return 0;
            return 1;
        }
        if (operation == "GetTrainerTradeskillRankValues")
        {
            if (!trainer.IsTradeSkillTrainer ||
                trainer.TradeSkillRank is not { } rank)
            {
                return 0;
            }
            lua_pushnumber(state, rank);
            lua_pushnumber(state, trainer.TradeSkillMaxRank);
            lua_pushnumber(state, trainer.TradeSkillRankModifier);
            return 3;
        }
        if (operation == "GetTrainerServiceTypeFilter")
        {
            var filter = RequiredFilter(
                state,
                "Usage: GetTrainerServiceTypeFilter(\"type\")",
                "Bad service type in GetTrainerServiceTypeFilter",
                allowAll: false);
            lua_pushboolean(state, trainer.EnabledServiceTypeFilters.Contains(filter) ? 1 : 0);
            return 1;
        }
        if (operation == "SetTrainerServiceTypeFilter")
        {
            const string usage =
                "Usage: SetTrainerServiceTypeFilter(type (string), on/off (bool) [, exclusive])";
            var filter = RequiredFilter(
                state,
                usage,
                "Bad service type in SetTrainerServiceTypeFilter",
                allowAll: true);
            if (filter.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                trainer.EnabledServiceTypeFilters.Clear();
                trainer.EnabledServiceTypeFilters.Add("available");
                trainer.EnabledServiceTypeFilters.Add("unavailable");
                trainer.EnabledServiceTypeFilters.Add("used");
                runtime.TriggerEvent("TRAINER_UPDATE");
                return 0;
            }

            if (lua_type(state, 2) != LUA_TBOOLEAN)
            {
                return luaL_error(
                    state,
                    "Missing on/off parameter in SetTrainerServiceTypeFilter");
            }

            if (lua_toboolean(state, 2) != 0)
            {
                if (IsExclusive(state, 3))
                    trainer.EnabledServiceTypeFilters.Clear();
                trainer.EnabledServiceTypeFilters.Add(filter);
            }
            else
            {
                trainer.EnabledServiceTypeFilters.Remove(filter);
            }
            runtime.TriggerEvent("TRAINER_UPDATE");
            return 0;
        }
        return 0;
    }

    private static void CloseTrainer(LuaRuntime runtime)
    {
        var interactions = runtime.PlayerInteractions;
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType = TrainerInteractionType;
        if (!interactions.HasActiveInteraction ||
            interactions.CurrentInteractionType != TrainerInteractionType)
        {
            return;
        }

        interactions.HasActiveInteraction = false;
        interactions.HasPendingInteraction = false;
        interactions.CurrentInteractionType = 0;
        interactions.PendingInteractionType = 0;
        interactions.ValidNpcInteractionTypes.Clear();
        runtime.TriggerEvent("TRAINER_CLOSED");
    }

    private static string RequiredFilter(
        lua_State state,
        string usage,
        string invalidFilterError,
        bool allowAll)
    {
        if (lua_isstring(state, 1) == 0)
        {
            luaL_error(state, usage);
            return string.Empty;
        }

        var filter = lua_tostring(state, 1) ?? string.Empty;
        if (filter.Equals("available", StringComparison.OrdinalIgnoreCase))
            return "available";
        if (filter.Equals("unavailable", StringComparison.OrdinalIgnoreCase))
            return "unavailable";
        if (filter.Equals("used", StringComparison.OrdinalIgnoreCase))
            return "used";
        if (allowAll &&
            filter.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return "all";
        }

        luaL_error(state, invalidFilterError);
        return string.Empty;
    }

    private static bool IsExclusive(lua_State state, int index) =>
        index <= lua_gettop(state) &&
        lua_isnumber(state, index) != 0 &&
        unchecked((int)lua_tonumber(state, index)) != 0;
}
