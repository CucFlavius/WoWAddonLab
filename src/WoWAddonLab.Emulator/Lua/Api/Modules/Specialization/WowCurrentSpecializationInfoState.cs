using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCurrentSpecializationInfoState(
    int Id,
    string? Name,
    string? Description,
    int? IconFileDataId,
    string? Role,
    int? PrimaryStat,
    int PointsSpent,
    string? Background,
    int PreviewPointsSpent,
    bool IsUnlocked);
