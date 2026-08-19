using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowAccountApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        RegisterExpansionConstants(state);
        LuaBindings.RegisterClosureGlobal(state, "IsTrialAccount", Callback);
        LuaBindings.RegisterClosureGlobal(state, "IsVeteranTrialAccount", Callback);
        LuaBindings.RegisterClosureGlobal(state, "IsRestrictedAccount", Callback);
        LuaBindings.RegisterClosureGlobal(state, "IsAccountSecured", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetExpansionTrialInfo", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetExpansionDisplayInfo", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetRestrictedAccountData", Callback);
        LuaBindings.RegisterClosureGlobal(state, "GetClassicExpansionLevel", Callback);
        LuaBindings.RegisterClosureGlobal(state, "ClassicExpansionAtLeast", Callback);
        LuaBindings.RegisterClosureGlobal(state, "ClassicExpansionAtMost", Callback);
        foreach (var function in new[]
                 {
                     "GetAccountExpansionLevel",
                     "GetClientDisplayExpansionLevel",
                     "GetCurrentRegionName",
                     "GetExpansionLevel",
                     "GetMaxLevelForExpansionLevel",
                     "GetMaxLevelForLatestExpansion",
                     "GetMaxLevelForPlayerExpansion",
                     "GetMaxPlayerLevel",
                     "GetMaximumExpansionLevel",
                     "GetMinimumExpansionLevel",
                     "GetNumExpansions",
                     "GetServerExpansionLevel"
                 })
            LuaBindings.RegisterClosureGlobal(state, function, Callback);

        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetIDFromBattleNetAccountGUID",
                     "IsGUIDBattleNetAccountType",
                     "IsGUIDRelatedToLocalAccount"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_AccountInfo");
    }

    private static void RegisterExpansionConstants(lua_State state)
    {
        var expansions = new (string Name, int Value)[]
        {
            ("LE_EXPANSION_CLASSIC", 0),
            ("LE_EXPANSION_BURNING_CRUSADE", 1),
            ("LE_EXPANSION_WRATH_OF_THE_LICH_KING", 2),
            ("LE_EXPANSION_CATACLYSM", 3),
            ("LE_EXPANSION_MISTS_OF_PANDARIA", 4),
            ("LE_EXPANSION_WARLORDS_OF_DRAENOR", 5),
            ("LE_EXPANSION_LEGION", 6),
            ("LE_EXPANSION_BATTLE_FOR_AZEROTH", 7),
            ("LE_EXPANSION_SHADOWLANDS", 8),
            ("LE_EXPANSION_DRAGONFLIGHT", 9),
            ("LE_EXPANSION_WAR_WITHIN", 10),
            ("LE_EXPANSION_MIDNIGHT", 11)
        };
        foreach (var (name, value) in expansions)
        {
            lua_pushinteger(state, value);
            lua_setglobal(state, name);
        }

        lua_pushinteger(state, expansions[^1].Value);
        lua_setglobal(state, "LE_EXPANSION_CURRENT");
        lua_pushinteger(state, expansions[^1].Value);
        lua_setglobal(state, "LE_EXPANSION_LAST");
        lua_pushinteger(state, expansions[^1].Value);
        lua_setglobal(state, "LE_EXPANSION_LEVEL_CURRENT");
        lua_pushinteger(state, expansions[^2].Value);
        lua_setglobal(state, "LE_EXPANSION_LEVEL_PREVIOUS");
        lua_pushinteger(state, expansions[^1].Value);
        lua_setglobal(state, "NUM_LE_EXPANSION_LEVELS");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var account = runtime.Account;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation == "GetIDFromBattleNetAccountGUID")
        {
            var guid = RequiredString(
                state,
                1,
                "Usage: local id = C_AccountInfo.GetIDFromBattleNetAccountGUID(battleNetAccountGUID)");
            account.BattleNetAccountIdsByGuid.TryGetValue(guid, out var accountId);
            lua_pushnumber(state, accountId);
            return 1;
        }
        if (operation == "IsGUIDBattleNetAccountType")
        {
            var guid = RequiredString(
                state,
                1,
                "Usage: local isBNet = C_AccountInfo.IsGUIDBattleNetAccountType(guid)");
            lua_pushboolean(state, account.BattleNetAccountIdsByGuid.ContainsKey(guid) ? 1 : 0);
            return 1;
        }
        if (operation == "IsGUIDRelatedToLocalAccount")
        {
            var guid = RequiredString(
                state,
                1,
                "Usage: local isLocalUser = C_AccountInfo.IsGUIDRelatedToLocalAccount(guid)");
            var isRelated = runtime.Units.Player.Guid.Equals(
                                guid,
                                StringComparison.OrdinalIgnoreCase) ||
                            account.RelatedGuids.Contains(guid);
            lua_pushboolean(state, isRelated ? 1 : 0);
            return 1;
        }
        if (operation == "GetCurrentRegionName")
        {
            lua_pushstring(
                state,
                WowLocalizationApi.RegionName(runtime.Localization.CurrentRegion));
            return 1;
        }
        if (operation == "GetExpansionTrialInfo")
        {
            var active = account.IsExpansionTrial &&
                         account.ExpansionTrialRemainingSeconds is > 0;
            lua_pushboolean(state, active ? 1 : 0);
            if (active && account.ExpansionTrialRemainingSeconds is { } remaining)
                lua_pushnumber(state, remaining);
            else
                lua_pushnil(state);
            return 2;
        }
        if (operation == "GetExpansionDisplayInfo")
        {
            const string usage =
                "Usage: local info = GetExpansionDisplayInfo(expansionLevel [, desiredReleaseType])";
            if (!TryReadRequiredUInt32(state, 1, out var expansionLevel))
                return luaL_error(state, usage);
            if (!TryReadOptionalInt32(state, 2, out var releaseType))
                return luaL_error(state, usage);

            WowExpansionDisplayInfoState? info = null;
            if (releaseType is { } requestedReleaseType)
            {
                account.ExpansionDisplayInfoByLevelAndReleaseType.TryGetValue(
                    (expansionLevel, requestedReleaseType),
                    out info);
            }
            if (info is null)
            {
                account.ExpansionDisplayInfoByLevel.TryGetValue(
                    expansionLevel,
                    out info);
            }
            if (info is null)
            {
                lua_pushnil(state);
                return 1;
            }

            PushExpansionDisplayInfo(state, info);
            return 1;
        }
        if (operation == "GetRestrictedAccountData")
        {
            lua_pushinteger(state, account.RestrictedMaximumLevel);
            lua_pushnumber(state, account.RestrictedMaximumMoney);
            lua_pushinteger(state, account.RestrictedProfessionCap);
            return 3;
        }
        if (operation == "GetClassicExpansionLevel")
        {
            lua_pushinteger(state, account.ExpansionLevel);
            return 1;
        }
        if (operation == "ClassicExpansionAtLeast")
        {
            if (!TryReadRequiredUInt32(state, 1, out _))
            {
                return luaL_error(
                    state,
                    "Usage: local isAtLeast = ClassicExpansionAtLeast(expansionLevel)");
            }

            lua_pushboolean(state, 1);
            return 1;
        }
        if (operation == "ClassicExpansionAtMost")
        {
            if (!TryReadRequiredUInt32(state, 1, out _))
            {
                return luaL_error(
                    state,
                    "Usage: local isAtMost = ClassicExpansionAtMost(expansionLevel)");
            }

            lua_pushboolean(state, 0);
            return 1;
        }
        if (operation == "GetMaxLevelForExpansionLevel")
        {
            if (!TryReadRequiredUInt32(state, 1, out var expansion))
            {
                return luaL_error(
                    state,
                    "Usage: local maxLevel = GetMaxLevelForExpansionLevel(expansionLevel)");
            }

            lua_pushinteger(
                state,
                expansion < account.NumberOfExpansions &&
                account.MaximumLevelByExpansion.TryGetValue(expansion, out var maximumLevel)
                    ? maximumLevel
                    : 0);
            return 1;
        }
        var number = operation switch
        {
            "GetAccountExpansionLevel" => account.AccountExpansionLevel,
            "GetClientDisplayExpansionLevel" => account.ClientDisplayExpansionLevel,
            "GetExpansionLevel" => account.ExpansionLevel,
            "GetMaxLevelForLatestExpansion" =>
                account.ServerExpansionLevel >= 0 &&
                account.ServerExpansionLevel < account.NumberOfExpansions &&
                account.MaximumLevelByExpansion.TryGetValue(
                    (uint)account.ServerExpansionLevel,
                    out var latestMaximumLevel)
                    ? latestMaximumLevel
                    : 0,
            "GetMaxLevelForPlayerExpansion" => account.MaximumLevelForPlayerExpansion,
            "GetMaxPlayerLevel" => account.MaximumPlayerLevel,
            "GetMaximumExpansionLevel" => account.MaximumExpansionLevel,
            "GetMinimumExpansionLevel" => account.MinimumExpansionLevel,
            "GetNumExpansions" => account.NumberOfExpansions,
            "GetServerExpansionLevel" => account.ServerExpansionLevel,
            _ => int.MinValue
        };
        if (number != int.MinValue)
        {
            lua_pushinteger(state, number);
            return 1;
        }
        var result = operation switch
        {
            "IsTrialAccount" => account.IsTrial,
            "IsVeteranTrialAccount" => account.IsVeteranTrial,
            "IsAccountSecured" => account.IsAccountSecured,
            _ => account.IsRestricted
        };
        lua_pushboolean(state, result ? 1 : 0);
        return 1;
    }

    private static bool TryReadRequiredUInt32(
        lua_State state,
        int index,
        out uint value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < uint.MinValue or > uint.MaxValue)
            return false;
        value = (uint)number;
        return true;
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

    private static bool TryReadOptionalInt32(
        lua_State state,
        int index,
        out int? value)
    {
        value = null;
        if (index > lua_gettop(state) || lua_isnoneornil(state, index) != 0)
            return true;
        if (lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < int.MinValue or > int.MaxValue)
            return false;
        value = (int)number;
        return true;
    }

    private static void PushExpansionDisplayInfo(
        lua_State state,
        WowExpansionDisplayInfoState info)
    {
        lua_createtable(state, 0, 9);
        PushIntegerField(state, "logo", info.Logo);
        PushIntegerField(state, "banner", info.Banner);

        lua_createtable(state, info.Features.Count, 0);
        for (var index = 0; index < info.Features.Count; index++)
        {
            var feature = info.Features[index];
            lua_createtable(state, 0, 2);
            PushIntegerField(state, "icon", feature.Icon);
            lua_pushstring(state, feature.Text);
            lua_setfield(state, -2, "text");
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "features");

        PushIntegerField(state, "highResBackgroundID", info.HighResBackgroundId);
        PushIntegerField(state, "lowResBackgroundID", info.LowResBackgroundId);
        lua_pushstring(state, info.TextureKit);
        lua_setfield(state, -2, "textureKit");
        PushOptionalIntegerField(state, "glueAmbianceSoundKit", info.GlueAmbianceSoundKit);
        PushOptionalIntegerField(state, "glueMusicSoundKit", info.GlueMusicSoundKit);
        PushOptionalIntegerField(state, "glueCreditsSoundKit", info.GlueCreditsSoundKit);
    }

    private static void PushIntegerField(lua_State state, string name, long value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, name);
    }

    private static void PushOptionalIntegerField(
        lua_State state,
        string name,
        int? value)
    {
        if (value is { } integer)
            lua_pushinteger(state, integer);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, name);
    }
}
