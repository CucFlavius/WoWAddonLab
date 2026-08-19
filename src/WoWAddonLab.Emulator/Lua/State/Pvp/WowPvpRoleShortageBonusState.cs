namespace WoWAddonLab.Emulator.Lua;

public sealed record WowPvpRoleShortageBonusState(
    IReadOnlyList<string?> ValidRoles,
    int RewardSpellId,
    int RewardItemId);
