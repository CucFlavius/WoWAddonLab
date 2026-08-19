using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSocialQueueApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetAllGroups",
        "GetConfig",
        "GetGroupForPlayer",
        "GetGroupInfo",
        "GetGroupMembers",
        "GetGroupQueues",
        "RequestToJoin",
        "SignalToastDisplayed"
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
        lua_setglobal(state, "C_SocialQueue");
    }

    private static int Dispatch(lua_State state)
    {
        var socialQueue = LuaBindings.GetRuntime(state).SocialQueue;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        return operation switch
        {
            "GetAllGroups" => GetAllGroups(state, socialQueue),
            "GetConfig" => GetConfig(state, socialQueue),
            "GetGroupForPlayer" => GetGroupForPlayer(state, socialQueue),
            "GetGroupInfo" => GetGroupInfo(state, socialQueue),
            "GetGroupMembers" => GetGroupMembers(state, socialQueue),
            "GetGroupQueues" => GetGroupQueues(state, socialQueue),
            "RequestToJoin" => RequestToJoin(state, socialQueue),
            "SignalToastDisplayed" =>
                SignalToastDisplayed(state, socialQueue),
            _ => 0
        };
    }

    private static int GetAllGroups(
        lua_State state,
        WowSocialQueueState socialQueue)
    {
        var allowNonJoinable = OptionalBoolean(state, 1);
        var allowNonQueuedGroups = OptionalBoolean(state, 2);
        var groups = socialQueue.Groups.Values
            .Where(group =>
                !StringComparer.OrdinalIgnoreCase.Equals(
                    group.Guid,
                    socialQueue.CurrentGroupGuid) &&
                (allowNonQueuedGroups || group.NumQueues != 0) &&
                (allowNonJoinable || group.CanJoin))
            .ToArray();

        lua_createtable(state, groups.Length, 0);
        for (var index = 0; index < groups.Length; index++)
        {
            PushGuid(state, groups[index].Guid);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetConfig(
        lua_State state,
        WowSocialQueueState socialQueue)
    {
        if (socialQueue.Config is not { } config)
            return 0;

        lua_createtable(state, 0, 23);
        SetBoolean(state, "TOASTS_DISABLED", config.ToastsDisabled);
        SetNumber(state, "TOAST_DURATION", config.ToastDuration);
        SetNumber(state, "DELAY_DURATION", config.DelayDuration);
        SetNumber(state, "QUEUE_MULTIPLIER", config.QueueMultiplier);
        SetNumber(state, "PLAYER_MULTIPLIER", config.PlayerMultiplier);
        SetNumber(state, "PLAYER_FRIEND_VALUE", config.PlayerFriendValue);
        SetNumber(state, "PLAYER_GUILD_VALUE", config.PlayerGuildValue);
        SetNumber(
            state,
            "THROTTLE_INITIAL_THRESHOLD",
            config.ThrottleInitialThreshold);
        SetNumber(
            state,
            "THROTTLE_DECAY_TIME",
            config.ThrottleDecayTime);
        SetNumber(
            state,
            "THROTTLE_PRIORITY_SPIKE",
            config.ThrottlePrioritySpike);
        SetNumber(
            state,
            "THROTTLE_MIN_THRESHOLD",
            config.ThrottleMinThreshold);
        SetNumber(
            state,
            "THROTTLE_PVP_PRIORITY_NORMAL",
            config.ThrottlePvpPriorityNormal);
        SetNumber(
            state,
            "THROTTLE_PVP_PRIORITY_LOW",
            config.ThrottlePvpPriorityLow);
        SetNumber(
            state,
            "THROTTLE_PVP_HONOR_THRESHOLD",
            config.ThrottlePvpHonorThreshold);
        SetNumber(
            state,
            "THROTTLE_LFGLIST_PRIORITY_DEFAULT",
            config.ThrottleLfgListPriorityDefault);
        SetNumber(
            state,
            "THROTTLE_LFGLIST_PRIORITY_ABOVE",
            config.ThrottleLfgListPriorityAbove);
        SetNumber(
            state,
            "THROTTLE_LFGLIST_PRIORITY_BELOW",
            config.ThrottleLfgListPriorityBelow);
        SetNumber(
            state,
            "THROTTLE_LFGLIST_ILVL_SCALING_ABOVE",
            config.ThrottleLfgListItemLevelScalingAbove);
        SetNumber(
            state,
            "THROTTLE_LFGLIST_ILVL_SCALING_BELOW",
            config.ThrottleLfgListItemLevelScalingBelow);
        SetNumber(
            state,
            "THROTTLE_RF_PRIORITY_ABOVE",
            config.ThrottleRfPriorityAbove);
        SetNumber(
            state,
            "THROTTLE_RF_ILVL_SCALING_ABOVE",
            config.ThrottleRfItemLevelScalingAbove);
        SetNumber(
            state,
            "THROTTLE_DF_MAX_ITEM_LEVEL",
            config.ThrottleDfMaxItemLevel);
        SetNumber(
            state,
            "THROTTLE_DF_BEST_PRIORITY",
            config.ThrottleDfBestPriority);
        return 1;
    }

    private static int GetGroupForPlayer(
        lua_State state,
        WowSocialQueueState socialQueue)
    {
        const string usage =
            "Usage: local groupGUID, isSoloQueueParty = C_SocialQueue.GetGroupForPlayer(playerGUID)";
        var playerGuid = RequiredGuid(state, 1, usage);
        if (!socialQueue.GroupsByPlayer.TryGetValue(
                playerGuid,
                out var playerGroup) ||
            !socialQueue.Groups.TryGetValue(
                playerGroup.GroupGuid,
                out var group))
        {
            return 0;
        }

        PushGuid(state, group.Guid);
        lua_pushboolean(state, group.IsSoloQueueParty ? 1 : 0);
        return 2;
    }

    private static int GetGroupInfo(
        lua_State state,
        WowSocialQueueState socialQueue)
    {
        const string usage =
            "Usage: local canJoin, numQueues, needTank, needHealer, needDamage, isSoloQueueParty, questSessionActive, leaderGUID = C_SocialQueue.GetGroupInfo(groupGUID)";
        var groupGuid = RequiredGuid(state, 1, usage);
        if (!TryGetExternalGroup(socialQueue, groupGuid, out var group))
            return 0;

        lua_pushboolean(state, group.CanJoin ? 1 : 0);
        lua_pushinteger(state, group.NumQueues);
        lua_pushboolean(state, group.NeedTank ? 1 : 0);
        lua_pushboolean(state, group.NeedHealer ? 1 : 0);
        lua_pushboolean(state, group.NeedDamage ? 1 : 0);
        lua_pushboolean(state, group.IsSoloQueueParty ? 1 : 0);
        lua_pushboolean(state, group.QuestSessionActive ? 1 : 0);
        PushGuid(state, group.LeaderGuid);
        return 8;
    }

    private static int GetGroupMembers(
        lua_State state,
        WowSocialQueueState socialQueue)
    {
        const string usage =
            "Usage: local groupMembers = C_SocialQueue.GetGroupMembers(groupGUID)";
        var groupGuid = RequiredGuid(state, 1, usage);
        if (!TryGetExternalGroup(socialQueue, groupGuid, out var group))
            return 0;

        lua_createtable(state, group.Members.Count, 0);
        for (var index = 0; index < group.Members.Count; index++)
        {
            var member = group.Members[index];
            lua_createtable(state, 0, 2);
            SetGuid(state, "guid", member.Guid);
            SetDatabaseId(state, "clubId", member.ClubId);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int GetGroupQueues(
        lua_State state,
        WowSocialQueueState socialQueue)
    {
        const string usage =
            "Usage: local queues = C_SocialQueue.GetGroupQueues(groupGUID)";
        var groupGuid = RequiredGuid(state, 1, usage);
        if (!TryGetExternalGroup(socialQueue, groupGuid, out var group) ||
            group.Queues.Count == 0)
        {
            return 0;
        }

        lua_createtable(state, group.Queues.Count, 0);
        for (var index = 0; index < group.Queues.Count; index++)
        {
            PushGroupQueueInfo(state, group.Queues[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int RequestToJoin(
        lua_State state,
        WowSocialQueueState socialQueue)
    {
        const string usage =
            "Usage: local requestSuccessful = C_SocialQueue.RequestToJoin(groupGUID [, applyAsTank, applyAsHealer, applyAsDamage])";
        var groupGuid = RequiredGuid(state, 1, usage);
        var applyAsTank = OptionalBoolean(state, 2);
        var applyAsHealer = OptionalBoolean(state, 3);
        var applyAsDamage = OptionalBoolean(state, 4);
        socialQueue.Requests.Add(
            new WowSocialQueueRequest(
                "RequestToJoin",
                [groupGuid, applyAsTank, applyAsHealer, applyAsDamage]));
        var result = socialQueue.JoinRequestResults.TryGetValue(
            groupGuid,
            out var configuredResult)
            ? configuredResult
            : socialQueue.DefaultJoinRequestResult;
        lua_pushboolean(state, result ? 1 : 0);
        return 1;
    }

    private static int SignalToastDisplayed(
        lua_State state,
        WowSocialQueueState socialQueue)
    {
        const string usage =
            "Usage: C_SocialQueue.SignalToastDisplayed(groupGUID, priority)";
        var groupGuid = RequiredGuid(state, 1, usage);
        var priority = RequiredFiniteNumber(state, 2, usage);
        socialQueue.Requests.Add(
            new WowSocialQueueRequest(
                "SignalToastDisplayed",
                [groupGuid, (float)priority]));
        return 0;
    }

    private static void PushGroupQueueInfo(
        lua_State state,
        WowSocialQueueGroupQueueInfo queue)
    {
        lua_createtable(state, 0, 7);
        SetInteger(state, "clientID", queue.ClientId);
        SetBoolean(state, "eligible", queue.Eligible);
        SetBoolean(state, "needTank", queue.NeedTank);
        SetBoolean(state, "needHealer", queue.NeedHealer);
        SetBoolean(state, "needDamage", queue.NeedDamage);
        SetBoolean(state, "isAutoAccept", queue.IsAutoAccept);
        PushQueueData(state, queue.QueueData);
        lua_setfield(state, -2, "queueData");
    }

    private static void PushQueueData(
        lua_State state,
        WowSocialQueueQueueData queueData)
    {
        lua_createtable(state, 0, 10);
        SetOptionalString(state, "queueType", queueData.QueueType);
        if (queueData.LfgIds is { } lfgIds)
        {
            lua_createtable(state, lfgIds.Count, 0);
            for (var index = 0; index < lfgIds.Count; index++)
            {
                lua_pushinteger(state, lfgIds[index]);
                lua_rawseti(state, -2, index + 1);
            }
        }
        else
        {
            lua_pushnil(state);
        }
        lua_setfield(state, -2, "lfgIDs");
        SetOptionalInteger(state, "lfgListID", queueData.LfgListId);
        SetOptionalInteger(state, "activityID", queueData.ActivityId);
        SetOptionalString(
            state,
            "battlefieldType",
            queueData.BattlefieldType);
        SetOptionalInteger(state, "listID", queueData.ListId);
        SetOptionalString(state, "mapName", queueData.MapName);
        SetOptionalBoolean(state, "rated", queueData.Rated);
        SetOptionalBoolean(state, "isBrawl", queueData.IsBrawl);
        SetOptionalInteger(state, "teamSize", queueData.TeamSize);
    }

    private static bool TryGetExternalGroup(
        WowSocialQueueState socialQueue,
        string guid,
        out WowSocialQueueGroup group)
    {
        if (StringComparer.OrdinalIgnoreCase.Equals(
                guid,
                socialQueue.CurrentGroupGuid))
        {
            group = null!;
            return false;
        }
        return socialQueue.Groups.TryGetValue(guid, out group!);
    }

    private static string RequiredGuid(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_type(state, index) != LUA_TSTRING)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static double RequiredFiniteNumber(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value))
        {
            luaL_error(state, usage);
            return 0;
        }
        return value;
    }

    private static bool OptionalBoolean(lua_State state, int index) =>
        index <= lua_gettop(state) && lua_toboolean(state, index) != 0;

    private static void PushGuid(lua_State state, string? value)
    {
        if (string.IsNullOrEmpty(value))
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }

    private static void SetGuid(
        lua_State state,
        string name,
        string? value)
    {
        PushGuid(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetDatabaseId(
        lua_State state,
        string name,
        ulong? value)
    {
        if (!value.HasValue)
        {
            lua_pushnil(state);
        }
        else if (value.Value > 0x1F_FFFF_FFFF_FFFFUL)
        {
            lua_pushstring(
                state,
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"0x{value.Value:X16}"));
        }
        else
        {
            lua_pushnumber(state, value.Value);
        }
        lua_setfield(state, -2, name);
    }

    private static void SetInteger(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string name,
        int? value)
    {
        if (value.HasValue)
            lua_pushinteger(state, value.Value);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, name);
    }

    private static void SetNumber(
        lua_State state,
        string name,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBoolean(
        lua_State state,
        string name,
        bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalBoolean(
        lua_State state,
        string name,
        bool? value)
    {
        if (value.HasValue)
            lua_pushboolean(state, value.Value ? 1 : 0);
        else
            lua_pushnil(state);
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
}
