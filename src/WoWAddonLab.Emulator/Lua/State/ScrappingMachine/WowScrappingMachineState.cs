namespace WoWAddonLab.Emulator.Lua;

public sealed class WowScrappingMachineState
{
    public WowItemLocation?[] PendingItems { get; } =
        new WowItemLocation?[9];

    public ISet<WowItemLocation> ScrappableItems { get; } =
        new HashSet<WowItemLocation>();

    public string? MachineName { get; set; }
    public WowItemLocation? CursorItemLocation { get; set; }
    public int CurrentScrappingIndex { get; set; }
    public bool IsScrapping { get; set; }
    public bool CanCastScrapSpell { get; set; } = true;
    public int ActiveSpellId { get; set; }

    public IList<WowScrappingRequest> ScrapRequests { get; } =
        new List<WowScrappingRequest>();
}
