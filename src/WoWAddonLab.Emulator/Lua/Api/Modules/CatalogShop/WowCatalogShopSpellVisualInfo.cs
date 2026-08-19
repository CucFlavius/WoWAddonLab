using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCatalogShopSpellVisualInfo(
    int AnimId = 0,
    int SpellVisualKitId = 0);
