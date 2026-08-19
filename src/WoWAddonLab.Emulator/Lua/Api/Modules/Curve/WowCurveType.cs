using System.Numerics;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal enum WowCurveType : byte
{
    Linear,
    Step,
    Cosine,
    Cubic
}
