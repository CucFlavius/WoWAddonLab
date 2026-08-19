using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowAchievementApi : LuaApiModule
{
    private const int GuildAchievementFlag = 0x4000;
    private const int StatisticAchievementFlag = 0x1;

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] GlobalFunctions =
    [
        "GetCategoryList",
        "GetGuildCategoryList",
        "GetStatisticsCategoryList",
        "GetCategoryInfo",
        "GetCategoryNumAchievements",
        "GetCategoryAchievementPoints",
        "GetComparisonCategoryNumAchievements",
        "GetAchievementInfo",
        "GetAchievementNumRewards",
        "GetAchievementReward",
        "GetAchievementNumCriteria",
        "GetAchievementCriteriaInfo",
        "GetAchievementCriteriaInfoByID",
        "SetAchievementComparisonUnit",
        "ClearAchievementComparisonUnit",
        "GetAchievementComparisonInfo",
        "GetPreviousAchievement",
        "GetNextAchievement",
        "GetAchievementCategory",
        "GetAchievementLink",
        "GetNumCompletedAchievements",
        "GetNumComparisonCompletedAchievements",
        "GetLatestCompletedAchievements",
        "GetLatestUpdatedStats",
        "GetLatestCompletedComparisonAchievements",
        "GetLatestUpdatedComparisonStats",
        "GetTotalAchievementPoints",
        "IsAchievementEligible",
        "GetStatistic",
        "GetComparisonStatistic",
        "GetComparisonAchievementPoints",
        "CanShowAchievementUI",
        "HasCompletedAnyAchievement",
        "SetAchievementSearchString",
        "ClearAchievementSearchString",
        "GetNumFilteredAchievements",
        "GetFilteredAchievementID",
        "SwitchAchievementSearchTab",
        "GetAchievementSearchProgress",
        "GetAchievementSearchSize",
        "GetGuildAchievementMembers",
        "GetGuildAchievementNumMembers",
        "GetGuildAchievementMemberInfo",
        "SetFocusedAchievement",
        "GetAchievementGuildRep"
    ];

    private static readonly string[] NamespaceFunctions =
    [
        "AreGuildAchievementsEnabled",
        "GetRewardItemID",
        "GetSupercedingAchievements",
        "IsGuildAchievement",
        "IsValidAchievement",
        "SetPortraitTexture"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in GlobalFunctions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);

        RegisterNamespace(state, "C_AchievementInfo", NamespaceFunctions);
        RegisterNamespace(
            state,
            "C_AchievementTelemetry",
            [
                "LinkAchievementInClub",
                "LinkAchievementInWhisper",
                "ShowAchievements"
            ]);
    }

    private static void RegisterNamespace(
        lua_State state,
        string namespaceName,
        IEnumerable<string> functions)
    {
        lua_newtable(state);
        foreach (var function in functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, namespaceName);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var provider = runtime.AchievementProvider;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        return operation switch
        {
            "GetCategoryList" => PushCategoryList(state, provider, CategoryKind.Normal),
            "GetGuildCategoryList" => PushCategoryList(state, provider, CategoryKind.Guild),
            "GetStatisticsCategoryList" =>
                PushCategoryList(state, provider, CategoryKind.Statistic),
            "GetCategoryInfo" => GetCategoryInfo(state, provider),
            "GetCategoryNumAchievements" =>
                GetCategoryNumAchievements(state, provider),
            "GetCategoryAchievementPoints" =>
                GetCategoryAchievementPoints(state, provider),
            "GetComparisonCategoryNumAchievements" =>
                GetComparisonCategoryNumAchievements(state),
            "GetAchievementInfo" => PushAchievementInfo(state, provider),
            "GetAchievementNumRewards" =>
                GetAchievementNumRewards(state, provider),
            "GetAchievementReward" => GetAchievementReward(state, provider),
            "GetAchievementNumCriteria" =>
                GetAchievementNumCriteria(state, provider),
            "GetAchievementCriteriaInfo" =>
                GetAchievementCriteriaInfo(state, provider, byId: false),
            "GetAchievementCriteriaInfoByID" =>
                GetAchievementCriteriaInfo(state, provider, byId: true),
            "SetAchievementComparisonUnit" =>
                SetAchievementComparisonUnit(state, runtime),
            "ClearAchievementComparisonUnit" =>
                ClearAchievementComparisonUnit(runtime),
            "GetAchievementComparisonInfo" =>
                GetAchievementComparisonInfo(state, runtime, provider),
            "GetPreviousAchievement" =>
                GetPreviousAchievement(state, provider),
            "GetNextAchievement" => GetNextAchievement(state, provider),
            "GetAchievementCategory" =>
                GetAchievementCategory(state, provider),
            "GetAchievementLink" => GetAchievementLink(state, runtime, provider),
            "GetNumCompletedAchievements" =>
                GetNumCompletedAchievements(state, provider),
            "GetNumComparisonCompletedAchievements" =>
                PushTwoZeroes(state),
            "GetLatestCompletedAchievements" =>
                PushLatestCompletedAchievements(state, provider),
            "GetLatestUpdatedStats" => 0,
            "GetLatestCompletedComparisonAchievements" => 0,
            "GetLatestUpdatedComparisonStats" => 0,
            "GetTotalAchievementPoints" =>
                GetTotalAchievementPoints(state, provider),
            "IsAchievementEligible" =>
                IsAchievementEligible(state, provider),
            "GetStatistic" => GetStatistic(state, provider),
            "GetComparisonStatistic" =>
                GetComparisonStatistic(state),
            "GetComparisonAchievementPoints" => PushZero(state),
            "CanShowAchievementUI" =>
                PushBoolean(state, runtime.Achievements.CanShowAchievementUi),
            "HasCompletedAnyAchievement" =>
                PushBoolean(
                    state,
                    runtime.Achievements.HasCompletedAnyAchievement ||
                    provider?.Achievements.Any(value => value.Completed) == true),
            "SetAchievementSearchString" =>
                SetAchievementSearchString(state, runtime, provider),
            "ClearAchievementSearchString" =>
                ClearAchievementSearchString(runtime),
            "GetNumFilteredAchievements" =>
                PushInteger(state, runtime.Achievements.FilteredAchievementIds.Count),
            "GetFilteredAchievementID" =>
                GetFilteredAchievementId(state, runtime),
            "SwitchAchievementSearchTab" =>
                SwitchAchievementSearchTab(state, runtime),
            "GetAchievementSearchProgress" =>
                PushInteger(state, provider?.Achievements.Count ?? 0),
            "GetAchievementSearchSize" =>
                PushInteger(state, provider?.Achievements.Count ?? 0),
            "GetGuildAchievementMembers" =>
                GetGuildAchievementMembers(state),
            "GetGuildAchievementNumMembers" =>
                GetGuildAchievementNumMembers(state),
            "GetGuildAchievementMemberInfo" =>
                GetGuildAchievementMemberInfo(state),
            "SetFocusedAchievement" =>
                SetFocusedAchievement(state, runtime, provider),
            "GetAchievementGuildRep" =>
                GetAchievementGuildRep(state, provider),
            "AreGuildAchievementsEnabled" => PushBoolean(state, true),
            "GetRewardItemID" => GetRewardItemId(state, provider),
            "GetSupercedingAchievements" =>
                GetSupercedingAchievements(state, provider),
            "IsGuildAchievement" =>
                IsGuildAchievement(state, provider),
            "IsValidAchievement" =>
                IsValidAchievement(state, provider),
            "SetPortraitTexture" =>
                SetAchievementPortraitTexture(state, runtime),
            "LinkAchievementInClub" or "LinkAchievementInWhisper" =>
                AchievementTelemetryLink(state, operation),
            "ShowAchievements" => 0,
            _ => 0
        };
    }

    private static int PushCategoryList(
        lua_State state,
        IWowAchievementProvider? provider,
        CategoryKind kind)
    {
        if (provider is null)
            return PushIntegerList(state, []);

        var categories = provider.Categories.Where(category =>
        {
            var achievements = provider.Achievements.Where(
                achievement => achievement.CategoryId == category.Id);
            return kind switch
            {
                CategoryKind.Guild => achievements.Any(IsGuild),
                CategoryKind.Statistic => achievements.Any(IsStatistic),
                _ => achievements.Any(achievement => !IsGuild(achievement))
            };
        });
        return PushIntegerList(state, categories.Select(value => value.Id));
    }

    private static int GetCategoryInfo(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage = "Usage: GetCategoryInfo(categoryID)";
        var categoryId = RequiredInt32(state, 1, usage);
        var category = provider?.Categories.FirstOrDefault(
            value => value.Id == categoryId);
        if (category is null)
            return 0;

        lua_pushstring(state, category.Name);
        lua_pushnumber(state, category.ParentId);
        lua_pushnumber(state, 0);
        return 3;
    }

    private static int GetCategoryNumAchievements(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage =
            "Usage: GetCategoryNumAchievements(categoryID, includeSuperceded)";
        var categoryId = RequiredInt32(state, 1, usage);
        _ = lua_toboolean(state, 2);
        var achievements = provider?.Achievements
            .Where(value => value.CategoryId == categoryId)
            .ToArray() ?? [];
        var completed = achievements.Count(value => value.Completed);
        lua_pushnumber(state, achievements.Length);
        lua_pushnumber(state, completed);
        lua_pushnumber(state, achievements.Length - completed);
        return 3;
    }

    private static int GetCategoryAchievementPoints(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage =
            "Usage: GetCategoryAchievementPoints(categoryID, includeSubCategories)";
        var categoryId = RequiredInt32(state, 1, usage);
        var includeSubCategories = RequiredBoolean(state, 2, usage);
        var categoryIds = new HashSet<int> { categoryId };
        if (includeSubCategories && provider is not null)
        {
            var added = true;
            while (added)
            {
                added = false;
                foreach (var category in provider.Categories)
                {
                    if (categoryIds.Contains(category.ParentId) &&
                        categoryIds.Add(category.Id))
                    {
                        added = true;
                    }
                }
            }
        }

        var points = provider?.Achievements
            .Where(value => value.Completed && categoryIds.Contains(value.CategoryId))
            .Sum(value => value.Points) ?? 0;
        return PushInteger(state, points);
    }

    private static int GetComparisonCategoryNumAchievements(lua_State state)
    {
        const string usage =
            "Usage: GetComparisonCategoryNumAchievements(categoryID)";
        _ = RequiredInt32(state, 1, usage);
        return PushZero(state);
    }

    private static int PushAchievementInfo(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage = "Usage: GetAchievementInfo(achievementID)";
        var first = RequiredInt32(state, 1, usage);
        if (provider is null)
            return 0;

        WowAchievementDefinition? achievement;
        if (lua_isnumber(state, 2) != 0)
        {
            var index = RequiredInt32(state, 2, usage) - 1;
            achievement = index < 0
                ? null
                : provider.Achievements
                    .Where(value => value.CategoryId == first)
                    .Skip(index)
                    .FirstOrDefault();
        }
        else
        {
            achievement = provider.Achievements.FirstOrDefault(
                value => value.Id == first);
        }
        if (achievement is null)
            return 0;

        lua_pushnumber(state, achievement.Id);
        lua_pushstring(state, achievement.Name);
        lua_pushnumber(state, achievement.Points);
        lua_pushboolean(state, achievement.Completed ? 1 : 0);
        PushOptionalInteger(state, achievement.CompletionMonth);
        PushOptionalInteger(state, achievement.CompletionDay);
        PushOptionalInteger(state, achievement.CompletionYear);
        lua_pushstring(state, achievement.Description);
        lua_pushnumber(state, achievement.Flags);
        if (achievement.IconFileDataId == 0)
            lua_pushnil(state);
        else
            lua_pushnumber(state, achievement.IconFileDataId);
        lua_pushstring(state, achievement.RewardText);
        lua_pushboolean(state, IsGuild(achievement) ? 1 : 0);
        lua_pushboolean(state, achievement.WasEarnedByMe ? 1 : 0);
        PushOptionalString(state, achievement.EarnedBy);
        lua_pushboolean(state, IsStatistic(achievement) ? 1 : 0);
        return 15;
    }

    private static int GetAchievementNumRewards(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage = "Usage: GetAchievementNumRewards(achievementID)";
        var achievementId = RequiredInt32(state, 1, usage);
        return PushBoolean(
            state,
            provider?.Achievements.Any(value => value.Id == achievementId) == true);
    }

    private static int GetAchievementReward(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage =
            "Usage: GetAchievementNumRewards(achievementID, rewardIndex)";
        var achievementId = RequiredInt32(state, 1, usage);
        _ = RequiredInt32(state, 2, usage);
        var achievement = FindAchievement(provider, achievementId);
        if (achievement is null)
            lua_pushnil(state);
        else
            lua_pushnumber(state, achievement.Points);
        return 1;
    }

    private static int GetAchievementNumCriteria(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage =
            "Usage: GetAchievementNumCriteria(achievementID [,countHidden])";
        var achievementId = RequiredInt32(state, 1, usage);
        return PushInteger(
            state,
            provider?.Criteria.Count(value =>
                value.AchievementId == achievementId) ?? 0);
    }

    private static int GetAchievementCriteriaInfo(
        lua_State state,
        IWowAchievementProvider? provider,
        bool byId)
    {
        var usage = byId
            ? "Usage: GetAchievementCriteriaInfoByID(achievementID, criteriaIndex)"
            : "Usage: GetAchievementCriteriaInfo(achievementID, criteriaIndex [,countHidden])";
        var achievementId = RequiredInt32(state, 1, usage);
        var selector = RequiredInt32(state, 2, usage);
        if (achievementId < 0 || selector < 0)
        {
            var message = byId
                ? "GetAchievementCriteriaInfoByID(achievementID, criteriaID), criteria not found"
                : "GetAchievementCriteriaInfo(achievementID, criteriaIndex [,countHidden]), criteria not found";
            return luaL_error(state, message);
        }

        WowAchievementCriteriaDefinition? criteria;
        if (byId)
        {
            criteria = provider?.Criteria.FirstOrDefault(value =>
                value.AchievementId == achievementId &&
                value.CriteriaId == selector);
        }
        else
        {
            criteria = selector <= 0
                ? null
                : provider?.Criteria
                    .Where(value => value.AchievementId == achievementId)
                    .Skip(selector - 1)
                    .FirstOrDefault();
        }
        if (criteria is null)
            return 0;

        lua_pushstring(state, criteria.Description);
        lua_pushnumber(state, criteria.Type);
        lua_pushboolean(state, criteria.Completed ? 1 : 0);
        lua_pushnumber(state, criteria.Quantity);
        lua_pushnumber(state, criteria.RequiredQuantity);
        PushOptionalString(state, criteria.CharacterName);
        lua_pushnumber(state, criteria.Flags);
        lua_pushnumber(state, criteria.AssetId);
        lua_pushstring(state, criteria.QuantityString);
        lua_pushnumber(state, criteria.CriteriaId);
        lua_pushboolean(state, criteria.Eligible ? 1 : 0);
        PushOptionalInteger(state, criteria.DurationSeconds);
        PushOptionalInteger(state, criteria.ElapsedSeconds);
        return 13;
    }

    private static int SetAchievementComparisonUnit(
        lua_State state,
        LuaRuntime runtime)
    {
        const string usage = "Usage: AddAchievementComparisonUnit(unitToken)";
        var unitToken = RequiredString(state, 1, usage);
        if (runtime.Units.Find(unitToken) is null)
            return 0;
        runtime.Achievements.ComparisonUnitToken = unitToken;
        lua_pushnumber(state, 1);
        return 1;
    }

    private static int ClearAchievementComparisonUnit(LuaRuntime runtime)
    {
        runtime.Achievements.ComparisonUnitToken = null;
        return 0;
    }

    private static int GetAchievementComparisonInfo(
        lua_State state,
        LuaRuntime runtime,
        IWowAchievementProvider? provider)
    {
        const string usage =
            "Usage: GetAchievementComparisonInfo(achievementID)";
        _ = RequiredInt32(state, 1, usage);
        if (runtime.Achievements.ComparisonUnitToken is null || provider is null)
            return 0;
        return 0;
    }

    private static int GetPreviousAchievement(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage = "Usage: GetPreviousAchievement(achievementID)";
        var achievement = FindAchievement(
            provider,
            RequiredInt32(state, 1, usage));
        if (achievement?.PreviousAchievementId is not { } previousId ||
            previousId == 0)
        {
            return 0;
        }
        return PushInteger(state, previousId);
    }

    private static int GetNextAchievement(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage = "Usage: GetNextAchievement(achievementID)";
        var achievementId = RequiredInt32(state, 1, usage);
        var next = provider?.Achievements.FirstOrDefault(
            value => value.PreviousAchievementId == achievementId);
        if (next is null)
            return 0;
        lua_pushnumber(state, next.Id);
        if (next.Completed)
        {
            lua_pushboolean(state, 1);
            return 2;
        }
        return 1;
    }

    private static int GetAchievementCategory(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage = "Usage: GetAchievementCategory(achievementID)";
        var achievement = FindAchievement(
            provider,
            RequiredInt32(state, 1, usage));
        return achievement is null
            ? 0
            : PushInteger(state, achievement.CategoryId);
    }

    private static int GetAchievementLink(
        lua_State state,
        LuaRuntime runtime,
        IWowAchievementProvider? provider)
    {
        const string usage = "Usage: GetAchievementLink(achievementID)";
        var achievement = FindAchievement(
            provider,
            RequiredInt32(state, 1, usage));
        if (achievement is null)
            return 0;

        var link =
            $"|cffffff00|Hachievement:{achievement.Id}:{runtime.Units.Player.Guid}:" +
            $"{(achievement.Completed ? 1 : 0)}:{achievement.CompletionMonth ?? 0}:" +
            $"{achievement.CompletionDay ?? 0}:{achievement.CompletionYear ?? 0}:" +
            $"0:0:0:0|h[{achievement.Name}]|h|r";
        lua_pushstring(state, link);
        return 1;
    }

    private static int GetNumCompletedAchievements(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        var guildView = lua_toboolean(state, 1) != 0;
        var achievements = provider?.Achievements
            .Where(value => IsGuild(value) == guildView)
            .ToArray() ?? [];
        lua_pushnumber(state, achievements.Length);
        lua_pushnumber(state, achievements.Count(value => value.Completed));
        return 2;
    }

    private static int PushLatestCompletedAchievements(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        var guildView = lua_toboolean(state, 1) != 0;
        var completed = provider?.Achievements
            .Where(value => value.Completed && IsGuild(value) == guildView)
            .Take(5)
            .ToArray() ?? [];
        foreach (var achievement in completed)
            lua_pushnumber(state, achievement.Id);
        return completed.Length;
    }

    private static int GetTotalAchievementPoints(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        var guildView = lua_toboolean(state, 1) != 0;
        var points = provider?.Achievements
            .Where(value =>
                value.Completed &&
                IsGuild(value) == guildView)
            .Sum(value => value.Points) ?? 0;
        return PushInteger(state, points);
    }

    private static int IsAchievementEligible(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage = "Usage: IsAchievementEligible(achievementID)";
        var achievement = FindAchievement(
            provider,
            RequiredInt32(state, 1, usage));
        return PushBoolean(state, achievement?.Eligible ?? true);
    }

    private static int GetStatistic(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage = "Usage: GetStatistic(achievementID)";
        var achievementId = RequiredInt32(state, 1, usage);
        var criteria = provider?.Criteria.FirstOrDefault(
            value => value.AchievementId == achievementId);
        lua_pushstring(state, criteria?.QuantityString ?? string.Empty);
        lua_pushboolean(state, 0);
        lua_pushnumber(state, achievementId);
        return 3;
    }

    private static int GetComparisonStatistic(lua_State state)
    {
        const string usage = "Usage: GetComparisonStatistic(achievementID)";
        _ = RequiredInt32(state, 1, usage);
        lua_pushstring(state, string.Empty);
        return 1;
    }

    private static int SetAchievementSearchString(
        lua_State state,
        LuaRuntime runtime,
        IWowAchievementProvider? provider)
    {
        const string usage =
            "Usage: SetAchievementSearchString(updatedSearch)";
        var search = RequiredString(state, 1, usage);
        runtime.Achievements.SearchString = search;
        RebuildSearchResults(runtime.Achievements, provider);
        return PushBoolean(state, true);
    }

    private static int ClearAchievementSearchString(LuaRuntime runtime)
    {
        runtime.Achievements.SearchString = string.Empty;
        runtime.Achievements.FilteredAchievementIds.Clear();
        return 0;
    }

    private static int GetFilteredAchievementId(
        lua_State state,
        LuaRuntime runtime)
    {
        const string usage = "Usage: GetFilteredAchievementID(index)";
        var index = RequiredInt32(state, 1, usage) - 1;
        if (index < 0 ||
            index >= runtime.Achievements.FilteredAchievementIds.Count)
        {
            return 0;
        }
        return PushInteger(
            state,
            runtime.Achievements.FilteredAchievementIds[index]);
    }

    private static int SwitchAchievementSearchTab(
        lua_State state,
        LuaRuntime runtime)
    {
        const string usage = "Usage: SwitchAchievementSearchTab(index)";
        var index = RequiredInt32(state, 1, usage);
        if (index is < 1 or > 3)
        {
            return luaL_error(
                state,
                "Usage: SwitchAchievementTab(index) - Index was out of range.");
        }
        runtime.Achievements.SearchTabIndex = index;
        return 0;
    }

    private static int GetGuildAchievementMembers(lua_State state)
    {
        const string usage =
            "Usage: GetGuildAchievementMembers(achievementID)";
        _ = RequiredInt32(state, 1, usage);
        return 0;
    }

    private static int GetGuildAchievementNumMembers(lua_State state)
    {
        const string usage =
            "Usage: GetGuildAchievementNumMembers(achievementID)";
        _ = RequiredInt32(state, 1, usage);
        return PushZero(state);
    }

    private static int GetGuildAchievementMemberInfo(lua_State state)
    {
        const string usage =
            "Usage: GetGuildAchievementMemberInfo(achievementID, index)";
        _ = RequiredInt32(state, 1, usage);
        _ = RequiredInt32(state, 2, usage);
        return 0;
    }

    private static int SetFocusedAchievement(
        lua_State state,
        LuaRuntime runtime,
        IWowAchievementProvider? provider)
    {
        if (lua_isnumber(state, 1) == 0)
            return 0;
        var achievement = FindAchievement(
            provider,
            UncheckedInt32(state, 1));
        if (achievement is not null && IsGuild(achievement))
            runtime.Achievements.FocusedAchievementId = achievement.Id;
        return 0;
    }

    private static int GetAchievementGuildRep(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        if (lua_isnumber(state, 1) == 0)
            return 0;
        _ = FindAchievement(provider, UncheckedInt32(state, 1));
        return PushBoolean(state, false);
    }

    private static int GetRewardItemId(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage =
            "Usage: local rewardItemID = C_AchievementInfo.GetRewardItemID(achievementID)";
        var achievement = FindAchievement(
            provider,
            RequiredInt32(state, 1, usage));
        PushOptionalInteger(state, achievement?.RewardItemId);
        return 1;
    }

    private static int GetSupercedingAchievements(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage =
            "Usage: local supercedingAchievements = C_AchievementInfo.GetSupercedingAchievements(achievementID)";
        var achievement = FindAchievement(
            provider,
            RequiredInt32(state, 1, usage));
        return PushIntegerList(
            state,
            achievement?.SupercedingAchievementIds ?? []);
    }

    private static int IsGuildAchievement(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage =
            "Usage: local isGuild = C_AchievementInfo.IsGuildAchievement(achievementId)";
        var achievement = FindAchievement(
            provider,
            RequiredInt32(state, 1, usage));
        return PushBoolean(state, achievement is not null && IsGuild(achievement));
    }

    private static int IsValidAchievement(
        lua_State state,
        IWowAchievementProvider? provider)
    {
        const string usage =
            "Usage: local isValidAchievement = C_AchievementInfo.IsValidAchievement(achievementId)";
        var achievementId = RequiredInt32(state, 1, usage);
        return PushBoolean(
            state,
            achievementId >= 0 &&
            FindAchievement(provider, achievementId) is not null);
    }

    private static int SetAchievementPortraitTexture(
        lua_State state,
        LuaRuntime runtime)
    {
        const string usage =
            "Usage: C_AchievementInfo.SetPortraitTexture(textureObject)";
        var target = LuaBindings.GetObject(runtime, 1);
        if (target is null ||
            !target.ObjectType.Equals(
                "Texture",
                StringComparison.OrdinalIgnoreCase))
        {
            return luaL_error(state, usage);
        }
        WowTextureApi.SetPortraitTexture(runtime, target, "player", false);
        return 0;
    }

    private static int AchievementTelemetryLink(
        lua_State state,
        string operation)
    {
        var usage =
            $"Usage: C_AchievementTelemetry.{operation}(achievementID)";
        _ = RequiredInt32(state, 1, usage);
        return 0;
    }

    private static void RebuildSearchResults(
        WowAchievementState state,
        IWowAchievementProvider? provider)
    {
        state.FilteredAchievementIds.Clear();
        if (provider is null || state.SearchString.Length == 0)
            return;
        foreach (var achievement in provider.Achievements)
        {
            if (achievement.Name.Contains(
                    state.SearchString,
                    StringComparison.OrdinalIgnoreCase) ||
                achievement.Description.Contains(
                    state.SearchString,
                    StringComparison.OrdinalIgnoreCase) ||
                achievement.RewardText.Contains(
                    state.SearchString,
                    StringComparison.OrdinalIgnoreCase))
            {
                state.FilteredAchievementIds.Add(achievement.Id);
            }
        }
    }

    private static WowAchievementDefinition? FindAchievement(
        IWowAchievementProvider? provider,
        int achievementId) =>
        provider?.Achievements.FirstOrDefault(value => value.Id == achievementId);

    private static bool IsGuild(WowAchievementDefinition achievement) =>
        (achievement.Flags & GuildAchievementFlag) != 0;

    private static bool IsStatistic(WowAchievementDefinition achievement) =>
        (achievement.Flags & StatisticAchievementFlag) != 0;

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return RaiseArgumentError(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value < int.MinValue ||
            value > int.MaxValue)
        {
            return RaiseArgumentError(state, usage);
        }
        return unchecked((int)value);
    }

    private static int UncheckedInt32(lua_State state, int index) =>
        unchecked((int)lua_tonumber(state, index));

    private static bool RequiredBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) ||
            lua_type(state, index) != LUA_TBOOLEAN)
        {
            RaiseArgumentError(state, usage);
        }
        return lua_toboolean(state, index) != 0;
    }

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isstring(state, index) == 0)
        {
            RaiseArgumentError(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static int RaiseArgumentError(lua_State state, string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static int PushInteger(lua_State state, int value)
    {
        lua_pushnumber(state, value);
        return 1;
    }

    private static int PushZero(lua_State state) => PushInteger(state, 0);

    private static int PushTwoZeroes(lua_State state)
    {
        lua_pushnumber(state, 0);
        lua_pushnumber(state, 0);
        return 2;
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static int PushIntegerList(
        lua_State state,
        IEnumerable<int> values)
    {
        var array = values.ToArray();
        lua_createtable(state, array.Length, 0);
        for (var index = 0; index < array.Length; index++)
        {
            lua_pushnumber(state, array[index]);
            lua_rawseti(state, -2, index + 1);
        }
        return 1;
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushnumber(state, value.Value);
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
    }

    private enum CategoryKind
    {
        Normal,
        Guild,
        Statistic
    }
}
