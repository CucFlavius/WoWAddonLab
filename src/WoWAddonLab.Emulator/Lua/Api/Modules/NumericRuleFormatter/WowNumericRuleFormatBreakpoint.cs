using System.Globalization;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed record WowNumericRuleFormatBreakpoint(
    double Threshold,
    double Step,
    NumericRuleFormatRounding Rounding,
    double? Minimum,
    double? Maximum,
    string Format,
    IReadOnlyList<WowNumericRuleFormatComponent> Components)
{
    public WowNumericRuleFormatBreakpoint Copy() => this with
    {
        Components = Components.ToArray()
    };
}
