using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBlackMarketItemState(
    int MarketId,
    string Name,
    uint? TextureFileId,
    int Quantity,
    string? ItemType,
    bool IsUsable,
    int Level,
    string? LevelType,
    string? SellerName,
    ulong MinBid,
    ulong MinIncrement,
    ulong CurrentBid,
    bool HasPlayerBid,
    int NumBids,
    int TimeLeftSeconds,
    string? Link,
    int Quality);
