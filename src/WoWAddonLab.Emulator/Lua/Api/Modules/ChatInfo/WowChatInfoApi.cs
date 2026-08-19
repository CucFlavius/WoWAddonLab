using System.Text;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowChatInfoApi : LuaApiModule
{
    private const string PerformEmoteUsage =
        "Usage: local success = C_ChatInfo.PerformEmote(emoteName [, targetName, suppressMoveError])";
    private const string RegisterPrefixUsage =
        "Usage: local result = C_ChatInfo.RegisterAddonMessagePrefix(prefix)";
    private const string SendAddonMessageUsage =
        "Usage: local result = C_ChatInfo.SendAddonMessage(params)";
    private const string SendAddonMessageLoggedUsage =
        "Usage: local result = C_ChatInfo.SendAddonMessageLogged(params)";
    private const string SendChatMessageUsage =
        "Usage: C_ChatInfo.SendChatMessage(params)";

    private static readonly lua_CFunction Callback = Dispatch;

    private static readonly Dictionary<string, int> ChatTypes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["SYSTEM"] = 0,
            ["SAY"] = 1,
            ["PARTY"] = 2,
            ["RAID"] = 3,
            ["GUILD"] = 4,
            ["OFFICER"] = 5,
            ["YELL"] = 6,
            ["WHISPER"] = 7,
            ["EMOTE"] = 10,
            ["CHANNEL"] = 17,
            ["AFK"] = 23,
            ["DND"] = 24,
            ["RAID_WARNING"] = 40,
            ["BN"] = 45,
            ["BN_WHISPER"] = 51,
            ["BN_WHISPER_INFORM"] = 52,
            ["INSTANCE_CHAT"] = 62,
            ["VOICE_TEXT"] = 66
        };

    private static readonly HashSet<int> AddonMessageChatTypes =
        [2, 3, 4, 5, 7, 17, 62];

    private static readonly string[] Functions =
    [
        "AreOutgoingAddonChatMessagesRestricted", "CancelEmote", "PerformEmote",
        "GetNumReservedChatWindows", "IsAddonMessagePrefixRegistered",
        "InChatMessagingLockdown", "IsValidChatLine",
        "RegisterAddonMessagePrefix", "SendAddonMessage",
        "SendAddonMessageLogged", "SendChatMessage", "CanPlayerSpeakLanguage"
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
        lua_setglobal(state, "C_ChatInfo");
    }

    private static int Dispatch(lua_State state)
    {
        var runtime = LuaBindings.GetRuntime(state);
        var chat = runtime.Chat;
        var operation = lua_tostring(state, lua_upvalueindex(1)) ?? string.Empty;
        switch (operation)
        {
            case "AreOutgoingAddonChatMessagesRestricted":
                lua_pushboolean(
                    state,
                    chat.OutgoingAddonChatMessagesRestricted ? 1 : 0);
                return 1;
            case "CancelEmote":
                chat.LastEmoteName = null;
                chat.LastEmoteTarget = null;
                chat.LastEmoteSuppressMoveError = false;
                return 0;
            case "PerformEmote":
            {
                if (!TryRequiredString(state, 1, out var emoteName))
                    return luaL_error(state, PerformEmoteUsage);
                if (!TryOptionalString(state, 2, out var targetName))
                    return luaL_error(state, PerformEmoteUsage);

                chat.LastEmoteName = emoteName;
                chat.LastEmoteTarget = targetName;
                chat.LastEmoteSuppressMoveError = lua_toboolean(state, 3) != 0;
                lua_pushboolean(
                    state,
                    chat.CanPerformEmotes && emoteName.Length > 0 ? 1 : 0);
                return 1;
            }
            case "GetNumReservedChatWindows":
                lua_pushinteger(state, 3);
                return 1;
            case "InChatMessagingLockdown":
                lua_pushboolean(state, chat.InChatMessagingLockdown ? 1 : 0);
                return 1;
            case "IsAddonMessagePrefixRegistered":
            {
                if (!TryRequiredString(state, 1, out var prefix))
                {
                    return luaL_error(
                        state,
                        "Usage: local isRegistered = " +
                        "C_ChatInfo.IsAddonMessagePrefixRegistered(prefix)");
                }
                lua_pushboolean(
                    state,
                    chat.RegisteredAddonMessagePrefixes.Contains(prefix)
                        ? 1
                        : 0);
                return 1;
            }
            case "IsValidChatLine":
            {
                if (lua_isnoneornil(state, 1) != 0)
                {
                    lua_pushboolean(state, 0);
                    return 1;
                }
                if (lua_isnumber(state, 1) == 0)
                {
                    return luaL_error(
                        state,
                        "Usage: local isValid = C_ChatInfo.IsValidChatLine([chatLine])");
                }

                var value = lua_tonumber(state, 1);
                var valid = double.IsFinite(value) &&
                            value >= 0 &&
                            value <= ulong.MaxValue &&
                            value == Math.Truncate(value) &&
                            chat.ValidChatLineIds.Contains((ulong)value);
                lua_pushboolean(state, valid ? 1 : 0);
                return 1;
            }
            case "RegisterAddonMessagePrefix":
            {
                if (!TryRequiredString(state, 1, out var prefix))
                    return luaL_error(state, RegisterPrefixUsage);

                var result = RegisterAddonMessagePrefix(chat, prefix);
                lua_pushinteger(state, result);
                return 1;
            }
            case "SendAddonMessage":
                return SendAddonMessage(runtime, state, false);
            case "SendAddonMessageLogged":
                return SendAddonMessage(runtime, state, true);
            case "CanPlayerSpeakLanguage":
            {
                if (!TryRequiredUInt32(state, 1, out var languageId))
                {
                    return luaL_error(
                        state,
                        "Usage: local canSpeakLanguage = C_ChatInfo.CanPlayerSpeakLanguage(languageId)");
                }

                var canSpeak =
                    chat.Languages.Any(language => language.Id == languageId) ||
                    runtime.Client.DefaultLanguageId == languageId ||
                    runtime.Client.AlternativeDefaultLanguageId == languageId;
                lua_pushboolean(state, canSpeak ? 1 : 0);
                return 1;
            }
            case "SendChatMessage":
            {
                if (!TryRequiredString(state, 1, out var message) ||
                    !TryOptionalChatType(state, 2, "SAY", out var chatType, out _) ||
                    !TryOptionalUInt32(state, 3, out var languageId) ||
                    !TryOptionalString(state, 4, out var target))
                {
                    return luaL_error(state, SendChatMessageUsage);
                }

                chat.LastSentChatMessage =
                    new WowSentChatMessageState(message, chatType, languageId, target);
                return 0;
            }
            default:
                return 0;
        }
    }

    private static int RegisterAddonMessagePrefix(WowChatState chat, string prefix)
    {
        var byteLength = Encoding.UTF8.GetByteCount(prefix);
        if (byteLength is 0 or > 16)
            return 2;
        if (chat.RegisteredAddonMessagePrefixes.Contains(prefix))
            return 1;
        if (chat.RegisteredAddonMessagePrefixes.Count >= 512)
            return 3;

        chat.RegisteredAddonMessagePrefixes.Add(prefix);
        return 0;
    }

    private static int SendAddonMessage(
        LuaRuntime runtime,
        lua_State state,
        bool isLogged)
    {
        var usage = isLogged ? SendAddonMessageLoggedUsage : SendAddonMessageUsage;
        if (!TryRequiredString(state, 1, out var prefix) ||
            !TryRequiredString(state, 2, out var message) ||
            !TryOptionalString(state, 3, out var optionalChatType) ||
            !TryOptionalString(state, 4, out var target))
        {
            return luaL_error(state, usage);
        }

        var chatType = optionalChatType ?? "PARTY";
        var hasKnownChatType = ChatTypes.TryGetValue(chatType, out var chatTypeId);
        if (hasKnownChatType)
        {
            chatType = ChatTypes.Keys.First(
                name => name.Equals(chatType, StringComparison.OrdinalIgnoreCase));
        }

        int result;
        var prefixBytes = Encoding.UTF8.GetByteCount(prefix);
        if (prefixBytes is 0 or > 16)
            result = 1;
        else if (message.Length == 0)
            result = 2;
        else if (!hasKnownChatType || !AddonMessageChatTypes.Contains(chatTypeId))
            result = 4;
        else if (runtime.Chat.OutgoingAddonChatMessagesRestricted)
            result = 11;
        else if ((chatTypeId is 2 or 3 or 62) &&
                 runtime.Group.GroupMemberCount <= 1 &&
                 !runtime.Group.IsInRaid)
            result = 5;
        else if ((chatTypeId is 4 or 5) && !runtime.Guild.IsInGuild)
            result = 10;
        else if ((chatTypeId is 7 or 17) && string.IsNullOrEmpty(target))
            result = 6;
        else if (chatTypeId == 17 && !IsKnownChannel(runtime.Chat, target!))
            result = 7;
        else
            result = 0;

        if (result == 0)
        {
            runtime.Chat.LastAddonMessage =
                new WowAddonMessageState(prefix, message, chatType, target, isLogged);
        }

        lua_pushinteger(state, result);
        return 1;
    }

    private static bool IsKnownChannel(WowChatState chat, string target)
    {
        if (!int.TryParse(target, out var channelId) || channelId < 1)
            return false;

        return chat.Windows.Values
            .SelectMany(window => window.Channels)
            .Any(channel => channel.Id == channelId);
    }

    private static bool TryRequiredString(
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

    private static bool TryOptionalString(
        lua_State state,
        int index,
        out string? value)
    {
        value = null;
        if (index > lua_gettop(state) || lua_isnoneornil(state, index) != 0)
            return true;
        if (lua_isstring(state, index) == 0)
            return false;

        value = lua_tostring(state, index);
        return true;
    }

    private static bool TryOptionalChatType(
        lua_State state,
        int index,
        string defaultValue,
        out string value,
        out int id)
    {
        value = defaultValue;
        id = ChatTypes[defaultValue];
        if (index > lua_gettop(state) || lua_isnoneornil(state, index) != 0)
            return true;
        if (lua_isstring(state, index) == 0)
            return false;

        var candidate = lua_tostring(state, index) ?? string.Empty;
        if (!ChatTypes.TryGetValue(candidate, out id))
            return false;

        value = ChatTypes.Keys.First(
            name => name.Equals(candidate, StringComparison.OrdinalIgnoreCase));
        return true;
    }

    private static bool TryRequiredUInt32(
        lua_State state,
        int index,
        out uint value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;

        var number = lua_tonumber(state, index);
        if (!double.IsFinite(number) || number < 0 || number > uint.MaxValue)
            return false;

        value = (uint)number;
        return true;
    }

    private static bool TryOptionalUInt32(
        lua_State state,
        int index,
        out uint? value)
    {
        value = null;
        if (index > lua_gettop(state) || lua_isnoneornil(state, index) != 0)
            return true;
        if (!TryRequiredUInt32(state, index, out var parsed))
            return false;

        value = parsed;
        return true;
    }
}
