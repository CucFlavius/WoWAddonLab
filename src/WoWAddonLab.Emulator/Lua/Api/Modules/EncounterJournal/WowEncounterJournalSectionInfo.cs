using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed record WowEncounterJournalSectionInfo
{
    public int SpellId { get; init; }
    public string? Title { get; init; }
    public string? Description { get; init; }
    public int HeaderType { get; init; }
    public uint AbilityIcon { get; init; }
    public int CreatureDisplayId { get; init; }
    public int UiModelSceneId { get; init; }
    public int? SiblingSectionId { get; init; }
    public int? FirstChildSectionId { get; init; }
    public bool FilteredByDifficulty { get; init; }
    public string Link { get; init; } = string.Empty;
    public bool StartsOpen { get; init; }
}
