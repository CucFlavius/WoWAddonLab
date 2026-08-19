using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAchievementCategoryDefinition(
    int Id,
    int ParentId,
    string Name,
    int Flags);
