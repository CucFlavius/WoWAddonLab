using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAccountStoreItemDefinition(
    int Id,
    int StoreFrontId,
    int CategoryId,
    int OrderIndex,
    string Name,
    string Description,
    int Price,
    int CurrencyId,
    int SpellId,
    int TransmogSetId,
    int CreatureDisplayInfoId,
    int UiModelSceneId,
    uint IconFileDataId,
    int Flags = 0,
    bool Nonrefundable = true);
