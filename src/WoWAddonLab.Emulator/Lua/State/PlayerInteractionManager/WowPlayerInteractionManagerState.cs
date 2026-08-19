namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPlayerInteractionManagerState
{
    public int CurrentInteractionType { get; set; }
    public int PendingInteractionType { get; set; }
    public bool HasActiveInteraction { get; set; }
    public bool HasPendingInteraction { get; set; }
    public bool IsReplacingUnit { get; set; }

    public IDictionary<string, bool> InteractUnitResults { get; } =
        new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

    public ISet<int> ValidNpcInteractionTypes { get; } = new HashSet<int>();
    public IList<WowPlayerInteractionRequest> InteractionRequests { get; } =
        new List<WowPlayerInteractionRequest>();

    public int ClearInteractionRequests { get; internal set; }
    public int ConfirmationInteractionRequests { get; internal set; }
    public int ReopenInteractionRequests { get; internal set; }
    public int? LastClearInteractionType { get; internal set; }
    public int? LastConfirmationInteractionType { get; internal set; }
}
