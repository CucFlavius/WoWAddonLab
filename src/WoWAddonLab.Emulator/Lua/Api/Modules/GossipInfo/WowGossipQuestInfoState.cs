using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowGossipQuestInfoState(
    string Title,
    int QuestLevel,
    bool IsTrivial,
    int? Frequency,
    bool? Repeatable,
    bool? IsComplete,
    bool IsLegendary,
    bool IsIgnored,
    int QuestId,
    bool IsImportant,
    bool IsMeta,
    int QuestInfoId);
