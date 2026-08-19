namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSoulbindPendingModification(
    int NodeId,
    int ConduitId,
    int Type,
    int SoulbindId,
    bool IsAutomatic);
