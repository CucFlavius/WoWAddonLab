using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPvpTalentInfoState(
    int TalentId,
    string? Name,
    int IconFileDataId,
    bool Selected,
    bool Available,
    int SpellId,
    bool Unlocked,
    bool Known,
    bool GrantedByAura,
    bool DependenciesUnmet,
    int DependenciesUnmetReason);
