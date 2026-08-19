using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCombatLogState
{
    public bool FilteredEventsEnabled { get; set; }
    public int EntryRetentionTime { get; set; } = 300;
    public int MessageLimit { get; set; } = 1000;
    public bool IsRestricted => true;
    public IList<WowCombatLogEntry> Entries { get; } = [];
    public IList<WowCombatLogEventFilter> EventFilters { get; } = [];
    public WowCombatLogEntry? CurrentEvent { get; set; }
    public int? CurrentEntryIndex { get; internal set; }
    public int ApplyFilterSettingsCount { get; internal set; }
    public int RefilterEntriesCount { get; internal set; }
    public WowCombatLogMessage? LastCreatedMessage { get; internal set; }
}
