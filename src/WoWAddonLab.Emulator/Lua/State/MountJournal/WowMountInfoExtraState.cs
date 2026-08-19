namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMountInfoExtraState(
    int? CreatureDisplayInfoId,
    string? Description,
    string? Source,
    bool IsSelfMount,
    int MountTypeId,
    int UiModelSceneId,
    int AnimationId,
    int SpellVisualKitId,
    bool DisablePlayerMountPreview);
