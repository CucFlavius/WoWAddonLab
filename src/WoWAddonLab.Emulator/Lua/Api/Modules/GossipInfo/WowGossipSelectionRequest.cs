using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGossipSelectionRequest(
    string Operation,
    int Value,
    string? Text,
    bool? Confirmed);
