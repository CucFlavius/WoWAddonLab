using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowLegacyTaxiNode(
    int NodeId,
    double X,
    double Y,
    string Name = "",
    int Cost = 0,
    string Type = "NONE",
    IReadOnlyList<int>? Route = null,
    bool RouteCalculationAttempted = false)
{
    public IReadOnlyList<int> RouteSlots { get; } = Route ?? [];
}
