using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTaskQuestPoiMapInfoState(
    int? ChildDepth,
    byte? QuestTagType,
    int QuestId,
    int NumObjectives,
    int MapId,
    double X,
    double Y,
    bool IsQuestStart,
    bool IsDaily,
    bool IsCombatAllyQuest,
    bool IsMeta,
    bool InProgress,
    bool IsMapIndicatorQuest);
