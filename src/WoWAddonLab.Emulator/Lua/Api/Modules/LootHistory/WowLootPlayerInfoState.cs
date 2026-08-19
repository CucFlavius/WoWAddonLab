using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowLootPlayerInfoState(
    string PlayerName,
    string PlayerGuid,
    string PlayerClass,
    bool IsSelf,
    uint State,
    bool IsWinner,
    int? Roll);
