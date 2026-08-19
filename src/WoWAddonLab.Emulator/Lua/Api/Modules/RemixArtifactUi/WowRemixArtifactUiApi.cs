using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowRemixArtifactUiApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly string[] Functions =
    [
        "ClearRemixArtifactItem", "GetAppearanceInfoByID",
        "GetArtifactArtInfo", "GetArtifactItemInfo",
        "GetCurrArtifactItemID", "GetCurrItemSpecIndex",
        "GetCurrTraitTreeID", "ItemInSlotIsRemixArtifact"
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
        lua_setglobal(state, "C_RemixArtifactUI");
    }

    private static int Dispatch(lua_State state)
    {
        var remixArtifact = LuaBindings.GetRuntime(state).RemixArtifactUi;
        var operation =
            lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "ClearRemixArtifactItem":
                remixArtifact.ClearRequests++;
                remixArtifact.CurrentArtifactItemId = null;
                remixArtifact.CurrentTraitTreeId = null;
                remixArtifact.SelectedItemSpecIndex = null;
                remixArtifact.ArtifactArtInfo = null;
                remixArtifact.ArtifactItemInfo = null;
                return 0;
            case "GetAppearanceInfoByID":
            {
                var appearanceId = RequiredUInt32(
                    state,
                    "Usage: local uiCameraID, altHandUICameraID = " +
                    "C_RemixArtifactUI.GetAppearanceInfoByID(" +
                    "artifactAppearanceID)");
                if (!remixArtifact.AppearanceInfoById.TryGetValue(
                        appearanceId,
                        out var appearance))
                {
                    return 0;
                }

                lua_pushnumber(state, appearance.UiCameraId);
                PushOptionalInteger(
                    state,
                    appearance.AltHandUiCameraId);
                return 2;
            }
            case "GetArtifactArtInfo":
                if (remixArtifact.ArtifactArtInfo is not { } artInfo)
                    return 0;
                lua_createtable(state, 0, 4);
                lua_pushstring(state, artInfo.TextureKit);
                lua_setfield(state, -2, "textureKit");
                lua_pushstring(state, artInfo.TitleName);
                lua_setfield(state, -2, "titleName");
                lua_pushnumber(state, artInfo.UiModelSceneId);
                lua_setfield(state, -2, "uiModelSceneID");
                lua_pushnumber(state, artInfo.SpellVisualKitId);
                lua_setfield(state, -2, "spellVisualKitID");
                return 1;
            case "GetArtifactItemInfo":
                if (remixArtifact.ArtifactItemInfo is not { } itemInfo)
                    return 0;
                lua_pushnumber(state, itemInfo.ItemId);
                PushOptionalInteger(state, itemInfo.AltItemId);
                lua_pushnumber(state, itemInfo.ArtifactAppearanceId);
                lua_pushnumber(state, itemInfo.AppearanceModId);
                PushOptionalInteger(state, itemInfo.ItemAppearanceId);
                PushOptionalInteger(
                    state,
                    itemInfo.AltItemAppearanceId);
                lua_pushboolean(state, itemInfo.AltOnTop ? 1 : 0);
                return 7;
            case "GetCurrArtifactItemID":
                PushOptionalInteger(
                    state,
                    remixArtifact.CurrentArtifactItemId);
                return 1;
            case "GetCurrItemSpecIndex":
                PushOptionalInteger(
                    state,
                    remixArtifact.SelectedItemSpecIndex ??
                    remixArtifact.EquippedItemSpecIndex);
                return 1;
            case "GetCurrTraitTreeID":
                PushOptionalInteger(
                    state,
                    remixArtifact.CurrentTraitTreeId);
                return 1;
            case "ItemInSlotIsRemixArtifact":
            {
                var slot = RequiredZeroBasedIndex(
                    state,
                    "Usage: local isRemixArtifact = " +
                    "C_RemixArtifactUI.ItemInSlotIsRemixArtifact(" +
                    "invSlot)");
                lua_pushboolean(
                    state,
                    remixArtifact.TraitTreeIdsByZeroBasedInventorySlot
                        .ContainsKey(slot)
                        ? 1
                        : 0);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static uint RequiredUInt32(
        lua_State state,
        string usage)
    {
        if (lua_gettop(state) < 1 || lua_isnumber(state, 1) == 0)
            return RaiseArgumentError(state, usage);

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) ||
            value < 0 ||
            value > uint.MaxValue)
        {
            return RaiseArgumentError(state, usage);
        }
        return unchecked((uint)value);
    }

    private static uint RequiredZeroBasedIndex(
        lua_State state,
        string usage)
    {
        if (lua_gettop(state) < 1 || lua_isnumber(state, 1) == 0)
            return RaiseArgumentError(state, usage);

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) ||
            value < 0 ||
            value > uint.MaxValue)
        {
            return RaiseArgumentError(state, usage);
        }
        return unchecked((uint)(long)Math.Truncate(value - 1.0));
    }

    private static uint RaiseArgumentError(
        lua_State state,
        string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static void PushOptionalInteger(
        lua_State state,
        int? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushnumber(state, value.Value);
    }
}
