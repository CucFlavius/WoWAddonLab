using System.Diagnostics;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAddOnProfilerCallEvent(
    string Name,
    ulong AllocatedBytes,
    ulong DeallocatedBytes,
    double ElapsedMilliseconds,
    int ElapsedTicks);
