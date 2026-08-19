namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTransmogSetDefinition(
    int SetId,
    string Name,
    int? BaseSetId,
    string? Description,
    string? Label,
    int ExpansionId,
    int PatchId,
    int UiOrder,
    int ClassMask,
    bool HiddenUntilCollected,
    string? RequiredFaction,
    bool LimitedTimeSet,
    bool GrantAsPrecedingVariant);
