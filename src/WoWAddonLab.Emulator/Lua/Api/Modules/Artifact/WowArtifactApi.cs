using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowArtifactApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "HasArtifactEquipped", Callback);
        lua_newtable(state);
        foreach (var function in new[]
                 {
                     "GetEquippedArtifactArtInfo",
                     "GetEquippedArtifactInfo",
                     "GetArtifactArtInfo",
                     "GetArtifactInfo",
                     "GetAppearanceInfoByID",
                     "GetArtifactTier",
                     "GetTotalPurchasedRanks",
                     "GetNumObtainedArtifacts",
                     "Clear",
                     "IsAtForge",
                     "IsEquippedArtifactDisabled",
                     "IsEquippedArtifactMaxed",
                     "IsViewedArtifactEquipped",
                     "IsArtifactDisabled",
                     "IsMaxedByRulesOrEffect"
                 })
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_ArtifactUI");
    }

    private static int Dispatch(lua_State state)
    {
        var artifact = LuaBindings.GetRuntime(state).Artifact;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "HasArtifactEquipped":
                PushBoolean(
                    state,
                    artifact.HasEquippedArtifact ||
                    artifact.EquippedArtifact is not null);
                return 1;
            case "GetArtifactArtInfo":
                return PushArtInfo(state, artifact.ViewedArtifact?.ArtInfo);
            case "GetEquippedArtifactArtInfo":
                return PushArtInfo(state, artifact.EquippedArtifact?.ArtInfo);
            case "GetArtifactInfo":
                return PushInfo(state, artifact.ViewedArtifact?.Info);
            case "GetEquippedArtifactInfo":
                return PushInfo(state, artifact.EquippedArtifact?.Info);
            case "GetAppearanceInfoByID":
            {
                var appearanceId = RequiredUInt32(
                    state,
                    "Usage: local artifactAppearanceSetID, artifactAppearanceID, " +
                    "appearanceName, displayIndex, unlocked, failureDescription, " +
                    "uiCameraID, altHandCameraID, swatchColor, modelOpacity, " +
                    "modelSaturation, obtainable = " +
                    "C_ArtifactUI.GetAppearanceInfoByID(artifactAppearanceID)");
                if (!artifact.AppearanceInfoById.TryGetValue(
                        appearanceId,
                        out var appearance))
                {
                    return 0;
                }

                lua_pushnumber(state, appearance.ArtifactAppearanceSetId);
                lua_pushnumber(state, appearance.ArtifactAppearanceId);
                lua_pushstring(state, appearance.AppearanceName);
                lua_pushnumber(state, appearance.DisplayIndex);
                PushBoolean(state, appearance.Unlocked);
                PushOptionalString(state, appearance.FailureDescription);
                lua_pushnumber(state, appearance.UiCameraId);
                PushOptionalInteger(state, appearance.AltHandCameraId);
                lua_pushnumber(state, appearance.SwatchColorRed);
                lua_pushnumber(state, appearance.SwatchColorGreen);
                lua_pushnumber(state, appearance.SwatchColorBlue);
                lua_pushnumber(state, appearance.ModelOpacity);
                lua_pushnumber(state, appearance.ModelSaturation);
                PushBoolean(state, appearance.Obtainable);
                return 14;
            }
            case "GetArtifactTier":
                if (artifact.ViewedArtifact is not { } viewedArtifact)
                    lua_pushnil(state);
                else
                    lua_pushinteger(
                        state,
                        viewedArtifact.Tier ??
                        viewedArtifact.Info?.Tier ??
                        0);
                return 1;
            case "GetTotalPurchasedRanks":
            {
                var total = 0;
                foreach (var rank in artifact.PurchasedPowerRanks)
                    total = unchecked(total + rank);
                lua_pushinteger(state, total);
                return 1;
            }
            case "GetNumObtainedArtifacts":
                lua_pushinteger(state, artifact.NumObtainedArtifacts);
                return 1;
            case "Clear":
                artifact.ViewedArtifact = null;
                artifact.ClearCount++;
                return 0;
            case "IsAtForge":
                PushBoolean(state, artifact.IsAtForge);
                return 1;
            case "IsEquippedArtifactDisabled":
                PushBoolean(state, artifact.IsEquippedArtifactDisabled);
                return 1;
            case "IsEquippedArtifactMaxed":
                PushBoolean(state, artifact.IsEquippedArtifactMaxed);
                return 1;
            case "IsViewedArtifactEquipped":
                PushBoolean(
                    state,
                    artifact.ViewedArtifact is { } viewed &&
                    artifact.EquippedArtifact is { } equipped &&
                    string.Equals(
                        viewed.ArtifactGuid,
                        equipped.ArtifactGuid,
                        StringComparison.Ordinal));
                return 1;
            case "IsArtifactDisabled":
                PushBoolean(state, artifact.IsArtifactDisabled);
                return 1;
            case "IsMaxedByRulesOrEffect":
                PushBoolean(state, artifact.IsMaxedByRulesOrEffect);
                return 1;
            default:
                return 0;
        }
    }

    private static int PushArtInfo(
        lua_State state,
        WowArtifactArtInfoState? artInfo)
    {
        if (artInfo is null)
            return 0;

        lua_newtable(state);
        PushOptionalString(state, artInfo.TextureKit);
        lua_setfield(state, -2, "textureKit");
        lua_pushstring(state, artInfo.TitleName);
        lua_setfield(state, -2, "titleName");
        PushColor(state, artInfo.TitleColor);
        lua_setfield(state, -2, "titleColor");
        PushColor(state, artInfo.BarConnectedColor);
        lua_setfield(state, -2, "barConnectedColor");
        PushColor(state, artInfo.BarDisconnectedColor);
        lua_setfield(state, -2, "barDisconnectedColor");
        lua_pushinteger(state, artInfo.UiModelSceneId);
        lua_setfield(state, -2, "uiModelSceneID");
        lua_pushinteger(state, artInfo.SpellVisualKitId);
        lua_setfield(state, -2, "spellVisualKitID");
        return 1;
    }

    private static int PushInfo(
        lua_State state,
        WowArtifactInfoState? info)
    {
        if (info is null)
            return 0;

        lua_pushinteger(state, info.ItemId);
        PushOptionalInteger(state, info.AltItemId);
        lua_pushstring(state, info.Name);
        if (info.IconFileDataId is > 0)
            lua_pushinteger(state, info.IconFileDataId.Value);
        else
            lua_pushnil(state);
        lua_pushnumber(state, info.TotalXp);
        lua_pushinteger(state, info.PointsSpent);
        lua_pushinteger(state, info.Quality);
        lua_pushinteger(state, info.ArtifactAppearanceId);
        lua_pushinteger(state, info.AppearanceModId);
        PushOptionalInteger(state, info.ItemAppearanceId);
        PushOptionalInteger(state, info.AltItemAppearanceId);
        PushBoolean(state, info.AltOnTop);
        lua_pushinteger(state, info.Tier);
        return 13;
    }

    private static void PushColor(
        lua_State state,
        WowArtifactColorState color)
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

        lua_newtable(state);
        lua_pushnumber(state, color.Red);
        lua_setfield(state, -2, "r");
        lua_pushnumber(state, color.Green);
        lua_setfield(state, -2, "g");
        lua_pushnumber(state, color.Blue);
        lua_setfield(state, -2, "b");
        lua_pushnumber(state, color.Alpha);
        lua_setfield(state, -2, "a");
    }

    private static void PushOptionalInteger(lua_State state, int? value)
    {
        if (value is { } integer)
            lua_pushinteger(state, integer);
        else
            lua_pushnil(state);
    }

    private static void PushOptionalString(lua_State state, string? value)
    {
        if (value is not null)
            lua_pushstring(state, value);
        else
            lua_pushnil(state);
    }

    private static void PushBoolean(lua_State state, bool value) =>
        lua_pushboolean(state, value ? 1 : 0);

    private static uint RequiredUInt32(lua_State state, string usage)
    {
        if (lua_gettop(state) < 1 || lua_isnumber(state, 1) == 0)
            return RaiseArgumentError(state, usage);

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) || value < 0 || value > uint.MaxValue)
            return RaiseArgumentError(state, usage);
        return unchecked((uint)value);
    }

    private static uint RaiseArgumentError(lua_State state, string usage)
    {
        luaL_error(state, usage);
        return 0;
    }
}
