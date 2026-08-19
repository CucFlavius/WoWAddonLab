using System.Diagnostics;

namespace WoWAddonLab.Emulator.Lua;

internal sealed record WowAddOnProfilerCallMeasurement(
    long StartedAtTicks,
    ulong StartedAllocatedBytes,
    ulong StartedDeallocatedBytes)
{
    public List<WowAddOnProfilerCallEvent> Events { get; } = [];
}
