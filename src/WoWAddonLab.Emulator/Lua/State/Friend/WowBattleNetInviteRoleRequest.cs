namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBattleNetInviteRoleRequest(
    uint AccountId,
    bool Tank,
    bool Healer,
    bool Damage);
