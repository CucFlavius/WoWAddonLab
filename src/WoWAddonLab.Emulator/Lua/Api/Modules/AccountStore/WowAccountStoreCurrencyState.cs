using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAccountStoreCurrencyState(
    int Amount = 0,
    int? MaximumQuantity = null,
    string Name = "",
    uint IconFileDataId = 0);
