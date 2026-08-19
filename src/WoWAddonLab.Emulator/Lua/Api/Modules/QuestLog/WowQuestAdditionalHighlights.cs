using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowQuestAdditionalHighlights(
    int UiMapId,
    bool WorldQuests,
    bool WorldQuestsElite,
    bool Dungeons,
    bool Treasures);
