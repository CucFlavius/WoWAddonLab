namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBattleNetFriendInviteState(
    uint AccountId,
    string DisplayName,
    bool IsBattleTag,
    uint InviteTime);
