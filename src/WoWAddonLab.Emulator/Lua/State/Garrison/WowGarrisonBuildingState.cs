namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGarrisonBuildingState(
    int BuildingId,
    int PlotId,
    int UiTab,
    string? TextureKit);
