using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowScenarioApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetBonusStepRewardQuestID",
        "GetBonusSteps",
        "GetInfo",
        "GetProvingGroundsInfo",
        "GetStepInfo",
        "GetSupersededObjectives",
        "IsInScenario",
        "ShouldShowCriteria",
        "TreatScenarioAsDungeon"
    ];

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "GetScenariosChoiceOrder", Callback);
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Scenario");
    }

    private static int Dispatch(lua_State state)
    {
        var scenario = LuaBindings.GetRuntime(state).Scenario;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        return operation switch
        {
            "GetBonusStepRewardQuestID" => GetBonusStepRewardQuestId(scenario, state),
            "GetBonusSteps" => GetBonusSteps(scenario, state),
            "GetInfo" => GetInfo(scenario, state),
            "GetProvingGroundsInfo" => GetProvingGroundsInfo(scenario, state),
            "GetStepInfo" => GetStepInfo(scenario, state),
            "GetSupersededObjectives" => GetSupersededObjectives(scenario, state),
            "GetScenariosChoiceOrder" => GetScenariosChoiceOrder(scenario, state),
            "IsInScenario" => PushBoolean(state, scenario.IsInScenario),
            "ShouldShowCriteria" => PushBoolean(state, scenario.ShouldShowCriteria),
            "TreatScenarioAsDungeon" => PushBoolean(state, scenario.Info?.Type == 3),
            _ => 0,
        };
    }

    private static int GetBonusStepRewardQuestId(
        WowScenarioState scenario,
        lua_State state)
    {
        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, "Usage: GetBonusStepRewardQuestID(stepIndex");

        var stepId = unchecked((int)lua_tonumber(state, 1));
        lua_pushinteger(
            state,
            scenario.StepsById.TryGetValue(stepId, out var step)
                ? step.RewardQuestId
                : 0);
        return 1;
    }

    private static int GetBonusSteps(WowScenarioState scenario, lua_State state)
    {
        PushReusableTable(state, validateOptionalTable: false);
        for (var index = 0; index < scenario.BonusStepIds.Count; index++)
        {
            lua_pushinteger(state, scenario.BonusStepIds[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetInfo(WowScenarioState scenario, lua_State state)
    {
        var info = scenario.Info;
        PushOptionalString(state, info?.Name);
        lua_pushnumber(state, info?.CurrentStage ?? 0);
        lua_pushnumber(state, info?.NumStages ?? 0);
        lua_pushnumber(state, info?.Flags ?? 0);
        lua_pushboolean(state, 0);
        lua_pushboolean(state, 0);
        lua_pushboolean(state, info?.IsComplete == true ? 1 : 0);
        lua_pushinteger(state, info?.Xp ?? 0);
        lua_pushinteger(state, info?.Money ?? 0);
        lua_pushinteger(state, info?.Type ?? 0);
        PushOptionalString(state, info?.AreaName);
        PushOptionalString(state, info?.UiTextureKit);
        lua_pushnumber(state, scenario.CurrentScenarioId);
        return 13;
    }

    private static int GetProvingGroundsInfo(
        WowScenarioState scenario,
        lua_State state)
    {
        var info = scenario.ProvingGrounds;
        lua_pushnumber(state, info.DifficultyId);
        lua_pushnumber(state, info.CurrentWave);
        lua_pushnumber(state, info.MaxWave);
        lua_pushnumber(state, info.Duration);
        return 4;
    }

    private static int GetStepInfo(WowScenarioState scenario, lua_State state)
    {
        var stepId = scenario.CurrentStepId;
        if (lua_isnumber(state, 1) != 0)
            stepId = unchecked((int)lua_tonumber(state, 1));

        if (!scenario.StepsById.TryGetValue(stepId, out var step))
        {
            lua_pushnil(state);
            lua_pushnil(state);
            lua_pushnumber(state, 0);
            lua_pushboolean(state, 0);
            lua_pushboolean(state, 0);
            lua_pushboolean(state, 0);
            lua_pushboolean(state, 0);
            lua_pushnumber(state, 0);
            lua_pushnil(state);
            lua_pushnil(state);
            lua_pushnil(state);
            lua_pushnil(state);
            return 12;
        }

        lua_pushstring(state, step.Title);
        lua_pushstring(state, step.Description);
        lua_pushnumber(state, step.NumCriteria);
        lua_pushboolean(state, step.StepFailed ? 1 : 0);
        lua_pushboolean(state, step.IsBonusStep ? 1 : 0);
        lua_pushboolean(state, step.IsForCurrentStepOnly ? 1 : 0);
        lua_pushboolean(state, step.ShouldShowBonusObjective ? 1 : 0);
        lua_pushnumber(state, step.Spells.Count);
        PushStepSpells(state, step.Spells);
        PushOptionalInteger(state, step.WeightedProgress);
        lua_pushinteger(state, step.RewardQuestId);
        PushOptionalInteger(state, step.WidgetSetId);
        return 12;
    }

    private static int GetSupersededObjectives(
        WowScenarioState scenario,
        lua_State state)
    {
        PushReusableTable(state, validateOptionalTable: false);
        for (var index = 0; index < scenario.SupersededObjectives.Count; index++)
        {
            var objective = scenario.SupersededObjectives[index];
            lua_createtable(state, 2, 0);
            lua_pushinteger(state, objective.ScenarioStepId);
            lua_rawseti(state, -2, 1);
            lua_pushinteger(state, objective.SupersedingScenarioStepId);
            lua_rawseti(state, -2, 2);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetScenariosChoiceOrder(
        WowScenarioState scenario,
        lua_State state)
    {
        PushReusableTable(state, validateOptionalTable: true);
        for (var index = 0; index < scenario.ChoiceOrder.Count; index++)
        {
            lua_pushnumber(state, scenario.ChoiceOrder[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static void PushStepSpells(
        lua_State state,
        IReadOnlyList<WowScenarioStepSpellInfoState> spells)
    {
        lua_createtable(state, spells.Count, 0);
        for (var index = 0; index < spells.Count; index++)
        {
            var spell = spells[index];
            lua_createtable(state, 0, 3);
            lua_pushinteger(state, spell.SpellId);
            lua_setfield(state, -2, "spellID");
            lua_pushstring(state, spell.Name);
            lua_setfield(state, -2, "spellName");
            PushOptionalInteger(state, spell.Icon);
            lua_setfield(state, -2, "spellIcon");
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushReusableTable(lua_State state, bool validateOptionalTable)
    {
        if (lua_istable(state, 1) != 0)
        {
            ClearTable(state, 1);
            lua_pushvalue(state, 1);
            return;
        }

        if (validateOptionalTable &&
            lua_gettop(state) >= 1 &&
            lua_isnil(state, 1) == 0)
        {
            luaL_error(state, "Usage: GetScenariosChoiceOrder([table])");
            return;
        }

        lua_newtable(state);
    }

    private static void ClearTable(lua_State state, int tableIndex)
    {
        var absoluteIndex = tableIndex > 0 || tableIndex <= LUA_REGISTRYINDEX
            ? tableIndex
            : lua_gettop(state) + tableIndex + 1;
        lua_newtable(state);
        var keysIndex = lua_gettop(state);
        var keyCount = 0;

        lua_pushnil(state);
        while (lua_next(state, absoluteIndex) != 0)
        {
            lua_pop(state, 1);
            lua_pushvalue(state, -1);
            lua_rawseti(state, keysIndex, ++keyCount);
        }

        for (var index = 1; index <= keyCount; index++)
        {
            lua_rawgeti(state, keysIndex, index);
            lua_pushnil(state);
            lua_settable(state, absoluteIndex);
        }

        lua_pop(state, 1);
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value.HasValue)
            lua_pushinteger(state, value.Value);
        else
            lua_pushnil(state);
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }
}
