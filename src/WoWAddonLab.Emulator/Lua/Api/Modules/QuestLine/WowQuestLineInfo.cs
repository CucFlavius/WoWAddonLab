using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowQuestLineInfo(
    string? QuestLineName,
    string? QuestName,
    int QuestLineId,
    int QuestId,
    float X,
    float Y,
    bool IsHidden,
    bool IsLegendary,
    bool IsLocalStory,
    bool IsDaily,
    bool IsCampaign,
    bool IsImportant,
    bool IsAccountCompleted,
    bool IsCombatAllyQuest,
    bool IsMeta,
    bool InProgress,
    bool IsQuestStart,
    uint FloorLocation,
    int StartMapId,
    bool IsDisplayable = true);
