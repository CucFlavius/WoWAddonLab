namespace WoWAddonLab.Emulator.Lua;

public sealed record WowMajorFactionRenownRewardState(
    int RenownRewardId,
    int UiOrder,
    bool IsAccountUnlock,
    int? ItemId,
    int? SpellId,
    int? MountId,
    int? TransmogId,
    int? TransmogSetId,
    int? TitleMaskId,
    int? TransmogIllusionSourceId,
    int? IconFileDataId,
    string? Name,
    string? Description,
    string? ToastDescription,
    int? RewardType,
    bool? IsCollected);
