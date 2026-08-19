using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowLfgApplicationInfo(
    int ResultId,
    int Status,
    int PendingStatus,
    int Duration,
    string Role);
