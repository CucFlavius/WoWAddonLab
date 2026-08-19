using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCharacterServicesApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        RegisterNamespace(
            state,
            "C_CharacterServices",
            "GetCharacterServiceDisplayData",
            "HasRequiredBoostForClassTrial");
        RegisterNamespace(
            state,
            "C_ClassTrial",
            "GetClassTrialLogoutTimeSeconds", "IsClassTrialCharacter");
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
        if (operation == "GetClassTrialLogoutTimeSeconds")
        {
            lua_pushnumber(state, 0);
            return 1;
        }
        if (operation == "HasRequiredBoostForClassTrial")
        {
            lua_pushboolean(state, 0);
            return 1;
        }
        if (operation == "IsClassTrialCharacter")
        {
            lua_pushboolean(state, 0);
            return 1;
        }

        var runtime = LuaBindings.GetRuntime(state);
        var boostType = lua_type(state, 1) == LUA_TNUMBER
            ? (int)lua_tonumber(state, 1)
            : 0;
        if (runtime.CharacterServiceProvider?.TryGetDisplayData(boostType, out var data) != true)
        {
            data = new WowCharacterServiceDisplayData(
                0,
                0,
                0,
                0,
                string.Empty,
                string.Empty,
                string.Empty,
                0,
                0,
                new WowCharacterServicePopupInfo(string.Empty, string.Empty, string.Empty),
                0,
                null);
        }

        lua_newtable(state);
        SetInteger(state, "boostType", data.BoostType);
        SetInteger(state, "vasType", data.VasType);
        SetInteger(state, "level", data.Level);
        SetInteger(state, "expansion", data.Expansion);
        SetString(state, "tooltipTitle", data.TooltipTitle);
        SetString(state, "tooltipDescription", data.TooltipDescription);
        SetString(state, "flowTitle", data.FlowTitle);
        SetInteger(state, "flags", data.Flags);
        SetInteger(state, "professionLevel", data.ProfessionLevel);
        lua_newtable(state);
        SetString(state, "title", data.PopupInfo.Title);
        SetString(state, "description", data.PopupInfo.Description);
        SetString(state, "textureKit", data.PopupInfo.TextureKit);
        lua_setfield(state, -2, "popupInfo");
        SetInteger(state, "icon", data.IconFileDataId);
        if (data.IconTextureKit is not null)
            SetString(state, "iconTextureKit", data.IconTextureKit);
        return 1;
    }

    private static void SetInteger(lua_State state, string name, long value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }
}
