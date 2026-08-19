namespace WoWAddonLab.Emulator.Lua;

public sealed class WowBankState
{
    public ISet<byte> ViewableBankTypes { get; } = new HashSet<byte>();
    public ISet<byte> UsableBankTypes { get; } = new HashSet<byte>();
    public IDictionary<byte, int> LockedReasonByBankType { get; } =
        new Dictionary<byte, int>();
    public IDictionary<byte, IList<WowBankTabData>> PurchasedTabsByBankType {
        get;
    } = new Dictionary<byte, IList<WowBankTabData>>();
    public IDictionary<byte, WowNextPurchasableBankTabData>
        NextPurchasableTabByBankType { get; } =
            new Dictionary<byte, WowNextPurchasableBankTabData>();

    public ulong DepositedMoney { get; set; }
    public bool AccountBankItemAllowed { get; set; }
    public bool IsBankFrameOpen { get; set; }

    public int AutoDepositRequestCount { get; internal set; }
    public byte? LastAutoDepositBankType { get; internal set; }
    public int PurchaseRequestCount { get; internal set; }
    public byte? LastPurchaseBankType { get; internal set; }
    public int DepositRequestCount { get; internal set; }
    public byte? LastDepositBankType { get; internal set; }
    public ulong? LastDepositAmount { get; internal set; }
    public int WithdrawRequestCount { get; internal set; }
    public byte? LastWithdrawBankType { get; internal set; }
    public ulong? LastWithdrawAmount { get; internal set; }
    public int TabSettingsRequestCount { get; internal set; }
    public WowBankTabSettingsRequest? LastTabSettingsRequest { get; internal set; }
}
