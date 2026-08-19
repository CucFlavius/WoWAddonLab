namespace WoWAddonLab.Emulator.Lua;

public sealed class WowTransmogSetState
{
    public ISet<int> CollectedSourceIds { get; } = new HashSet<int>();
    public ISet<int> FavoriteVisualIds { get; } = new HashSet<int>();
    public ISet<int> CollectedSetIds { get; } = new HashSet<int>();
    public ISet<int> FavoriteSetIds { get; } = new HashSet<int>();
}
