namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowMapAssignment(
    int Id,
    int UiMapId,
    int OrderIndex,
    int MapId,
    int AreaId,
    double UiMinimumX,
    double UiMinimumY,
    double UiMaximumX,
    double UiMaximumY,
    double RegionMinimumX,
    double RegionMinimumY,
    double RegionMinimumZ,
    double RegionMaximumX,
    double RegionMaximumY,
    double RegionMaximumZ);
