using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCombatLogMessage(
    string Message,
    byte Red,
    byte Green,
    byte Blue,
    int Order);
