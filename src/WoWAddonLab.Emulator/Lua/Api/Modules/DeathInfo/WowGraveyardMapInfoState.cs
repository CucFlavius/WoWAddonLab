using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGraveyardMapInfoState(
    int AreaPoiId,
    double X,
    double Y,
    string? Name,
    int TextureIndex,
    int GraveyardId,
    bool IsGraveyardSelectable);
