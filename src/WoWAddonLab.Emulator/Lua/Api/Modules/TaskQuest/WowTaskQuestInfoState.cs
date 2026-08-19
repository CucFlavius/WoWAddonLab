using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTaskQuestInfoState(
    string? QuestTitle,
    int? FactionId,
    bool? Capped,
    bool? DisplayAsObjective);
