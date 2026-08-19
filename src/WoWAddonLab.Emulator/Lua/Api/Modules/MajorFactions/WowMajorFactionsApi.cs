using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowMajorFactionsApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetCurrentRenownLevel", "GetMajorFactionData", "GetMajorFactionIDs",
        "GetMajorFactionRenownInfo", "GetRenownLevels", "GetRenownNPCFactionID",
        "GetRenownRewardsForLevel", "HasMaximumRenown",
        "IsMajorFactionHiddenFromExpansionPage", "IsWeeklyRenownCapped",
        "ShouldDisplayMajorFactionAsJourney", "ShouldUseJourneyRewardTrack"
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
        lua_setglobal(state, "C_MajorFactions");
    }

    private static int Dispatch(lua_State state)
    {
        var major = LuaBindings.GetRuntime(state).MajorFactions;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetCurrentRenownLevel":
            {
                var id = RequiredId(state, operation);
                lua_pushinteger(
                    state,
                    major.CurrentRenownLevels.TryGetValue(id, out var level) ? level : 0);
                return 1;
            }
            case "GetMajorFactionData":
            {
                var id = RequiredId(state, operation);
                if (!major.Factions.TryGetValue(id, out var data))
                    lua_pushnil(state);
                else
                    PushMajorFactionData(state, data);
                return 1;
            }
            case "GetMajorFactionIDs":
            {
                var expansionId = OptionalInt32(state, 1, operation);
                lua_newtable(state);
                var index = 1;
                foreach (var data in major.Factions.Values)
                {
                    if (expansionId is not null && data.ExpansionId != expansionId)
                        continue;
                    lua_pushinteger(state, data.FactionId);
                    lua_rawseti(state, -2, index++);
                }
                return 1;
            }
            case "GetMajorFactionRenownInfo":
            {
                var id = RequiredId(state, operation);
                if (!major.RenownInfo.TryGetValue(id, out var info))
                    lua_pushnil(state);
                else
                {
                    lua_newtable(state);
                    SetInteger(state, "renownLevel", info.RenownLevel);
                    SetInteger(
                        state,
                        "renownReputationEarned",
                        info.RenownReputationEarned);
                    SetInteger(
                        state,
                        "renownLevelThreshold",
                        info.RenownLevelThreshold);
                }
                return 1;
            }
            case "GetRenownLevels":
            {
                var id = RequiredId(state, operation);
                major.RenownLevels.TryGetValue(id, out var levels);
                levels ??= [];
                lua_newtable(state);
                for (var index = 0; index < levels.Count; index++)
                {
                    var level = levels[index];
                    lua_newtable(state);
                    SetInteger(state, "factionID", level.FactionId);
                    SetInteger(state, "level", level.Level);
                    SetBoolean(state, "locked", level.Locked);
                    SetBoolean(state, "isMilestone", level.IsMilestone);
                    SetBoolean(state, "isCapstone", level.IsCapstone);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "GetRenownNPCFactionID":
                lua_pushinteger(state, major.RenownNpcFactionId);
                return 1;
            case "GetRenownRewardsForLevel":
            {
                var id = RequiredId(state, operation);
                var level = RequiredInt32(state, 2, operation);
                major.RenownRewards.TryGetValue((id, level), out var rewards);
                rewards ??= [];
                lua_newtable(state);
                for (var index = 0; index < rewards.Count; index++)
                {
                    PushRenownReward(state, rewards[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "HasMaximumRenown":
                return PushMembership(state, major.MaximumRenownFactions, operation);
            case "IsMajorFactionHiddenFromExpansionPage":
                return PushMembership(
                    state,
                    major.HiddenFromExpansionPageFactions,
                    operation);
            case "IsWeeklyRenownCapped":
                return PushMembership(state, major.WeeklyCappedFactions, operation);
            case "ShouldDisplayMajorFactionAsJourney":
                return PushMembership(state, major.JourneyDisplayFactions, operation);
            case "ShouldUseJourneyRewardTrack":
                return PushMembership(state, major.JourneyRewardTrackFactions, operation);
            default:
                return 0;
        }
    }

    private static int PushMembership(
        lua_State state,
        ISet<int> ids,
        string operation)
    {
        PushBoolean(state, ids.Contains(RequiredId(state, operation)));
        return 1;
    }

    private static void PushMajorFactionData(
        lua_State state,
        WowMajorFactionDataState data)
    {
        lua_newtable(state);
        SetOptionalString(state, "name", data.Name);
        SetOptionalString(state, "description", data.Description);
        lua_newtable(state);
        for (var index = 0; index < data.Highlights.Count; index++)
        {
            var highlight = data.Highlights[index];
            lua_newtable(state);
            SetOptionalString(state, "title", highlight.Title);
            SetOptionalString(state, "description", highlight.Description);
            SetInteger(state, "level", highlight.Level);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "highlights");
        SetInteger(state, "factionID", data.FactionId);
        SetInteger(state, "expansionID", data.ExpansionId);
        SetInteger(state, "bountySetID", data.BountySetId);
        SetBoolean(state, "isUnlocked", data.IsUnlocked);
        SetBoolean(state, "useJourneyUnlockToast", data.UseJourneyUnlockToast);
        SetOptionalString(state, "unlockDescription", data.UnlockDescription);
        SetInteger(state, "uiPriority", data.UiPriority);
        SetInteger(state, "renownLevel", data.RenownLevel);
        SetInteger(state, "maxLevel", data.MaxLevel);
        SetInteger(state, "renownReputationEarned", data.RenownReputationEarned);
        SetInteger(state, "renownLevelThreshold", data.RenownLevelThreshold);
        SetOptionalString(state, "textureKit", data.TextureKit);
        SetInteger(state, "celebrationSoundKit", data.CelebrationSoundKit);
        SetInteger(
            state,
            "renownFanfareSoundKitID",
            data.RenownFanfareSoundKitId);
        PushOptionalColor(state, data.FactionFontColor);
        lua_setfield(state, -2, "factionFontColor");
        SetOptionalInteger(
            state,
            "renownTrackLevelEffectID",
            data.RenownTrackLevelEffectId);
        SetOptionalInteger(state, "playerCompanionID", data.PlayerCompanionId);
    }

    private static void PushRenownReward(
        lua_State state,
        WowMajorFactionRenownRewardState reward)
    {
        lua_newtable(state);
        SetInteger(state, "renownRewardID", reward.RenownRewardId);
        SetInteger(state, "uiOrder", reward.UiOrder);
        SetBoolean(state, "isAccountUnlock", reward.IsAccountUnlock);
        SetOptionalInteger(state, "itemID", reward.ItemId);
        SetOptionalInteger(state, "spellID", reward.SpellId);
        SetOptionalInteger(state, "mountID", reward.MountId);
        SetOptionalInteger(state, "transmogID", reward.TransmogId);
        SetOptionalInteger(state, "transmogSetID", reward.TransmogSetId);
        SetOptionalInteger(state, "titleMaskID", reward.TitleMaskId);
        SetOptionalInteger(
            state,
            "transmogIllusionSourceID",
            reward.TransmogIllusionSourceId);
        SetOptionalInteger(state, "icon", reward.IconFileDataId);
        SetOptionalString(state, "name", reward.Name);
        SetOptionalString(state, "description", reward.Description);
        SetOptionalString(state, "toastDescription", reward.ToastDescription);
        SetOptionalInteger(state, "rewardType", reward.RewardType);
        SetOptionalBoolean(state, "isCollected", reward.IsCollected);
    }

    private static void PushOptionalColor(
        lua_State state,
        WowMajorFactionColorState? color)
    {
        if (color is null)
        {
            lua_pushnil(state);
            return;
        }
        lua_newtable(state);
        SetOptionalString(state, "baseTag", color.BaseTag);
        lua_getglobal(state, "CreateColor");
        if (lua_isfunction(state, -1) != 0)
        {
            lua_pushnumber(state, color.Red);
            lua_pushnumber(state, color.Green);
            lua_pushnumber(state, color.Blue);
            lua_pushnumber(state, color.Alpha);
            if (lua_pcall(state, 4, 1, 0) == 0)
            {
                lua_setfield(state, -2, "color");
                return;
            }
            lua_pop(state, 1);
        }
        else
        {
            lua_pop(state, 1);
        }
        lua_newtable(state);
        lua_pushnumber(state, color.Red);
        lua_setfield(state, -2, "r");
        lua_pushnumber(state, color.Green);
        lua_setfield(state, -2, "g");
        lua_pushnumber(state, color.Blue);
        lua_setfield(state, -2, "b");
        lua_pushnumber(state, color.Alpha);
        lua_setfield(state, -2, "a");
        lua_setfield(state, -2, "color");
    }

    private static int RequiredId(lua_State state, string operation) =>
        RequiredInt32(state, 1, operation);

    private static int RequiredInt32(lua_State state, int index, string operation)
    {
        if (lua_isnumber(state, index) == 0)
            return luaL_error(state, $"Usage: C_MajorFactions.{operation}(...)");
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, $"Usage: C_MajorFactions.{operation}(...)");
        return unchecked((int)value);
    }

    private static int? OptionalInt32(
        lua_State state,
        int index,
        string operation)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
            return null;
        return RequiredInt32(state, index, operation);
    }

    private static void PushBoolean(lua_State state, bool value) =>
        lua_pushboolean(state, value ? 1 : 0);

    private static void SetInteger(lua_State state, string field, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetBoolean(lua_State state, string field, bool value)
    {
        PushBoolean(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalInteger(
        lua_State state,
        string field,
        int? value)
    {
        if (value is { } integer)
            lua_pushinteger(state, integer);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalBoolean(
        lua_State state,
        string field,
        bool? value)
    {
        if (value is { } boolean)
            PushBoolean(state, boolean);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        if (value is not null)
            lua_pushstring(state, value);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, field);
    }
}
