using System.Globalization;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowAbbreviatedNumberFormatterState : IWowNumericFormatterState
{
    public List<WowNumberAbbreviationBreakpoint> Breakpoints { get; } = [];

    public static WowAbbreviatedNumberFormatterState CreateDefault()
    {
        var value = new WowAbbreviatedNumberFormatterState();
        value.Reset();
        return value;
    }

    public WowAbbreviatedNumberFormatterState Copy()
    {
        var copy = new WowAbbreviatedNumberFormatterState();
        copy.Breakpoints.AddRange(Breakpoints);
        return copy;
    }

    public void Reset()
    {
        Breakpoints.Clear();
        Breakpoints.AddRange(
        [
            new(1e13, "FOURTH_NUMBER_CAP_NO_SPACE", 1e12, 1, true),
            new(1e12, "FOURTH_NUMBER_CAP_NO_SPACE", 1e11, 10, true),
            new(1e10, "THIRD_NUMBER_CAP_NO_SPACE", 1e9, 1, true),
            new(1e9, "THIRD_NUMBER_CAP_NO_SPACE", 1e8, 10, true),
            new(1e7, "SECOND_NUMBER_CAP_NO_SPACE", 1e6, 1, true),
            new(1e6, "SECOND_NUMBER_CAP_NO_SPACE", 1e5, 10, true),
            new(1e4, "FIRST_NUMBER_CAP_NO_SPACE", 1e3, 1, true),
            new(1e3, "FIRST_NUMBER_CAP_NO_SPACE", 1e2, 10, true)
        ]);
    }

    public void Sort() => Breakpoints.Sort(
        static (left, right) => right.Breakpoint.CompareTo(left.Breakpoint));
}
