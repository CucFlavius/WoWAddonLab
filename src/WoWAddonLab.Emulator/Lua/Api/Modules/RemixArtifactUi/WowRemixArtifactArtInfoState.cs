using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowRemixArtifactArtInfoState(
    string TextureKit,
    string TitleName,
    int UiModelSceneId,
    int SpellVisualKitId);
