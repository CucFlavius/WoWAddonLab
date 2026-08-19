using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAccountStoreCategoryDefinition(
    int Id,
    int StoreFrontId,
    int OrderIndex,
    string Name,
    int Type,
    uint IconFileDataId);
