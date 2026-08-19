namespace WoWAddonLab.Emulator.Lua;

public sealed class WowDateAndTimeState
{
    public DateTimeOffset? CurrentTimeOverride { get; set; }
    public long SecondsUntilDailyReset { get; set; }
    public long SecondsUntilWeeklyReset { get; set; }
    public long WeeklyResetStartTime { get; set; }
    public TimeSpan? LocalUtcOffsetOverride { get; set; }

    public DateTimeOffset CurrentTime => CurrentTimeOverride ?? DateTimeOffset.Now;
    public TimeSpan LocalUtcOffset =>
        LocalUtcOffsetOverride ??
        TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now);
}
