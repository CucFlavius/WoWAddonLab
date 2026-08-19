namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCurrencyInfoState
{
    public WowCurrencyInfoState()
    {
        Currencies[1602] = new WowCurrencyDefinition
        {
            CurrencyId = 1602,
            Name = "Conquest",
            IconFileId = 463448
        };
        Currencies[1792] = new WowCurrencyDefinition
        {
            CurrencyId = 1792,
            Name = "Honor",
            IconFileId = 463450
        };
    }

    public IDictionary<int, WowCurrencyDefinition> Currencies { get; } =
        new Dictionary<int, WowCurrencyDefinition>();

    public IList<int> CurrencyList { get; } = new List<int>();
    public IDictionary<(int CurrencyId, int Quantity), WowBasicCurrencyInfo>
        ContainerInfo { get; } =
        new Dictionary<(int, int), WowBasicCurrencyInfo>();
    public IDictionary<int, WowBasicCurrencyInfo> BasicInfo { get; } =
        new Dictionary<int, WowBasicCurrencyInfo>();
    public IDictionary<int, WowPlayerCurrencyCategoryInfo> Categories { get; } =
        new Dictionary<int, WowPlayerCurrencyCategoryInfo>();
    public IDictionary<int, IReadOnlyList<WowCharacterCurrencyData>>
        AccountCharacterData { get; } =
        new Dictionary<int, IReadOnlyList<WowCharacterCurrencyData>>();
    public IList<WowCurrencyTransferTransaction> TransferTransactions { get; } =
        new List<WowCurrencyTransferTransaction>();
    public IDictionary<int, WowCurrencyTransferEligibility>
        TransferEligibility { get; } =
        new Dictionary<int, WowCurrencyTransferEligibility>();
    public ISet<(int CurrencyId, int Quantity)> CurrencyContainers { get; } =
        new HashSet<(int, int)>();

    public WowCurrencyFilterType Filter { get; set; }
    public bool AccountCharacterCurrencyDataReady { get; set; } = true;
    public bool CurrencyTransferTransactionDataReady { get; set; } = true;
    public bool CurrencyTransferInProgress { get; set; }
    public int? LastPickedUpCurrencyId { get; set; }
    public int? LastRequestedAccountCurrencyId { get; set; }
    public (string CharacterGuid, int CurrencyId, uint Quantity)?
        LastCurrencyTransferRequest { get; set; }

    public WowCurrencyDefinition? Find(int currencyId) =>
        Currencies.TryGetValue(currencyId, out var definition)
            ? definition
            : null;
}
