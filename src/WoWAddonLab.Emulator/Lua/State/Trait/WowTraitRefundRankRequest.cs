namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTraitRefundRankRequest(
    int ConfigId,
    int NodeId,
    bool ClearEdges);
