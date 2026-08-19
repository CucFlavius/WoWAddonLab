namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGarrisonPlotInstanceState(
    int BuildingPlotInstanceId,
    float X,
    float Y,
    string? Name,
    string AtlasName);
