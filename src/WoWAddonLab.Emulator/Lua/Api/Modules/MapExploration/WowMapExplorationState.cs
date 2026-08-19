using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowMapExplorationState
{
    public IDictionary<(int UiMapId, float X, float Y), IReadOnlyList<int>?>
        AreaIdsByPosition { get; } =
        new Dictionary<(int UiMapId, float X, float Y), IReadOnlyList<int>?>();

    public Func<int, float, float, IReadOnlyList<int>?>? AreaIdsResolver { get; set; }

    public IDictionary<int, IReadOnlyList<WowMapExplorationTextureInfo>>
        TexturesByMapId { get; } =
        new Dictionary<int, IReadOnlyList<WowMapExplorationTextureInfo>>();
}
