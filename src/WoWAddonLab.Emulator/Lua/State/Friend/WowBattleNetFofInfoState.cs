namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBattleNetFofInfoState(
    uint AccountId,
    string DisplayName,
    bool IsMutualFriend);
