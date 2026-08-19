using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowQuestPoiMapInfo(
    int? ChildDepth,
    int? QuestTagType,
    int QuestId,
    int NumObjectives,
    int MapId,
    float X,
    float Y,
    bool IsQuestStart,
    bool IsDaily,
    bool IsCombatAllyQuest,
    bool IsMeta,
    bool InProgress,
    bool IsMapIndicatorQuest);
