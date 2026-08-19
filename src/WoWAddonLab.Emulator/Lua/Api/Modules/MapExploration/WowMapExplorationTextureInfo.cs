using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMapExplorationTextureInfo(
    int TextureWidth,
    int TextureHeight,
    int OffsetX,
    int OffsetY,
    bool IsShownByMouseOver,
    bool IsDrawOnTopLayer,
    IReadOnlyList<int> FileDataIds,
    WowMapExplorationHitRect HitRect);
