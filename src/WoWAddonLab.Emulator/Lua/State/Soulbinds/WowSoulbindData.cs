namespace WoWAddonLab.Emulator.Lua;

public sealed record WowSoulbindData(
    int Id,
    int CovenantId,
    string? Name,
    string? Description,
    string? TextureKit,
    bool Unlocked,
    int CvarIndex,
    WowSoulbindTreeData Tree,
    WowSoulbindModelSceneData ModelSceneData,
    int ActivationSoundKitId,
    string? PlayerConditionReason)
{
    public static WowSoulbindData Empty { get; } =
        new(
            0,
            0,
            string.Empty,
            string.Empty,
            null,
            false,
            0,
            WowSoulbindTreeData.Empty,
            WowSoulbindModelSceneData.Empty,
            0,
            null);
}
