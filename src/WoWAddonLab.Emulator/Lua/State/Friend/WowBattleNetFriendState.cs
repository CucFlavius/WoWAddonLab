namespace WoWAddonLab.Emulator.Lua;

public sealed record WowBattleNetFriendState(
    uint AccountId,
    string DisplayName,
    bool Online,
    bool Favorite);
