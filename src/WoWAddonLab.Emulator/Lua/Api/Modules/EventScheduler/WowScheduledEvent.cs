using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowScheduledEvent(
    string EventKey,
    int EventId,
    int AreaPoiId,
    long StartTime,
    long EndTime,
    long Duration,
    bool RewardsClaimed,
    WowEventSchedulerDisplayInfo DisplayInfo);
