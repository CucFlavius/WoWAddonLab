namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPetInfoState
{
    public string? TalentTreeName { get; set; }
    public bool AssistMode { get; set; }
    public string? LastRename { get; set; }
    public int? LastRenamedPetNumber { get; set; }
    public int? LastAbandonedPetNumber { get; set; }
    public IDictionary<int, int> SpellIdsByActionId { get; } = new Dictionary<int, int>();
    public ISet<int> PassiveActionIds { get; } = new HashSet<int>();
}
