using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAdventureMapZoneChoiceState(
    int QuestId,
    string TextureKit,
    string Name,
    string Description,
    double? NormalizedX = null,
    double? NormalizedY = null,
    int? InsetIndex = null);
