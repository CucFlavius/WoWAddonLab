using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAzeriteEssenceInfoState(
    uint Id,
    string? Name,
    int Rank,
    bool Unlocked,
    bool Valid,
    int Icon,
    int MaxRank = 4);
