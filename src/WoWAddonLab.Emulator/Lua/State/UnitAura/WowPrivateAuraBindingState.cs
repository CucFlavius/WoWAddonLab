namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPrivateAuraBindingState(
    uint AnchorId,
    int AuraFrameObjectId,
    int IconRegionObjectId,
    int DurationRegionObjectId);
