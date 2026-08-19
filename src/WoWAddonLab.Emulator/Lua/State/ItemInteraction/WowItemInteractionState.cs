namespace WoWAddonLab.Emulator.Lua;

public sealed class WowItemInteractionState
{
    public int SlotIndex { get; set; } = -1;
    public int InteractionRecordId { get; set; }
    public int InteractionSpellId { get; set; }
    public WowItemLocation? PendingItem { get; internal set; }
    public WowItemInteractionChargeInfo ChargeInfo { get; set; } =
        new(0, 0, 0);
    public WowItemInteractionInfo? Info { get; set; }

    public IDictionary<WowItemLocation, WowItemInteractionConversionCost>
        ConversionCosts { get; } =
            new Dictionary<
                WowItemLocation,
                WowItemInteractionConversionCost>();

    public ISet<WowItemLocation> EligiblePendingItems { get; } =
        new HashSet<WowItemLocation>();

    public IList<WowItemInteractionPerformRequest> PerformRequests { get; } =
        new List<WowItemInteractionPerformRequest>();
}
