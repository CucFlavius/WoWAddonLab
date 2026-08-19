using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowModelSceneActorDisplayDefinition(
    int Id,
    int Animation,
    int AnimationVariation,
    double AnimationSpeed,
    int? AnimationKitId,
    int? SpellVisualKitId,
    double Alpha,
    double Scale);
