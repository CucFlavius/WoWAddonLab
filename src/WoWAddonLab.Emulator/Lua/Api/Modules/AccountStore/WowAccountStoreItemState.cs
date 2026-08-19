using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAccountStoreItemState(
    WowAccountStoreItemStatus Status = WowAccountStoreItemStatus.Unowned,
    WowAccountStoreItemMode Mode = WowAccountStoreItemMode.Hidden,
    double? RefundSecondsRemaining = null);
