namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAzeriteEmpoweredItemData
{
    public IList<WowAzeriteEmpoweredItemTierInfo> Tiers { get; } =
        new List<WowAzeriteEmpoweredItemTierInfo>();

    public ISet<int> SelectedPowerIds { get; } = new HashSet<int>();

    public ISet<int> SelectablePowerIds { get; } = new HashSet<int>();

    public bool? HasAnyUnselectedPowersOverride { get; set; }

    public bool HasBeenViewedFlag { get; set; }

    internal bool HasAnyUnselectedPowers =>
        HasAnyUnselectedPowersOverride ??
        Tiers.Any(tier =>
            tier.AzeritePowerIds.Count > 0 &&
            !tier.AzeritePowerIds.Any(SelectedPowerIds.Contains));

    internal bool HasBeenViewed =>
        HasBeenViewedFlag || SelectedPowerIds.Count > 0;

    internal bool TryGetTierIndex(int powerId, out byte tierIndex)
    {
        for (var index = 0; index < Tiers.Count && index <= byte.MaxValue; index++)
        {
            if (!Tiers[index].AzeritePowerIds.Contains(powerId))
                continue;
            tierIndex = (byte)index;
            return true;
        }

        tierIndex = 0;
        return false;
    }
}
