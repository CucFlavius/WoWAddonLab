using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowLegacyFeatureApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "GetNumArchaeologyRaces", Callback);
        LuaBindings.RegisterClosureGlobal(state, "CloseResearch", Callback);
        RegisterNamespace(
            state,
            "C_CovenantSanctumUI",
            "CanAccessReservoir",
            "CanDepositAnima",
            "DepositAnima",
            "EndInteraction",
            "GetAnimaInfo",
            "GetCurrentTalentTreeID",
            "GetFeatures",
            "GetRenownLevel",
            "GetRenownLevels",
            "GetRenownRewardsForLevel",
            "GetSanctumType",
            "GetSoulCurrencies",
            "HasMaximumRenown",
            "IsPlayerInRenownCatchUpMode",
            "IsWeeklyRenownCapped",
            "RequestCatchUpState");
        RegisterNamespace(
            state,
            "C_DelvesUI",
            "GetRoleNodeForCompanion", "GetCurioNodeForCompanion", "GetTraitTreeForCompanion",
            "GetCreatureDisplayInfoForCompanion", "GetTieredEntrancePDEID",
            "GetPlayerCompanionPDEID", "GetFactionForCompanion", "HasActiveDelve", "GetActiveDelveTier",
            "GetDelveEntranceMapID",
            "GetDelveEntranceBackgroundWidgetSetID", "GetDelveEntranceDescriptionString",
            "GetDelveEntranceHeaderString", "GetDelveEntranceTiers",
            "GetDelveEntranceTitleString", "GetDelvesMinRequiredLevel",
            "GetTieredEntranceOptionalAffixTraitTreeID", "IsDelveEntranceTierEnabled",
            "RequestPartyEligibilityForDelveTiers", "SelectDelveEntranceTier",
            "HasActiveTale", "GetActiveTieredEntranceProgress", "RequestClearActiveTale");
        RegisterNamespace(state, "C_FrameManager", "GetFrameVisibilityState");
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
            case "GetSoulCurrencies":
            case "GetFeatures":
            case "GetRenownLevels":
            case "GetRenownRewardsForLevel":
                lua_newtable(state);
                return 1;
            case "GetDelveEntranceTiers":
                lua_newtable(state);
                return 1;
            case "GetNumArchaeologyRaces":
                lua_pushinteger(state, 0);
                return 1;
            case "GetAnimaInfo":
                lua_pushinteger(state, 0);
                lua_pushinteger(state, 0);
                return 2;
            case "GetRenownLevel":
            case "GetDelvesMinRequiredLevel":
            case "GetDelveEntranceMapID":
            case "GetFactionForCompanion":
            case "GetTraitTreeForCompanion":
                lua_pushinteger(state, 0);
                return 1;
            case "CanAccessReservoir":
            case "CanDepositAnima":
            case "HasMaximumRenown":
            case "IsPlayerInRenownCatchUpMode":
            case "IsWeeklyRenownCapped":
            case "CanUpgradeItem":
            case "GetFrameVisibilityState":
            case "HasActiveDelve":
            case "IsDelveEntranceTierEnabled":
                lua_pushboolean(state, 0);
                return 1;
            case "GetCurrentTalentTreeID":
            case "GetSanctumType":
            case "CloseResearch":
            case "DepositAnima":
            case "EndInteraction":
            case "RequestCatchUpState":
            case "RequestPartyEligibilityForDelveTiers":
            case "SelectDelveEntranceTier":
                return 0;
            case "GetDelveEntranceDescriptionString":
            case "GetDelveEntranceHeaderString":
            case "GetDelveEntranceTitleString":
                lua_pushstring(state, string.Empty);
                return 1;
            case "HasActiveTale":
                lua_pushboolean(state, LuaBindings.GetRuntime(state).DelvesTale.HasActiveTale ? 1 : 0);
                return 1;
            case "GetActiveTieredEntranceProgress":
            {
                var progress = LuaBindings.GetRuntime(state).DelvesTale.ActiveTieredEntranceProgress;
                lua_createtable(state, progress.Count, 0);
                for (var index = 0; index < progress.Count; index++)
                {
                    lua_createtable(state, 0, 3);
                    lua_pushinteger(state, progress[index].QuestId);
                    lua_setfield(state, -2, "questID");
                    lua_pushstring(state, progress[index].Description);
                    lua_setfield(state, -2, "description");
                    lua_pushboolean(state, progress[index].Completed ? 1 : 0);
                    lua_setfield(state, -2, "completed");
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            }
            case "RequestClearActiveTale":
                LuaBindings.GetRuntime(state).DelvesTale.ClearActiveTaleRequestCount++;
                return 0;
            default:
                return 0;
        }
    }
}
