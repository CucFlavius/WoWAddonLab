using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowBattleNetApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "BNCheckBattleTagInviteToRecentAlly",
        "GetAccountInfoByGUID",
        "GetAccountInfoByID",
        "SendGameData"
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
        lua_setglobal(state, "C_BattleNet");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "BNCheckBattleTagInviteToRecentAlly":
                runtime.Friends.BattleNetRecentAllyInviteChecks.Add(
                    RequiredString(
                        state,
                        1,
                        "Usage: C_BattleNet.BNCheckBattleTagInviteToRecentAlly(guid)"));
                return 0;
            case "GetAccountInfoByGUID":
            {
                var guid = RequiredString(
                    state,
                    1,
                    "Usage: local accountInfo = C_BattleNet.GetAccountInfoByGUID(guid)");
                runtime.Friends.BattleNetAccountsByGuid.TryGetValue(guid, out var account);
                PushAccountInfo(state, account);
                return 1;
            }
            case "GetAccountInfoByID":
            {
                var accountId = RequiredUInt32(
                    state,
                    1,
                    "Usage: local accountInfo = C_BattleNet.GetAccountInfoByID(id [, wowAccountGUID])");
                var account = runtime.Friends.BattleNetAccountsByGuid.Values.FirstOrDefault(
                    value => value.AccountId == accountId);
                PushAccountInfo(state, account);
                return 1;
            }
            case "SendGameData":
                lua_pushboolean(state, 1);
                return 1;
            default:
                return 0;
        }
    }

    private static void PushAccountInfo(lua_State state, WowBattleNetAccountInfoState? account)
    {
        if (account is null)
        {
            lua_pushnil(state);
            return;
        }

        lua_createtable(state, 0, 15);
        SetNumber(state, "bnetAccountID", account.AccountId);
        SetString(state, "accountName", account.AccountName);
        SetString(state, "battleTag", account.BattleTag);
        SetBoolean(state, "isFriend", account.IsFriend);
        SetBoolean(state, "isBattleTagFriend", account.IsBattleTagFriend);
        SetNumber(state, "lastOnlineTime", account.LastOnlineTime);
        SetBoolean(state, "isAFK", account.IsAfk);
        SetBoolean(state, "isDND", account.IsDnd);
        SetBoolean(state, "isFavorite", account.IsFavorite);
        SetBoolean(state, "appearOffline", account.AppearOffline);
        SetString(state, "customMessage", account.CustomMessage);
        SetNumber(state, "customMessageTime", account.CustomMessageTime);
        SetString(state, "note", account.Note);
        SetNumber(state, "rafLinkType", account.RafLinkType);
        PushGameAccountInfo(state, account.GameAccountInfo);
        lua_setfield(state, -2, "gameAccountInfo");
    }

    private static void PushGameAccountInfo(
        lua_State state,
        WowBattleNetGameAccountInfoState account)
    {
        lua_createtable(state, 0, 22);
        SetNumber(state, "gameAccountID", account.GameAccountId);
        SetString(state, "clientProgram", account.ClientProgram);
        SetBoolean(state, "isOnline", account.IsOnline);
        SetBoolean(state, "isGameBusy", account.IsGameBusy);
        SetBoolean(state, "isGameAFK", account.IsGameAfk);
        SetOptionalNumber(state, "wowProjectID", account.WowProjectId);
        SetOptionalString(state, "characterName", account.CharacterName);
        SetOptionalString(state, "realmName", account.RealmName);
        SetOptionalString(state, "realmDisplayName", account.RealmDisplayName);
        SetOptionalNumber(state, "realmID", account.RealmId);
        SetOptionalString(state, "factionName", account.FactionName);
        SetOptionalString(state, "raceName", account.RaceName);
        SetOptionalNumber(state, "classID", account.ClassId);
        SetOptionalString(state, "className", account.ClassName);
        SetOptionalString(state, "areaName", account.AreaName);
        SetOptionalNumber(state, "characterLevel", account.CharacterLevel);
        SetOptionalString(state, "richPresence", account.RichPresence);
        SetOptionalString(state, "playerGuid", account.PlayerGuid);
        SetBoolean(state, "canSummon", account.CanSummon);
        SetBoolean(state, "hasFocus", account.HasFocus);
        SetNumber(state, "regionID", account.RegionId);
        SetBoolean(state, "isInCurrentRegion", account.IsInCurrentRegion);
        SetOptionalNumber(state, "timerunningSeasonID", account.TimerunningSeasonId);
    }

    private static string RequiredString(lua_State state, int index, string usage)
    {
        if (lua_type(state, index) != LUA_TSTRING)
        {
            luaL_error(state, usage);
            return string.Empty;
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static uint RequiredUInt32(lua_State state, int index, string usage)
    {
        if (lua_isnumber(state, index) == 0)
            return (uint)luaL_error(state, usage);
        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue || value != Math.Truncate(value))
            return (uint)luaL_error(state, usage);
        return (uint)value;
    }

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalString(lua_State state, string name, string? value)
    {
        if (value is null)
            return;
        SetString(state, name, value);
    }

    private static void SetNumber(lua_State state, string name, double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalNumber(lua_State state, string name, int? value)
    {
        if (value is null)
            return;
        SetNumber(state, name, value.Value);
    }

    private static void SetBoolean(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }
}
