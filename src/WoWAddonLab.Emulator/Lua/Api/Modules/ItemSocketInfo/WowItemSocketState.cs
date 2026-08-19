using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowItemSocketState(
    string? ExistingName = null,
    uint? ExistingIconFileDataId = null,
    bool ExistingGemMatchesSocket = false,
    string? ExistingLink = null,
    string? NewName = null,
    uint? NewIconFileDataId = null,
    bool NewGemMatchesSocket = false,
    string? NewLink = null,
    string? SocketType = null);
