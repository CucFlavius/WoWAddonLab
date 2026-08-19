using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowChatApi : LuaApiModule
{
    private static readonly lua_CFunction Callback = Dispatch;
    private static readonly HashSet<string> MessageGroups = new(
    [
        "SYSTEM", "SAY", "PARTY", "RAID", "GUILD", "OFFICER", "YELL",
        "WHISPER", "WHISPER_INFORM", "REPLY", "EMOTE", "TEXT_EMOTE",
        "MONSTER_SAY", "MONSTER_PARTY", "MONSTER_YELL", "MONSTER_WHISPER",
        "MONSTER_EMOTE", "CHANNEL", "CHANNEL_JOIN", "CHANNEL_LEAVE",
        "CHANNEL_LIST", "CHANNEL_NOTICE", "CHANNEL_NOTICE_USER", "AFK", "DND",
        "IGNORED", "SKILL", "LOOT", "MONEY", "OPENING", "TRADESKILLS",
        "PET_INFO", "COMBAT_MISC_INFO", "COMBAT_XP_GAIN", "COMBAT_HONOR_GAIN",
        "COMBAT_FACTION_CHANGE", "BG_SYSTEM_NEUTRAL", "BG_SYSTEM_ALLIANCE",
        "BG_SYSTEM_HORDE", "RAID_LEADER", "RAID_WARNING", "RAID_BOSS_EMOTE",
        "RAID_BOSS_WHISPER", "FILTERED", "BATTLEGROUND",
        "BATTLEGROUND_LEADER", "RESTRICTED", "BN_WHISPER",
        "BN_WHISPER_INFORM", "BN_CONVERSATION", "BN_CONVERSATION_NOTICE",
        "BN_CONVERSATION_LIST", "BN_INLINE_TOAST", "COMMUNITIES_CHANNEL"
    ], StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> FramePoints = new(
    [
        "TOPLEFT", "TOP", "TOPRIGHT", "LEFT", "CENTER", "RIGHT",
        "BOTTOMLEFT", "BOTTOM", "BOTTOMRIGHT"
    ], StringComparer.OrdinalIgnoreCase);

    private static readonly string[] Functions =
    [
        "AddChatWindowChannel",
        "AddChatWindowMessages",
        "GetChatWindowChannels",
        "GetChannelList",
        "GetChatWindowInfo",
        "GetChatWindowMessages",
        "GetChatWindowSavedDimensions",
        "GetChatWindowSavedPosition",
        "GetChannelDisplayInfo",
        "GetLanguageByIndex",
        "GetNumDisplayChannels",
        "GetNumGroupChannels",
        "GetNumLanguages",
        "RemoveChatWindowChannel",
        "RemoveChatWindowMessages",
        "SetChatWindowAlpha",
        "SetChatWindowColor",
        "SetChatWindowDocked",
        "SetChatWindowLocked",
        "SetChatWindowName",
        "SetChatWindowSavedDimensions",
        "SetChatWindowSavedPosition",
        "SetChatWindowShown",
        "SetChatWindowSize",
        "SetChatWindowUninteractable"
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
            case "GetNumDisplayChannels":
                lua_pushinteger(state, runtime.Chat.GetWindow(1).Channels.Count);
                return 1;
            case "GetNumGroupChannels":
                lua_pushinteger(state, runtime.Chat.NumGroupChannels);
                return 1;
            case "GetChannelDisplayInfo":
            {
                if (!TryReadRequiredInt32(state, 1, out var index))
                    return luaL_error(state, "Usage: GetChannelDisplayInfo(index)");
                var channels = runtime.Chat.GetWindow(1).Channels;
                if (index <= 0 || index > channels.Count)
                    return 0;
                var channel = channels[index - 1];
                lua_pushstring(state, channel.Name);
                lua_pushboolean(state, channel.IsHeader ? 1 : 0);
                if (channel.IsHeader)
                    lua_pushboolean(state, channel.IsCollapsed ? 1 : 0);
                else
                    lua_pushnil(state);
                if (channel.IsHeader)
                    lua_pushnil(state);
                else
                    lua_pushinteger(state, channel.Id);
                if (channel.MemberCount != 0)
                    lua_pushinteger(state, channel.MemberCount);
                else
                    lua_pushnil(state);
                if (channel.IsHeader)
                    lua_pushnil(state);
                else
                    lua_pushboolean(state, channel.IsActive ? 1 : 0);
                lua_pushstring(state, channel.Category);
                lua_pushinteger(state, channel.ChannelType);
                return 8;
            }
            case "GetChannelList":
            {
                var channels = runtime.Chat.GetWindow(1).Channels;
                foreach (var channel in channels)
                {
                    lua_pushinteger(state, channel.Id);
                    lua_pushstring(state, channel.Name);
                    lua_pushboolean(state, channel.IsDisabled ? 1 : 0);
                }
                return channels.Count * 3;
            }
            case "GetChatWindowMessages":
            {
                if (!TryWindow(runtime, state, "Usage: GetChatWindowMessages(index)", out var window))
                    return 0;
                foreach (var messageGroup in window.MessageGroups)
                    lua_pushstring(state, messageGroup);
                return window.MessageGroups.Count;
            }
            case "AddChatWindowMessages":
            {
                if (!TryWindowAndString(
                        runtime,
                        state,
                        "Usage: AddChatWindowMessages(index, \"messageGroup\")",
                        out var window,
                        out var messageGroup))
                    return 0;
                if (!MessageGroups.Contains(messageGroup))
                    return 0;
                if (!window.MessageGroups.Contains(messageGroup, StringComparer.OrdinalIgnoreCase))
                    window.MessageGroups.Add(messageGroup);
                return 0;
            }
            case "RemoveChatWindowMessages":
            {
                if (!TryWindowAndString(
                        runtime,
                        state,
                        "Usage: RemoveChatWindowMessages(index, \"messageGroup\")",
                        out var window,
                        out var messageGroup))
                    return 0;
                if (!MessageGroups.Contains(messageGroup))
                    return 0;
                RemoveMatching(window.MessageGroups, messageGroup);
                return 0;
            }
            case "GetChatWindowChannels":
            {
                if (!TryWindow(runtime, state, "Usage: GetChatWindowChannels(index)", out var window))
                    return 0;
                foreach (var channel in window.Channels)
                {
                    lua_pushstring(state, channel.Name);
                    lua_pushinteger(state, channel.Id);
                }
                return window.Channels.Count * 2;
            }
            case "AddChatWindowChannel":
            {
                if (!TryWindowAndString(
                        runtime,
                        state,
                        "Usage: AddChatWindowChannel(index, \"channel\")",
                        out var window,
                        out var channelName))
                    return 0;
                var channel = runtime.Chat.GetWindow(1).Channels.FirstOrDefault(value =>
                    value.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase));
                var channelId = channel?.Id ?? 0;
                if (!window.Channels.Any(value =>
                        value.Name.Equals(channelName, StringComparison.OrdinalIgnoreCase)))
                {
                    window.Channels.Add(channel ?? new WowChatChannelState(channelName, 0));
                }
                lua_pushinteger(state, channelId);
                return 1;
            }
            case "RemoveChatWindowChannel":
            {
                if (!TryWindowAndString(
                        runtime,
                        state,
                        "Usage: RemoveChatWindowChannel(index, \"channel\")",
                        out var window,
                        out var channelName))
                    return 0;
                for (var index = window.Channels.Count - 1; index >= 0; index--)
                {
                    if (window.Channels[index].Name.Equals(
                            channelName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        window.Channels.RemoveAt(index);
                    }
                }
                return 0;
            }
            case "GetChatWindowInfo":
            {
                if (!TryWindow(runtime, state, "Usage: GetChatWindowInfo(index)", out var window))
                    return 0;
                lua_pushstring(state, window.Name);
                lua_pushnumber(state, window.FontSize);
                lua_pushnumber(state, window.Red);
                lua_pushnumber(state, window.Green);
                lua_pushnumber(state, window.Blue);
                lua_pushnumber(state, window.Alpha);
                lua_pushboolean(state, window.Shown ? 1 : 0);
                lua_pushboolean(state, window.Locked ? 1 : 0);
                if (window.DockedOrder is { } dockedOrder)
                    lua_pushinteger(state, dockedOrder);
                else
                    lua_pushnil(state);
                lua_pushboolean(state, window.Uninteractable ? 1 : 0);
                return 10;
            }
            case "GetChatWindowSavedDimensions":
            {
                if (!TryWindow(
                        runtime,
                        state,
                        "Usage: GetChatWindowSavedDimensions(index)",
                        out var window))
                    return 0;
                lua_pushnumber(state, window.SavedWidth);
                lua_pushnumber(state, window.SavedHeight);
                return 2;
            }
            case "GetChatWindowSavedPosition":
            {
                if (!TryWindow(
                        runtime,
                        state,
                        "Usage: GetChatWindowSavedPosition(index)",
                        out var window))
                    return 0;
                if (window.SavedPoint is not { } point)
                    return 0;
                lua_pushstring(state, point);
                lua_pushnumber(state, window.SavedXOffset);
                lua_pushnumber(state, window.SavedYOffset);
                return 3;
            }
            case "SetChatWindowSavedDimensions":
            {
                const string usage =
                    "Usage: SetChatWindowSavedDimensions(index, width, height)";
                if (!TryReadRequiredInt32(state, 1, out var windowIndex) ||
                    !TryReadRequiredNumber(state, 2, out var width) ||
                    !TryReadRequiredNumber(state, 3, out var height))
                    return luaL_error(state, usage);
                if (!TryWindowAtIndex(runtime, windowIndex, out var window))
                    return 0;
                window.SavedWidth = (float)width;
                window.SavedHeight = (float)height;
                return 0;
            }
            case "SetChatWindowSavedPosition":
            {
                const string usage =
                    "Usage: SetChatWindowSavedPosition(index, \"point\", xOffsetRatio, yOffsetRatio)";
                if (!TryReadRequiredInt32(state, 1, out var windowIndex) ||
                    lua_isstring(state, 2) == 0 ||
                    !TryReadRequiredNumber(state, 3, out var xOffset) ||
                    !TryReadRequiredNumber(state, 4, out var yOffset))
                    return luaL_error(state, usage);
                if (!TryWindowAtIndex(runtime, windowIndex, out var window))
                    return 0;
                var point = lua_tostring(state, 2) ?? string.Empty;
                if (!FramePoints.Contains(point))
                    return luaL_error(state, "Unknown Region Point");
                window.SavedPoint = point.ToUpperInvariant();
                window.SavedXOffset = (float)xOffset;
                window.SavedYOffset = (float)yOffset;
                return 0;
            }
            case "SetChatWindowName":
            {
                if (!TryWindow(
                        runtime,
                        state,
                        "Usage: SetChatWindowName(index, \"name\")",
                        out var window))
                    return 0;
                var name = lua_tostring(state, 2) ?? string.Empty;
                window.Name = name.Length > 127 ? name[..127] : name;
                return 0;
            }
            case "SetChatWindowSize":
            {
                const string usage = "Usage: SetChatWindowSize(index, size)";
                if (!TryReadRequiredInt32(state, 1, out var windowIndex) ||
                    !TryReadRequiredNumber(state, 2, out var fontSize))
                    return luaL_error(state, usage);
                if (!TryWindowAtIndex(runtime, windowIndex, out var window))
                    return 0;
                var integerSize = (int)fontSize;
                if (integerSize > 0)
                    window.FontSize = integerSize;
                return 0;
            }
            case "SetChatWindowColor":
            {
                const string usage = "Usage: SetChatWindowColor(index, r, g, b)";
                if (!TryReadRequiredInt32(state, 1, out var windowIndex) ||
                    !TryReadRequiredNumber(state, 2, out var red) ||
                    !TryReadRequiredNumber(state, 3, out var green) ||
                    !TryReadRequiredNumber(state, 4, out var blue))
                    return luaL_error(state, usage);
                if (!TryWindowAtIndex(runtime, windowIndex, out var window))
                    return 0;
                window.Red = QuantizeChatColor(red);
                window.Green = QuantizeChatColor(green);
                window.Blue = QuantizeChatColor(blue);
                return 0;
            }
            case "SetChatWindowAlpha":
            {
                const string usage = "Usage: SetChatWindowAlpha(index, alpha)";
                if (!TryReadRequiredInt32(state, 1, out var windowIndex) ||
                    !TryReadRequiredNumber(state, 2, out var alpha))
                    return luaL_error(state, usage);
                if (!TryWindowAtIndex(runtime, windowIndex, out var window))
                    return 0;
                window.Alpha = QuantizeChatColor(alpha);
                return 0;
            }
            case "SetChatWindowShown":
            {
                if (!TryWindow(
                        runtime,
                        state,
                        "Usage: SetChatWindowShown(index, shown)",
                        out var window))
                    return 0;
                window.Shown = Boolean(state, 2);
                return 0;
            }
            case "SetChatWindowLocked":
            {
                if (!TryWindow(
                        runtime,
                        state,
                        "Usage: SetChatWindowLocked(index, locked)",
                        out var window))
                    return 0;
                window.Locked = Boolean(state, 2);
                return 0;
            }
            case "SetChatWindowDocked":
            {
                if (!TryWindow(
                        runtime,
                        state,
                        "Usage: SetChatWindowDocked(index, docked)",
                        out var window))
                    return 0;
                var dockedOrder = TryReadRequiredNumber(state, 2, out var order)
                    ? (int)order
                    : 0;
                window.DockedOrder = dockedOrder == 0 ? null : dockedOrder;
                return 0;
            }
            case "SetChatWindowUninteractable":
            {
                if (!TryWindow(
                        runtime,
                        state,
                        "Usage: SetChatWindowUninteractable(index, uninteractable)",
                        out var window))
                    return 0;
                window.Uninteractable = Boolean(state, 2);
                return 0;
            }
            case "GetNumLanguages":
                lua_pushinteger(state, Languages(runtime).Count);
                return 1;
            case "GetLanguageByIndex":
            {
                if (!TryReadRequiredInt32(state, 1, out var parsedIndex))
                    return luaL_error(state, "Usage: GetLanguageByIndex(index)");
                var index = parsedIndex - 1;
                var languages = Languages(runtime);
                if (index < 0 || index >= languages.Count)
                    return 0;
                lua_pushstring(state, languages[index].Name);
                lua_pushinteger(state, languages[index].Id);
                return 2;
            }
            default:
                return 0;
        }
    }

    private static bool Boolean(lua_State state, int index) =>
        lua_toboolean(state, index) != 0;

    private static IReadOnlyList<WowLanguageState> Languages(LuaRuntime runtime)
    {
        if (runtime.Chat.Languages.Count > 0)
            return runtime.Chat.Languages.ToArray();

        var languages = new List<WowLanguageState>
        {
            new(runtime.Client.DefaultLanguage, runtime.Client.DefaultLanguageId)
        };
        if (runtime.Client.AlternativeDefaultLanguage is { } alternative &&
            runtime.Client.AlternativeDefaultLanguageId is { } alternativeId &&
            alternativeId != runtime.Client.DefaultLanguageId)
        {
            languages.Add(new WowLanguageState(alternative, alternativeId));
        }
        return languages;
    }

    private static void RemoveMatching(IList<string> values, string value)
    {
        for (var index = values.Count - 1; index >= 0; index--)
        {
            if (values[index].Equals(value, StringComparison.OrdinalIgnoreCase))
                values.RemoveAt(index);
        }
    }

    private static bool TryWindow(
        LuaRuntime runtime,
        lua_State state,
        string usage,
        out WowChatWindowState window)
    {
        if (!TryReadRequiredInt32(state, 1, out var index))
        {
            window = null!;
            luaL_error(state, usage);
            return false;
        }
        if (index is < 1 or > 10)
        {
            window = null!;
            return false;
        }
        window = runtime.Chat.GetWindow(index);
        return true;
    }

    private static bool TryWindowAndString(
        LuaRuntime runtime,
        lua_State state,
        string usage,
        out WowChatWindowState window,
        out string value)
    {
        if (!TryReadRequiredInt32(state, 1, out var index) ||
            lua_isstring(state, 2) == 0)
        {
            luaL_error(state, usage);
            window = null!;
            value = string.Empty;
            return false;
        }
        value = lua_tostring(state, 2) ?? string.Empty;
        if (!TryWindowAtIndex(runtime, index, out window))
        {
            return false;
        }
        return true;
    }

    private static bool TryWindowAtIndex(
        LuaRuntime runtime,
        int index,
        out WowChatWindowState window)
    {
        if (index is < 1 or > 10)
        {
            window = null!;
            return false;
        }
        window = runtime.Chat.GetWindow(index);
        return true;
    }

    private static bool TryReadRequiredInt32(lua_State state, int index, out int value)
    {
        value = 0;
        if (!TryReadRequiredNumber(state, index, out var number) ||
            number is < int.MinValue or > int.MaxValue)
            return false;
        value = (int)number;
        return true;
    }

    private static bool TryReadRequiredNumber(
        lua_State state,
        int index,
        out double value)
    {
        value = 0;
        if (index > lua_gettop(state) || lua_isnumber(state, index) == 0)
            return false;
        value = lua_tonumber(state, index);
        return double.IsFinite(value);
    }

    private static double QuantizeChatColor(double value) =>
        unchecked((byte)(int)(value * 255.0)) / 255.0;
}
