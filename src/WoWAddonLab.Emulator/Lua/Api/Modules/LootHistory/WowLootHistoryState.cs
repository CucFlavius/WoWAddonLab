using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowLootHistoryState
{
    public int Time { get; set; }
    public IList<WowLootEncounterState> Encounters { get; } =
        new List<WowLootEncounterState>();
    public IDictionary<int, IReadOnlyList<WowLootDropState>>
        SortedDropsByEncounterId { get; } =
        new Dictionary<int, IReadOnlyList<WowLootDropState>>();
}
