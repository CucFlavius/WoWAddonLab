using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCatalogShopRefundableDecorInfo(
    string DecorGuid,
    int TimeRemainingSeconds,
    string Name,
    string Price,
    uint? ProductId = null);
