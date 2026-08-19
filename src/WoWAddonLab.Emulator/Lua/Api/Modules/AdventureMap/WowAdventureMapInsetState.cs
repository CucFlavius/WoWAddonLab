using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAdventureMapInsetState(
    int MapId,
    string Title,
    string Description,
    string CollapsedIcon,
    int AreaTableId,
    int NumDetailTiles,
    double? NormalizedX = null,
    double? NormalizedY = null,
    int LinkId = -1)
{
    public IList<int?> DetailTileFileIds { get; } = new List<int?>();
}
