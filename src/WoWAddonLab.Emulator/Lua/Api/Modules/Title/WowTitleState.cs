using LuaNET.Lua51;
using System.Text;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTitleState(
    int Id,
    string Name,
    bool IsKnown = true,
    bool IsPlayerTitle = true);
