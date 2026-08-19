using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowModelSceneDefinition(
    int Id,
    int Type,
    int Flags,
    IReadOnlyList<int> CameraIds,
    IReadOnlyList<int> ActorIds);
