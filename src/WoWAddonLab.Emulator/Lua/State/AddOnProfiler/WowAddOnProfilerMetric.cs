using System.Diagnostics;

namespace WoWAddonLab.Emulator.Lua;

public enum WowAddOnProfilerMetric
{
    SessionAverageTime = 0,
    RecentAverageTime = 1,
    EncounterAverageTime = 2,
    LastTime = 3,
    PeakTime = 4,
    CountTimeOver1Ms = 5,
    CountTimeOver5Ms = 6,
    CountTimeOver10Ms = 7,
    CountTimeOver50Ms = 8,
    CountTimeOver100Ms = 9,
    CountTimeOver500Ms = 10,
    CountTimeOver1000Ms = 11
}
