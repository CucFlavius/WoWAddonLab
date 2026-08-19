using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBarberShopColor(
    double Red,
    double Green,
    double Blue,
    double Alpha = 1);
