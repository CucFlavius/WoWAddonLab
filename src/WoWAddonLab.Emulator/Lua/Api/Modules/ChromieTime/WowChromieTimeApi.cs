using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowChromieTimeApi : LuaApiModule
{
    private const int ChromieTimeInteractionType = 45;

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CloseUI",
        "GetChromieTimeExpansionOption",
        "GetChromieTimeExpansionOptions",
        "SelectChromieTimeOption"
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
        lua_setglobal(state, "C_ChromieTime");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var chromieTime = runtime.ChromieTime;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "CloseUI":
                Close(runtime);
                return 0;
            case "GetChromieTimeExpansionOption":
            {
                const string usage =
                    "Usage: local info = C_ChromieTime.GetChromieTimeExpansionOption(expansionRecID)";
                var expansionRecordId = RequiredInt32(state, 1, usage);
                var option = chromieTime.ExpansionOptions.FirstOrDefault(
                    value => value.Id == expansionRecordId);
                if (option is null)
                    lua_pushnil(state);
                else
                    PushExpansionInfo(state, option);
                return 1;
            }
            case "GetChromieTimeExpansionOptions":
                lua_createtable(
                    state,
                    chromieTime.ExpansionOptions.Count,
                    0);
                for (var index = 0;
                     index < chromieTime.ExpansionOptions.Count;
                     index++)
                {
                    PushExpansionInfo(
                        state,
                        chromieTime.ExpansionOptions[index]);
                    lua_rawseti(state, -2, index + 1);
                }
                return 1;
            case "SelectChromieTimeOption":
                chromieTime.LastSelectedExpansionInfoId = RequiredInt32(
                    state,
                    1,
                    "Usage: C_ChromieTime.SelectChromieTimeOption(chromieTimeExpansionInfoId)");
                return 0;
            default:
                return 0;
        }
    }

    private static void Close(LuaRuntime runtime)
    {
        var interactions = runtime.PlayerInteractions;
        interactions.ClearInteractionRequests++;
        interactions.LastClearInteractionType = ChromieTimeInteractionType;
        if (!interactions.HasActiveInteraction ||
            interactions.CurrentInteractionType !=
                ChromieTimeInteractionType)
        {
            return;
        }

        interactions.HasActiveInteraction = false;
        interactions.HasPendingInteraction = false;
        interactions.CurrentInteractionType = 0;
        interactions.PendingInteractionType = 0;
        interactions.ValidNpcInteractionTypes.Clear();
        runtime.TriggerEvent("CHROMIE_TIME_CLOSE");
    }

    private static void PushExpansionInfo(
        lua_State state,
        WowChromieTimeExpansionInfoState option)
    {
        lua_createtable(state, 0, 9);
        SetNumber(state, "id", option.Id);
        SetOptionalString(state, "name", option.Name);
        SetOptionalString(state, "description", option.Description);
        SetOptionalString(state, "mapAtlas", option.MapAtlas);
        SetOptionalString(state, "previewAtlas", option.PreviewAtlas);
        SetBoolean(state, "completed", option.Completed);
        SetBoolean(state, "alreadyOn", option.AlreadyOn);
        SetBoolean(state, "recommended", option.Recommended);
        SetNumber(state, "sortPriority", option.SortPriority);
    }

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

    private static int RaiseArgumentError(lua_State state, string usage)
    {
        luaL_error(state, usage);
        return 0;
    }

    private static void SetNumber(
        lua_State state,
        string field,
        double value)
    {
        lua_pushnumber(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetOptionalString(
        lua_State state,
        string field,
        string? value)
    {
        if (value is null)
            lua_pushnil(state);
        else
            lua_pushstring(state, value);
        lua_setfield(state, -2, field);
    }

    private static void SetBoolean(
        lua_State state,
        string field,
        bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, field);
    }
}
