using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowVoiceChatApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "BeginLocalCapture",
        "CanAccessSettings",
        "CanPlayerUseVoiceChat",
        "EndLocalCapture",
        "GetAvailableInputDevices",
        "GetAvailableOutputDevices",
        "GetCommunicationMode",
        "GetInputVolume",
        "GetMasterVolumeScale",
        "GetOutputVolume",
        "GetVADSensitivity",
        "IsEnabled",
        "IsLoggedIn",
        "IsVoiceChatConnected",
        "SetCommunicationMode",
        "SetInputDevice",
        "SetInputVolume",
        "SetMasterVolumeScale",
        "SetOutputDevice",
        "SetOutputVolume",
        "SetVADSensitivity"
    ];

    public override void Register(lua_State state)
    {
        RegisterEnums(state);

        lua_newtable(state);
        foreach (var function in Functions)
        {
            lua_pushstring(state, function);
            lua_pushcclosure(state, Callback, 1);
            lua_setfield(state, -2, function);
        }
        lua_setglobal(state, "C_VoiceChat");
    }

    private static int Dispatch(lua_State state)
    {
        var voiceChat = LuaBindings.GetRuntime(state).VoiceChat;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "BeginLocalCapture":
                if (!TryReadRequiredBoolean(state, 1, out var listenToLocalUser))
                    return luaL_error(
                        state,
                        "Usage: C_VoiceChat.BeginLocalCapture(listenToLocalUser)");
                voiceChat.IsCapturingLocally = true;
                voiceChat.ListenToLocalUser = listenToLocalUser;
                return 0;
            case "EndLocalCapture":
                voiceChat.IsCapturingLocally = false;
                return 0;
            case "CanAccessSettings":
                return PushBoolean(state, voiceChat.CanAccessSettings);
            case "CanPlayerUseVoiceChat":
                return PushBoolean(state, voiceChat.CanPlayerUseVoiceChat);
            case "IsEnabled":
                return PushBoolean(state, voiceChat.IsEnabled);
            case "IsLoggedIn":
                return PushBoolean(state, voiceChat.IsLoggedIn);
            case "IsVoiceChatConnected":
                return PushBoolean(state, voiceChat.IsConnected);
            case "GetAvailableInputDevices":
                return voiceChat.AreInputDevicesAvailable
                    ? PushDevices(state, voiceChat.InputDevices, voiceChat.InputDeviceId)
                    : PushNil(state);
            case "GetAvailableOutputDevices":
                return voiceChat.AreOutputDevicesAvailable
                    ? PushDevices(state, voiceChat.OutputDevices, voiceChat.OutputDeviceId)
                    : PushNil(state);
            case "GetCommunicationMode":
                return voiceChat.IsCommunicationModeAvailable
                    ? PushNumber(state, voiceChat.CommunicationMode)
                    : PushNil(state);
            case "SetCommunicationMode":
                if (!TryReadCommunicationMode(state, 1, out var communicationMode))
                    return luaL_error(
                        state,
                        "Usage: C_VoiceChat.SetCommunicationMode(communicationMode)");
                voiceChat.CommunicationMode = communicationMode;
                return 0;
            case "GetInputVolume":
                return voiceChat.IsInputVolumeAvailable
                    ? PushNumber(state, voiceChat.InputVolume)
                    : PushNil(state);
            case "SetInputVolume":
                if (!TryReadRequiredInt32(state, 1, out var inputVolume))
                    return luaL_error(state, "Usage: C_VoiceChat.SetInputVolume(volume)");
                voiceChat.InputVolume = Math.Clamp(inputVolume, 0, 100);
                return 0;
            case "GetMasterVolumeScale":
                return PushNumber(state, Math.Clamp(voiceChat.MasterVolumeScale, 0, 1));
            case "SetMasterVolumeScale":
                if (!TryReadRequiredFloat(state, 1, out var masterVolumeScale))
                    return luaL_error(
                        state,
                        "Usage: C_VoiceChat.SetMasterVolumeScale(scale)");
                voiceChat.MasterVolumeScale = Math.Clamp(masterVolumeScale, 0, 1);
                return 0;
            case "GetOutputVolume":
                return voiceChat.IsOutputVolumeAvailable
                    ? PushNumber(state, voiceChat.OutputVolume)
                    : PushNil(state);
            case "SetOutputVolume":
                if (!TryReadRequiredInt32(state, 1, out var outputVolume))
                    return luaL_error(state, "Usage: C_VoiceChat.SetOutputVolume(volume)");
                voiceChat.OutputVolume = Math.Clamp(outputVolume, 0, 100);
                return 0;
            case "GetVADSensitivity":
                return voiceChat.IsVadSensitivityAvailable
                    ? PushNumber(state, voiceChat.VadSensitivity)
                    : PushNil(state);
            case "SetVADSensitivity":
                if (!TryReadRequiredInt32(state, 1, out var vadSensitivity))
                    return luaL_error(
                        state,
                        "Usage: C_VoiceChat.SetVADSensitivity(sensitivity)");
                voiceChat.VadSensitivity = Math.Clamp(vadSensitivity, 0, 100);
                return 0;
            case "SetInputDevice":
                if (!TryReadRequiredString(state, 1, out var inputDeviceId))
                    return luaL_error(
                        state,
                        "Usage: C_VoiceChat.SetInputDevice(deviceID)");
                voiceChat.InputDeviceId = inputDeviceId;
                return 0;
            case "SetOutputDevice":
                if (!TryReadRequiredString(state, 1, out var outputDeviceId))
                    return luaL_error(
                        state,
                        "Usage: C_VoiceChat.SetOutputDevice(deviceID)");
                voiceChat.OutputDeviceId = outputDeviceId;
                return 0;
            default:
                return 0;
        }
    }

    private static int PushDevices(
        lua_State state,
        IReadOnlyList<WowVoiceAudioDeviceState> devices,
        string activeDeviceId)
    {
        lua_newtable(state);
        for (var index = 0; index < devices.Count; index++)
        {
            var device = devices[index];
            lua_newtable(state);
            SetStringField(state, "deviceID", device.DeviceId);
            SetStringField(state, "displayName", device.DisplayName);
            SetBooleanField(
                state,
                "isActive",
                device.IsActive ||
                string.Equals(device.DeviceId, activeDeviceId, StringComparison.Ordinal));
            SetBooleanField(state, "isSystemDefault", device.IsSystemDefault);
            SetBooleanField(state, "isCommsDefault", device.IsCommsDefault);
            lua_rawseti(state, -2, index + 1);
        }

        return 1;
    }

    private static int PushBoolean(lua_State state, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        return 1;
    }

    private static int PushNumber(lua_State state, double value)
    {
        lua_pushnumber(state, value);
        return 1;
    }

    private static int PushNil(lua_State state)
    {
        lua_pushnil(state);
        return 1;
    }

    private static bool TryReadRequiredBoolean(
        lua_State state,
        int index,
        out bool value)
    {
        value = false;
        if (index > lua_gettop(state) || lua_isnil(state, index) != 0)
            return false;
        value = lua_toboolean(state, index) != 0;
        return true;
    }

    private static bool TryReadRequiredInt32(
        lua_State state,
        int index,
        out int value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < int.MinValue or > int.MaxValue)
            return false;
        value = (int)number;
        return true;
    }

    private static bool TryReadRequiredFloat(
        lua_State state,
        int index,
        out double value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number is < -float.MaxValue or > float.MaxValue)
            return false;
        value = (float)number;
        return true;
    }

    private static bool TryReadRequiredString(
        lua_State state,
        int index,
        out string value)
    {
        value = string.Empty;
        if (index > lua_gettop(state) || lua_isstring(state, index) == 0)
            return false;
        value = lua_tostring(state, index) ?? string.Empty;
        return true;
    }

    private static bool TryReadCommunicationMode(
        lua_State state,
        int index,
        out int value)
    {
        if (!TryReadRequiredInt32(state, index, out value))
            return false;
        return value is 0 or 1;
    }

    private static void RegisterEnums(lua_State state)
    {
        lua_getglobal(state, "Enum");
        if (lua_type(state, -1) != LUA_TTABLE)
        {
            lua_pop(state, 1);
            lua_newtable(state);
        }

        lua_newtable(state);
        SetIntegerField(state, "PushToTalk", 0);
        SetIntegerField(state, "OpenMic", 1);
        lua_setfield(state, -2, "CommunicationMode");

        lua_newtable(state);
        SetIntegerField(state, "NumValues", 2);
        SetIntegerField(state, "MinValue", 0);
        SetIntegerField(state, "MaxValue", 1);
        lua_setfield(state, -2, "CommunicationModeMeta");
        lua_setglobal(state, "Enum");
    }

    private static void SetStringField(lua_State state, string name, string value)
    {
        lua_pushstring(state, value);
        lua_setfield(state, -2, name);
    }

    private static void SetBooleanField(lua_State state, string name, bool value)
    {
        lua_pushboolean(state, value ? 1 : 0);
        lua_setfield(state, -2, name);
    }

    private static void SetIntegerField(lua_State state, string name, int value)
    {
        lua_pushinteger(state, value);
        lua_setfield(state, -2, name);
    }
}
