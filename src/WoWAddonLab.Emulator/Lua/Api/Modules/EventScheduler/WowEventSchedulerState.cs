using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowEventSchedulerState
{
    private long _lastRequestMilliseconds = long.MinValue;

    public bool CanShowEvents { get; set; }
    public bool HasData { get; set; }
    public List<WowOngoingEvent> OngoingEvents { get; } = [];
    public List<WowScheduledEvent> ScheduledEvents { get; } = [];
    public Dictionary<int, int> UiMapIdByAreaPoiId { get; } = [];
    public Dictionary<int, string> ZoneNameByAreaPoiId { get; } = [];
    public HashSet<string> SavedReminders { get; } = new(StringComparer.Ordinal);
    public Func<long> MonotonicMillisecondsProvider { get; set; } =
        static () => Environment.TickCount64;
    public int RequestEventsInvocationCount { get; internal set; }
    public int EventRequestsSent { get; internal set; }

    internal bool TryRequestEvents()
    {
        RequestEventsInvocationCount++;
        var now = MonotonicMillisecondsProvider();
        if (_lastRequestMilliseconds != long.MinValue &&
            now - _lastRequestMilliseconds < 5_000)
        {
            return false;
        }

        _lastRequestMilliseconds = now;
        EventRequestsSent++;
        return true;
    }
}
