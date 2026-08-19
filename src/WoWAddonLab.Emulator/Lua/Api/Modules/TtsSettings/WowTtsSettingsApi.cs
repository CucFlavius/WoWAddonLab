using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowTtsSettingsApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly string[] Functions =
    [
        "GetChannelEnabled",
        "GetCharacterSettingsSaved",
        "GetChatTypeEnabled",
        "GetSetting",
        "GetSpeechRate",
        "GetSpeechVolume",
        "GetVoiceOptionID",
        "GetVoiceOptionName",
        "MarkCharacterSettingsSaved",
        "SetChannelEnabled",
        "SetChannelKeyEnabled",
        "SetChatTypeEnabled",
        "SetDefaultSettings",
        "SetSetting",
        "SetSpeechRate",
        "SetSpeechVolume",
        "SetVoiceOption",
        "SetVoiceOptionName",
        "ShouldOverrideMessage"
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
        lua_setglobal(state, "C_TTSSettings");
    }

    private static int Dispatch(lua_State state)
    {
        var settings = LuaBindings.GetRuntime(state).TtsSettings;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "GetSpeechRate":
                lua_pushinteger(state, settings.SpeechRate);
                return 1;
            case "SetSpeechRate":
            {
                var value = RequiredInt32(
                    state,
                    1,
                    "Usage: C_TTSSettings.SetSpeechRate(newVal)");
                if (value is >= -10 and <= 10)
                {
                    settings.SpeechRate = value;
                }
                return 0;
            }
            case "GetSpeechVolume":
                lua_pushnumber(state, settings.SpeechVolume);
                return 1;
            case "SetSpeechVolume":
            {
                var value = RequiredUInt32(
                    state,
                    1,
                    "Usage: C_TTSSettings.SetSpeechVolume(newVal)");
                if (value <= 100)
                {
                    settings.SpeechVolume = value;
                }
                return 0;
            }
            case "GetSetting":
            {
                var setting = RequiredEnum(
                    state,
                    1,
                    4,
                    "Usage: local enabled = C_TTSSettings.GetSetting(setting)");
                settings.BooleanSettings.TryGetValue(
                    setting,
                    out var booleanSetting);
                lua_pushboolean(
                    state,
                    booleanSetting ? 1 : 0);
                return 1;
            }
            case "SetSetting":
            {
                var setting = RequiredEnum(
                    state,
                    1,
                    4,
                    "Usage: C_TTSSettings.SetSetting(setting [, newVal])");
                settings.BooleanSettings[setting] = OptionalBoolean(
                    state,
                    2,
                    "Usage: C_TTSSettings.SetSetting(setting [, newVal])");
                return 0;
            }
            case "GetVoiceOptionID":
            {
                var voiceType = RequiredEnum(
                    state,
                    1,
                    1,
                    "Usage: local voiceID = C_TTSSettings.GetVoiceOptionID(voiceType)");
                settings.VoiceOptionIds.TryGetValue(
                    voiceType,
                    out var voiceOptionId);
                lua_pushnumber(state, voiceOptionId);
                return 1;
            }
            case "SetVoiceOption":
            {
                const string usage =
                    "Usage: C_TTSSettings.SetVoiceOption(voiceType, voiceID)";
                var voiceType = RequiredEnum(state, 1, 1, usage);
                settings.VoiceOptionIds[voiceType] =
                    RequiredUInt32(state, 2, usage);
                return 0;
            }
            case "GetVoiceOptionName":
            {
                var voiceType = RequiredEnum(
                    state,
                    1,
                    1,
                    "Usage: local voiceName = C_TTSSettings.GetVoiceOptionName(voiceType)");
                if (settings.VoiceOptionNames.TryGetValue(
                        voiceType,
                        out var voiceName))
                {
                    lua_pushstring(state, voiceName);
                }
                else
                {
                    lua_pushnil(state);
                }
                return 1;
            }
            case "SetVoiceOptionName":
            {
                const string usage =
                    "Usage: C_TTSSettings.SetVoiceOptionName(voiceType, voiceName)";
                var voiceType = RequiredEnum(state, 1, 1, usage);
                settings.VoiceOptionNames[voiceType] =
                    RequiredString(state, 2, usage);
                return 0;
            }
            case "GetCharacterSettingsSaved":
                lua_pushboolean(state, settings.CharacterSettingsSaved ? 1 : 0);
                return 1;
            case "MarkCharacterSettingsSaved":
                settings.CharacterSettingsSaved = true;
                return 0;
            case "GetChatTypeEnabled":
            {
                var chatName = RequiredString(
                    state,
                    1,
                    "Usage: local enabled = C_TTSSettings.GetChatTypeEnabled(chatName)");
                settings.ChatTypes.TryGetValue(
                    chatName,
                    out var chatTypeEnabled);
                lua_pushboolean(state, chatTypeEnabled ? 1 : 0);
                return 1;
            }
            case "SetChatTypeEnabled":
            {
                const string usage =
                    "Usage: C_TTSSettings.SetChatTypeEnabled(chatName [, newVal])";
                settings.ChatTypes[RequiredString(state, 1, usage)] =
                    OptionalBoolean(state, 2, usage);
                return 0;
            }
            case "GetChannelEnabled":
            {
                var channelInfo = RequiredChannelInfo(
                    state,
                    "Usage: local enabled = C_TTSSettings.GetChannelEnabled(channelInfo)");
                settings.Channels.TryGetValue(
                    channelInfo.Key,
                    out var channelEnabled);
                lua_pushboolean(state, channelEnabled ? 1 : 0);
                return 1;
            }
            case "SetChannelEnabled":
            {
                const string usage =
                    "Usage: C_TTSSettings.SetChannelEnabled(channelInfo [, newVal])";
                var channelInfo = RequiredChannelInfo(state, usage);
                settings.Channels[channelInfo.Key] =
                    OptionalBoolean(state, 2, usage);
                return 0;
            }
            case "SetChannelKeyEnabled":
            {
                const string usage =
                    "Usage: C_TTSSettings.SetChannelKeyEnabled(channelKey [, newVal])";
                settings.Channels[RequiredString(state, 1, usage)] =
                    OptionalBoolean(state, 2, usage);
                return 0;
            }
            case "SetDefaultSettings":
                settings.ResetToDefaults();
                return 0;
            case "ShouldOverrideMessage":
            {
                const string usage =
                    "Usage: local overrideMessage = C_TTSSettings.ShouldOverrideMessage(language, messageText)";
                var language = RequiredUInt32(state, 1, usage);
                var message = RequiredString(state, 2, usage);
                lua_pushboolean(
                    state,
                    settings.OverrideMessages.Contains((language, message))
                        ? 1
                        : 0);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static WowTtsChannelInfoState RequiredChannelInfo(
        lua_State state,
        string usage)
    {
        if (lua_istable(state, 1) == 0)
        {
            luaL_error(state, usage);
        }

        return new WowTtsChannelInfoState(
            RequiredTableString(state, "name", usage),
            RequiredTableString(state, "shortcut", usage),
            RequiredTableInt32(state, "localID", usage),
            RequiredTableUInt32(state, "instanceID", usage),
            RequiredTableInt32(state, "zoneChannelID", usage),
            RequiredTableEnum(state, "channelType", 3, usage));
    }

    private static string RequiredTableString(
        lua_State state,
        string field,
        string usage)
    {
        lua_getfield(state, 1, field);
        if (lua_type(state, -1) != LUA_TSTRING)
        {
            lua_pop(state, 1);
            luaL_error(state, usage);
            return string.Empty;
        }
        var value = lua_tostring(state, -1) ?? string.Empty;
        lua_pop(state, 1);
        return value;
    }

    private static int RequiredTableInt32(
        lua_State state,
        string field,
        string usage)
    {
        lua_getfield(state, 1, field);
        var value = ReadInt32(state, -1, usage);
        lua_pop(state, 1);
        return value;
    }

    private static uint RequiredTableUInt32(
        lua_State state,
        string field,
        string usage)
    {
        lua_getfield(state, 1, field);
        var value = ReadUInt32(state, -1, usage);
        lua_pop(state, 1);
        return value;
    }

    private static int RequiredTableEnum(
        lua_State state,
        string field,
        int maximum,
        string usage)
    {
        var value = RequiredTableInt32(state, field, usage);
        if (value < 0 || value > maximum)
        {
            luaL_error(state, usage);
        }
        return value;
    }

    private static int RequiredInt32(
        lua_State state,
        int index,
        string usage) =>
        ReadInt32(state, index, usage);

    private static int ReadInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
        }
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) ||
            number < int.MinValue ||
            number > int.MaxValue)
        {
            luaL_error(state, usage);
        }
        return (int)number;
    }

    private static uint RequiredUInt32(
        lua_State state,
        int index,
        string usage) =>
        ReadUInt32(state, index, usage);

    private static uint ReadUInt32(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_isnumber(state, index) == 0)
        {
            luaL_error(state, usage);
        }
        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number < 0 || number > uint.MaxValue)
        {
            luaL_error(state, usage);
        }
        return (uint)number;
    }

    private static int RequiredEnum(
        lua_State state,
        int index,
        int maximum,
        string usage)
    {
        var value = RequiredInt32(state, index, usage);
        if (value < 0 || value > maximum)
        {
            luaL_error(state, usage);
        }
        return value;
    }

    private static string RequiredString(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) != LUA_TSTRING)
        {
            luaL_error(state, usage);
        }
        return lua_tostring(state, index) ?? string.Empty;
    }

    private static bool OptionalBoolean(
        lua_State state,
        int index,
        string usage)
    {
        if (lua_type(state, index) is LUA_TNONE or LUA_TNIL)
        {
            return false;
        }
        if (lua_type(state, index) != LUA_TBOOLEAN)
        {
            luaL_error(state, usage);
        }
        return lua_toboolean(state, index) != 0;
    }
}
