namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTieredEntranceProgress(int QuestId, string Description, bool Completed);

public sealed class WowDelvesTaleState
{
    public bool HasActiveTale { get; set; }
    public List<WowTieredEntranceProgress> ActiveTieredEntranceProgress { get; } = [];
    public int ClearActiveTaleRequestCount { get; set; }
}
