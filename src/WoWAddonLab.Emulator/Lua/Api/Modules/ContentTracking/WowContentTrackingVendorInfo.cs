using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowContentTrackingVendorInfo(
    string? CreatureName = null,
    string? ZoneName = null,
    int? CurrencyType = null,
    ulong? Cost = null);
