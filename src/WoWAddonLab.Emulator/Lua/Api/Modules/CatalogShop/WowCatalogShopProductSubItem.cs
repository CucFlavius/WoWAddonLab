using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCatalogShopProductSubItem(
    string Name,
    int ItemId,
    int ItemAppearanceId,
    string InvType,
    byte Quality);
