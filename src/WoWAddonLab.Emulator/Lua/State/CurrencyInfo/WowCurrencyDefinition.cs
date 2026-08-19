namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCurrencyDefinition
{
    public int CurrencyId { get; init; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsHeader { get; set; }
    public bool IsHeaderExpanded { get; set; }
    public int CurrencyListDepth { get; set; }
    public bool IsTypeUnused { get; set; }
    public bool IsShowInBackpack { get; set; }
    public int Quantity { get; set; }
    public int TrackedQuantity { get; set; }
    public int IconFileId { get; set; }
    public int MaxQuantity { get; set; }
    public bool CanEarnPerWeek { get; set; }
    public int QuantityEarnedThisWeek { get; set; }
    public bool IsTradeable { get; set; }
    public byte Quality { get; set; }
    public int MaxWeeklyQuantity { get; set; }
    public int TotalEarned { get; set; }
    public bool Discovered { get; set; }
    public bool UseTotalEarnedForMaxQuantity { get; set; }
    public bool IsAccountWide { get; set; }
    public float? TransferPercentage { get; set; } = 0;
    public int RechargingCycleDurationMilliseconds { get; set; }
    public int RechargingAmountPerCycle { get; set; }
    public int? FactionId { get; set; }
    public bool? WarModeBonusApplies { get; set; }
    public bool? LimitWarModeBonusOncePerTooltip { get; set; }

    public bool IsAccountTransferable =>
        TransferPercentage is > 0;
}
