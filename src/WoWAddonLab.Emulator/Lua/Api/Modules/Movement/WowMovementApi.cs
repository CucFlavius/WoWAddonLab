using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowMovementApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "MoveForwardStart", "MoveForwardStop",
        "MoveBackwardStart", "MoveBackwardStop",
        "TurnLeftStart", "TurnLeftStop",
        "TurnRightStart", "TurnRightStop",
        "StrafeLeftStart", "StrafeLeftStop",
        "StrafeRightStart", "StrafeRightStop",
        "JumpOrAscendStart", "AscendStop"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var movement = LuaBindings.GetRuntime(state).Movement;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "MoveForwardStart":
                movement.MovingForward = true;
                break;
            case "MoveForwardStop":
                movement.MovingForward = false;
                break;
            case "MoveBackwardStart":
                movement.MovingBackward = true;
                break;
            case "MoveBackwardStop":
                movement.MovingBackward = false;
                break;
            case "TurnLeftStart":
                movement.TurningLeft = true;
                break;
            case "TurnLeftStop":
                movement.TurningLeft = false;
                break;
            case "TurnRightStart":
                movement.TurningRight = true;
                break;
            case "TurnRightStop":
                movement.TurningRight = false;
                break;
            case "StrafeLeftStart":
                movement.StrafingLeft = true;
                if (movement.StrafeAlsoTurns)
                    movement.TurningLeft = true;
                break;
            case "StrafeLeftStop":
                movement.StrafingLeft = false;
                if (movement.StrafeAlsoTurns)
                    movement.TurningLeft = false;
                break;
            case "StrafeRightStart":
                movement.StrafingRight = true;
                if (movement.StrafeAlsoTurns)
                    movement.TurningRight = true;
                break;
            case "StrafeRightStop":
                movement.StrafingRight = false;
                if (movement.StrafeAlsoTurns)
                    movement.TurningRight = false;
                break;
            case "JumpOrAscendStart":
                movement.Ascending = true;
                break;
            case "AscendStop":
                movement.Ascending = false;
                break;
        }
        return 0;
    }
}
