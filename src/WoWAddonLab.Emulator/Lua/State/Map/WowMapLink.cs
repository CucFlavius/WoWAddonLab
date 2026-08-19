namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowMapLink(
    int Id,
    int ParentMapId,
    int ChildMapId,
    int OrderIndex,
    double MinimumX,
    double MinimumY,
    double MaximumX,
    double MaximumY,
    int Flags,
    uint OverrideHighlightFileDataId = 0,
    string? OverrideHighlightAtlasId = null,
    int PlayerConditionId = 0);
