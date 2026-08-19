using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowProfessionApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    public override void Register(lua_State state)
    {
        LuaBindings.RegisterClosureGlobal(state, "GetProfessions", Callback);
        lua_newtable(state);
        RegisterFunction(state, "GetNewSpecReminderProfName");
        RegisterFunction(state, "ShouldShowPointsReminder");
        lua_setglobal(state, "C_ProfSpecs");
    }

    private static void RegisterFunction(lua_State state, string name)
    {
        lua_pushstring(state, name);
        lua_pushcclosure(state, Callback, 1);
        lua_setfield(state, -2, name);
    }

    private static int Dispatch(lua_State state)
    {
        var professions = LuaBindings.GetRuntime(state).Professions;
        switch (lua_tostring(state, lua_upvalueindex(1)))
        {
            case "GetNewSpecReminderProfName":
                if (professions.NewSpecReminderProfessionName is { } professionName)
                    lua_pushstring(state, professionName);
                else
                    lua_pushnil(state);
                return 1;
            case "ShouldShowPointsReminder":
                lua_pushboolean(state, professions.ShouldShowPointsReminder ? 1 : 0);
                return 1;
        }

        PushOptional(state, professions.PrimaryProfession1);
        PushOptional(state, professions.PrimaryProfession2);
        PushOptional(state, professions.Archaeology);
        PushOptional(state, professions.Fishing);
        PushOptional(state, professions.Cooking);
        lua_pushnil(state);
        return 6;
    }

    private static void PushOptional(lua_State state, int? value)
    {
        if (value is { } index)
            lua_pushinteger(state, index);
        else
            lua_pushnil(state);
    }
}
