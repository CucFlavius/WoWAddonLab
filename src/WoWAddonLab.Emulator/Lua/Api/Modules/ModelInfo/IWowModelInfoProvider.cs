using WoWAddonLab.Emulator.UI;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public interface IWowModelInfoProvider
{
    bool TryGetScene(int id, out WowModelSceneDefinition scene);
    bool TryGetActor(int id, out WowModelSceneActorDefinition actor);
    bool TryGetActorDisplay(int id, out WowModelSceneActorDisplayDefinition display);
    bool TryGetCamera(int id, out WowModelSceneCameraDefinition camera);
}
