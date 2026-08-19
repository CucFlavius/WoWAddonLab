using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCameraApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "CameraZoomIn",
        "CameraZoomOut",
        "GetCameraZoom",
        "MoveViewDownStart",
        "MoveViewDownStop",
        "MoveViewInStart",
        "MoveViewInStop",
        "MoveViewLeftStart",
        "MoveViewLeftStop",
        "MoveViewOutStart",
        "MoveViewOutStop",
        "MoveViewRightStart",
        "MoveViewRightStop",
        "MoveViewUpStart",
        "MoveViewUpStop",
        "ResetView",
        "SaveView",
        "SetView"
    ];

    public override void Register(lua_State state)
    {
        foreach (var function in Functions)
            LuaBindings.RegisterClosureGlobal(state, function, Callback);
    }

    private static int Dispatch(lua_State state)
    {
        var camera = LuaBindings.GetRuntime(state).Camera;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        if (operation == "GetCameraZoom")
        {
            lua_pushnumber(state, camera.Zoom);
            return 1;
        }

        if (operation == "CameraZoomIn" || operation == "CameraZoomOut")
            return Zoom(state, camera, operation == "CameraZoomIn");

        if (TryGetMovementDirection(operation, out var direction))
        {
            if (operation.EndsWith("Start", StringComparison.Ordinal))
                return StartMovement(state, camera, direction);

            camera.ActiveMovements.Remove(direction);
            return 0;
        }

        var usage = $"Usage: {operation}(viewModeIndex)";
        if (lua_isnumber(state, 1) == 0)
            return luaL_error(state, usage);

        var value = lua_tonumber(state, 1);
        if (!double.IsFinite(value) || value < int.MinValue || value > int.MaxValue)
            return luaL_error(state, usage);

        var viewIndex = (int)value;
        if (viewIndex is < 1 or > 5)
            return 0;

        switch (operation)
        {
            case "SaveView":
                camera.SavedViewZooms[viewIndex] = camera.Zoom;
                return 0;
            case "SetView":
                camera.CurrentViewIndex = viewIndex;
                if (camera.SavedViewZooms.TryGetValue(viewIndex, out var zoom))
                    camera.Zoom = zoom;
                return 0;
            case "ResetView":
                camera.SavedViewZooms.Remove(viewIndex);
                if (camera.CurrentViewIndex == viewIndex)
                    camera.CurrentViewIndex = null;
                return 0;
            default:
                return 0;
        }
    }

    private static int Zoom(
        lua_State state,
        WowCameraState camera,
        bool inward)
    {
        var distance = lua_isnumber(state, 1) != 0
            ? (float)lua_tonumber(state, 1)
            : 1f;
        var direction = inward
            ? WowCameraMovementDirection.In
            : WowCameraMovementDirection.Out;
        camera.ActiveMovements[direction] = new WowCameraMovementState(
            direction,
            distance,
            0,
            true);

        if (float.IsFinite(distance))
        {
            camera.Zoom = Math.Clamp(
                camera.Zoom + (inward ? -distance : distance),
                0,
                camera.MaximumZoom);
        }

        return 0;
    }

    private static int StartMovement(
        lua_State state,
        WowCameraState camera,
        WowCameraMovementDirection direction)
    {
        var speed = OptionalNonNegativeNumber(
            state,
            1,
            1,
            "speed cannot be negative");
        var timeout = OptionalNonNegativeNumber(
            state,
            2,
            0,
            "timeout cannot be negative");
        var immediate = lua_toboolean(state, 3) != 0;
        camera.ActiveMovements[direction] = new WowCameraMovementState(
            direction,
            speed,
            timeout,
            immediate);
        return 0;
    }

    private static float OptionalNonNegativeNumber(
        lua_State state,
        int index,
        float defaultValue,
        string negativeError)
    {
        if (lua_isnoneornil(state, index) != 0)
            return defaultValue;
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, $"bad argument #{index} (number expected)");
            return defaultValue;
        }

        var value = (float)lua_tonumber(state, index);
        if (value < 0)
        {
            luaL_error(state, $"bad argument #{index} ({negativeError})");
            return defaultValue;
        }
        return value;
    }

    private static bool TryGetMovementDirection(
        string operation,
        out WowCameraMovementDirection direction)
    {
        direction = operation switch
        {
            "MoveViewInStart" or "MoveViewInStop" => WowCameraMovementDirection.In,
            "MoveViewOutStart" or "MoveViewOutStop" => WowCameraMovementDirection.Out,
            "MoveViewRightStart" or "MoveViewRightStop" => WowCameraMovementDirection.Right,
            "MoveViewLeftStart" or "MoveViewLeftStop" => WowCameraMovementDirection.Left,
            "MoveViewUpStart" or "MoveViewUpStop" => WowCameraMovementDirection.Up,
            "MoveViewDownStart" or "MoveViewDownStop" => WowCameraMovementDirection.Down,
            _ => default
        };
        return operation.StartsWith("MoveView", StringComparison.Ordinal);
    }
}
