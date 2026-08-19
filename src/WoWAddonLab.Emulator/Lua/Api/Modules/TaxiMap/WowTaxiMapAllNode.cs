using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTaxiMapAllNode(
    int NodeId,
    double X,
    double Y,
    string? Name,
    uint State,
    int SlotIndex,
    string? TextureKit = null,
    bool UseSpecialIcon = false,
    string? SpecialIconCostString = null,
    bool IsMapLayerTransition = false);
