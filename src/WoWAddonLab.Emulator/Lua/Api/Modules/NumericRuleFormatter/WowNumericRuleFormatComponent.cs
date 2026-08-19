using System.Globalization;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal readonly record struct WowNumericRuleFormatComponent(
    double Divisor,
    double Modulus,
    double Step,
    NumericRuleFormatRounding Rounding);
