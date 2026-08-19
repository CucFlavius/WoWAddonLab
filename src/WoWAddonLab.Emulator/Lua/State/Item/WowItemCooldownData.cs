using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowItemCooldownData(
    double StartTimeSeconds,
    double DurationSeconds,
    bool EnableCooldownTimer);
