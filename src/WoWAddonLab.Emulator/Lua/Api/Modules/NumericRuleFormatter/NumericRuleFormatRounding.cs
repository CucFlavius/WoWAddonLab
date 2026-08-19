using System.Globalization;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal enum NumericRuleFormatRounding
{
    Nearest = 0,
    Up = 1,
    Down = 2
}
