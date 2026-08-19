namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRuneforgePowerLists(
    IReadOnlyList<int> PrimaryPowerIds,
    IReadOnlyList<int> OtherPowerIds);
