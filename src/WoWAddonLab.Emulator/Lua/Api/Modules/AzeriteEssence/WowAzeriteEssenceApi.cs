using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowAzeriteEssenceApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "ActivateEssence",
        "CanActivateEssence",
        "CanDeactivateEssence",
        "CanOpenUI",
        "ClearPendingActivationEssence",
        "CloseForge",
        "GetEssenceHyperlink",
        "GetEssenceInfo",
        "GetEssences",
        "GetMilestoneEssence",
        "GetMilestoneInfo",
        "GetMilestoneSpell",
        "GetMilestones",
        "GetNumUnlockedEssences",
        "GetNumUsableEssences",
        "GetPendingActivationEssence",
        "HasNeverActivatedAnyEssences",
        "HasPendingActivationEssence",
        "IsAtForge",
        "SetPendingActivationEssence",
        "UnlockMilestone"
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
        lua_setglobal(state, "C_AzeriteEssence");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var essence = runtime.AzeriteEssence;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetMilestones":
            {
                var milestones = Milestones(runtime);
                if (milestones.Count == 0)
                    return 0;
                return PushMilestones(state, runtime, milestones);
            }
            case "GetMilestoneInfo":
            {
                const string usage =
                    "Usage: local info = " +
                    "C_AzeriteEssence.GetMilestoneInfo(milestoneID)";
                var id = RequiredInt32(state, 1, usage);
                var milestone = Milestones(runtime).FirstOrDefault(value => value.Definition.Id == id);
                if (milestone is null)
                    return 0;
                PushMilestone(state, runtime, milestone);
                return 1;
            }
            case "GetMilestoneSpell":
            {
                const string usage =
                    "Usage: local spellID = " +
                    "C_AzeriteEssence.GetMilestoneSpell(milestoneID)";
                var definition = Definition(
                    runtime,
                    RequiredInt32(state, 1, usage));
                if (definition is null)
                    return 0;
                lua_pushinteger(state, definition.SpellId);
                return 1;
            }
            case "GetMilestoneEssence":
            {
                const string usage =
                    "Usage: local essenceID = " +
                    "C_AzeriteEssence.GetMilestoneEssence(milestoneID)";
                if (essence.ActiveEssenceByMilestoneId.TryGetValue(
                        RequiredInt32(state, 1, usage),
                        out var activeEssenceId) &&
                    activeEssenceId != 0)
                {
                    lua_pushinteger(state, activeEssenceId);
                    return 1;
                }
                return 0;
            }
            case "CanOpenUI":
                lua_pushboolean(state, essence.CanOpenUi ? 1 : 0);
                return 1;
            case "IsAtForge":
                lua_pushboolean(
                    state,
                    IsAtForge(runtime) ? 1 : 0);
                return 1;
            case "CloseForge":
            {
                var wasAtForge = IsAtForge(runtime);
                essence.IsAtForge = false;
                ClearForgeInteraction(runtime.PlayerInteractions);
                if (wasAtForge)
                    runtime.TriggerEvent("AZERITE_ESSENCE_FORGE_CLOSE");
                return 0;
            }
            case "SetPendingActivationEssence":
            {
                const string usage =
                    "Usage: C_AzeriteEssence." +
                    "SetPendingActivationEssence(essenceID)";
                var essenceId = RequiredInt32(state, 1, usage);
                if (essence.PendingActivationEssenceId != essenceId &&
                    IsEssenceUnlocked(essence, essenceId))
                {
                    essence.PendingActivationEssenceId = essenceId;
                    runtime.TriggerEvent(
                        "PENDING_AZERITE_ESSENCE_CHANGED",
                        essenceId);
                }
                return 0;
            }
            case "ClearPendingActivationEssence":
            {
                if (essence.PendingActivationEssenceId is null or 0)
                    return 0;
                essence.PendingActivationEssenceId = null;
                runtime.TriggerEvent(
                    "PENDING_AZERITE_ESSENCE_CHANGED",
                    new object?[] { null });
                return 0;
            }
            case "GetPendingActivationEssence":
                lua_pushinteger(state, essence.PendingActivationEssenceId ?? 0);
                return 1;
            case "HasPendingActivationEssence":
                lua_pushboolean(
                    state,
                    essence.PendingActivationEssenceId is not null and not 0
                        ? 1
                        : 0);
                return 1;
            case "HasNeverActivatedAnyEssences":
                lua_pushboolean(
                    state,
                    essence.ActiveEssenceByMilestoneId.Values.All(
                        essenceId => essenceId == 0)
                        ? 1
                        : 0);
                return 1;
            case "CanActivateEssence":
            {
                const string usage =
                    "Usage: local canActivate = " +
                    "C_AzeriteEssence.CanActivateEssence(" +
                    "essenceID, milestoneID)";
                var essenceId = RequiredInt32(state, 1, usage);
                var milestoneId = RequiredInt32(state, 2, usage);
                var canActivate = CanActivate(
                    runtime,
                    essenceId,
                    milestoneId);
                lua_pushboolean(state, canActivate ? 1 : 0);
                return 1;
            }
            case "CanDeactivateEssence":
            {
                const string usage =
                    "Usage: local canDeactivate = " +
                    "C_AzeriteEssence.CanDeactivateEssence(milestoneID)";
                var milestoneId = RequiredInt32(state, 1, usage);
                essence.ActiveEssenceByMilestoneId.TryGetValue(
                    milestoneId,
                    out var activeEssenceId);
                lua_pushboolean(
                    state,
                    (essence.DeactivatableMilestoneIds.Contains(milestoneId) ||
                     activeEssenceId != 0)
                        ? 1
                        : 0);
                return 1;
            }
            case "ActivateEssence":
            {
                const string usage =
                    "Usage: C_AzeriteEssence." +
                    "ActivateEssence(essenceID, milestoneID)";
                var essenceId = RequiredInt32(state, 1, usage);
                var milestoneId = RequiredInt32(state, 2, usage);
                if (CanActivate(runtime, essenceId, milestoneId))
                {
                    essence.ActivationRequests.Add(
                        new WowAzeriteEssenceActivationRequest(
                            essenceId,
                            milestoneId));
                }
                return 0;
            }
            case "UnlockMilestone":
            {
                const string usage =
                    "Usage: C_AzeriteEssence." +
                    "UnlockMilestone(milestoneID)";
                var milestoneId = RequiredInt32(state, 1, usage);
                if (!essence.UnlockedMilestoneIds.Contains(milestoneId) &&
                    essence.UnlockableMilestoneIds.Contains(milestoneId))
                {
                    essence.UnlockMilestoneRequests.Add(milestoneId);
                }
                return 0;
            }
            case "GetEssences":
                return PushEssences(state, essence);
            case "GetEssenceInfo":
            {
                const string usage =
                    "Usage: local info = " +
                    "C_AzeriteEssence.GetEssenceInfo(essenceID)";
                var essenceId = RequiredUInt32(state, 1, usage);
                var info = essence.Essences.FirstOrDefault(
                    value => value.Id == essenceId);
                if (info is null)
                    return 0;
                PushEssence(state, info);
                return 1;
            }
            case "GetEssenceHyperlink":
            {
                const string usage =
                    "Usage: local link = " +
                    "C_AzeriteEssence.GetEssenceHyperlink(essenceID, rank)";
                var essenceId = RequiredInt32(state, 1, usage);
                var requestedRank = RequiredInt32(state, 2, usage);
                var rank = requestedRank == 0 ? 1 : requestedRank;
                var info = essence.Essences.FirstOrDefault(
                    value => value.Id == unchecked((uint)essenceId));
                if (info is null ||
                    rank < 1 ||
                    rank > info.MaxRank)
                {
                    lua_pushstring(state, string.Empty);
                    return 1;
                }
                lua_pushstring(
                    state,
                    $"|cnIQ{rank + 1}:|Hazessence:{essenceId}:{rank}|h" +
                    $"[{info.Name ?? string.Empty}]|h|r");
                return 1;
            }
            case "GetNumUnlockedEssences":
            {
                var count = essence.NumUnlockedEssencesOverride ??
                    essence.UnlockedEssenceIds.Count;
                if (essence.NumUnlockedEssencesOverride is null &&
                    count == 0)
                {
                    count = essence.Essences.Count(value => value.Unlocked);
                }
                lua_pushinteger(state, count);
                return 1;
            }
            case "GetNumUsableEssences":
                lua_pushinteger(
                    state,
                    essence.Essences.Count(
                        value => value.Unlocked && value.Valid));
                return 1;
            default:
                return 0;
        }
    }

    private static int PushMilestones(
        lua_State state,
        LuaRuntime runtime,
        IReadOnlyList<ResolvedMilestone> milestones)
    {
        lua_createtable(state, milestones.Count, 0);
        var index = 1;
        foreach (var milestone in milestones)
        {
            PushMilestone(state, runtime, milestone);
            lua_rawseti(state, -2, index++);
        }
        return 1;
    }

    private static void PushMilestone(
        lua_State state,
        LuaRuntime runtime,
        ResolvedMilestone milestone)
    {
        var definition = milestone.Definition;
        var essence = runtime.AzeriteEssence;
        lua_newtable(state);
        SetInteger(state, "ID", definition.Id);
        SetInteger(state, "requiredLevel", definition.RequiredLevel);
        SetBoolean(state, "canUnlock", essence.UnlockableMilestoneIds.Contains(definition.Id));
        SetBoolean(state, "unlocked", essence.UnlockedMilestoneIds.Contains(definition.Id));
        if (definition.AzeriteEssenceType == 3)
        {
            var rank = essence.UnlockedMilestoneIds.Contains(definition.Id)
                ? Math.Max(0, essence.HeartLevel - 128)
                : 0;
            SetInteger(state, "rank", rank);
        }
        if (milestone.Slot is { } slot)
            SetInteger(state, "slot", slot);
    }

    private static int PushEssences(
        lua_State state,
        WowAzeriteEssenceState essence)
    {
        if (essence.Essences.Count == 0)
            return 0;

        lua_createtable(state, essence.Essences.Count, 0);
        for (var index = 0; index < essence.Essences.Count; index++)
        {
            PushEssence(state, essence.Essences[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static void PushEssence(
        lua_State state,
        WowAzeriteEssenceInfoState essence)
    {
        lua_createtable(state, 0, 6);
        SetInteger(state, "ID", essence.Id);
        SetOptionalString(state, "name", essence.Name);
        SetInteger(state, "rank", essence.Rank);
        SetBoolean(state, "unlocked", essence.Unlocked);
        SetBoolean(state, "valid", essence.Valid);
        SetInteger(state, "icon", essence.Icon);
    }

    private static IReadOnlyList<ResolvedMilestone> Milestones(LuaRuntime runtime)
    {
        var definitions = runtime.AzeriteEssenceProvider?.Milestones ?? [];
        var slot = 0;
        var result = new List<ResolvedMilestone>(definitions.Count);
        foreach (var definition in definitions)
        {
            int? resolvedSlot = null;
            if (definition.AzeriteEssenceType is not 2 and not 3)
                resolvedSlot = slot++;
            result.Add(new ResolvedMilestone(definition, resolvedSlot));
        }
        return result;
    }

    private static WowAzeriteMilestoneDefinition? Definition(LuaRuntime runtime, int id) =>
        runtime.AzeriteEssenceProvider?.Milestones.FirstOrDefault(value => value.Id == id);

    private static bool CanActivate(
        LuaRuntime runtime,
        int essenceId,
        int milestoneId)
    {
        var state = runtime.AzeriteEssence;
        if (state.ActivatablePairs.Count > 0)
        {
            return state.ActivatablePairs.Contains(
                (essenceId, milestoneId));
        }

        return essenceId > 0 &&
            IsEssenceUnlocked(state, essenceId) &&
            state.UnlockedMilestoneIds.Contains(milestoneId) &&
            Definition(runtime, milestoneId) is not null;
    }

    private static bool IsEssenceUnlocked(
        WowAzeriteEssenceState state,
        int essenceId) =>
        state.UnlockedEssenceIds.Contains(essenceId) ||
        state.Essences.Any(
            value =>
                value.Id == unchecked((uint)essenceId) &&
                value.Unlocked);

    private static bool IsAtForge(LuaRuntime runtime) =>
        runtime.AzeriteEssence.IsAtForge ||
        (runtime.PlayerInteractions.HasActiveInteraction &&
         runtime.PlayerInteractions.CurrentInteractionType == 56);

    private static void ClearForgeInteraction(
        WowPlayerInteractionManagerState interactions)
    {
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType = 56;
        if (!interactions.HasActiveInteraction ||
            interactions.CurrentInteractionType != 56)
        {
            return;
        }
        interactions.HasActiveInteraction = false;
        interactions.CurrentInteractionType = 0;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_gettop(state) < index || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value is < int.MinValue or > int.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (int)value;
    }

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_gettop(state) < index || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value is < uint.MinValue or > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (uint)value;
    }

    private static void SetInteger(
        lua_State state,
        string name,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBoolean(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalString(
        lua_State state,
        string name,
        string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private sealed record ResolvedMilestone(
        WowAzeriteMilestoneDefinition Definition,
        int? Slot);
}
