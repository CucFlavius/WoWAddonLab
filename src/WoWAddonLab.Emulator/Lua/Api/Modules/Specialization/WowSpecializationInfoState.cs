using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSpecializationInfoState(
    int Id,
    string? Name,
    string? Description,
    int? IconFileDataId,
    string? Role,
    bool Recommended,
    bool AllowedForBoost,
    int? MasterySpell1,
    int? MasterySpell2);
