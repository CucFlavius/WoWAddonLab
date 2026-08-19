using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowVignetteState
{
    public List<string> Guids { get; } = [];
    public Dictionary<string, WowVignetteInfo> InfoByGuid { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, float> HealthPercentByGuid { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, (int Minimum, int Maximum)> RecommendedGroupSizeByGuid
        { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, Dictionary<int, WowVignettePosition>> PositionsByGuid
        { get; } = new(StringComparer.OrdinalIgnoreCase);

    public string? BestUniqueGuid { get; set; }
}
