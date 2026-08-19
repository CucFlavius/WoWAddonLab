namespace WoWAddonLab.Emulator.Lua;

public sealed class WowStorePublicState
{
    public bool IsEnabled { get; set; } = true;

    public ISet<uint> PurchaseableProductGroupIds { get; } =
        new HashSet<uint>();

    public bool? LastReportedShown { get; internal set; }

    public string? LastContextKey { get; internal set; }

    public int UiShownReportCount { get; internal set; }
}
