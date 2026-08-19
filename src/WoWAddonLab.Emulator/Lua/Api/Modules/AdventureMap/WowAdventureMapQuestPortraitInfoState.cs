using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowAdventureMapQuestPortraitInfoState(
    int PortraitDisplayId,
    int MountPortraitDisplayId,
    string Name,
    string Text,
    int? ModelSceneId = null);
