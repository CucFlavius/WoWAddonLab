using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowExecutedClickBindingState(
    string TargetToken,
    string Button,
    uint Modifiers,
    int Type,
    int ActionId);
