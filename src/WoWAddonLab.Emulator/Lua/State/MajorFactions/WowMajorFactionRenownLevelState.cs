namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMajorFactionRenownLevelState(
    int FactionId,
    int Level,
    bool Locked,
    bool IsMilestone,
    bool IsCapstone);
