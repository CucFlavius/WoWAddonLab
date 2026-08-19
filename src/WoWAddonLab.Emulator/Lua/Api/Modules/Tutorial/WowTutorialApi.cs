using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTutorialApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CanResetTutorials",
        "GetNextCompleatedTutorial",
        "GetPrevCompleatedTutorial",
        "GetTutorialsEnabled",
        "IsTutorialFlagged",
        "ResetTutorials",
        "TriggerTutorial"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;

        switch (operation)
        {
            case "CanResetTutorials":
            case "GetTutorialsEnabled":
                lua_pushboolean(state, 1);
                return 1;
            case "IsTutorialFlagged":
            {
                var flag = lua_type(state, 1) == LUA_TNUMBER
                    ? (int)lua_tonumber(state, 1)
                    : 0;
                lua_pushboolean(state, runtime.Client.TutorialFlags.Contains(flag) ? 1 : 0);
                return 1;
            }
            case "ResetTutorials":
                runtime.Client.TutorialFlags.Clear();
                return 0;
            case "TriggerTutorial":
                if (lua_type(state, 1) == LUA_TNUMBER)
                    runtime.Client.TutorialFlags.Add((int)lua_tonumber(state, 1));
                return 0;
            default:
                return 0;
        }
    }
}
