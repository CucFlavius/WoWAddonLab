namespace WoWAddonLab.Emulator.Lua;

public sealed record WowHousingHouseInfoState(
    int PlotId,
    string HouseName,
    string OwnerName,
    int? PlotCost,
    string NeighborhoodName,
    double? MoveOutTime,
    bool? PlotReserved,
    string? NeighborhoodGuid,
    string? HouseGuid);
