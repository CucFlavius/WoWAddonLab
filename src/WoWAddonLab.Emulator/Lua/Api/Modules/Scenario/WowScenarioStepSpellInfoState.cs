using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowScenarioStepSpellInfoState(
    int SpellId,
    string Name,
    int? Icon);
