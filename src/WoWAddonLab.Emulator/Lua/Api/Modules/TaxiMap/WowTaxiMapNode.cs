using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTaxiMapNode(
    int NodeId,
    double X,
    double Y,
    string? Name,
    string? AtlasName,
    uint Faction,
    string? TextureKit = null,
    bool IsUndiscovered = false);
