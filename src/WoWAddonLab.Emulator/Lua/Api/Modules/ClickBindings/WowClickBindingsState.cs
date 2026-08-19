using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowClickBindingsState
{
    public bool TutorialShown { get; set; }
    public bool AssumePositiveSpellIdsAreClickBindable { get; set; } = true;
    public ISet<int> ClickBindableSpellIds { get; } = new HashSet<int>();
    public ISet<int> NonClickBindableSpellIds { get; } = new HashSet<int>();
    public IDictionary<string, int> SpellIdsByName { get; } =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    public IList<WowClickBindingInfoState> Profile { get; } =
        new List<WowClickBindingInfoState>();
    public WowExecutedClickBindingState? LastExecutedBinding { get; set; }

    internal bool CanSpellBeClickBound(int spellId) =>
        spellId > 0 &&
        !NonClickBindableSpellIds.Contains(spellId) &&
        (AssumePositiveSpellIdsAreClickBindable || ClickBindableSpellIds.Contains(spellId));
}
