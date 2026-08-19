namespace WoWAddonLab.Emulator.Lua;

public sealed record WowActionLossOfControlInfo(
    double StartTime = 0,
    double Duration = 0,
    double ModRate = 0,
    bool IsActive = false,
    bool ShouldReplaceNormalCooldown = false);
