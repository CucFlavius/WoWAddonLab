using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowContributionMapInfo(
    int AreaPoiId,
    double X,
    double Y,
    string? Name,
    string AtlasName,
    int CollectorCreatureId);
