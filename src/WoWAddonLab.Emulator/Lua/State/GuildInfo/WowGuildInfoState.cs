namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGuildInfoState
{
    public bool IsInGuild { get; set; }
    public bool IsGuildLeader { get; set; }
    public bool CanGuildInvite { get; set; }
    public bool GuildRenameRequired { get; set; }
    public bool AreGuildEventsEnabled { get; set; }
    public bool CanEditOfficerNote { get; set; }
    public bool CanSpeakInGuildChat { get; set; }
    public bool CanViewOfficerNote { get; set; }
    public bool IsEncounterGuildNewsEnabled { get; set; }
    public bool IsGuildOfficer { get; set; }
    public bool RenameStatusRequestAccepted { get; set; }
    public int GuildRankCount { get; set; }
    public int GuildBankTabCount { get; set; }
    public int CurrentGuildBankTab { get; set; } = 1;
    public int GuildFactionGroup { get; set; }
    public int GuildPerkCount { get; set; }
    public int? SelectedGuildRankOrder { get; set; }
    public int GuildNewsFilterMask { get; set; }
    public string InfoText { get; set; } = string.Empty;
    public string Motd { get; set; } = string.Empty;
    public WowClubFinderTabardInfoState? DefaultTabardInfo { get; set; }
    public IReadOnlyList<int?>? LegacyTabardFileIds { get; set; }

    public IList<WowGuildNewsInfo> News { get; } = [];

    public IDictionary<string, int> RankOrderByGuid { get; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<int, IReadOnlyList<bool>> RankFlagsByOrder { get; } =
        new Dictionary<int, IReadOnlyList<bool>>();

    public IDictionary<string, WowClubFinderTabardInfoState> TabardInfoByUnit {
        get;
    } = new Dictionary<string, WowClubFinderTabardInfoState>(
        StringComparer.OrdinalIgnoreCase);

    public ISet<string> MemberNames { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public IDictionary<WowGuildRankAssignmentKey, bool>
        RankAssignmentAllowed { get; } =
            new Dictionary<WowGuildRankAssignmentKey, bool>();

    public IDictionary<WowGuildRecipeQueryKey, int>
        UpdatedRecipeSpellIds { get; } =
            new Dictionary<WowGuildRecipeQueryKey, int>();

    public IDictionary<string, WowGuildNote> NotesByGuid { get; } =
        new Dictionary<string, WowGuildNote>(
            StringComparer.OrdinalIgnoreCase);

    public IList<WowGuildInfoRequest> Requests { get; } = [];
}
