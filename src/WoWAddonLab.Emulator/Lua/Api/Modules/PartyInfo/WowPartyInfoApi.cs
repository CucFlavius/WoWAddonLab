using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowPartyInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AllowedToDoPartyConversion",
        "CanFormCrossFactionParties",
        "ChallengeModeRestrictionsActive",
        "ConvertToParty",
        "ConvertToRaid",
        "GetActiveCategories",
        "GetAvailableLootMethods",
        "GetLootMethod",
        "GetInstanceAbandonShutdownTime",
        "GetInstanceAbandonVoteResponse",
        "GetInstanceAbandonVoteRequirements",
        "GetNumInstanceAbandonGroupVoteResponses",
        "GetInstanceAbandonVoteTime",
        "GetRestrictPings",
        "IsCrossFactionParty",
        "IsChallengeModeKeystoneOwner",
        "IsDelveComplete",
        "IsDelveInProgress",
        "IsLootMethodAvailable",
        "IsPartyFull",
        "IsPartyInJailersTower",
        "IsPartyWalkIn",
        "InviteUnit",
        "LeaveParty",
        "SetInstanceAbandonVoteResponse",
        "SetLootMethod",
        "SetRestrictPings"
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
        lua_setglobal(state, "C_PartyInfo");
        LuaBindings.RegisterClosureGlobal(state, "RequestGuildPartyState", Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var party = runtime.PartyInfo;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "AllowedToDoPartyConversion":
            {
                var toRaid = RequiredTruthyBoolean(
                    state,
                    1,
                    "Usage: local allowed = C_PartyInfo.AllowedToDoPartyConversion(toRaid)");
                lua_pushboolean(
                    state,
                    IsPartyConversionAllowed(runtime, toRaid) ? 1 : 0);
                return 1;
            }
            case "CanFormCrossFactionParties":
                lua_pushboolean(state, party.CanFormCrossFactionParties ? 1 : 0);
                return 1;
            case "ChallengeModeRestrictionsActive":
                lua_pushboolean(
                    state,
                    party.ChallengeModeRestrictionsActive ? 1 : 0);
                return 1;
            case "GetLootMethod":
                lua_pushinteger(state, runtime.Group.Resolve() is null ? 5 : party.LootMethod);
                PushOptionalInteger(state, party.LootMasterPartyIndex);
                PushOptionalInteger(state, party.LootMasterRaidIndex);
                return 3;
            case "GetAvailableLootMethods":
                lua_createtable(state, party.AvailableLootMethods.Count, 0);
                for (var index = 0; index < party.AvailableLootMethods.Count; index++)
                {
                    lua_pushinteger(state, party.AvailableLootMethods[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "IsLootMethodAvailable":
            {
                var method = RequiredLootMethod(
                    state,
                    "Usage: local available = C_PartyInfo.IsLootMethodAvailable(method)");
                lua_pushboolean(state, party.AvailableLootMethods.Contains(method) ? 1 : 0);
                return 1;
            }
            case "SetLootMethod":
            {
                const string usage =
                    "Usage: local success = C_PartyInfo.SetLootMethod(method [, lootMaster])";
                var method = RequiredLootMethod(state, usage);
                var lootMaster = OptionalString(state, 2, usage);
                var success = party.CanSetLootMethod && runtime.Group.Resolve() is not null;
                if (success)
                {
                    party.LootMethod = method;
                    party.LootMasterName = lootMaster;
                }
                lua_pushboolean(state, success ? 1 : 0);
                return 1;
            }
            case "InviteUnit":
            {
                const string usage = "Usage: C_PartyInfo.InviteUnit(targetName)";
                var target = RequiredString(state, 1, usage);
                if (target.Length < 306)
                {
                    party.LastInviteTarget = target;
                    party.InviteRequestCount++;
                }
                return 0;
            }
            case "LeaveParty":
            {
                var requestedCategory = OptionalPartyCategory(
                    state,
                    "Usage: C_PartyInfo.LeaveParty([category])");
                var group = ResolveGroup(runtime, requestedCategory);
                if (group is not null)
                {
                    group.IsInRaid = false;
                    group.SubgroupMemberCount = 0;
                    group.GroupMemberCount = 0;
                    party.LeaveRequestCount++;
                    runtime.TriggerEvent("GROUP_LEFT", requestedCategory);
                    runtime.TriggerEvent("GROUP_ROSTER_UPDATE");
                }
                return 0;
            }
            case "IsCrossFactionParty":
            {
                var requestedCategory = OptionalPartyCategory(
                    state,
                    "Usage: local isCrossFactionParty = C_PartyInfo.IsCrossFactionParty([category])");
                var category = ResolveCategory(runtime, requestedCategory);
                var isCrossFaction = category is { } activeCategory &&
                                     party.IsCrossFactionPartyByCategory.GetValueOrDefault(
                                         (int)activeCategory,
                                         party.IsCrossFactionParty);
                lua_pushboolean(state, isCrossFaction ? 1 : 0);
                return 1;
            }
            case "IsPartyFull":
            {
                var requestedCategory = OptionalPartyCategory(
                    state,
                    "Usage: local isFull = C_PartyInfo.IsPartyFull([category])");
                var group = ResolveGroup(runtime, requestedCategory);
                var capacity = group?.IsInRaid == true ? 40 : 5;
                lua_pushboolean(
                    state,
                    group is not null && group.GroupMemberCount >= capacity ? 1 : 0);
                return 1;
            }
            case "IsPartyWalkIn":
            {
                var category = runtime.Group.ResolveCategory();
                var isWalkIn = category is { } activeCategory &&
                               party.IsPartyWalkInByCategory.GetValueOrDefault(
                                   (int)activeCategory,
                                   party.IsPartyWalkIn);
                lua_pushboolean(state, isWalkIn ? 1 : 0);
                return 1;
            }
            case "IsPartyInJailersTower":
                lua_pushboolean(state, IsPartyInJailersTower(runtime) ? 1 : 0);
                return 1;
            case "IsChallengeModeKeystoneOwner":
                lua_pushboolean(
                    state,
                    runtime.Group.Resolve() is not null &&
                    string.Equals(
                        party.ChallengeModeKeystoneOwnerGuid,
                        runtime.Units.Player.Guid,
                        StringComparison.OrdinalIgnoreCase)
                        ? 1
                        : 0);
                return 1;
            case "IsDelveComplete":
                lua_pushboolean(state, GetDelveState(runtime) == 1 ? 1 : 0);
                return 1;
            case "IsDelveInProgress":
                lua_pushboolean(state, GetDelveState(runtime) == 0 ? 1 : 0);
                return 1;
            case "GetRestrictPings":
                lua_pushinteger(
                    state,
                    runtime.Group.Resolve() is null ? 0 : party.RestrictPingsTo);
                return 1;
            case "GetInstanceAbandonVoteTime":
            {
                var hasGroup = runtime.Group.Resolve() is not null;
                lua_pushnumber(
                    state,
                    hasGroup ? party.InstanceAbandonVoteDuration : 0);
                lua_pushnumber(
                    state,
                    hasGroup ? party.InstanceAbandonVoteTimeLeft : 0);
                return 2;
            }
            case "GetInstanceAbandonShutdownTime":
            {
                var hasGroup = runtime.Group.Resolve() is not null;
                lua_pushnumber(
                    state,
                    hasGroup ? party.InstanceAbandonShutdownDuration : 0);
                lua_pushnumber(
                    state,
                    hasGroup ? party.InstanceAbandonShutdownTimeLeft : 0);
                return 2;
            }
            case "GetInstanceAbandonVoteResponse":
                if (runtime.Group.Resolve() is not null &&
                    party.InstanceAbandonVoteTimeLeft > 0 &&
                    party.InstanceAbandonVoteResponse is { } response)
                    lua_pushboolean(state, response ? 1 : 0);
                else
                    lua_pushnil(state);
                return 1;
            case "GetInstanceAbandonVoteRequirements":
            {
                var hasGroup = runtime.Group.Resolve() is not null;
                lua_pushinteger(
                    state,
                    hasGroup ? party.InstanceAbandonVotesRequired : 0);
                lua_pushinteger(
                    state,
                    hasGroup ? party.InstanceAbandonKeystoneOwnerVoteWeight : 0);
                return 2;
            }
            case "GetNumInstanceAbandonGroupVoteResponses":
                lua_pushinteger(
                    state,
                    runtime.Group.Resolve() is not null &&
                    party.InstanceAbandonVoteTimeLeft > 0
                        ? party.InstanceAbandonGroupVoteResponseCount
                        : 0);
                return 1;
            case "SetInstanceAbandonVoteResponse":
            {
                var submittedResponse = RequiredTruthyBoolean(
                    state,
                    1,
                    "Usage: C_PartyInfo.SetInstanceAbandonVoteResponse(response)");
                if (runtime.Group.Resolve() is not null &&
                    party.InstanceAbandonVoteTimeLeft > 0)
                {
                    party.InstanceAbandonVoteResponse = submittedResponse;
                }
                return 0;
            }
            case "SetRestrictPings":
            {
                var restrictTo = RequiredRestrictPingsTo(state);
                if (runtime.Group.Resolve() is not null &&
                    party.CanSetRestrictPings &&
                    (runtime.Units.Player.IsGroupLeader ||
                     runtime.Units.Player.IsGroupAssistant))
                {
                    party.RestrictPingsTo = restrictTo;
                }
                return 0;
            }
            case "GetActiveCategories":
            {
                var homeActive = runtime.Group.Home.IsPresent;
                var instanceActive = runtime.Group.Instance.IsPresent;
                if (!homeActive && !instanceActive)
                    return 0;

                lua_createtable(
                    state,
                    (homeActive ? 1 : 0) + (instanceActive ? 1 : 0),
                    0);
                var index = 1;
                if (homeActive)
                {
                    lua_pushinteger(state, (int)WowPartyCategory.Home);
                    lua_rawseti(state, -2, index++);
                }
                if (instanceActive)
                {
                    lua_pushinteger(state, (int)WowPartyCategory.Instance);
                    lua_rawseti(state, -2, index);
                }
                return 1;
            }
            case "ConvertToRaid":
                if (IsPartyConversionAllowed(runtime, toRaid: true))
                {
                    runtime.Group.Home.IsInRaid = true;
                    runtime.TriggerEvent("GROUP_ROSTER_UPDATE");
                }
                return 0;
            case "ConvertToParty":
                if (IsPartyConversionAllowed(runtime, toRaid: false))
                {
                    runtime.Group.Home.IsInRaid = false;
                    runtime.TriggerEvent("GROUP_ROSTER_UPDATE");
                }
                return 0;
            case "RequestGuildPartyState":
                if (runtime.Guild.IsInGuild)
                {
                    party.GuildPartyStateRequestCount++;
                    runtime.TriggerEvent(
                        "GUILD_PARTY_STATE_UPDATED",
                        party.IsGuildParty);
                }
                return 0;
            default:
                return 0;
        }
    }

    private static bool IsPartyConversionAllowed(LuaRuntime runtime, bool toRaid)
    {
        var group = runtime.Group.Home;
        if (!runtime.PartyInfo.PartyConversionAllowed ||
            !group.IsPresent ||
            !runtime.Units.Player.IsGroupLeader)
        {
            return false;
        }

        return toRaid
            ? !group.IsInRaid
            : group.IsInRaid && group.GroupMemberCount <= 5;
    }

    private static WowGroupCategoryState? ResolveGroup(
        LuaRuntime runtime,
        int? requestedCategory) =>
        requestedCategory switch
        {
            null => runtime.Group.Resolve(),
            (int)WowPartyCategory.Home =>
                runtime.Group.Home.IsPresent ? runtime.Group.Home : null,
            (int)WowPartyCategory.Instance =>
                runtime.Group.Instance.IsPresent ? runtime.Group.Instance : null,
            _ => null
        };

    private static WowPartyCategory? ResolveCategory(
        LuaRuntime runtime,
        int? requestedCategory) =>
        requestedCategory switch
        {
            null => runtime.Group.ResolveCategory(),
            (int)WowPartyCategory.Home when runtime.Group.Home.IsPresent =>
                WowPartyCategory.Home,
            (int)WowPartyCategory.Instance when runtime.Group.Instance.IsPresent =>
                WowPartyCategory.Instance,
            _ => null
        };

    private static bool IsPartyInJailersTower(LuaRuntime runtime)
    {
        const int jailersTowerMapId = 2453;
        if (runtime.Units.Player.Position?.MapId != jailersTowerMapId)
            return false;

        var category = runtime.Group.ResolveCategory();
        if (category is null)
            return true;

        var categoryId = (int)category.Value;
        foreach (var unit in runtime.Units.All.Values)
        {
            if (ReferenceEquals(unit, runtime.Units.Player))
                continue;

            var belongsToGroup = unit.InPartyByPartyCategory.TryGetValue(
                categoryId,
                out var inCategory)
                    ? inCategory
                    : unit.IsInParty;
            if (belongsToGroup && unit.Position?.MapId != jailersTowerMapId)
                return false;
        }

        return true;
    }

    private static uint GetDelveState(LuaRuntime runtime)
    {
        const uint delveStateWorldStateId = 25316;
        return runtime.WorldStates.GetValue(delveStateWorldStateId);
    }

    private static bool RequiredTruthyBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) == LUA_TNONE)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static int? OptionalPartyCategory(lua_State state, string usage)
    {
        var type = lua_type(state, 1);
        if (type is LUA_TNONE or LUA_TNIL)
            return null;
        if (lua_isnumber(state, 1) == 0)
        {
            luaL_error(state, usage);
            return null;
        }

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) ||
            value < 0 ||
            value > uint.MaxValue)
        {
            luaL_error(state, usage);
            return null;
        }

        var zeroBased = (long)Math.Truncate(value - 1);
        return zeroBased switch
        {
            0 => (int)WowPartyCategory.Home,
            1 => (int)WowPartyCategory.Instance,
            _ => 0
        };
    }

    private static int RequiredRestrictPingsTo(lua_State state)
    {
        const string usage = "Usage: C_PartyInfo.SetRestrictPings(restrictTo)";
        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, usage);

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
            return luaL_error(state, usage);
        var converted = (int)value;
        if ((uint)converted > 3)
            return luaL_error(state, usage);
        return converted;
    }

    private static byte RequiredLootMethod(lua_State state, string usage)
    {
        if (lua_isnumber(state, 1) == 0)
            return (byte)luaL_error(state, usage);
        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) || value < 0 || value > 5 || value != Math.Truncate(value))
            return (byte)luaL_error(state, usage);
        return (byte)value;
    }

    private static string RequiredString(lua_State state, int index, string usage)
    {
        if (index > lua_gettop(state) || lua_type(state, index) != LUA_TSTRING)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static string? OptionalString(lua_State state, int index, string usage)
    {
        var type = lua_type(state, index);
        if (type is LUA_TNONE or LUA_TNIL)
            return null;
        return RequiredString(state, index, usage);
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value is { } number)
            lua_pushinteger(state, number);
        else
            lua_pushnil(state);
    }
}
