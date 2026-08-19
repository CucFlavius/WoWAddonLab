using System.Diagnostics;

namespace WoWAddonLab.Emulator.Lua;

internal sealed record WowAddOnProfilerCallResults(
    double ElapsedMilliseconds,
    int ElapsedTicks,
    ulong AllocatedBytes,
    ulong DeallocatedBytes,
    IReadOnlyList<WowAddOnProfilerCallEvent> Events);
