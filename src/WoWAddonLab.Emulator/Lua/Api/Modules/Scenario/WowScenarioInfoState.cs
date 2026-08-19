using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowScenarioInfoState(
    string Name,
    int CurrentStage,
    int NumStages,
    int Flags,
    bool IsComplete,
    int Xp,
    int Money,
    int Type,
    string? AreaName,
    string? UiTextureKit);
