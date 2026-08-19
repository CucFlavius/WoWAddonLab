using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowClickBindingInfoState(
    int Type,
    int ActionId,
    string Button,
    uint Modifiers);
