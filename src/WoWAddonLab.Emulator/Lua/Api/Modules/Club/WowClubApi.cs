using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowClubApi : LuaApiModule
{
    private const ulong MaximumExactLuaInteger = 0x1FFFFFFFFFFFFF;
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetClubInfo",
        "GetGuildClubId",
        "GetAvatarIdList",
        "GetMemberInfoForSelf",
        "GetSubscribedClubs",
        "GetInvitationsForSelf",
        "GetInvitationCandidates",
        "GetTickets",
        "RequestTickets",
        "IsEnabled",
        "IsRestricted",
        "ShouldAllowClubType",
        "ClearClubPresenceSubscription",
        "ClearAutoAdvanceStreamViewMarker",
        "Flush",
        "DoesAnyCommunityHaveUnreadMessages"
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
        lua_setglobal(state, "C_Club");
    }

    private static int Dispatch(lua_State state)
    {
        var clubs = LuaBindings.GetRuntime(state).Clubs;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "ClearAutoAdvanceStreamViewMarker":
                clubs.ClearAutoAdvanceStreamViewMarkerRequests++;
                return 0;
            case "ClearClubPresenceSubscription":
                clubs.ClearClubPresenceSubscriptionRequests++;
                return 0;
            case "DoesAnyCommunityHaveUnreadMessages":
                PushBoolean(state, clubs.AnyCommunityHasUnreadMessages);
                return 1;
            case "Flush":
                clubs.FlushRequests++;
                return 0;
            case "GetAvatarIdList":
            {
                var clubType = RequiredClubType(
                    state,
                    1,
                    "Usage: local avatarIds = C_Club.GetAvatarIdList(clubType)");
                if (!clubs.AvatarIdsByClubType.TryGetValue(
                        clubType,
                        out var avatarIds))
                {
                    lua_pushnil(state);
                    return 1;
                }

                PushInt32Array(state, avatarIds);
                return 1;
            }
            case "GetClubInfo":
            {
                var clubId = RequiredDatabaseId(
                    state,
                    1,
                    "Usage: local info = C_Club.GetClubInfo(clubId)");
                if (!clubs.ClubInfoById.TryGetValue(clubId, out var club))
                {
                    lua_pushnil(state);
                    return 1;
                }
                PushClubInfo(state, club);
                return 1;
            }
            case "GetGuildClubId":
                if (clubs.GuildClubId is { } guildClubId)
                    lua_pushnumber(state, guildClubId);
                else
                    lua_pushnil(state);
                return 1;
            case "GetInvitationCandidates":
            {
                const string usage =
                    "Usage: local candidates = C_Club.GetInvitationCandidates([filter, maxResults, cursorPosition, allowFullMatch], clubId)";
                var filter = OptionalString(state, 1, usage);
                var maxResults = OptionalUInt32(state, 2, usage);
                var cursorPosition = OptionalInt32(state, 3, usage);
                var allowFullMatch = OptionalBoolean(state, 4, usage);
                var clubId = RequiredDatabaseId(state, 5, usage);
                clubs.InvitationCandidateQueries.Add(
                    new WowClubInvitationCandidateQuery(
                        filter,
                        maxResults,
                        cursorPosition,
                        allowFullMatch,
                        clubId));

                var candidates =
                    clubs.InvitationCandidatesByClubId.TryGetValue(
                        clubId,
                        out var configured)
                        ? configured
                        : [];
                lua_createtable(state, candidates.Count, 0);
                for (var index = 0; index < candidates.Count; index++)
                {
                    PushInvitationCandidate(state, candidates[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "GetInvitationsForSelf":
                lua_createtable(state, clubs.InvitationsForSelf.Count, 0);
                for (var index = 0;
                     index < clubs.InvitationsForSelf.Count;
                     index++)
                {
                    PushSelfInvitation(state, clubs.InvitationsForSelf[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "GetMemberInfoForSelf":
            {
                var clubId = RequiredDatabaseId(
                    state,
                    1,
                    "Usage: local info = C_Club.GetMemberInfoForSelf(clubId)");
                if (!clubs.SelfMemberInfoByClubId.TryGetValue(
                        clubId,
                        out var member))
                {
                    lua_pushnil(state);
                    return 1;
                }
                PushMemberInfo(state, member);
                return 1;
            }
            case "GetSubscribedClubs":
                lua_createtable(state, clubs.SubscribedClubs.Count, 0);
                for (var index = 0; index < clubs.SubscribedClubs.Count; index++)
                {
                    PushClubInfo(state, clubs.SubscribedClubs[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "GetTickets":
            {
                var clubId = RequiredDatabaseId(
                    state,
                    1,
                    "Usage: local tickets = C_Club.GetTickets(clubId)");
                var tickets = clubs.TicketsByClubId.TryGetValue(
                    clubId,
                    out var configured)
                    ? configured
                    : [];
                lua_createtable(state, tickets.Count, 0);
                for (var index = 0; index < tickets.Count; index++)
                {
                    PushTicket(state, tickets[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "IsEnabled":
                PushBoolean(state, clubs.Enabled);
                return 1;
            case "IsRestricted":
                lua_pushnumber(state, clubs.RestrictionReason);
                return 1;
            case "RequestTickets":
                clubs.TicketRequests.Add(
                    RequiredDatabaseId(
                        state,
                        1,
                        "Usage: C_Club.RequestTickets(clubId)"));
                return 0;
            case "ShouldAllowClubType":
            {
                var clubType = RequiredClubType(
                    state,
                    1,
                    "Usage: local clubTypeIsAllowed = C_Club.ShouldAllowClubType(clubType)");
                PushBoolean(
                    state,
                    clubType switch
                    {
                        0 => clubs.AllowBattleNetClubType,
                        1 => clubs.AllowCharacterClubType,
                        2 => true,
                        _ => false
                    });
                return 1;
            }
            default:
                return 0;
        }
    }

    private static int RequiredClubType(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value is < 0 or > 3)
        {
            return luaL_error(state, usage);
        }
        return value;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            return luaL_error(state, usage);
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
        {
            return luaL_error(state, usage);
        }
        return (int)value;
    }

    private static ulong RequiredDatabaseId(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) == LUA_TSTRING)
        {
            var text = lua_tostring(state, index) ?? string.Empty;
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                if (ulong.TryParse(
                        text.AsSpan(2),
                        NumberStyles.AllowHexSpecifier,
                        CultureInfo.InvariantCulture,
                        out var parsed) &&
                    parsed > MaximumExactLuaInteger)
                {
                    return parsed;
                }
                luaL_error(state, usage);
                return 0;
            }
        }

        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < 0 ||
            number > MaximumExactLuaInteger)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (ulong)number;
    }

    private static string? OptionalString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return null;
        }
        if (lua_isstring(state, index) == 0)
        {
            luaL_error(state, usage);
            return null;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static uint? OptionalUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return null;
        }
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return null;
        }
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
        {
            luaL_error(state, usage);
            return null;
        }
        return (uint)value;
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string usage) =>
        lua_type(state, index) is LUA_TNONE or LUA_TNIL
            ? null
            : RequiredInt32(state, index, usage);

    private static bool? OptionalBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return null;
        }
        if (lua_type(state, index) != LUA_TBOOLEAN)
        {
            luaL_error(state, usage);
            return null;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static void PushClubInfo(lua_State state, WowClubInfoState club)
    {
        lua_createtable(state, 0, 12);
        SetDatabaseId(state, "clubId", club.ClubId);
        SetString(state, "name", club.Name);
        SetOptionalString(state, "shortName", club.ShortName);
        SetString(state, "description", club.Description);
        SetString(state, "broadcast", club.Broadcast);
        SetInteger(state, "clubType", club.ClubType);
        SetInteger(state, "avatarId", club.AvatarId);
        SetOptionalInteger(state, "memberCount", club.MemberCount);
        SetOptionalNumber(state, "favoriteTimeStamp", club.FavoriteTimeStamp);
        SetOptionalNumber(state, "joinTime", club.JoinTime);
        SetOptionalBoolean(
            state,
            "socialQueueingEnabled",
            club.SocialQueueingEnabled);
        SetOptionalBoolean(state, "crossFaction", club.CrossFaction);
    }

    private static void PushMemberInfo(
        lua_State state,
        WowClubMemberInfoState member)
    {
        lua_createtable(state, 0, 31);
        SetBoolean(state, "isSelf", member.IsSelf);
        SetDatabaseId(state, "memberId", member.MemberId);
        SetOptionalString(state, "name", member.Name);
        SetOptionalInteger(state, "role", member.Role);
        SetNumber(state, "presence", member.Presence);
        SetOptionalInteger(state, "clubType", member.ClubType);
        SetOptionalString(state, "guid", member.Guid);
        SetOptionalInteger(state, "bnetAccountId", member.BnetAccountId);
        SetOptionalString(state, "memberNote", member.MemberNote);
        SetOptionalString(state, "officerNote", member.OfficerNote);
        SetOptionalInteger(state, "classID", member.ClassId);
        SetOptionalInteger(state, "race", member.Race);
        SetOptionalInteger(state, "level", member.Level);
        SetOptionalString(state, "zone", member.Zone);
        SetOptionalInteger(
            state,
            "achievementPoints",
            member.AchievementPoints);
        SetOptionalInteger(state, "profession1ID", member.Profession1Id);
        SetOptionalInteger(state, "profession1Rank", member.Profession1Rank);
        SetOptionalString(state, "profession1Name", member.Profession1Name);
        SetOptionalInteger(state, "profession2ID", member.Profession2Id);
        SetOptionalInteger(state, "profession2Rank", member.Profession2Rank);
        SetOptionalString(state, "profession2Name", member.Profession2Name);
        SetOptionalInteger(state, "lastOnlineYear", member.LastOnlineYear);
        SetOptionalInteger(state, "lastOnlineMonth", member.LastOnlineMonth);
        SetOptionalInteger(state, "lastOnlineDay", member.LastOnlineDay);
        SetOptionalInteger(state, "lastOnlineHour", member.LastOnlineHour);
        SetOptionalString(state, "guildRank", member.GuildRank);
        SetOptionalInteger(state, "guildRankOrder", member.GuildRankOrder);
        SetOptionalBoolean(state, "isRemoteChat", member.IsRemoteChat);
        SetOptionalInteger(
            state,
            "overallDungeonScore",
            member.OverallDungeonScore);
        SetOptionalInteger(state, "faction", member.Faction);
        SetOptionalInteger(
            state,
            "timerunningSeasonID",
            member.TimerunningSeasonId);
    }

    private static void PushInvitationCandidate(
        lua_State state,
        WowClubInvitationCandidateState candidate)
    {
        lua_createtable(state, 0, 4);
        SetDatabaseId(state, "memberId", candidate.MemberId);
        SetString(state, "name", candidate.Name);
        SetInteger(state, "priority", candidate.Priority);
        SetNumber(state, "status", candidate.Status);
    }

    private static void PushSelfInvitation(
        lua_State state,
        WowClubSelfInvitationState invitation)
    {
        lua_createtable(state, 0, 4);
        SetDatabaseId(state, "invitationId", invitation.InvitationId);
        PushClubInfo(state, invitation.Club);
        lua_setfield(state, -2, "club");
        PushMemberInfo(state, invitation.Inviter);
        lua_setfield(state, -2, "inviter");
        lua_createtable(state, invitation.Leaders.Count, 0);
        for (var index = 0; index < invitation.Leaders.Count; index++)
        {
            PushMemberInfo(state, invitation.Leaders[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "leaders");
    }

    private static void PushTicket(lua_State state, WowClubTicketState ticket)
    {
        lua_createtable(state, 0, 7);
        SetString(state, "ticketId", ticket.TicketId);
        SetInteger(state, "allowedRedeemCount", ticket.AllowedRedeemCount);
        SetInteger(state, "currentRedeemCount", ticket.CurrentRedeemCount);
        SetNumber(state, "creationTime", ticket.CreationTime);
        SetNumber(state, "expirationTime", ticket.ExpirationTime);
        SetOptionalDatabaseId(
            state,
            "defaultStreamId",
            ticket.DefaultStreamId);
        PushMemberInfo(state, ticket.Creator);
        lua_setfield(state, -2, "creator");
    }

    private static void PushInt32Array(
        lua_State state,
        IList<int> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushDatabaseId(lua_State state, ulong value)
    {
        if (value <= MaximumExactLuaInteger)
        {
            lua_pushnumber(state, value);
        }
        else
        {
            lua_pushstring(state, $"0x{value:X16}");
        }
    }

    private static void SetDatabaseId(
        lua_State state,
        string key,
        ulong value)
    {
        PushDatabaseId(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetOptionalDatabaseId(
        lua_State state,
        string key,
        ulong? value)
    {
        if (value is null)
        {
            lua_pushnil(state);
        }
        else
        {
            PushDatabaseId(state, value.Value);
        }
        lua_setfield(state, -2, key);
    }

    private static void SetString(lua_State state, string key, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetOptionalString(
        lua_State state,
        string key,
        string? value)
    {
        if (value is null)
        {
            lua_pushnil(state);
        }
        else
        {
            lua_pushstring(state, value);
        }
        lua_setfield(state, -2, key);
    }

    private static void SetInteger(lua_State state, string key, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string key,
        int? value)
    {
        if (value is null)
        {
            lua_pushnil(state);
        }
        else
        {
            lua_pushinteger(state, value.Value);
        }
        lua_setfield(state, -2, key);
    }

    private static void SetNumber(lua_State state, string key, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetNumber(lua_State state, string key, uint value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, key);
    }

    private static void SetOptionalNumber(
        lua_State state,
        string key,
        long? value)
    {
        if (value is null)
        {
            lua_pushnil(state);
        }
        else
        {
            lua_pushnumber(state, value.Value);
        }
        lua_setfield(state, -2, key);
    }

    private static void SetBoolean(lua_State state, string key, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, key);
    }

    private static void SetOptionalBoolean(
        lua_State state,
        string key,
        bool? value)
    {
        if (value is null)
        {
            lua_pushnil(state);
        }
        else
        {
            lua_pushboolean(state, value.Value ? 1 : 0);
        }
        lua_setfield(state, -2, key);
    }

    private static void PushBoolean(lua_State state, bool value) =>
        lua_pushboolean(state, value ? 1 : 0);
}
