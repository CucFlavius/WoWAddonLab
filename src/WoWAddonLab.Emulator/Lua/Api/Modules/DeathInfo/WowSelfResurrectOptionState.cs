using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSelfResurrectOptionState(
    string Name,
    byte OptionType,
    int Id,
    bool CanUse,
    bool IsLimited,
    int Priority);
