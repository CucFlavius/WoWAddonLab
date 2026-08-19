using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowClientFeatureApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        RegisterNamespace(
            state,
            "C_AuthChallenge",
            "SetFrame", "Submit", "Cancel", "OnTabPressed");
        RegisterNamespace(
            state,
            "C_PhotoSharing",
            "BeginAuthorizationFlow", "ClearAuthorization", "IsAuthorized", "IsEnabled");
        RegisterNamespace(
            state,
            "C_PingSecure",
            "CreateFrame", "SetPendingPingOffScreenCallback", "SetPingCooldownStartedCallback",
            "SetPingPinFrameAddedCallback", "SetPingPinFrameRemovedCallback",
            "SetPingPinFrameScreenClampStateUpdatedCallback", "SetPingRadialWheelCreatedCallback",
            "SetSendMacroPingCallback", "SetTogglePingListenerCallback");
        RegisterNamespace(
            state,
            "C_AssistedCombat",
            "GetActionSpell", "GetNextCastSpell", "GetRotationSpells", "IsAvailable");
        RegisterNamespace(
            state,
            "C_RecruitAFriend",
            "GetRAFInfo", "GetRAFSystemInfo", "IsEnabled", "IsRecruitingEnabled");
        RegisterNamespace(
            state,
            "C_WowTokenPublic",
            "GetCommerceSystemStatus", "GetCurrentMarketPrice", "UpdateMarketPrice");
        RegisterNamespace(
            state,
            "C_WowTokenSecure",
            "CancelRedeem", "CanRedeemForBalance", "ConfirmBuyToken", "ConfirmSellToken",
            "GetBalanceRedeemAmount", "GetBalanceRedemptionInfo", "GetGameTimeRedemptionInfo",
            "GetPriceLockDuration", "GetRemainingGameTime", "GetTokenCount",
            "IsRedemptionStillValid", "RedeemToken", "RedeemTokenConfirm",
            "SetBalanceAmountString", "WillKickFromWorld");
        RegisterNamespace(state, "C_ZoneAbility", "GetActiveAbilities");
        RegisterNamespace(state, "C_ExternalEventURL", "HasURL", "IsNew", "LaunchURL");
        RegisterNamespace(
            state,
            "C_CooldownViewer",
            "GetCooldownViewerCategorySet", "GetCooldownViewerCooldownInfo");
        RegisterNamespace(
            state,
            "C_EncounterTimeline",
            "AddEditModeEvents", "CancelEditModeEvents", "GetCurrentTime",
            "GetEventColor", "GetEventHighlightTime", "GetEventInfo", "GetEventList",
            "GetEventState", "GetEventTimer", "GetEventTrack", "GetTrackList",
            "GetTrackType", "GetViewType", "HasActiveEvents", "HasPausedEvents", "HasVisibleEvents",
            "IsEventBlocked", "IsFeatureAvailable", "IsFeatureEnabled",
            "SetEventIconTextures", "SetViewType");
        RegisterNamespace(
            state,
            "C_EncounterWarnings",
            "GetColorForSeverity", "GetEditModeWarningInfo", "GetPlayCustomSoundsWhenHidden",
            "GetSoundKitForSeverity", "GetWarningsShown", "IsFeatureAvailable",
            "IsFeatureEnabled", "PlaySound", "SetPlayCustomSoundsWhenHidden", "SetWarningsShown");
    }

    private static void RegisterNamespace(lua_State state, string name, params string[] functions)
    {
        lua_newtable(state);
        foreach (var function in functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, name);
    }

    private static int Dispatch(lua_State state)
    {
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "IsAuthorized":
            case "IsEnabled":
            case "IsRecruitingEnabled":
            case "CanRedeemForBalance":
            case "IsRedemptionStillValid":
            case "WillKickFromWorld":
            case "HasActiveEvents":
            case "HasPausedEvents":
            case "HasVisibleEvents":
            case "IsEventBlocked":
            case "IsFeatureAvailable":
            case "IsFeatureEnabled":
            case "IsAvailable":
            case "HasURL":
            case "IsNew":
                lua_pushboolean(state, 0);
                return 1;
            case "GetRAFInfo":
            case "GetRAFSystemInfo":
            case "GetEventColor":
            case "GetEventInfo":
            case "GetEventState":
            case "GetEventTimer":
            case "GetEventTrack":
            case "GetTrackType":
            case "GetColorForSeverity":
            case "GetEditModeWarningInfo":
                return 0;
            case "GetCommerceSystemStatus":
                lua_pushboolean(state, 0);
                lua_pushboolean(state, 0);
                lua_pushboolean(state, 0);
                return 3;
            case "GetActiveAbilities":
            case "GetCooldownViewerCategorySet":
            case "GetEventList":
            case "GetTrackList":
            case "GetRotationSpells":
                lua_newtable(state);
                return 1;
            case "GetBalanceRedemptionInfo":
                lua_pushnumber(state, 0);
                lua_pushnumber(state, 0);
                lua_pushboolean(state, 0);
                lua_pushinteger(state, 0);
                return 4;
            case "GetGameTimeRedemptionInfo":
                lua_pushboolean(state, 0);
                lua_pushnumber(state, 0);
                return 2;
            case "GetBalanceRedeemAmount":
            case "GetPriceLockDuration":
            case "GetRemainingGameTime":
            case "GetTokenCount":
            case "AddEditModeEvents":
            case "GetCurrentTime":
            case "GetEventHighlightTime":
            case "GetViewType":
            case "GetSoundKitForSeverity":
            case "PlaySound":
                lua_pushnumber(state, 0);
                return 1;
            case "GetPlayCustomSoundsWhenHidden":
            case "GetWarningsShown":
                lua_pushboolean(state, 0);
                return 1;
            default:
                return 0;
        }
    }
}
