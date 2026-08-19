using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCatalogShopDecorQuantity(
    int PlacedQuantity,
    int StoredQuantity);
