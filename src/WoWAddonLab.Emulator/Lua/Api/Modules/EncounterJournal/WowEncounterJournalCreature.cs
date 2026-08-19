using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowEncounterJournalCreature
{
    public int CreatureId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int DisplayInfoId { get; init; }
    public uint? IconImage { get; init; }
    public int UiModelSceneId { get; init; }
}
