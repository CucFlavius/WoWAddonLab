using System.Diagnostics;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAddOnPerformanceMessage(
    WowAddOnPerformanceMessageType Type,
    WowAddOnProfilerMetric Metric,
    string? AddOnName,
    double MetricValue,
    double ThresholdValue);
