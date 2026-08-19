using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCovenantsApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetActiveCovenantID",
                     "GetCovenantData",
                     "GetCovenantIDs"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_Covenants");

        lua_newtable(state);
        lua_pushstring(state, "CloseFromUI");
        lua_pushcclosure(state, Callback, 1);
        lua_setfield(state, -2, "CloseFromUI");
        lua_setglobal(state, "C_CovenantPreview");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var covenants = runtime.Covenants;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetActiveCovenantID":
                lua_pushinteger(state, covenants.ActiveCovenantId);
                return 1;
            case "GetCovenantData":
            {
                var covenantId = RequiredInt32(
                    state,
                    1,
                    "Usage: local data = C_Covenants.GetCovenantData(covenantID)");
                if (!covenants.CovenantDataById.TryGetValue(
                        covenantId,
                        out var covenant))
                {
                    lua_pushnil(state);
                    return 1;
                }

                PushCovenantData(state, covenant);
                return 1;
            }
            case "GetCovenantIDs":
                lua_createtable(state, covenants.CovenantIds.Count, 0);
                for (var index = 0; index < covenants.CovenantIds.Count; index++)
                {
                    lua_pushinteger(state, covenants.CovenantIds[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "CloseFromUI":
                if (covenants.PreviewActive)
                {
                    covenants.PreviewCloseFromUiRequests++;
                }
                else
                {
                    ClearInteraction(runtime.PlayerInteractions, 46);
                }
                return 0;
            default:
                return 0;
        }
    }

    private static int RequiredInt32(lua_State state, int index, string usage)
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

    private static void PushCovenantData(
        lua_State state,
        WowCovenantDataState covenant)
    {
        lua_createtable(state, 0, 15);
        SetInteger(state, "ID", covenant.Id);
        SetOptionalString(state, "textureKit", covenant.TextureKit);
        SetInteger(state, "celebrationSoundKit", covenant.CelebrationSoundKit);
        SetInteger(
            state,
            "animaChannelSelectSoundKit",
            covenant.AnimaChannelSelectSoundKit);
        SetInteger(
            state,
            "animaChannelActiveSoundKit",
            covenant.AnimaChannelActiveSoundKit);
        SetInteger(state, "animaGemsFullSoundKit", covenant.AnimaGemsFullSoundKit);
        SetInteger(state, "animaNewGemSoundKit", covenant.AnimaNewGemSoundKit);
        SetInteger(
            state,
            "animaReinforceSelectSoundKit",
            covenant.AnimaReinforceSelectSoundKit);
        SetInteger(
            state,
            "upgradeTabSelectSoundKitID",
            covenant.UpgradeTabSelectSoundKitId);
        SetInteger(
            state,
            "reservoirFullSoundKitID",
            covenant.ReservoirFullSoundKitId);
        SetInteger(
            state,
            "beginResearchSoundKitID",
            covenant.BeginResearchSoundKitId);
        SetInteger(
            state,
            "renownFanfareSoundKitID",
            covenant.RenownFanfareSoundKitId);
        SetInteger(state, "factionID", covenant.FactionId);
        SetOptionalString(state, "name", covenant.Name);

        lua_createtable(state, covenant.SoulbindIds.Count, 0);
        for (var index = 0; index < covenant.SoulbindIds.Count; index++)
        {
            lua_pushinteger(state, covenant.SoulbindIds[index]);
            lua_rawseti(state, -2, index + 1);
        }
        lua_setfield(state, -2, "soulbindIDs");
    }

    private static void SetInteger(lua_State state, string key, int value)
    {
        lua_pushinteger(state, value);
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

    private static void ClearInteraction(
        WowPlayerInteractionManagerState interactions,
        int interactionType)
    {
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType = interactionType;
        if (!interactions.HasActiveInteraction ||
            interactions.CurrentInteractionType != interactionType)
        {
            return;
        }

        interactions.HasActiveInteraction = false;
        interactions.HasPendingInteraction = false;
        interactions.CurrentInteractionType = 0;
        interactions.PendingInteractionType = 0;
        interactions.ValidNpcInteractionTypes.Clear();
    }
}
