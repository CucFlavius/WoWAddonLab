using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowSecondsFormatterValue(byte value)
{
    public byte StaticValue { get; private set; } = value;
    public WowCurveState? Curve { get; private set; }
    public int CurveReference { get; private set; }

    public void SetStatic(LuaRuntime runtime, byte staticValue)
    {
        Release(runtime);
        StaticValue = staticValue;
    }

    public void SetCurve(
        LuaRuntime runtime,
        WowCurveState curve,
        int curveReference)
    {
        Release(runtime);
        Curve = curve;
        CurveReference = curveReference;
    }

    public void Release(LuaRuntime runtime)
    {
        runtime.ReleaseReference(CurveReference);
        CurveReference = 0;
        Curve = null;
    }
}
