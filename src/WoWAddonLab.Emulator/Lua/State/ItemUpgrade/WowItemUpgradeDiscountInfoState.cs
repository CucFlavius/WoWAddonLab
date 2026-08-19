namespace WoWAddonLab.Emulator.Lua;

public sealed record WowItemUpgradeDiscountInfoState(
    bool IsDiscounted,
    int DiscountHighWatermark,
    bool IsPartialTwoHandDiscount,
    bool IsAccountWideDiscount,
    bool DoesCurrentCharacterMeetHighWatermark);
