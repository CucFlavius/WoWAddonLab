using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSecondsFormatterState : IWowNumericFormatterState
{
    public double ApproximationSeconds { get; set; }
    public SecondsFormatterAbbreviation DefaultAbbreviation { get; set; }
    public bool CanRoundUpIntervals { get; set; }
    public bool CanRoundUpLastUnit { get; set; }
    public bool ConvertToLower { get; set; }
    public SecondsFormatterIntervalWhitespace Whitespace { get; set; }
    public WowSecondsFormatterValue MaxInterval { get; } = new(3);
    public WowSecondsFormatterValue MinInterval { get; } = new(0);
    public WowSecondsFormatterValue DesiredUnitCount { get; } = new(2);
    public double MillisecondsThreshold { get; set; }

    public void Reset(LuaRuntime runtime)
    {
        ApproximationSeconds = 0;
        DefaultAbbreviation = SecondsFormatterAbbreviation.None;
        CanRoundUpIntervals = false;
        CanRoundUpLastUnit = false;
        ConvertToLower = false;
        Whitespace = SecondsFormatterIntervalWhitespace.Preserve;
        MaxInterval.SetStatic(runtime, 3);
        MinInterval.SetStatic(runtime, 0);
        DesiredUnitCount.SetStatic(runtime, 2);
        MillisecondsThreshold = 0;
    }

    public void ReleaseCurveReferences(LuaRuntime runtime)
    {
        MaxInterval.Release(runtime);
        MinInterval.Release(runtime);
        DesiredUnitCount.Release(runtime);
    }
}
