using System.Globalization;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowNumericRuleFormatterState : IWowNumericFormatterState
{
    public List<WowNumericRuleFormatBreakpoint> Breakpoints { get; } = [];

    public WowNumericRuleFormatterState Copy()
    {
        var copy = new WowNumericRuleFormatterState();
        copy.Breakpoints.AddRange(Breakpoints.Select(value => value.Copy()));
        return copy;
    }

    public void Sort() => Breakpoints.Sort(
        static (left, right) => right.Threshold.CompareTo(left.Threshold));
}
