using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowContributionStateInfo(
    uint State,
    double PercentageComplete,
    long? TimeOfNextStateChange,
    int StartTime)
{
    internal static WowContributionStateInfo Empty { get; } =
        new(0, 0, null, 0);
}
