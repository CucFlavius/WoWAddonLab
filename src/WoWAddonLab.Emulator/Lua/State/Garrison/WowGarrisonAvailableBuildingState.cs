namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGarrisonAvailableBuildingState(
    int BuildingId,
    int? PlotId,
    string? Name,
    int? Icon,
    bool NeedsPlan,
    int Cost,
    int GoldCost,
    string BuildTime);
