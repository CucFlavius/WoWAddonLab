using System.Globalization;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal enum DurationTextBindingProperty
{
    RemainingDuration,
    RemainingPercent,
    ElapsedDuration,
    ElapsedPercent,
    TotalDuration,
    StartTime,
    EndTime
}
