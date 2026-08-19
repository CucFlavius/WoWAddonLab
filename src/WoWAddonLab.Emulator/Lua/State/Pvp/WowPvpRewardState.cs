namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPvpRewardState(
    int Honor,
    int Experience,
    IReadOnlyList<WowPvpRewardItemState>? ItemRewards,
    IReadOnlyList<WowPvpCurrencyRewardState>? CurrencyRewards,
    WowPvpRoleShortageBonusState? RoleShortageBonus);
