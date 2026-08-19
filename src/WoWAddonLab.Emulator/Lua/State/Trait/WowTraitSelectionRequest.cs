namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTraitSelectionRequest(
    int ConfigId,
    int NodeId,
    int? NodeEntryId,
    bool ClearEdges);
