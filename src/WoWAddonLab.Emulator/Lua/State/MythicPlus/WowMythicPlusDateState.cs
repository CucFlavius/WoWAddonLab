namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMythicPlusDateState(
    int Year,
    int Month,
    int Day,
    int Hour,
    int Minute,
    int ZeroBasedWeekday);
