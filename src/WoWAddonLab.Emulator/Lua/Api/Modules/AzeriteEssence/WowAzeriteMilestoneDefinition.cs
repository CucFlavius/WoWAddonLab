using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAzeriteMilestoneDefinition(
    int Id,
    int RequiredLevel,
    int AzeritePowerId,
    int AzeriteEssenceType,
    bool IsHeartEssenceUnlock,
    int SpellId);
