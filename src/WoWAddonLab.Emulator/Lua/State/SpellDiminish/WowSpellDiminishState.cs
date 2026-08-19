namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSpellDiminishState
{
    public IDictionary<WowSpellDiminishCategory, WowSpellDiminishCategoryInfo>
        Categories { get; } =
        new Dictionary<WowSpellDiminishCategory, WowSpellDiminishCategoryInfo>();

    public bool PvpRuntimeFilterEnabled { get; set; }
    public ISet<WowSpellDiminishCategory> PvpTrackedCategories { get; } =
        new HashSet<WowSpellDiminishCategory>();
}
