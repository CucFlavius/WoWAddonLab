using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowClubFinderApi : LuaApiModule
{
    private const ulong MaximumExactLuaInteger = 0x1FFFFFFFFFFFFF;
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "ApplicantAcceptClubInvite",
        "ApplicantDeclineClubInvite",
        "CancelMembershipRequest",
        "CheckAllPlayerApplicantSettings",
        "ClearAllFinderCache",
        "ClearClubApplicantsCache",
        "ClearClubFinderPostingsCache",
        "DoesPlayerBelongToClubFromClubGUID",
        "GetClubFinderDisableReason",
        "GetClubRecruitmentSettings",
        "GetClubTypeFromFinderGUID",
        "GetFocusIndexFromFlag",
        "GetPlayerApplicantLocaleFlags",
        "GetPlayerApplicantSettings",
        "GetPlayerClubApplicationStatus",
        "GetPlayerSettingsFocusFlagsSelectedCount",
        "GetPostingIDFromClubFinderGUID",
        "GetRecruitingClubInfoFromClubID",
        "GetRecruitingClubInfoFromFinderGUID",
        "GetStatusOfPostingFromClubId",
        "GetTotalMatchingCommunityListSize",
        "GetTotalMatchingGuildListSize",
        "HasAlreadyAppliedToLinkedPosting",
        "HasPostingBeenDelisted",
        "IsCommunityFinderEnabled",
        "IsEnabled",
        "IsListingEnabledFromFlags",
        "IsPostingBanned",
        "IsValidSearchString",
        "LookupClubPostingFromClubFinderGUID",
        "PlayerGetClubInvitationList",
        "PlayerRequestPendingClubsList",
        "PlayerReturnPendingCommunitiesList",
        "PlayerReturnPendingGuildsList",
        "PostClub",
        "RequestApplicantList",
        "RequestClubsList",
        "RequestMembershipToClub",
        "RequestNextCommunityPage",
        "RequestNextGuildPage",
        "RequestPostingInformationFromClubId",
        "RequestSubscribedClubPostingIDs",
        "ResetClubPostingMapCache",
        "RespondToApplicant",
        "ReturnClubApplicantList",
        "ReturnMatchingCommunityList",
        "ReturnMatchingGuildList",
        "ReturnPendingClubApplicantList",
        "SendChatWhisper",
        "SetAllRecruitmentSettings",
        "SetPlayerApplicantLocaleFlags",
        "SetPlayerApplicantSettings",
        "SetRecruitmentLocale",
        "SetRecruitmentSettings",
        "ShouldShowClubFinder"
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
        lua_setglobal(state, "C_ClubFinder");
    }

    private static int Dispatch(lua_State state)
    {
        var finder = LuaBindings.GetRuntime(state).ClubFinder;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "ApplicantAcceptClubInvite":
                finder.AcceptedInvitationGuids.Add(RequiredGuid(
                    state,
                    1,
                    Usage(operation, "clubFinderGUID")));
                return 0;
            case "ApplicantDeclineClubInvite":
                finder.DeclinedInvitationGuids.Add(RequiredGuid(
                    state,
                    1,
                    Usage(operation, "clubFinderGUID")));
                return 0;
            case "CancelMembershipRequest":
                finder.CancelledMembershipGuids.Add(RequiredGuid(
                    state,
                    1,
                    Usage(operation, "clubFinderGUID")));
                return 0;
            case "CheckAllPlayerApplicantSettings":
                CheckAllPlayerApplicantSettings(finder);
                return 0;
            case "ClearAllFinderCache":
                finder.ClearAllFinderCacheRequests++;
                finder.ClearClubApplicantsCacheRequests++;
                finder.ClearClubFinderPostingsCacheRequests++;
                return 0;
            case "ClearClubApplicantsCache":
                finder.ClearClubApplicantsCacheRequests++;
                return 0;
            case "ClearClubFinderPostingsCache":
                finder.ClearClubFinderPostingsCacheRequests++;
                return 0;
            case "DoesPlayerBelongToClubFromClubGUID":
            {
                var guid = RequiredGuid(state, 1, Usage(operation, "clubFinderGUID"));
                PushBoolean(
                    state,
                    finder.PlayerBelongsToClubByFinderGuid.TryGetValue(
                        guid,
                        out var belongs) &&
                    belongs);
                return 1;
            }
            case "GetClubFinderDisableReason":
                PushOptionalInteger(state, finder.DisableReason);
                return 1;
            case "GetClubRecruitmentSettings":
                PushSettings(state, finder.ClubRecruitmentSettings);
                return 1;
            case "GetClubTypeFromFinderGUID":
            {
                var guid = RequiredGuid(state, 1, Usage(operation, "clubFinderGUID"));
                if (!finder.ClubTypeByFinderGuid.TryGetValue(guid, out var clubType))
                    return 0;
                lua_pushinteger(state, clubType);
                return 1;
            }
            case "GetFocusIndexFromFlag":
            {
                var flags = RequiredUInt32(state, 1, Usage(operation, "flags"));
                lua_pushinteger(state, GetFocusIndex(flags));
                return 1;
            }
            case "GetPlayerApplicantLocaleFlags":
                lua_pushnumber(state, finder.PlayerApplicantLocaleFlags);
                return 1;
            case "GetPlayerApplicantSettings":
                PushSettings(state, finder.PlayerApplicantSettings);
                return 1;
            case "GetPlayerClubApplicationStatus":
            {
                var guid = RequiredGuid(state, 1, Usage(operation, "clubFinderGUID"));
                finder.ApplicationStatusByFinderGuid.TryGetValue(guid, out var status);
                lua_pushinteger(state, status);
                return 1;
            }
            case "GetPlayerSettingsFocusFlagsSelectedCount":
                lua_pushnumber(state, CountSelectedFocuses(finder.PlayerApplicantSettings));
                return 1;
            case "GetPostingIDFromClubFinderGUID":
            {
                var guid = RequiredGuid(state, 1, Usage(operation, "clubFinderGUID"));
                if (finder.PostingIdByFinderGuid.TryGetValue(guid, out var postingId))
                    lua_pushinteger(state, postingId);
                else
                    lua_pushnil(state);
                return 1;
            }
            case "GetRecruitingClubInfoFromClubID":
            {
                var clubId = RequiredUInt53(state, 1, Usage(operation, "clubId"));
                if (finder.RecruitingClubInfoByClubId.TryGetValue(clubId, out var info))
                    PushRecruitingClubInfo(state, info);
                else
                    lua_pushnil(state);
                return 1;
            }
            case "GetRecruitingClubInfoFromFinderGUID":
            {
                var guid = RequiredGuid(state, 1, Usage(operation, "clubFinderGUID"));
                if (!finder.RecruitingClubInfoByFinderGuid.TryGetValue(guid, out var info))
                    info = new WowRecruitingClubInfoState();
                PushRecruitingClubInfo(state, info);
                return 1;
            }
            case "GetStatusOfPostingFromClubId":
            {
                var postingId = RequiredUInt53(state, 1, Usage(operation, "postingID"));
                finder.PostingStatusFlagsById.TryGetValue(postingId, out var flags);
                PushIntArray(state, flags ?? []);
                return 1;
            }
            case "GetTotalMatchingCommunityListSize":
                lua_pushnumber(state, finder.TotalMatchingCommunityListSize);
                return 1;
            case "GetTotalMatchingGuildListSize":
                lua_pushnumber(state, finder.TotalMatchingGuildListSize);
                return 1;
            case "HasAlreadyAppliedToLinkedPosting":
            {
                var guid = RequiredGuid(state, 1, Usage(operation, "clubFinderGUID"));
                PushBoolean(state, finder.AppliedFinderGuids.Contains(guid));
                return 1;
            }
            case "HasPostingBeenDelisted":
            {
                var postingId = RequiredUInt53(state, 1, Usage(operation, "postingID"));
                PushBoolean(state, finder.DelistedPostingIds.Contains(postingId));
                return 1;
            }
            case "IsCommunityFinderEnabled":
                PushBoolean(state, finder.CommunityFinderEnabled);
                return 1;
            case "IsEnabled":
                PushBoolean(state, finder.Enabled);
                return 1;
            case "IsListingEnabledFromFlags":
            {
                var flags = RequiredUInt32(state, 1, Usage(operation, "flags"));
                PushBoolean(state, (flags & (1u << 12)) != 0);
                return 1;
            }
            case "IsPostingBanned":
            {
                var postingId = RequiredUInt53(state, 1, Usage(operation, "postingID"));
                PushBoolean(state, finder.BannedPostingIds.Contains(postingId));
                return 1;
            }
            case "IsValidSearchString":
            {
                var value = RequiredString(state, 1, Usage(operation, "name"));
                PushBoolean(state, !finder.InvalidSearchStrings.Contains(value));
                return 1;
            }
            case "LookupClubPostingFromClubFinderGUID":
                finder.LookupRequests.Add(new WowClubFinderLookupRequest(
                    RequiredGuid(state, 1, Usage(operation, "clubFinderGUID, isLinkedPosting")),
                    RequiredBoolean(state, 2, Usage(operation, "clubFinderGUID, isLinkedPosting"))));
                return 0;
            case "PlayerGetClubInvitationList":
                if (!finder.InvitationListAvailable)
                    return 0;
                PushRecruitingClubInfoArray(state, finder.ClubInvitations);
                return 1;
            case "PlayerRequestPendingClubsList":
                finder.PendingClubListRequestTypes.Add(
                    RequiredClubType(state, 1, Usage(operation, "type")));
                return 0;
            case "PlayerReturnPendingCommunitiesList":
                PushRecruitingClubInfoArray(state, finder.PendingCommunities);
                return 1;
            case "PlayerReturnPendingGuildsList":
                PushRecruitingClubInfoArray(state, finder.PendingGuilds);
                return 1;
            case "PostClub":
                return PostClub(state, finder);
            case "RequestApplicantList":
                finder.ApplicantListRequestTypes.Add(
                    RequiredClubType(state, 1, Usage(operation, "type")));
                return 0;
            case "RequestClubsList":
                finder.ClubsListRequests.Add(new WowClubFinderClubsListRequest(
                    RequiredBoolean(
                        state,
                        1,
                        Usage(operation, "guildListRequested, searchString, specIDs")),
                    RequiredString(
                        state,
                        2,
                        Usage(operation, "guildListRequested, searchString, specIDs")),
                    RequiredIntArray(
                        state,
                        3,
                        Usage(operation, "guildListRequested, searchString, specIDs"))));
                return 0;
            case "RequestMembershipToClub":
                finder.MembershipRequests.Add(new WowClubFinderMembershipRequest(
                    RequiredGuid(
                        state,
                        1,
                        Usage(operation, "clubFinderGUID, comment, specIDs")),
                    RequiredString(
                        state,
                        2,
                        Usage(operation, "clubFinderGUID, comment, specIDs")),
                    RequiredIntArray(
                        state,
                        3,
                        Usage(operation, "clubFinderGUID, comment, specIDs"))));
                return 0;
            case "RequestNextCommunityPage":
                finder.CommunityPageRequests.Add(ReadPageRequest(state, operation));
                return 0;
            case "RequestNextGuildPage":
                finder.GuildPageRequests.Add(ReadPageRequest(state, operation));
                return 0;
            case "RequestPostingInformationFromClubId":
            {
                var clubId = RequiredUInt53(state, 1, Usage(operation, "clubId"));
                finder.PostingInformationRequests.Add(clubId);
                PushBoolean(
                    state,
                    finder.Enabled &&
                    finder.PostingInformationAvailableForClubIds.Contains(clubId));
                return 1;
            }
            case "RequestSubscribedClubPostingIDs":
                finder.RequestSubscribedClubPostingIdsRequests++;
                return 0;
            case "ResetClubPostingMapCache":
                finder.ResetClubPostingMapCacheRequests++;
                finder.RequestSubscribedClubPostingIdsRequests++;
                return 0;
            case "RespondToApplicant":
                finder.ApplicantResponses.Add(ReadApplicantResponse(state, operation));
                return 0;
            case "ReturnClubApplicantList":
            {
                var clubId = RequiredUInt53(state, 1, Usage(operation, "clubId"));
                finder.ClubApplicantsByClubId.TryGetValue(clubId, out var applicants);
                PushApplicantArray(state, applicants ?? []);
                return 1;
            }
            case "ReturnMatchingCommunityList":
                PushRecruitingClubInfoArray(state, finder.MatchingCommunities);
                return 1;
            case "ReturnMatchingGuildList":
                PushRecruitingClubInfoArray(state, finder.MatchingGuilds);
                return 1;
            case "ReturnPendingClubApplicantList":
            {
                var clubId = RequiredUInt53(state, 1, Usage(operation, "clubId"));
                finder.PendingClubApplicantsByClubId.TryGetValue(clubId, out var applicants);
                PushApplicantArray(state, applicants ?? []);
                return 1;
            }
            case "SendChatWhisper":
                finder.WhisperRequests.Add(new WowClubFinderWhisperRequest(
                    RequiredGuid(
                        state,
                        1,
                        Usage(operation, "clubFinderGUID, playerGUID, applicantType, name")),
                    RequiredGuid(
                        state,
                        2,
                        Usage(operation, "clubFinderGUID, playerGUID, applicantType, name")),
                    RequiredClubType(
                        state,
                        3,
                        Usage(operation, "clubFinderGUID, playerGUID, applicantType, name")),
                    RequiredString(
                        state,
                        4,
                        Usage(operation, "clubFinderGUID, playerGUID, applicantType, name"))));
                return 0;
            case "SetAllRecruitmentSettings":
                ApplySettingsFlags(
                    finder.ClubRecruitmentSettings,
                    RequiredUInt32(state, 1, Usage(operation, "value")));
                return 0;
            case "SetPlayerApplicantLocaleFlags":
                finder.PlayerApplicantLocaleFlags =
                    RequiredUInt32(state, 1, Usage(operation, "localeFlags"));
                return 0;
            case "SetPlayerApplicantSettings":
                SetPlayerApplicantSetting(
                    finder.PlayerApplicantSettings,
                    RequiredUInt32(state, 1, Usage(operation, "index, checked")),
                    RequiredBoolean(state, 2, Usage(operation, "index, checked")));
                return 0;
            case "SetRecruitmentLocale":
                finder.RecruitmentLocale =
                    RequiredUInt32(state, 1, Usage(operation, "locale"));
                return 0;
            case "SetRecruitmentSettings":
                SetRecruitmentSetting(
                    finder.ClubRecruitmentSettings,
                    RequiredUInt32(state, 1, Usage(operation, "index, checked")),
                    RequiredBoolean(state, 2, Usage(operation, "index, checked")));
                return 0;
            case "ShouldShowClubFinder":
                PushBoolean(state, finder.ShouldShow);
                return 1;
            default:
                return 0;
        }
    }

    private static int PostClub(lua_State state, WowClubFinderState finder)
    {
        const string arguments =
            "clubId, itemLevelRequirement, name, description, avatarId, specs, type [, crossFaction]";
        finder.PostClubRequests.Add(new WowClubFinderPostClubRequest(
            RequiredUInt53(state, 1, Usage("PostClub", arguments)),
            RequiredInt32(state, 2, Usage("PostClub", arguments)),
            RequiredString(state, 3, Usage("PostClub", arguments)),
            RequiredString(state, 4, Usage("PostClub", arguments)),
            RequiredUInt32(state, 5, Usage("PostClub", arguments)),
            RequiredIntArray(state, 6, Usage("PostClub", arguments)),
            RequiredClubType(state, 7, Usage("PostClub", arguments)),
            OptionalBoolean(state, 8, Usage("PostClub", arguments)) ?? false));
        PushBoolean(state, finder.PostClubSucceeds);
        return 1;
    }

    private static WowClubFinderPageRequest ReadPageRequest(
        lua_State state,
        string operation)
    {
        var usage = Usage(operation, "startingIndex, pageSize");
        return new WowClubFinderPageRequest(
            RequiredInt32(state, 1, usage),
            RequiredInt32(state, 2, usage));
    }

    private static WowClubFinderApplicantResponse ReadApplicantResponse(
        lua_State state,
        string operation)
    {
        const string arguments =
            "clubFinderGUID, playerGUID, shouldAccept, requestType, playerName, forceAccept [, reported]";
        var usage = Usage(operation, arguments);
        return new WowClubFinderApplicantResponse(
            RequiredGuid(state, 1, usage),
            RequiredGuid(state, 2, usage),
            RequiredBoolean(state, 3, usage),
            RequiredClubType(state, 4, usage),
            RequiredString(state, 5, usage),
            RequiredBoolean(state, 6, usage),
            OptionalBoolean(state, 7, usage));
    }

    private static void CheckAllPlayerApplicantSettings(WowClubFinderState finder)
    {
        var settings = finder.PlayerApplicantSettings;
        settings.PlayStyleDungeon = true;
        settings.PlayStyleRaids = true;
        settings.PlayStylePvp = true;
        settings.PlayStyleRp = true;
        settings.PlayStyleSocial = true;
        settings.SizeSmall = true;
        settings.SizeMedium = true;
        settings.SizeLarge = true;
        settings.RoleTank = true;
        settings.RoleHealer = true;
        settings.RoleDps = true;
        finder.PlayerApplicantLocaleFlags = uint.MaxValue;
    }

    private static int GetFocusIndex(uint flags)
    {
        for (var index = 1; index <= 5; index++)
        {
            if ((flags & (1u << index)) != 0)
                return index;
        }
        return 0;
    }

    private static int CountSelectedFocuses(WowClubFinderSettingsState settings) =>
        (settings.PlayStyleDungeon ? 1 : 0) +
        (settings.PlayStyleRaids ? 1 : 0) +
        (settings.PlayStylePvp ? 1 : 0) +
        (settings.PlayStyleRp ? 1 : 0) +
        (settings.PlayStyleSocial ? 1 : 0);

    private static void ApplySettingsFlags(
        WowClubFinderSettingsState settings,
        uint flags)
    {
        settings.PlayStyleDungeon = HasFlag(flags, 1);
        settings.PlayStyleRaids = HasFlag(flags, 2);
        settings.PlayStylePvp = HasFlag(flags, 3);
        settings.PlayStyleRp = HasFlag(flags, 4);
        settings.PlayStyleSocial = HasFlag(flags, 5);
        settings.SizeSmall = HasFlag(flags, 6);
        settings.SizeMedium = HasFlag(flags, 7);
        settings.SizeLarge = HasFlag(flags, 8);
        settings.RoleTank = HasFlag(flags, 9);
        settings.RoleHealer = HasFlag(flags, 10);
        settings.RoleDps = HasFlag(flags, 11);
        settings.EnableListing = HasFlag(flags, 12);
        settings.MaxLevelOnly = HasFlag(flags, 13);
        settings.AutoAccept = HasFlag(flags, 14);
        settings.CrossFaction = HasFlag(flags, 17);
        settings.SortRelevance = HasFlag(flags, 18);
        settings.SortMembers = HasFlag(flags, 19);
        settings.SortNewest = HasFlag(flags, 20);
    }

    private static void SetPlayerApplicantSetting(
        WowClubFinderSettingsState settings,
        uint index,
        bool value)
    {
        if (index is >= 6 and <= 8)
        {
            settings.SizeSmall = false;
            settings.SizeMedium = false;
            settings.SizeLarge = false;
        }
        else if (index is >= 18 and <= 20)
        {
            settings.SortRelevance = false;
            settings.SortMembers = false;
            settings.SortNewest = false;
        }
        SetSetting(settings, index, value);
    }

    private static void SetRecruitmentSetting(
        WowClubFinderSettingsState settings,
        uint index,
        bool value)
    {
        if (index is >= 1 and <= 5)
        {
            settings.PlayStyleDungeon = false;
            settings.PlayStyleRaids = false;
            settings.PlayStylePvp = false;
            settings.PlayStyleRp = false;
            settings.PlayStyleSocial = false;
        }
        SetSetting(settings, index, value);
        if (index is >= 1 and <= 5 &&
            !settings.PlayStyleDungeon &&
            !settings.PlayStyleRaids &&
            !settings.PlayStylePvp &&
            !settings.PlayStyleRp &&
            !settings.PlayStyleSocial)
        {
            settings.PlayStyleSocial = true;
        }
    }

    private static void SetSetting(
        WowClubFinderSettingsState settings,
        uint index,
        bool value)
    {
        switch (index)
        {
            case 1: settings.PlayStyleDungeon = value; break;
            case 2: settings.PlayStyleRaids = value; break;
            case 3: settings.PlayStylePvp = value; break;
            case 4: settings.PlayStyleRp = value; break;
            case 5: settings.PlayStyleSocial = value; break;
            case 6: settings.SizeSmall = value; break;
            case 7: settings.SizeMedium = value; break;
            case 8: settings.SizeLarge = value; break;
            case 9: settings.RoleTank = value; break;
            case 10: settings.RoleHealer = value; break;
            case 11: settings.RoleDps = value; break;
            case 12: settings.EnableListing = value; break;
            case 13: settings.MaxLevelOnly = value; break;
            case 14: settings.AutoAccept = value; break;
            case 17: settings.CrossFaction = value; break;
            case 18: settings.SortRelevance = value; break;
            case 19: settings.SortMembers = value; break;
            case 20: settings.SortNewest = value; break;
        }
    }

    private static void PushSettings(
        lua_State state,
        WowClubFinderSettingsState settings)
    {
        lua_createtable(state, 0, 18);
        SetBoolean(state, "playStyleDungeon", settings.PlayStyleDungeon);
        SetBoolean(state, "playStyleRaids", settings.PlayStyleRaids);
        SetBoolean(state, "playStylePvp", settings.PlayStylePvp);
        SetBoolean(state, "playStyleRP", settings.PlayStyleRp);
        SetBoolean(state, "playStyleSocial", settings.PlayStyleSocial);
        SetBoolean(state, "roleTank", settings.RoleTank);
        SetBoolean(state, "roleHealer", settings.RoleHealer);
        SetBoolean(state, "roleDps", settings.RoleDps);
        SetBoolean(state, "sizeSmall", settings.SizeSmall);
        SetBoolean(state, "sizeMedium", settings.SizeMedium);
        SetBoolean(state, "sizeLarge", settings.SizeLarge);
        SetBoolean(state, "maxLevelOnly", settings.MaxLevelOnly);
        SetBoolean(state, "enableListing", settings.EnableListing);
        SetBoolean(state, "sortRelevance", settings.SortRelevance);
        SetBoolean(state, "sortMembers", settings.SortMembers);
        SetBoolean(state, "sortNewest", settings.SortNewest);
        SetBoolean(state, "autoAccept", settings.AutoAccept);
        SetBoolean(state, "crossFaction", settings.CrossFaction);
    }

    private static void PushRecruitingClubInfoArray(
        lua_State state,
        IEnumerable<WowRecruitingClubInfoState> values)
    {
        var entries = values as IList<WowRecruitingClubInfoState> ??
                      values.ToArray();
        lua_createtable(state, entries.Count, 0);
        for (var index = 0; index < entries.Count; index++)
        {
            PushRecruitingClubInfo(state, entries[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushRecruitingClubInfo(
        lua_State state,
        WowRecruitingClubInfoState info)
    {
        lua_createtable(state, 0, 20);
        SetString(state, "clubFinderGUID", info.ClubFinderGuid);
        SetInteger(state, "numActiveMembers", info.NumActiveMembers);
        SetString(state, "name", info.Name);
        SetString(state, "comment", info.Comment);
        SetString(state, "guildLeader", info.GuildLeader);
        SetBoolean(state, "isGuild", info.IsGuild);
        SetInteger(state, "emblemInfo", info.EmblemInfo);
        PushOptionalTabardInfo(state, info.TabardInfo);
        lua_setfield(state, -2, "tabardInfo");
        SetIntArray(state, "recruitingSpecIds", info.RecruitingSpecIds);
        SetInteger(state, "recruitmentFlags", info.RecruitmentFlags);
        SetBoolean(state, "localeSet", info.LocaleSet);
        SetInteger(state, "recruitmentLocale", info.RecruitmentLocale);
        SetInteger(state, "minILvl", info.MinItemLevel);
        SetInteger(state, "cached", info.Cached);
        SetInteger(state, "cacheRequested", info.CacheRequested);
        SetString(state, "lastPosterGUID", info.LastPosterGuid);
        SetDatabaseId(state, "clubId", info.ClubId);
        SetInteger(state, "lastUpdatedTime", info.LastUpdatedTime);
        SetBoolean(state, "isCrossFaction", info.IsCrossFaction);
        SetOptionalString(state, "realmName", info.RealmName);
    }

    private static void PushOptionalTabardInfo(
        lua_State state,
        WowClubFinderTabardInfoState? tabard)
    {
        if (tabard is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_createtable(state, 0, 5);
        PushColor(state, tabard.BackgroundColor);
        lua_setfield(state, -2, "backgroundColor");
        PushColor(state, tabard.BorderColor);
        lua_setfield(state, -2, "borderColor");
        PushColor(state, tabard.EmblemColor);
        lua_setfield(state, -2, "emblemColor");
        SetInteger(state, "emblemFileID", tabard.EmblemFileId);
        SetInteger(state, "emblemStyle", tabard.EmblemStyle);
    }

    private static void PushColor(
        lua_State state,
        WowClubFinderColorState color)
    {
        lua_getglobal(state, "CreateColor");
        if (lua_isfunction(state, -1) != 0)
        {
            lua_pushnumber(state, color.Red);
            lua_pushnumber(state, color.Green);
            lua_pushnumber(state, color.Blue);
            lua_pushnumber(state, color.Alpha);
            if (lua_pcall(state, 4, 1, 0) == 0 &&
                lua_type(state, -1) == LUA_TTABLE)
            {
                return;
            }
            lua_pop(state, 1);
        }
        else
        {
            lua_pop(state, 1);
        }

        lua_createtable(state, 0, 4);
        SetNumber(state, "r", color.Red);
        SetNumber(state, "g", color.Green);
        SetNumber(state, "b", color.Blue);
        SetNumber(state, "a", color.Alpha);
    }

    private static void PushApplicantArray(
        lua_State state,
        IEnumerable<WowClubFinderApplicantInfoState> values)
    {
        var entries = values as IList<WowClubFinderApplicantInfoState> ??
                      values.ToArray();
        lua_createtable(state, entries.Count, 0);
        for (var index = 0; index < entries.Count; index++)
        {
            PushApplicant(state, entries[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void PushApplicant(
        lua_State state,
        WowClubFinderApplicantInfoState applicant)
    {
        lua_createtable(state, 0, 13);
        SetString(state, "clubFinderGUID", applicant.ClubFinderGuid);
        SetString(state, "playerGUID", applicant.PlayerGuid);
        SetInteger(state, "closed", applicant.Closed);
        SetString(state, "name", applicant.Name);
        SetString(state, "message", applicant.Message);
        SetInteger(state, "level", applicant.Level);
        SetInteger(state, "classID", applicant.ClassId);
        SetInteger(state, "ilvl", applicant.ItemLevel);
        SetIntArray(state, "specIds", applicant.SpecIds);
        SetInteger(state, "requestStatus", applicant.RequestStatus);
        SetBoolean(state, "lookupSuccess", applicant.LookupSuccess);
        SetInteger(state, "lastUpdatedTime", applicant.LastUpdatedTime);
        SetInteger(state, "faction", applicant.Faction);
    }

    private static string RequiredGuid(
        lua_State state,
        int index,
        string usage) =>
        RequiredString(state, index, usage);

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

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);
        return (int)value;
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
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (uint)value;
    }

    private static ulong RequiredUInt53(
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
        if (!double.IsFinite(value) || value < 0 || value > MaximumExactLuaInteger)
        {
            luaL_error(state, usage);
            return 0;
        }
        return (ulong)value;
    }

    private static int RequiredClubType(
        lua_State state,
        int index,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value is < 0 or > 3)
            return luaL_error(state, usage);
        return value;
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TBOOLEAN)
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static bool? OptionalBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return null;
        return RequiredBoolean(state, index, usage);
    }

    private static IReadOnlyList<int> RequiredIntArray(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TTABLE)
        {
            luaL_error(state, usage);
            return [];
        }
        var count = (int)lua_objlen(state, index);
        var result = new List<int>(count);
        for (var item = 1; item <= count; item++)
        {
            lua_rawgeti(state, index, item);
            result.Add(RequiredInt32(state, -1, usage));
            lua_pop(state, 1);
        }
        return result;
    }

    private static string Usage(string operation, string arguments) =>
        $"Usage: C_ClubFinder.{operation}({arguments})";

    private static bool HasFlag(uint flags, int index) =>
        (flags & (1u << index)) != 0;

    private static void PushBoolean(lua_State state, bool value) =>
        lua_pushboolean(state, value ? 1 : 0);

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value.HasValue)
            lua_pushinteger(state, value.Value);
        else
            lua_pushnil(state);
    }

    private static void PushIntArray(
        lua_State state,
        IEnumerable<int> values)
    {
        var entries = values as IList<int> ?? values.ToArray();
        lua_createtable(state, entries.Count, 0);
        for (var index = 0; index < entries.Count; index++)
        {
            lua_pushinteger(state, entries[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void SetBoolean(lua_State state, string field, bool value)
    {
        PushBoolean(state, value);
        lua_setfield(state, -2, field);
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

    private static void SetIntArray(
        lua_State state,
        string field,
        IEnumerable<int> values)
    {
        PushIntArray(state, values);
        lua_setfield(state, -2, field);
    }

    private static void SetDatabaseId(
        lua_State state,
        string field,
        ulong value)
    {
        if (value <= MaximumExactLuaInteger)
        {
            lua_pushnumber(state, value);
        }
        else
        {
            lua_pushstring(
                state,
                "0x" + value.ToString("X16", CultureInfo.InvariantCulture));
        }
        lua_setfield(state, -2, field);
    }
}
