namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPartyInfoState
{
    public bool PartyConversionAllowed { get; set; } = true;
    public bool CanFormCrossFactionParties { get; set; } = true;
    public bool IsCrossFactionParty { get; set; }
    public Dictionary<int, bool> IsCrossFactionPartyByCategory { get; } =
        new Dictionary<int, bool>();
    public bool IsPartyWalkIn { get; set; }
    public Dictionary<int, bool> IsPartyWalkInByCategory { get; } =
        new Dictionary<int, bool>();
    public int RestrictPingsTo { get; set; }
    public bool CanSetRestrictPings { get; set; } = true;
    public string? ChallengeModeKeystoneOwnerGuid { get; set; }
    public bool ChallengeModeRestrictionsActive { get; set; }
    public bool IsGuildParty { get; set; }
    public int GuildPartyStateRequestCount { get; set; }
    public double InstanceAbandonVoteDuration { get; set; }
    public double InstanceAbandonVoteTimeLeft { get; set; }
    public bool? InstanceAbandonVoteResponse { get; set; }
    public int InstanceAbandonVotesRequired { get; set; }
    public int InstanceAbandonKeystoneOwnerVoteWeight { get; set; }
    public int InstanceAbandonGroupVoteResponseCount { get; set; }
    public double InstanceAbandonShutdownDuration { get; set; }
    public double InstanceAbandonShutdownTimeLeft { get; set; }
    public byte LootMethod { get; set; } = 5;
    public IList<byte> AvailableLootMethods { get; } = new List<byte> { 5 };
    public int? LootMasterPartyIndex { get; set; }
    public int? LootMasterRaidIndex { get; set; }
    public string? LootMasterName { get; set; }
    public bool CanSetLootMethod { get; set; } = true;
    public string? LastInviteTarget { get; set; }
    public int InviteRequestCount { get; set; }
    public int LeaveRequestCount { get; set; }
}
