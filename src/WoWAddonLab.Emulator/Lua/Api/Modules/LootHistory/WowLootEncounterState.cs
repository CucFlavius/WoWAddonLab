using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowLootEncounterState
{
    public int EncounterId { get; init; }
    public string EncounterName { get; init; } = string.Empty;
    public int StartTime { get; init; }
    public int Duration { get; init; }
}
