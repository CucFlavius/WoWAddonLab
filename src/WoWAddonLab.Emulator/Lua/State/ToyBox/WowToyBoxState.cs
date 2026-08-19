namespace WoWAddonLab.Emulator.Lua;

public sealed class WowToyBoxState
{
    public ISet<int> OwnedItemIds { get; } = new HashSet<int>();
}
