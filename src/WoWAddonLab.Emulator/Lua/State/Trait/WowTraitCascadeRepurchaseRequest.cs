namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTraitCascadeRepurchaseRequest(
    int ConfigId,
    int NodeId,
    int? EntryId);
