using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAuctionHouseItemKey(
    uint ItemId,
    ushort ItemLevel,
    ushort ItemSuffix,
    uint BattlePetSpeciesId);
