using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCatalogShopTelemetryEntry(
    string Operation,
    int CategoryId,
    int SectionId,
    int ProductId,
    bool WasCodeSelection = false);
