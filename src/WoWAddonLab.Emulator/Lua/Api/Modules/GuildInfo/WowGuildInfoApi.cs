using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowGuildInfoApi : LuaApiModule
{
    private const int GuildRankFlagCount = 22;
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AreGuildEventsEnabled",
        "CanEditOfficerNote",
        "CanSpeakInGuildChat",
        "CanViewOfficerNote",
        "Demote",
        "Disband",
        "GetGuildNewsInfo",
        "GetGuildRankOrder",
        "GetGuildTabardInfo",
        "GetInfoText",
        "GetMOTD",
        "GuildControlGetRankFlags",
        "GuildRoster",
        "Invite",
        "IsEncounterGuildNewsEnabled",
        "IsGuildOfficer",
        "IsGuildRankAssignmentAllowed",
        "IsGuildReputationEnabled",
        "Leave",
        "MemberExistsByName",
        "Promote",
        "QueryGuildMemberRecipes",
        "QueryGuildMembersForRecipe",
        "RemoveFromGuild",
        "RequestGuildRename",
        "RequestGuildRenameRefund",
        "RequestRenameNameCheck",
        "RequestRenameStatus",
        "SetGuildRankOrder",
        "SetInfoText",
        "SetLeader",
        "SetMOTD",
        "SetNote",
        "Uninvite"
    ];

    public override void Register(lua_State state)
    {
        RegisterLegacyGlobals(state);
        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_GuildInfo");
    }

    private static void RegisterLegacyGlobals(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "IsInGuild", Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GuildControlGetNumRanks",
            Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetNumGuildBankTabs",
            Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetCurrentGuildBankTab",
            Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GuildControlSetRank",
            Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetGuildFactionGroup",
            Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetGuildLogoInfo", Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "RequestGuildChallengeInfo",
            Callback);
        LuaBindings.RegisterClosureGlobal(state, "IsGuildLeader", Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetGuildTabardFiles",
            Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetGuildRenameRequired",
            Callback);
        LuaBindings.RegisterClosureGlobal(state, "CanGuildInvite", Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetNumGuildPerks",
            Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "RequestGuildRewards",
            Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "QueryGuildEventLog",
            Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "GetGuildNewsFilters",
            Callback);
        LuaBindings.RegisterClosureGlobal(
            state,
            "SetGuildNewsFilter",
            Callback);
        LuaBindings.RegisterClosureGlobal(state, "DeclineGuild", Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var guild = LuaBindings.GetRuntime(state).Guild;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        return operation switch
        {
            "AreGuildEventsEnabled" =>
                PushBoolean(state, guild.AreGuildEventsEnabled),
            "CanEditOfficerNote" =>
                PushBoolean(state, guild.CanEditOfficerNote),
            "CanSpeakInGuildChat" =>
                PushBoolean(state, guild.CanSpeakInGuildChat),
            "CanViewOfficerNote" =>
                PushBoolean(state, guild.CanViewOfficerNote),
            "Demote" => RecordRequiredString(
                state,
                guild,
                operation,
                "Usage: C_GuildInfo.Demote(name)"),
            "Disband" => Record(guild, operation),
            "GetGuildNewsInfo" => GetGuildNewsInfo(state, guild),
            "GetGuildLogoInfo" => GetGuildLogoInfo(state, guild),
            "GetGuildRankOrder" => GetGuildRankOrder(state, guild),
            "GetGuildTabardInfo" => GetGuildTabardInfo(state, guild),
            "GetInfoText" => PushString(state, guild.InfoText),
            "GetMOTD" => PushString(state, guild.Motd),
            "GuildControlGetRankFlags" =>
                GuildControlGetRankFlags(state, guild),
            "GuildRoster" => Record(guild, operation),
            "Invite" => RecordRequiredString(
                state,
                guild,
                operation,
                "Usage: C_GuildInfo.Invite(name)"),
            "IsEncounterGuildNewsEnabled" =>
                PushBoolean(state, guild.IsEncounterGuildNewsEnabled),
            "IsGuildOfficer" => PushBoolean(state, guild.IsGuildOfficer),
            "IsGuildRankAssignmentAllowed" =>
                IsGuildRankAssignmentAllowed(state, guild),
            "IsGuildReputationEnabled" => PushBoolean(state, true),
            "Leave" => Record(guild, operation),
            "MemberExistsByName" => MemberExistsByName(state, guild),
            "Promote" => RecordRequiredString(
                state,
                guild,
                operation,
                "Usage: C_GuildInfo.Promote(name)"),
            "QueryGuildMemberRecipes" =>
                QueryGuildMemberRecipes(state, guild),
            "QueryGuildMembersForRecipe" =>
                QueryGuildMembersForRecipe(state, guild),
            "RemoveFromGuild" => RemoveFromGuild(state, guild),
            "RequestGuildRename" => RecordRequiredString(
                state,
                guild,
                operation,
                "Usage: C_GuildInfo.RequestGuildRename(desiredName)"),
            "RequestGuildRenameRefund" => Record(guild, operation),
            "RequestRenameNameCheck" => RecordRequiredString(
                state,
                guild,
                operation,
                "Usage: C_GuildInfo.RequestRenameNameCheck(desiredName)"),
            "RequestRenameStatus" => RequestRenameStatus(state, guild),
            "SetGuildRankOrder" => SetGuildRankOrder(state, guild),
            "SetInfoText" => SetInfoText(state, guild),
            "SetLeader" => RecordRequiredString(
                state,
                guild,
                operation,
                "Usage: C_GuildInfo.SetLeader(name)"),
            "SetMOTD" => SetMotd(state, guild),
            "SetNote" => SetNote(state, guild),
            "Uninvite" => RecordRequiredString(
                state,
                guild,
                operation,
                "Usage: C_GuildInfo.Uninvite(name)"),
            _ => DispatchLegacy(state, guild, operation)
        };
    }

    private static int GetGuildNewsInfo(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage =
            "Usage: local newsInfo = C_GuildInfo.GetGuildNewsInfo(index)";
        var index = RequiredOneBasedIndex(state, 1, usage);
        if (index < 1 || index > guild.News.Count)
            return 0;

        var news = guild.News[index - 1];
        lua_createtable(state, 0, 12);
        SetBoolean(state, "isSticky", news.IsSticky);
        SetBoolean(state, "isHeader", news.IsHeader);
        SetInteger(state, "newsType", news.NewsType);
        SetOptionalString(state, "whoText", news.WhoText);
        SetOptionalString(state, "whatText", news.WhatText);
        SetInteger(state, "newsDataID", news.NewsDataId);
        SetIntArray(state, "data", news.Data);
        SetInteger(state, "weekday", news.Weekday);
        SetInteger(state, "day", news.Day);
        SetInteger(state, "month", news.Month);
        SetInteger(state, "year", news.Year);
        SetInteger(
            state,
            "guildMembersPresent",
            news.GuildMembersPresent);
        return 1;
    }

    private static int GetGuildLogoInfo(
        lua_State state,
        WowGuildInfoState guild)
    {
        WowClubFinderTabardInfoState? tabard;
        if (lua_isstring(state, 1) != 0)
        {
            var unit = lua_tostring(state, 1) ?? string.Empty;
            if (!guild.TabardInfoByUnit.TryGetValue(unit, out tabard) &&
                (!unit.Equals("player", StringComparison.OrdinalIgnoreCase) ||
                 (tabard = guild.DefaultTabardInfo) is null))
                return 0;
        }
        else if (!guild.TabardInfoByUnit.TryGetValue("player", out tabard))
        {
            tabard = guild.DefaultTabardInfo;
        }

        if (tabard is null || tabard.EmblemFileId == 0)
            return 0;

        PushLegacyColor(state, tabard.BackgroundColor);
        PushLegacyColor(state, tabard.BorderColor);
        PushLegacyColor(state, tabard.EmblemColor);
        lua_pushnumber(state, tabard.EmblemFileId);
        lua_pushnumber(state, tabard.EmblemStyle);
        return 11;
    }

    private static void PushLegacyColor(
        lua_State state,
        WowClubFinderColorState color)
    {
        lua_pushnumber(state, ToByteChannel(color.Red));
        lua_pushnumber(state, ToByteChannel(color.Green));
        lua_pushnumber(state, ToByteChannel(color.Blue));
    }

    private static int ToByteChannel(double normalized)
    {
        return Math.Clamp(
            (int)Math.Round(
                normalized * 255d,
                MidpointRounding.AwayFromZero),
            byte.MinValue,
            byte.MaxValue);
    }

    private static int GetGuildRankOrder(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage =
            "Usage: local rankOrder = C_GuildInfo.GetGuildRankOrder(guid)";
        var guid = RequiredGuid(state, 1, usage);
        lua_pushinteger(
            state,
            guild.RankOrderByGuid.TryGetValue(guid, out var rankOrder)
                ? rankOrder
                : 1);
        return 1;
    }

    private static int GetGuildTabardInfo(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage =
            "Usage: local tabardInfo = C_GuildInfo.GetGuildTabardInfo([unit])";
        var unit = OptionalString(state, 1, usage);
        WowClubFinderTabardInfoState? tabard;
        if (unit is not null &&
            guild.TabardInfoByUnit.TryGetValue(unit, out var unitTabard))
        {
            tabard = unitTabard;
        }
        else
        {
            tabard = guild.DefaultTabardInfo;
        }

        PushOptionalTabardInfo(state, tabard);
        return 1;
    }

    private static int GuildControlGetRankFlags(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage =
            "Usage: local permissions = C_GuildInfo.GuildControlGetRankFlags(rankOrder)";
        var rankOrder = RequiredOneBasedIndex(state, 1, usage);
        guild.RankFlagsByOrder.TryGetValue(rankOrder, out var flags);
        lua_createtable(state, GuildRankFlagCount, 0);
        for (var index = 0; index < GuildRankFlagCount; index++)
        {
            var enabled = flags is not null &&
                index < flags.Count &&
                flags[index];
            lua_pushboolean(state, enabled ? 1 : 0);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static int IsGuildRankAssignmentAllowed(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage =
            "Usage: local isGuildRankAssignmentAllowed = C_GuildInfo.IsGuildRankAssignmentAllowed(guid, rankOrder)";
        var guid = RequiredGuid(state, 1, usage);
        var rankOrder = RequiredOneBasedIndex(state, 2, usage);
        var key = new WowGuildRankAssignmentKey(guid, rankOrder);
        return PushBoolean(
            state,
            guild.RankAssignmentAllowed.TryGetValue(key, out var allowed) &&
            allowed);
    }

    private static int MemberExistsByName(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage =
            "Usage: local exists = C_GuildInfo.MemberExistsByName(name)";
        return PushBoolean(
            state,
            guild.MemberNames.Contains(RequiredString(state, 1, usage)));
    }

    private static int QueryGuildMemberRecipes(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage =
            "Usage: C_GuildInfo.QueryGuildMemberRecipes(guildMemberGUID, skillLineID)";
        var guid = RequiredGuid(state, 1, usage);
        var skillLineId = RequiredInt32(state, 2, usage);
        guild.Requests.Add(
            new WowGuildInfoRequest(
                "QueryGuildMemberRecipes",
                [guid, skillLineId]));
        return 0;
    }

    private static int QueryGuildMembersForRecipe(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage =
            "Usage: local updatedRecipeSpellID = C_GuildInfo.QueryGuildMembersForRecipe(skillLineID, recipeSpellID [, recipeLevel])";
        var skillLineId = RequiredInt32(state, 1, usage);
        var recipeSpellId = RequiredInt32(state, 2, usage);
        var recipeLevel = OptionalOneBasedIndex(state, 3, usage);
        var key = new WowGuildRecipeQueryKey(
            skillLineId,
            recipeSpellId,
            recipeLevel);
        guild.Requests.Add(
            new WowGuildInfoRequest(
                "QueryGuildMembersForRecipe",
                [skillLineId, recipeSpellId, recipeLevel]));
        if (!guild.UpdatedRecipeSpellIds.TryGetValue(
                key,
                out var updatedRecipeSpellId))
        {
            return 0;
        }

        lua_pushinteger(state, updatedRecipeSpellId);
        return 1;
    }

    private static int RemoveFromGuild(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage =
            "Usage: C_GuildInfo.RemoveFromGuild(guid)";
        var guid = RequiredGuid(state, 1, usage);
        guild.Requests.Add(
            new WowGuildInfoRequest("RemoveFromGuild", [guid]));
        return 0;
    }

    private static int RequestRenameStatus(
        lua_State state,
        WowGuildInfoState guild)
    {
        guild.Requests.Add(
            new WowGuildInfoRequest("RequestRenameStatus", []));
        return PushBoolean(state, guild.RenameStatusRequestAccepted);
    }

    private static int SetGuildRankOrder(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage =
            "Usage: C_GuildInfo.SetGuildRankOrder(guid, rankOrder)";
        var guid = RequiredGuid(state, 1, usage);
        var rankOrder = RequiredOneBasedIndex(state, 2, usage);

        var zeroBasedRankOrder = rankOrder - 1;
        if (zeroBasedRankOrder is >= 1 and <= 10)
        {
            guild.Requests.Add(
                new WowGuildInfoRequest(
                    "SetGuildRankOrder",
                    [guid, rankOrder]));
            guild.RankOrderByGuid[guid] = rankOrder;
        }
        return 0;
    }

    private static int SetInfoText(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage =
            "Usage: C_GuildInfo.SetInfoText(infoText)";
        var infoText = RequiredString(state, 1, usage);
        guild.InfoText = infoText;
        guild.Requests.Add(
            new WowGuildInfoRequest("SetInfoText", [infoText]));
        return 0;
    }

    private static int SetMotd(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage = "Usage: C_GuildInfo.SetMOTD(motd)";
        var motd = RequiredString(state, 1, usage);
        guild.Motd = motd;
        guild.Requests.Add(new WowGuildInfoRequest("SetMOTD", [motd]));
        return 0;
    }

    private static int SetNote(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage =
            "Usage: C_GuildInfo.SetNote(guid, note, isPublic)";
        var guid = RequiredGuid(state, 1, usage);
        var note = RequiredString(state, 2, usage);
        var isPublic = RequiredBoolean(state, 3, usage);
        guild.NotesByGuid[guid] = new WowGuildNote(note, isPublic);
        guild.Requests.Add(
            new WowGuildInfoRequest(
                "SetNote",
                [guid, note, isPublic]));
        return 0;
    }

    private static int RecordRequiredString(
        lua_State state,
        WowGuildInfoState guild,
        string operation,
        string usage)
    {
        guild.Requests.Add(
            new WowGuildInfoRequest(
                operation,
                [RequiredString(state, 1, usage)]));
        return 0;
    }

    private static int Record(
        WowGuildInfoState guild,
        string operation)
    {
        guild.Requests.Add(new WowGuildInfoRequest(operation, []));
        return 0;
    }

    private static int DispatchLegacy(
        lua_State state,
        WowGuildInfoState guild,
        string operation)
    {
        if (operation == "IsInGuild")
            return PushBoolean(state, guild.IsInGuild);
        if (operation == "IsGuildLeader")
            return PushBoolean(state, guild.IsGuildLeader);
        if (operation == "GetGuildRenameRequired")
            return PushBoolean(state, guild.GuildRenameRequired);
        if (operation == "CanGuildInvite")
            return PushBoolean(state, guild.CanGuildInvite);
        if (operation == "GetNumGuildPerks")
        {
            lua_pushinteger(state, guild.GuildPerkCount);
            return 1;
        }
        if (operation == "RequestGuildRewards")
            return Record(guild, operation);
        if (operation == "GetGuildNewsFilters")
        {
            for (var index = 0; index < 9; index++)
            {
                lua_pushboolean(
                    state,
                    (guild.GuildNewsFilterMask & (1 << index)) != 0
                        ? 1
                        : 0);
            }
            return 9;
        }
        if (operation == "SetGuildNewsFilter")
            return SetGuildNewsFilter(state, guild);
        if (operation == "QueryGuildEventLog")
        {
            if (guild.IsInGuild)
                return Record(guild, operation);
            return 0;
        }
        if (operation == "DeclineGuild")
            return Record(guild, operation);
        if (operation == "GuildControlGetNumRanks")
        {
            lua_pushinteger(state, guild.GuildRankCount);
            return 1;
        }
        if (operation == "GetNumGuildBankTabs")
        {
            lua_pushinteger(state, guild.GuildBankTabCount);
            return 1;
        }
        if (operation == "GetCurrentGuildBankTab")
        {
            lua_pushinteger(state, guild.CurrentGuildBankTab);
            return 1;
        }
        if (operation == "GetGuildFactionGroup")
        {
            lua_pushinteger(state, guild.GuildFactionGroup);
            return 1;
        }
        if (operation == "GuildControlSetRank")
            return GuildControlSetRank(state, guild);
        if (operation == "RequestGuildChallengeInfo")
            return Record(guild, operation);
        if (operation == "GetGuildTabardFiles")
            return GetGuildTabardFiles(state, guild);
        return 0;
    }

    private static int GuildControlSetRank(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage = "Usage: GuildControlSetRank(rankOrder)";
        var rankOrder = RequiredInt32(state, 1, usage);
        guild.SelectedGuildRankOrder =
            rankOrder >= 1 && rankOrder <= guild.GuildRankCount
                ? rankOrder
                : null;
        return 0;
    }

    private static int SetGuildNewsFilter(
        lua_State state,
        WowGuildInfoState guild)
    {
        const string usage = "Usage: SetGuildNewsFilter(index, bool)";
        var index = RequiredInt32(state, 1, usage);
        var enabled = RequiredInt32(state, 2, usage) != 0;
        var bit = 1 << ((index - 1) & 31);
        guild.GuildNewsFilterMask = enabled
            ? guild.GuildNewsFilterMask | bit
            : guild.GuildNewsFilterMask & ~bit;
        guild.Requests.Add(
            new WowGuildInfoRequest(
                "SetGuildNewsFilter",
                [index, enabled]));
        return 0;
    }

    private static int GetGuildTabardFiles(
        lua_State state,
        WowGuildInfoState guild)
    {
        if (guild.LegacyTabardFileIds is null ||
            guild.LegacyTabardFileIds.Count < 6)
        {
            return 0;
        }

        for (var index = 0; index < 6; index++)
        {
            var fileId = guild.LegacyTabardFileIds[index];
            if (fileId.HasValue)
                lua_pushinteger(state, fileId.Value);
            else
                lua_pushnil(state);
        }
        return 6;
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
        if (index > lua_gettop(state) || lua_isstring(state, index) == 0)
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
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        return RequiredString(state, index, usage);
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return luaL_error(state, usage);
        }
        return (int)value;
    }

    private static int RequiredOneBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
            return luaL_error(state, usage);
        return unchecked((int)(uint)value);
    }

    private static int? OptionalOneBasedIndex(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return null;
        return RequiredOneBasedIndex(state, index, usage);
    }

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state))
        {
            luaL_error(state, usage);
            return false;
        }
        return lua_toboolean(state, index) != 0;
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static int PushString(lua_State state, string value)
    {
        lua_pushstring(state, value);
        return 1;
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

    private static void SetIntArray(
        lua_State state,
        string name,
        IReadOnlyList<int> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, name);
    }

    private static void SetInteger(
        lua_State state,
        string name,
        int value)
    {
        lua_pushinteger(state, value);
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
