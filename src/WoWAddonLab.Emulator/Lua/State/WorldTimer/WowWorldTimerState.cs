namespace WoWAddonLab.Emulator.Lua;

public sealed class WowWorldTimerState
{
    public IDictionary<int, WowWorldElapsedTimerState> Timers { get; } =
        new Dictionary<int, WowWorldElapsedTimerState>();
}
