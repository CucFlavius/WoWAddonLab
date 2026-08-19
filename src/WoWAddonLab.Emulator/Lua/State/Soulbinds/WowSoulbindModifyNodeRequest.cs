namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSoulbindModifyNodeRequest(
    int NodeId,
    int ConduitId,
    int Type);
