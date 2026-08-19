using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowContributionCollectorApi : LuaApiModule
{
    private const int ContributionInteractionType = 41;
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "Close", "Contribute", "GetActive", "GetAtlases", "GetBuffs",
        "GetContributionAppearance", "GetContributionCollectorsForMap",
        "GetContributionResult", "GetDescription", "GetManagedContributionsForCreatureID",
        "GetName", "GetOrderIndex", "GetRequiredContributionCurrency",
        "GetRequiredContributionItem", "GetRewardQuestID", "GetState",
        "HasPendingContribution", "IsAwaitingRewardQuestData"
    ];

    public override void Register(lua_State state)
    {
        RegisterEnums(state);

        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_ContributionCollector");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var contributions = runtime.ContributionCollector;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "Close":
                ClearInteraction(runtime.PlayerInteractions);
                return 0;
            case "Contribute":
            {
                var contributionId = RequiredUInt32(
                    state,
                    1,
                    "Usage: C_ContributionCollector.Contribute(contributionID)");
                if (GetContributionResult(
                        contributionId,
                        contributions,
                        runtime.PlayerInteractions) == 0)
                {
                    contributions.ContributionRequestCount++;
                    contributions.LastContributionId = contributionId;
                    contributions.PendingContributionIds.Add(contributionId);
                }
                return 0;
            }
            case "GetActive":
                return PushVariadicIntegers(state, contributions.ActiveContributionIds);
            case "GetAtlases":
            {
                var contributionId = RequiredUInt32(
                    state,
                    1,
                    "Usage: local atlasName = C_ContributionCollector.GetAtlases(contributionID)");
                PushOptionalStringArray(
                    state,
                    contributions.AtlasesByContributionId.TryGetValue(
                        contributionId,
                        out var atlases)
                        ? atlases
                        : []);
                return 1;
            }
            case "GetBuffs":
            {
                var contributionId = RequiredUInt32(
                    state,
                    1,
                    "Usage: local (spellID)* = C_ContributionCollector.GetBuffs(contributionID)");
                return PushVariadicIntegers(
                    state,
                    contributions.BuffIdsByContributionId.TryGetValue(
                        contributionId,
                        out var buffIds)
                        ? buffIds
                        : []);
            }
            case "GetContributionAppearance":
            {
                const string usage =
                    "Usage: local appearance = C_ContributionCollector.GetContributionAppearance(contributionID, contributionState)";
                var contributionId = RequiredUInt32(state, 1, usage);
                var contributionState = RequiredContributionState(state, 2, usage);
                if (!contributions.AppearanceByContributionAndState.TryGetValue(
                        (contributionId, contributionState),
                        out var appearance))
                {
                    lua_pushnil(state);
                    return 1;
                }

                PushAppearance(state, appearance);
                return 1;
            }
            case "GetContributionCollectorsForMap":
            {
                var mapId = RequiredInt32(
                    state,
                    1,
                    "Usage: local contributionCollectors = C_ContributionCollector.GetContributionCollectorsForMap(uiMapID)");
                PushCollectorArray(
                    state,
                    contributions.CollectorsByMapId.TryGetValue(
                        mapId,
                        out var collectors)
                        ? collectors
                        : []);
                return 1;
            }
            case "GetContributionResult":
            {
                var contributionId = RequiredUInt32(
                    state,
                    1,
                    "Usage: local result = C_ContributionCollector.GetContributionResult(contributionID)");
                lua_pushinteger(
                    state,
                    GetContributionResult(
                        contributionId,
                        contributions,
                        runtime.PlayerInteractions));
                return 1;
            }
            case "GetDescription":
            {
                var contributionId = RequiredUInt32(
                    state,
                    1,
                    "Usage: local description = C_ContributionCollector.GetDescription(contributionID)");
                lua_pushstring(
                    state,
                    contributions.DefinitionsById.TryGetValue(
                        contributionId,
                        out var definition)
                        ? definition.Description
                        : string.Empty);
                return 1;
            }
            case "GetManagedContributionsForCreatureID":
            {
                var creatureId = RequiredInt32(
                    state,
                    1,
                    "Usage: local (contributionID)* = C_ContributionCollector.GetManagedContributionsForCreatureID(creatureID)");
                return PushVariadicIntegers(
                    state,
                    contributions.ManagedContributionIdsByCreatureId.TryGetValue(
                        creatureId,
                        out var contributionIds)
                        ? contributionIds
                        : []);
            }
            case "GetName":
            {
                var contributionId = RequiredUInt32(
                    state,
                    1,
                    "Usage: local name = C_ContributionCollector.GetName(contributionID)");
                lua_pushstring(
                    state,
                    contributions.DefinitionsById.TryGetValue(
                        contributionId,
                        out var definition)
                        ? definition.Name
                        : string.Empty);
                return 1;
            }
            case "GetOrderIndex":
            {
                var contributionId = RequiredUInt32(
                    state,
                    1,
                    "Usage: local orderIndex = C_ContributionCollector.GetOrderIndex(contributionID)");
                lua_pushinteger(
                    state,
                    contributions.DefinitionsById.TryGetValue(
                        contributionId,
                        out var definition)
                        ? definition.OrderIndex
                        : 0);
                return 1;
            }
            case "GetRequiredContributionCurrency":
            {
                var contributionId = RequiredUInt32(
                    state,
                    1,
                    "Usage: local currencyID, currencyAmount = C_ContributionCollector.GetRequiredContributionCurrency(contributionID)");
                if (!contributions.DefinitionsById.TryGetValue(
                        contributionId,
                        out var definition) ||
                    definition.RequiredCurrency is not { } currency)
                {
                    return 0;
                }

                lua_pushinteger(state, currency.CurrencyId);
                lua_pushinteger(state, currency.Amount);
                return 2;
            }
            case "GetRequiredContributionItem":
            {
                var contributionId = RequiredUInt32(
                    state,
                    1,
                    "Usage: local itemID, itemCount = C_ContributionCollector.GetRequiredContributionItem(contributionID)");
                if (!contributions.DefinitionsById.TryGetValue(
                        contributionId,
                        out var definition) ||
                    definition.RequiredItem is not { } item)
                {
                    return 0;
                }

                lua_pushinteger(state, item.ItemId);
                lua_pushinteger(state, item.Count);
                return 2;
            }
            case "GetRewardQuestID":
            {
                var contributionId = RequiredUInt32(
                    state,
                    1,
                    "Usage: local questID = C_ContributionCollector.GetRewardQuestID(contributionID)");
                if (contributions.DefinitionsById.TryGetValue(
                        contributionId,
                        out var definition) &&
                    definition.RewardQuestId.HasValue)
                {
                    lua_pushinteger(state, definition.RewardQuestId.Value);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            case "GetState":
            {
                var contributionId = RequiredUInt32(
                    state,
                    1,
                    "Usage: local contributionState, contributionPercentageComplete, timeOfNextStateChange, startTime = C_ContributionCollector.GetState(contributionID)");
                contributions.StateByContributionId.TryGetValue(
                    contributionId,
                    out var contributionState);
                contributionState ??= WowContributionStateInfo.Empty;

                lua_pushnumber(state, contributionState.State);
                lua_pushnumber(state, (float)contributionState.PercentageComplete);
                if (contributionState.TimeOfNextStateChange.HasValue)
                    lua_pushnumber(state, contributionState.TimeOfNextStateChange.Value);
                else
                    lua_pushnil(state);
                lua_pushnumber(state, contributionState.StartTime);
                return 4;
            }
            case "HasPendingContribution":
            {
                var contributionId = RequiredUInt32(
                    state,
                    1,
                    "Usage: local hasPending = C_ContributionCollector.HasPendingContribution(contributionID)");
                lua_pushboolean(
                    state,
                    contributions.PendingContributionIds.Contains(contributionId)
                        ? 1
                        : 0);
                return 1;
            }
            case "IsAwaitingRewardQuestData":
            {
                var contributionId = RequiredUInt32(
                    state,
                    1,
                    "Usage: local awaitingData = C_ContributionCollector.IsAwaitingRewardQuestData(contributionID)");
                lua_pushboolean(
                    state,
                    contributions.DefinitionsById.ContainsKey(contributionId) &&
                    contributions.AwaitingRewardQuestDataIds.Contains(contributionId)
                        ? 1
                        : 0);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static int GetContributionResult(
        uint contributionId,
        WowContributionCollectorState contributions,
        WowPlayerInteractionManagerState interactions)
    {
        if (contributions.ResultByContributionId.TryGetValue(
                contributionId,
                out var result))
        {
            return result;
        }

        if (!interactions.HasActiveInteraction ||
            interactions.CurrentInteractionType != ContributionInteractionType)
        {
            return 1;
        }

        return contributions.DefinitionsById.ContainsKey(contributionId) ? 0 : 2;
    }

    private static void PushAppearance(
        lua_State state,
        WowContributionAppearance appearance)
    {
        lua_createtable(state, 0, 7);
        SetOptionalString(state, "stateName", appearance.StateName);
        PushColorMixin(state, appearance.StateColor);
        lua_setfield(state, -2, "stateColor");
        SetOptionalString(state, "tooltipLine", appearance.TooltipLine);
        SetBoolean(
            state,
            "tooltipUseTimeRemaining",
            appearance.TooltipUseTimeRemaining);
        SetOptionalString(state, "statusBarAtlas", appearance.StatusBarAtlas);
        SetOptionalString(state, "borderAtlas", appearance.BorderAtlas);
        SetOptionalString(state, "bannerAtlas", appearance.BannerAtlas);
    }

    private static void PushCollectorArray(
        lua_State state,
        IReadOnlyList<WowContributionMapInfo> collectors)
    {
        lua_createtable(state, collectors.Count, 0);
        for (var index = 0; index < collectors.Count; index++)
        {
            var collector = collectors[index];
            lua_createtable(state, 0, 5);
            SetInteger(state, "areaPoiID", collector.AreaPoiId);

            lua_createtable(state, 0, 2);
            SetNumber(state, "x", collector.X);
            SetNumber(state, "y", collector.Y);
            ApplyMixinToTopTable(state, "Vector2DMixin");
            lua_setfield(state, -2, "position");

            SetOptionalString(state, "name", collector.Name);
            SetString(state, "atlasName", collector.AtlasName);
            SetInteger(
                state,
                "collectorCreatureID",
                collector.CollectorCreatureId);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushOptionalStringArray(
        lua_State state,
        IReadOnlyList<string?> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            if (values[index] is { } value)
                lua_pushstring(state, value);
            else
                lua_pushnil(state);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static int PushVariadicIntegers(
        lua_State state,
        IEnumerable<int> values)
    {
        var count = 0;
        foreach (var value in values)
        {
            lua_pushinteger(state, value);
            count++;
        }
        return count;
    }

    private static void PushColorMixin(
        lua_State state,
        WowContributionColor color)
    {
        lua_createtable(state, 0, 4);
        SetNumber(state, "r", color.Red);
        SetNumber(state, "g", color.Green);
        SetNumber(state, "b", color.Blue);
        SetNumber(state, "a", color.Alpha);
        ApplyMixinToTopTable(state, "ColorMixin");
    }

    private static void ApplyMixinToTopTable(lua_State state, string mixinName)
    {
        var target = AbsoluteIndex(state, -1);
        lua_getglobal(state, mixinName);
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            return;
        }

        var mixin = AbsoluteIndex(state, -1);
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

    private static int AbsoluteIndex(lua_State state, int index) =>
        index > 0 || index <= LUA_REGISTRYINDEX
            ? index
            : lua_gettop(state) + index + 1;

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return unchecked((uint)luaL_error(state, usage));
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < uint.MinValue || value > uint.MaxValue)
            return unchecked((uint)luaL_error(state, usage));
        return unchecked((uint)value);
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)value);
    }

    private static uint RequiredContributionState(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredUInt32(state, index, usage);
        if (value > 4)
            return unchecked((uint)luaL_error(state, usage));
        return value;
    }

    private static void ClearInteraction(
        WowPlayerInteractionManagerState interactions)
    {
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType = ContributionInteractionType;
        if (!interactions.HasActiveInteraction ||
            interactions.CurrentInteractionType != ContributionInteractionType)
        {
            return;
        }

        interactions.HasActiveInteraction = false;
        interactions.HasPendingInteraction = false;
        interactions.CurrentInteractionType = 0;
        interactions.PendingInteractionType = 0;
        interactions.ValidNpcInteractionTypes.Clear();
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_istable(state, -1) == 0)
        {
            lua_pop(state, 1);
            lua_newtable(state);
            lua_pushvalue(state, -1);
            lua_setglobal(state, "Enum");
        }

        lua_createtable(state, 0, 8);
        SetInteger(state, "Success", 0);
        SetInteger(state, "MustBeNearNpc", 1);
        SetInteger(state, "IncorrectState", 2);
        SetInteger(state, "InvalidID", 3);
        SetInteger(state, "QuestDataMissing", 4);
        SetInteger(state, "FailedConditionCheck", 5);
        SetInteger(state, "UnableToCompleteTurnIn", 6);
        SetInteger(state, "InternalError", 7);
        lua_setfield(state, -2, "ContributionResult");

        lua_createtable(state, 0, 3);
        SetInteger(state, "NumValues", 8);
        SetInteger(state, "MinValue", 0);
        SetInteger(state, "MaxValue", 7);
        lua_setfield(state, -2, "ContributionResultMeta");
        lua_pop(state, 1);
    }

    private static void SetInteger(lua_State state, string field, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetNumber(lua_State state, string field, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetBoolean(lua_State state, string field, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, field);
    }

    private static void SetString(lua_State state, string field, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }
}
