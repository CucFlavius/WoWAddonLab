using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowFriendApi : LuaApiModule
{
    private const int MaximumFriends = 100;
    private const int MaximumIgnores = 50;
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AddFriend", "AddIgnore", "DelIgnore", "DelIgnoreByIndex", "GetFriendInfo",
        "GetFriendInfoByIndex", "GetIgnoreName", "GetNumFriends", "GetNumIgnores",
        "GetNumOnlineFriends", "GetNumWhoResults", "GetSelectedFriend", "GetSelectedIgnore",
        "GetWhoInfo", "RemoveFriendByIndex", "SendWho", "SetSelectedFriend",
        "SetSelectedIgnore", "SetWhoToUi", "ShowFriends", "SortWho"
    ];

    private static readonly string[] BattleNetFunctions =
    [
        "BNCheckBattleTagInviteToGuildMember",
        "BNCheckBattleTagInviteToUnit",
        "BNConnected",
        "BNDeclineFriendInvite",
        "BNFeaturesEnabled",
        "BNFeaturesEnabledAndConnected",
        "BNGetBlockedInfo",
        "BNGetDisplayName",
        "BNGetFOFInfo",
        "BNGetFriendIndex",
        "BNGetFriendInviteInfo",
        "BNGetInfo",
        "BNGetNumBlocked",
        "BNGetNumFOF",
        "BNGetNumFriendInvites",
        "BNGetNumFriends",
        "BNGetSelectedBlock",
        "BNIsBlocked",
        "BNIsFriend",
        "BNIsSelf",
        "BNRemoveFriend",
        "BNRequestFOFInfo",
        "BNRequestInviteFriend",
        "BNSendFriendInvite",
        "BNSendFriendInviteByID",
        "BNSetBlocked",
        "BNSetFriendFavoriteFlag",
        "BNSetFriendNote",
        "BNSetSelectedBlock",
        "BNSetSelectedFriend",
        "BNSummonFriendByIndex"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in BattleNetFunctions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_FriendList");
    }

    private static int Dispatch(lua_State state)
    {
        var friends = LuaBindings.GetRuntime(state).Friends;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "AddFriend":
                friends.AddFriendRequests.Add(new WowAddFriendRequest(
                    RequiredString(state, 1, Usage(operation, "name [, notes]")),
                    OptionalString(state, 2, Usage(operation, "name [, notes]"))));
                return 0;
            case "AddIgnore":
            {
                var name = RequiredString(state, 1, Usage(operation, "name"));
                var canAdd =
                    friends.Ignores.Count < MaximumIgnores &&
                    !friends.Ignores.Take(MaximumIgnores).Contains(
                        name,
                        StringComparer.OrdinalIgnoreCase);
                if (canAdd)
                    friends.AddIgnoreRequests.Add(name);
                PushBoolean(state, canAdd);
                return 1;
            }
            case "DelIgnore":
            {
                var name = RequiredString(state, 1, Usage(operation, "name"));
                var existing = friends.Ignores.Take(MaximumIgnores).FirstOrDefault(
                    ignore => ignore.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (existing is not null)
                    friends.RemoveIgnoreRequests.Add(existing);
                PushBoolean(state, existing is not null);
                return 1;
            }
            case "DelIgnoreByIndex":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    1,
                    Usage(operation, "index"));
                var ignore = At(friends.Ignores, index, MaximumIgnores);
                if (ignore is not null)
                    friends.RemoveIgnoreRequests.Add(ignore);
                return 0;
            }
            case "BNFeaturesEnabled":
                PushBoolean(state, friends.BattleNetFeaturesEnabled);
                return 1;
            case "BNConnected":
            case "BNFeaturesEnabledAndConnected":
                PushBoolean(
                    state,
                    friends.BattleNetFeaturesEnabled && friends.BattleNetConnected);
                return 1;
            case "BNGetInfo":
                if (!friends.BattleNetFeaturesEnabled)
                    return 0;
                lua_pushnil(state);
                PushOptionalString(state, friends.BattleTag);
                lua_pushnumber(state, friends.BattleNetToonId);
                PushOptionalString(state, friends.BroadcastText);
                PushBoolean(state, friends.BattleNetAfk);
                PushBoolean(state, friends.BattleNetDnd);
                PushBoolean(state, friends.RealIdEnabled);
                return 7;
            case "BNGetNumFriends":
            {
                var useTypedFriends = friends.BattleNetFriends.Count > 0;
                lua_pushinteger(
                    state,
                    useTypedFriends
                        ? friends.BattleNetFriends.Count
                        : friends.BattleNetFriendCount);
                lua_pushinteger(
                    state,
                    useTypedFriends
                        ? friends.BattleNetFriends.Count(friend => friend.Online)
                        : friends.BattleNetOnlineFriendCount);
                lua_pushinteger(
                    state,
                    useTypedFriends
                        ? friends.BattleNetFriends.Count(friend => friend.Favorite)
                        : friends.BattleNetFavoriteFriendCount);
                lua_pushinteger(
                    state,
                    useTypedFriends
                        ? friends.BattleNetFriends.Count(friend =>
                            friend.Online && friend.Favorite)
                        : friends.BattleNetOnlineFavoriteFriendCount);
                return 4;
            }
            case "BNGetDisplayName":
            {
                var accountId = RequiredUInt32(
                    state,
                    1,
                    "Usage: BNGetDisplayName(bnetIdAccount)");
                lua_pushstring(
                    state,
                    friends.FindBattleNetDisplayName(accountId) ?? string.Empty);
                return 1;
            }
            case "BNGetNumBlocked":
                lua_pushinteger(state, friends.BattleNetBlockedAccounts.Count);
                return 1;
            case "BNCheckBattleTagInviteToGuildMember":
                friends.BattleNetGuildMemberInviteChecks.Add(
                    RequiredString(
                        state,
                        1,
                        "Usage: BNCheckBattleTagInviteToGuildMember(\"name\")"));
                return 0;
            case "BNCheckBattleTagInviteToUnit":
                friends.BattleNetUnitInviteChecks.Add(
                    RequiredString(
                        state,
                        1,
                        "Usage: BNCheckBattleTagInviteToUnit(\"unit\")"));
                return 0;
            case "BNIsBlocked":
            {
                var accountId = RequiredUInt32(state, 1, "Usage: BNIsBlocked(ID)");
                if (!friends.KnowsBattleNetAccountId(accountId))
                    return 0;
                PushBoolean(
                    state,
                    friends.BattleNetBlockedAccounts.Any(blocked =>
                        blocked.AccountId == accountId));
                return 1;
            }
            case "BNIsFriend":
            {
                var accountId = RequiredUInt32(
                    state,
                    1,
                    "Usage: BNIsFriend(bnetIdAccount)");
                PushBoolean(
                    state,
                    friends.BattleNetFriends.Any(friend =>
                        friend.AccountId == accountId));
                return 1;
            }
            case "BNIsSelf":
            {
                var accountId = RequiredUInt32(
                    state,
                    1,
                    "Usage: BNIsSelf(presenceID)");
                PushBoolean(state, friends.BattleNetAccountId == accountId);
                return 1;
            }
            case "BNGetBlockedInfo":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: BNGetBlockedInfo(index)");
                var blocked = At(friends.BattleNetBlockedAccounts, index);
                if (blocked is null)
                    return luaL_error(state, "Invalid index.");
                lua_pushnumber(state, blocked.AccountId);
                lua_pushstring(state, blocked.DisplayName);
                return 2;
            }
            case "BNGetSelectedBlock":
            {
                var selected = friends.SelectedBattleNetBlockedAccountId;
                var selectedIndex = selected.HasValue
                    ? friends.BattleNetBlockedAccounts
                        .Select((blocked, index) => (blocked, index))
                        .Where(entry => entry.blocked.AccountId == selected.Value)
                        .Select(entry => entry.index + 1)
                        .FirstOrDefault()
                    : 0;
                lua_pushinteger(state, selectedIndex);
                return 1;
            }
            case "BNSetSelectedBlock":
            {
                var oneBased = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: BNSetSelectedBlock(index)");
                var blocked = At(friends.BattleNetBlockedAccounts, oneBased);
                if (blocked is null)
                {
                    return luaL_error(
                        state,
                        $"Block index {unchecked(oneBased + 1)} too large, only " +
                        $"{friends.BattleNetBlockedAccounts.Count} blocked.");
                }
                friends.SelectedBattleNetBlockedAccountId = blocked.AccountId;
                return 0;
            }
            case "BNSetBlocked":
            {
                if (lua_gettop(state) != 2)
                    return luaL_error(state, "Usage: BNSetBlocked(ID, true/false)");
                var accountId = RequiredUInt32(
                    state,
                    1,
                    "Usage: BNSetBlocked(ID, true/false)");
                if (friends.KnowsBattleNetAccountId(accountId))
                {
                    friends.BattleNetBlockedRequests.Add(
                        new WowBattleNetBlockedRequest(
                            accountId,
                            lua_toboolean(state, 2) != 0));
                }
                return 0;
            }
            case "BNGetFriendIndex":
            {
                var accountId = RequiredUInt32(
                    state,
                    1,
                    "Usage: BNGetFriendIndex(presenceID)");
                var friendIndex = friends.BattleNetFriends
                    .Select((friend, index) => (friend, index))
                    .Where(entry => entry.friend.AccountId == accountId)
                    .Select(entry => entry.index + 1)
                    .FirstOrDefault();
                if (friendIndex == 0)
                    return 0;
                lua_pushinteger(state, friendIndex);
                return 1;
            }
            case "BNSetSelectedFriend":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: BNSetSelectedFriend(index)");
                if (index >= friends.BattleNetFriends.Count)
                {
                    return luaL_error(
                        state,
                        $"Friend index {unchecked(index + 1)} too large, only " +
                        $"{friends.BattleNetFriends.Count} friends.");
                }
                friends.SelectedBattleNetFriend = checked((int)index + 1);
                return 0;
            }
            case "BNSummonFriendByIndex":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    1,
                    "Usage: BNSummonFriendByIndex(index)");
                var friend = At(friends.BattleNetFriends, index);
                if (friend is not null)
                    friends.BattleNetSummonFriendRequests.Add(friend.AccountId);
                return 0;
            }
            case "BNRemoveFriend":
            {
                var accountId = RequiredUInt32(
                    state,
                    1,
                    "Usage: BNRemoveFriend(presenceID)");
                if (friends.KnowsBattleNetAccountId(accountId))
                    friends.BattleNetRemoveFriendRequests.Add(accountId);
                return 0;
            }
            case "BNSetFriendNote":
            {
                var accountId = RequiredUInt32(
                    state,
                    1,
                    "Usage: BNSetFriendNote(presenceID, noteText)");
                var note = RequiredString(
                    state,
                    2,
                    "Usage: BNSetFriendNote(presenceID, noteText)");
                if (friends.KnowsBattleNetAccountId(accountId))
                {
                    friends.BattleNetFriendNoteRequests.Add(
                        new WowBattleNetFriendNoteRequest(accountId, note));
                }
                return 0;
            }
            case "BNSetFriendFavoriteFlag":
            {
                if (lua_gettop(state) != 2)
                {
                    return luaL_error(
                        state,
                        "Usage: BNSetFriendFavoriteFlag(presenceID, favorite)");
                }
                var accountId = RequiredUInt32(
                    state,
                    1,
                    "Usage: BNSetFriendFavoriteFlag(presenceID, favorite)");
                if (friends.KnowsBattleNetAccountId(accountId))
                {
                    friends.BattleNetFavoriteRequests.Add(
                        new WowBattleNetFavoriteRequest(
                            accountId,
                            lua_toboolean(state, 2) != 0));
                }
                return 0;
            }
            case "BNGetNumFriendInvites":
                lua_pushinteger(state, friends.BattleNetFriendInvites.Count);
                return 1;
            case "BNGetFriendInviteInfo":
            {
                var invite = At(
                    friends.BattleNetFriendInvites,
                    RequiredOneBasedIndex(
                        state,
                        1,
                        "Usage: BNGetFriendInviteInfo(index)"));
                if (invite is null)
                    return 0;
                lua_pushnumber(state, invite.AccountId);
                lua_pushstring(state, invite.DisplayName);
                PushBoolean(state, invite.IsBattleTag);
                lua_pushnil(state);
                lua_pushnumber(state, invite.InviteTime);
                return 5;
            }
            case "BNSendFriendInvite":
            {
                var target = RequiredString(
                    state,
                    1,
                    "Usage: BNSendFriendInvite(battletag/email)");
                if (target.Contains('@') || target.Contains('#'))
                    friends.BattleNetFriendInviteRequests.Add(target);
                return 0;
            }
            case "BNSendFriendInviteByID":
            {
                var accountId = RequiredUInt32(
                    state,
                    1,
                    "Usage: BNSendFriendInviteByID(ID)");
                if (friends.KnowsBattleNetAccountId(accountId))
                    friends.BattleNetFriendInviteByIdRequests.Add(accountId);
                return 0;
            }
            case "BNDeclineFriendInvite":
            {
                var accountId = RequiredUInt32(
                    state,
                    1,
                    "Usage: BNAcceptFriendInvite(ID)");
                if (friends.BattleNetFriendInvites.Any(invite =>
                        invite.AccountId == accountId))
                {
                    friends.BattleNetDeclineFriendInviteRequests.Add(accountId);
                }
                return 0;
            }
            case "BNRequestFOFInfo":
            {
                var accountId = RequiredUInt32(
                    state,
                    1,
                    "Usage: BNRequestFOF(ID)");
                if (!friends.KnowsBattleNetAccountId(accountId))
                    return 0;
                friends.BattleNetFofRequests.Add(accountId);
                PushBoolean(state, friends.BattleNetFofRequestResult);
                return 1;
            }
            case "BNGetNumFOF":
                _ = RequiredUInt32(state, 1, "Usage: BNGetNumFOF(ID)");
                lua_pushinteger(
                    state,
                    friends.BattleNetFofEntries.Count(entry => entry.IsMutualFriend));
                lua_pushinteger(
                    state,
                    friends.BattleNetFofEntries.Count(entry => !entry.IsMutualFriend));
                return 2;
            case "BNGetFOFInfo":
            {
                if (lua_isnumber(state, 3) == 0)
                    return luaL_error(state, "Usage: BNGetFOFInfo(mutual, non, index)");
                var includeMutual = lua_toboolean(state, 1) != 0;
                var includeNonMutual = lua_toboolean(state, 2) != 0;
                if (!includeMutual && !includeNonMutual)
                    return luaL_error(state, "Must select mutual and/or non.");
                var index = RequiredOneBasedIndex(
                    state,
                    3,
                    "Usage: BNGetFOFInfo(mutual, non, index)");
                var entries = friends.BattleNetFofEntries.Where(entry =>
                    (entry.IsMutualFriend && includeMutual) ||
                    (!entry.IsMutualFriend && includeNonMutual));
                var entry = index <= int.MaxValue
                    ? entries.ElementAtOrDefault((int)index)
                    : null;
                if (entry is null)
                    return 0;
                lua_pushnumber(state, entry.AccountId);
                lua_pushstring(state, entry.DisplayName);
                PushBoolean(state, entry.IsMutualFriend);
                return 3;
            }
            case "BNRequestInviteFriend":
            {
                var accountId = RequiredUInt32(
                    state,
                    1,
                    "Usage: BNRequestInviteFriend(presenceID, [tank], [heal], [dps])");
                if (friends.BattleNetFriends.Any(friend =>
                        friend.AccountId == accountId))
                {
                    friends.BattleNetInviteRoleRequests.Add(
                        new WowBattleNetInviteRoleRequest(
                            accountId,
                            lua_toboolean(state, 2) != 0,
                            lua_toboolean(state, 3) != 0,
                            lua_toboolean(state, 4) != 0));
                }
                return 0;
            }
            case "GetNumFriends":
                lua_pushinteger(state, Math.Min(friends.Friends.Count, MaximumFriends));
                return 1;
            case "GetNumOnlineFriends":
                lua_pushinteger(
                    state,
                    friends.Friends.Take(MaximumFriends).Count(friend => friend.Connected));
                return 1;
            case "GetNumIgnores":
                lua_pushinteger(state, Math.Min(friends.Ignores.Count, MaximumIgnores));
                return 1;
            case "GetNumWhoResults":
                lua_pushinteger(state, friends.WhoResults.Count);
                lua_pushinteger(state, friends.WhoResults.Count);
                return 2;
            case "GetSelectedFriend":
                PushOptionalInteger(state, friends.SelectedFriend);
                return 1;
            case "SetSelectedFriend":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    1,
                    Usage(operation, "index"));
                friends.SelectedFriend =
                    index < MaximumFriends && index < friends.Friends.Count
                        ? checked((int)index + 1)
                        : null;
                return 0;
            }
            case "GetSelectedIgnore":
                PushOptionalInteger(state, friends.SelectedIgnore);
                return 1;
            case "SetSelectedIgnore":
            {
                var index = RequiredOneBasedIndex(
                    state,
                    1,
                    Usage(operation, "index"));
                friends.SelectedIgnore =
                    index < MaximumIgnores && index < friends.Ignores.Count
                        ? checked((int)index + 1)
                        : null;
                return 0;
            }
            case "SetWhoToUi":
                if (lua_gettop(state) < 1)
                    return luaL_error(state, Usage(operation, "whoToUi"));
                friends.WhoResultsToUi = lua_toboolean(state, 1) != 0;
                return 0;
            case "GetFriendInfoByIndex":
                return PushFriend(
                    state,
                    At(
                        friends.Friends,
                        RequiredOneBasedIndex(
                            state,
                            1,
                            Usage(operation, "index")),
                        MaximumFriends));
            case "GetFriendInfo":
            {
                var name = RequiredString(state, 1, Usage(operation, "name"));
                return PushFriend(
                    state,
                    friends.Friends.Take(MaximumFriends).FirstOrDefault(friend =>
                        friend.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
            }
            case "GetIgnoreName":
                PushOptionalString(
                    state,
                    At(
                        friends.Ignores,
                        RequiredOneBasedIndex(
                            state,
                            1,
                            Usage(operation, "index")),
                        MaximumIgnores));
                return 1;
            case "RemoveFriendByIndex":
            {
                var friend = At(
                    friends.Friends,
                    RequiredOneBasedIndex(
                        state,
                        1,
                        Usage(operation, "index")),
                    MaximumFriends);
                if (friend is not null)
                    friends.RemoveFriendRequests.Add(friend.Guid);
                return 0;
            }
            case "SendWho":
                friends.WhoRequests.Add(new WowWhoRequest(
                    RequiredString(state, 1, Usage(operation, "filter [, origin]")),
                    OptionalInt32(state, 2, Usage(operation, "filter [, origin]"))));
                return 0;
            case "GetWhoInfo":
                return PushWhoInfo(
                    state,
                    At(
                        friends.WhoResults,
                        RequiredOneBasedIndex(
                            state,
                            1,
                            Usage(operation, "index"))));
            case "ShowFriends":
                friends.ShowFriendsRequests++;
                return 0;
            case "SortWho":
                friends.WhoSortRequests.Add(
                    RequiredString(state, 1, Usage(operation, "sorting")));
                return 0;
            default:
                return 0;
        }
    }

    private static int PushFriend(lua_State state, WowFriendInfoState? friend)
    {
        if (friend is null)
            return 0;
        lua_newtable(state);
        SetBoolean(state, "connected", friend.Connected);
        SetString(state, "name", friend.Name);
        SetOptionalString(state, "className", friend.ClassName);
        SetOptionalString(state, "area", friend.Area);
        SetOptionalString(state, "notes", friend.Notes);
        SetString(state, "guid", friend.Guid);
        SetInteger(state, "level", friend.Level);
        SetBoolean(state, "dnd", friend.IsDnd);
        SetBoolean(state, "afk", friend.IsAfk);
        SetUnsignedInteger(state, "rafLinkType", friend.RafLinkType);
        return 1;
    }

    private static int PushWhoInfo(lua_State state, WowWhoInfoState? info)
    {
        if (info is null)
            return 0;
        lua_createtable(state, 0, 9);
        SetString(state, "fullName", info.FullName);
        SetString(state, "fullGuildName", info.FullGuildName);
        SetInteger(state, "level", info.Level);
        SetString(state, "raceStr", info.Race);
        SetString(state, "classStr", info.Class);
        SetString(state, "area", info.Area);
        SetOptionalString(state, "filename", info.Filename);
        SetInteger(state, "gender", info.Gender);
        SetOptionalInteger(state, "timerunningSeasonID", info.TimerunningSeasonId);
        return 1;
    }

    private static T? At<T>(
        IList<T> values,
        uint zeroBasedIndex,
        int maximum = int.MaxValue) =>
        zeroBasedIndex < maximum && zeroBasedIndex < values.Count
            ? values[(int)zeroBasedIndex]
            : default;

    private static uint RequiredOneBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        var zeroBased = value - 1.0;
        if (zeroBased < int.MinValue || zeroBased > int.MaxValue)
            return uint.MaxValue;
        return unchecked((uint)(int)zeroBased);
    }

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return unchecked((uint)(int)value);
    }

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TSTRING)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static string? OptionalString(
        lua_State state,
        int index,
        string usage)
    {
        var type = lua_type(state, index);
        if (type is LUA_TNONE or LUA_TNIL)
            return null;
        if (type != LUA_TSTRING)
        {
            luaL_error(state, usage);
            return null;
        }
        return lua_tostring(state, index);
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string usage)
    {
        var type = lua_type(state, index);
        if (type is LUA_TNONE or LUA_TNIL)
            return null;
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return null;
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
        {
            luaL_error(state, usage);
            return null;
        }
        return (int)value;
    }

    private static string Usage(string operation, string arguments) =>
        $"Usage: C_FriendList.{operation}({arguments})";

    private static void PushBoolean(lua_State state, bool value) =>
        lua_pushboolean(state, value ? 1 : 0);

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null) lua_pushnil(state); else lua_pushstring(state, value);
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value.HasValue) lua_pushinteger(state, value.Value); else lua_pushnil(state);
    }

    private static void SetString(lua_State state, string key, string value)
    {
        lua_pushstring(state, value); lua_setfield(state, -2, key);
    }

    private static void SetOptionalString(lua_State state, string key, string? value)
    {
        PushOptionalString(state, value); lua_setfield(state, -2, key);
    }

    private static void SetInteger(lua_State state, string key, int value)
    {
        lua_pushinteger(state, value); lua_setfield(state, -2, key);
    }

    private static void SetUnsignedInteger(lua_State state, string key, uint value)
    {
        lua_pushnumber(state, value); lua_setfield(state, -2, key);
    }

    private static void SetOptionalInteger(lua_State state, string key, int? value)
    {
        PushOptionalInteger(state, value); lua_setfield(state, -2, key);
    }

    private static void SetBoolean(lua_State state, string key, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0); lua_setfield(state, -2, key);
    }
}
