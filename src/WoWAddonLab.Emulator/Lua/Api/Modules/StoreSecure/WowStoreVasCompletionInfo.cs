using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowStoreVasCompletionInfo(
    int Result,
    string? Guid,
    string Name,
    bool FactionChanged);
