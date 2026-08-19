using System.Globalization;
using System.Runtime.InteropServices;
using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal readonly record struct WowDurationMetrics(
    bool IsZero,
    bool HasExpired,
    double StartTime,
    double EndTime,
    double TotalDuration,
    double ElapsedDuration,
    double RemainingDuration,
    double ElapsedPercent,
    double RemainingPercent);
