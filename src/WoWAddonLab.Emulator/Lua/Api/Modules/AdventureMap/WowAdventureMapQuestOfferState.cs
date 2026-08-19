using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAdventureMapQuestOfferState(
    int QuestId,
    bool IsTrivial,
    int Frequency,
    bool IsLegendary,
    string Title,
    string Description,
    double? NormalizedX = null,
    double? NormalizedY = null,
    int? InsetIndex = null);
