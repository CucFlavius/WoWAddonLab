using System.Diagnostics;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAddOnProfilerResult(
    string AddOnName,
    double MetricValue);
