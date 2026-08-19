namespace WoWAddonLab.Emulator.Lua;

public interface IWowFactionProvider
{
    bool TryGetFriendshipReputation(
        int factionId,
        out WowGossipFriendshipReputationState reputation);

    bool TryGetFriendshipRanks(
        int factionId,
        out WowGossipFriendshipRanksState ranks);
}
