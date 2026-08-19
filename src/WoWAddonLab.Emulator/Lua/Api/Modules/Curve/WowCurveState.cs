using System.Numerics;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowCurveState
{
    public WowCurveType Type { get; set; }
    public List<Vector2> Points { get; } = [];

    public WowCurveState Copy()
    {
        var copy = new WowCurveState { Type = Type };
        copy.Points.AddRange(Points);
        return copy;
    }
}
