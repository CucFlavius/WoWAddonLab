using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAdventureMapState
{
    public int MapId { get; set; } = 84;
    public string? TextureKit { get; set; } = "adventure-map";
    public IList<WowAdventureMapZoneChoiceState> ZoneChoices { get; } =
        new List<WowAdventureMapZoneChoiceState>();
    public IList<WowAdventureMapQuestOfferState> QuestOffers { get; } =
        new List<WowAdventureMapQuestOfferState>();
    public IList<WowAdventureMapInsetState> Insets { get; } =
        new List<WowAdventureMapInsetState>();
    public IDictionary<int, WowAdventureMapQuestInfoState> Quests { get; } =
        new Dictionary<int, WowAdventureMapQuestInfoState>();
    public IDictionary<int, WowAdventureMapQuestPortraitInfoState> QuestPortraits { get; } =
        new Dictionary<int, WowAdventureMapQuestPortraitInfoState>();
    public ISet<int> StartableQuestIds { get; } = new HashSet<int>();
    public int? LastStartedQuestId { get; internal set; }
}
