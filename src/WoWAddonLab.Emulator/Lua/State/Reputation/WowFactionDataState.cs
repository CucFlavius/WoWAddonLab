namespace WoWAddonLab.Emulator.Lua;

public sealed class WowFactionDataState
{
    public required int FactionId { get; init; }
    public string? Name { get; init; }
    public string? Description { get; set; }
    public int Reaction { get; set; }
    public long CurrentReactionThreshold { get; set; }
    public long NextReactionThreshold { get; set; }
    public long CurrentStanding { get; set; }
    public bool AtWarWith { get; set; }
    public bool CanToggleAtWar { get; set; }
    public bool IsChild { get; set; }
    public bool IsHeader { get; set; }
    public bool IsHeaderWithRep { get; set; }
    public bool IsCollapsed { get; set; }
    public bool IsWatched { get; set; }
    public bool HasBonusRepGain { get; set; }
    public bool CanSetInactive { get; set; }
    public bool IsAccountWide { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsParagon { get; set; }
    public bool IsParagonForCurrentPlayer { get; set; }
    public bool IsMajorFaction { get; set; }
}
