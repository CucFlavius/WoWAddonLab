namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSoulbindModelSceneData(
    int CreatureDisplayInfoId,
    int ModelSceneActorId)
{
    public static WowSoulbindModelSceneData Empty { get; } =
        new(0, 0);
}
