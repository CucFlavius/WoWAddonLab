using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowModelInfoApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "AddActiveModelScene",
        "AddActiveModelSceneActor",
        "ClearActiveModelScene",
        "ClearActiveModelSceneActor",
        "GetModelSceneActorDisplayInfoByID",
        "GetModelSceneActorInfoByID",
        "GetModelSceneCameraInfoByID",
        "GetModelSceneInfoByID"
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
        lua_setglobal(state, "C_ModelInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "AddActiveModelScene":
                _ = RequiredObject(
                    runtime,
                    1,
                    "ModelScene",
                    "Usage: C_ModelInfo.AddActiveModelScene(modelSceneFrame, modelSceneID)");
                _ = RequiredUInt32(
                    state,
                    2,
                    "Usage: C_ModelInfo.AddActiveModelScene(modelSceneFrame, modelSceneID)");
                return 0;
            case "AddActiveModelSceneActor":
                _ = RequiredObject(
                    runtime,
                    1,
                    "ModelSceneActor",
                    "Usage: C_ModelInfo.AddActiveModelSceneActor(modelSceneFrameActor, modelSceneActorID)");
                _ = RequiredUInt32(
                    state,
                    2,
                    "Usage: C_ModelInfo.AddActiveModelSceneActor(modelSceneFrameActor, modelSceneActorID)");
                return 0;
            case "ClearActiveModelScene":
                _ = RequiredObject(
                    runtime,
                    1,
                    "ModelScene",
                    "Usage: C_ModelInfo.ClearActiveModelScene(modelSceneFrame)");
                return 0;
            case "ClearActiveModelSceneActor":
                _ = RequiredObject(
                    runtime,
                    1,
                    "ModelSceneActor",
                    "Usage: C_ModelInfo.ClearActiveModelSceneActor(modelSceneFrameActor)");
                return 0;
            case "GetModelSceneInfoByID":
            {
                var id = RequiredProviderId(
                    state,
                    "Usage: local modelSceneType, modelCameraIDs, modelActorsIDs, flags = C_ModelInfo.GetModelSceneInfoByID(modelSceneID)");
                var provider = runtime.ModelInfoProvider;
                if (provider is null || !provider.TryGetScene(id, out var scene))
                    return 0;
                lua_pushinteger(state, unchecked((byte)scene.Type));
                PushIntegerArray(state, scene.CameraIds);
                PushIntegerArray(state, scene.ActorIds);
                lua_pushinteger(state, unchecked((sbyte)scene.Flags));
                return 4;
            }
            case "GetModelSceneActorInfoByID":
            {
                var id = RequiredProviderId(
                    state,
                    "Usage: local actorInfo = C_ModelInfo.GetModelSceneActorInfoByID(modelActorID)");
                var provider = runtime.ModelInfoProvider;
                if (provider is null || !provider.TryGetActor(id, out var actor))
                    return 0;
                PushActor(state, id, actor);
                return 1;
            }
            case "GetModelSceneActorDisplayInfoByID":
            {
                var id = RequiredProviderId(
                    state,
                    "Usage: local actorDisplayInfo = C_ModelInfo.GetModelSceneActorDisplayInfoByID(modelActorDisplayID)");
                var provider = runtime.ModelInfoProvider;
                if (provider is null || !provider.TryGetActorDisplay(id, out var display))
                    return 0;
                PushActorDisplay(state, display);
                return 1;
            }
            case "GetModelSceneCameraInfoByID":
            {
                var id = RequiredProviderId(
                    state,
                    "Usage: local modelSceneCameraInfo = C_ModelInfo.GetModelSceneCameraInfoByID(modelSceneCameraID)");
                var provider = runtime.ModelInfoProvider;
                if (provider is null || !provider.TryGetCamera(id, out var camera))
                    return 0;
                PushCamera(state, id, camera);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static void PushActor(
        lua_State state,
        int requestedId,
        WowModelSceneActorDefinition actor)
    {
        lua_newtable(state);
        SetInteger(state, "modelActorID", requestedId);
        SetString(state, "scriptTag", actor.ScriptTag);
        PushVector(state, actor.Position);
        lua_setfield(state, -2, "position");
        SetNumber(state, "yaw", actor.Yaw);
        SetNumber(state, "pitch", actor.Pitch);
        SetNumber(state, "roll", actor.Roll);
        SetOptionalNumber(
            state,
            "normalizeScaleAggressiveness",
            actor.NormalizeScaleAggressiveness is > 0
                ? actor.NormalizeScaleAggressiveness
                : null);
        SetBoolean(state, "useCenterForOriginX", actor.UseCenterForOriginX);
        SetBoolean(state, "useCenterForOriginY", actor.UseCenterForOriginY);
        SetBoolean(state, "useCenterForOriginZ", actor.UseCenterForOriginZ);
        SetOptionalInteger(
            state,
            "modelActorDisplayID",
            actor.DisplayId is 0 ? null : actor.DisplayId);
    }

    private static void PushActorDisplay(
        lua_State state,
        WowModelSceneActorDisplayDefinition display)
    {
        lua_newtable(state);
        SetInteger(state, "animation", display.Animation);
        SetInteger(state, "animationVariation", display.AnimationVariation);
        SetNumber(state, "animSpeed", display.AnimationSpeed);
        SetOptionalInteger(
            state,
            "animationKitID",
            display.AnimationKitId is 0 ? null : display.AnimationKitId);
        SetOptionalInteger(
            state,
            "spellVisualKitID",
            display.SpellVisualKitId is 0 ? null : display.SpellVisualKitId);
        SetNumber(state, "alpha", display.Alpha);
        SetNumber(state, "scale", display.Scale);
    }

    private static void PushCamera(
        lua_State state,
        int requestedId,
        WowModelSceneCameraDefinition camera)
    {
        lua_newtable(state);
        SetInteger(state, "modelSceneCameraID", requestedId);
        SetString(state, "scriptTag", camera.ScriptTag);
        var isOrbitCamera = unchecked((byte)camera.CameraType) == 0;
        SetString(state, "cameraType", isOrbitCamera ? "OrbitCamera" : string.Empty);
        PushVector(
            state,
            isOrbitCamera ? camera.Target : new WowVector3(0, 0, 0));
        lua_setfield(state, -2, "target");
        SetNumber(state, "yaw", isOrbitCamera ? camera.Yaw : 0);
        SetNumber(state, "pitch", isOrbitCamera ? camera.Pitch : 0);
        SetNumber(state, "roll", isOrbitCamera ? camera.Roll : 0);
        SetNumber(state, "zoomDistance", isOrbitCamera ? camera.ZoomDistance : 0);
        SetNumber(
            state,
            "minZoomDistance",
            isOrbitCamera ? camera.MinZoomDistance : 0);
        SetNumber(
            state,
            "maxZoomDistance",
            isOrbitCamera ? camera.MaxZoomDistance : 0);
        PushVector(
            state,
            isOrbitCamera
                ? camera.ZoomedTargetOffset
                : new WowVector3(0, 0, 0));
        lua_setfield(state, -2, "zoomedTargetOffset");
        SetNumber(
            state,
            "zoomedYawOffset",
            isOrbitCamera ? camera.ZoomedYawOffset : 0);
        SetNumber(
            state,
            "zoomedPitchOffset",
            isOrbitCamera ? camera.ZoomedPitchOffset : 0);
        SetNumber(
            state,
            "zoomedRollOffset",
            isOrbitCamera ? camera.ZoomedRollOffset : 0);
        SetInteger(
            state,
            "flags",
            isOrbitCamera ? unchecked((byte)camera.Flags) : 0);
    }

    private static void PushVector(lua_State state, WowVector3 vector)
    {
        lua_newtable(state);
        var targetIndex = lua_gettop(state);
        lua_getglobal(state, "Vector3DMixin");
        if (lua_istable(state, -1) != 0)
        {
            var mixinIndex = lua_gettop(state);
            lua_pushnil(state);
            while (lua_next(state, mixinIndex) != 0)
            {
                lua_pushvalue(state, -2);
                lua_pushvalue(state, -2);
                lua_rawset(state, targetIndex);
                lua_pop(state, 1);
            }
        }
        lua_pop(state, 1);
        SetNumber(state, "x", vector.X);
        SetNumber(state, "y", vector.Y);
        SetNumber(state, "z", vector.Z);
    }

    private static void PushIntegerArray(lua_State state, IReadOnlyList<int> values)
    {
        lua_createtable(state, values.Count, 0);
        for (var index = 0; index < values.Count; index++)
        {
            lua_pushinteger(state, values[index]);
            lua_rawseti(state, -2, index + 1);
        }
    }

    private static void SetInteger(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalInteger(lua_State state, string name, int? value)
    {
        if (value.HasValue)
            lua_pushinteger(state, value.Value);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, name);
    }

    private static void SetNumber(lua_State state, string name, double value)
    {
        lua_pushnumber(state, (float)value);
        lua_setfield(state, -2, name);
    }

    private static void SetOptionalNumber(lua_State state, string name, double? value)
    {
        if (value.HasValue)
            lua_pushnumber(state, (float)value.Value);
        else
            lua_pushnil(state);
        lua_setfield(state, -2, name);
    }

    private static void SetString(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBoolean(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

    private static int RequiredProviderId(lua_State state, string usage) =>
        unchecked((int)RequiredUInt32(state, 1, usage));

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
            return 0;
        }

        var value = lua_tonumber(state, index);
        if (!double.IsFinite(value) ||
            value is < uint.MinValue or > uint.MaxValue)
        {
            luaL_error(state, usage);
            return 0;
        }

        return unchecked((uint)value);
    }

    private static UiObject RequiredObject(
        LuaRuntime runtime,
        int index,
        string objectType,
        string usage)
    {
        var state = runtime.State;
        var value =
            index <= lua_gettop(state) &&
            lua_istable(state, index) != 0
                ? LuaBindings.GetObject(runtime, index)
                : null;
        if (value is null ||
            !value.ObjectType.Equals(
                objectType,
                StringComparison.OrdinalIgnoreCase))
        {
            luaL_error(state, usage);
        }

        return value!;
    }
}
