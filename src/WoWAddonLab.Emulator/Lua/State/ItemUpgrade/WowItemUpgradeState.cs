namespace WoWAddonLab.Emulator.Lua;

public sealed class WowItemUpgradeState
{
    public bool IsOpen { get; set; }

    public WowItemUpgradeItemInfoState? CurrentItemInfo { get; set; }

    public ISet<WowItemLocation> UpgradableItemLocations { get; } =
        new HashSet<WowItemLocation>();

    public int ClearRequestCount { get; set; }

    public int CloseRequestCount { get; set; }
}
