using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactFactionCatalog : TactCatalog, IWowFactionProvider
{
    private readonly IReadOnlyDictionary<int, WowGossipFriendshipReputationState>
        _reputations;
    private readonly IReadOnlyDictionary<int, WowGossipFriendshipRanksState> _ranks;

    private TactFactionCatalog(
        IReadOnlyDictionary<int, WowGossipFriendshipReputationState> reputations,
        IReadOnlyDictionary<int, WowGossipFriendshipRanksState> ranks)
    {
        _reputations = reputations;
        _ranks = ranks;
    }

    public int Count => _reputations.Count;

    public bool TryGetFriendshipReputation(
        int factionId,
        out WowGossipFriendshipReputationState reputation) =>
        _reputations.TryGetValue(factionId, out reputation!);

    public bool TryGetFriendshipRanks(
        int factionId,
        out WowGossipFriendshipRanksState ranks) =>
        _ranks.TryGetValue(factionId, out ranks!);

    public static TactFactionCatalog Load(TactAssetSource tact, string build)
    {
        var friendshipRows = tact.Database.Load("FriendshipReputation", build).Values
            .ToDictionary(row => Integer(row, "ID"));
        var reactions = tact.Database.Load("FriendshipRepReaction", build).Values
            .GroupBy(row => Integer(row, "FriendshipRepID"))
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderBy(row => Integer(row, "ReactionThreshold"))
                    .ToArray());
        var reputations = new Dictionary<int, WowGossipFriendshipReputationState>();
        var ranks = new Dictionary<int, WowGossipFriendshipRanksState>();

        foreach (var row in tact.Database.Load("Faction", build).Values)
        {
            var factionId = Integer(row, "ID");
            var friendshipId = Integer(row, "FriendshipRepID");
            var name = Text(row, "Name_lang", "Name");
            var description = Text(row, "Description_lang", "Description");
            friendshipRows.TryGetValue(friendshipId, out var friendship);
            reactions.TryGetValue(friendshipId, out var reactionRows);
            var firstReaction = reactionRows?.FirstOrDefault();
            var lastReaction = reactionRows?.LastOrDefault();
            var isFriendship = friendshipId != 0 && friendship is not null;

            reputations[factionId] = new WowGossipFriendshipReputationState(
                isFriendship ? factionId : 0,
                0,
                lastReaction is null ? 0 : Integer(lastReaction, "ReactionThreshold"),
                string.IsNullOrEmpty(name) ? null : name,
                friendship is null
                    ? description
                    : Text(friendship, "Description_lang", "Description"),
                friendship is null ? 0 : Integer(friendship, "TextureFileID"),
                firstReaction is null
                    ? string.Empty
                    : Text(firstReaction, "Reaction_lang", "Reaction"),
                firstReaction is null ? 0 : Integer(firstReaction, "ReactionThreshold"),
                reactionRows is { Length: > 1 }
                    ? Integer(reactionRows[1], "ReactionThreshold")
                    : null,
                false,
                firstReaction is null
                    ? null
                    : Integer(firstReaction, "OverrideColor"));

            if (isFriendship)
                ranks[factionId] = new WowGossipFriendshipRanksState(
                    reactionRows is { Length: > 0 } ? 1 : 0,
                    reactionRows?.Length ?? 0);
        }

        return new TactFactionCatalog(reputations, ranks);
    }
}
