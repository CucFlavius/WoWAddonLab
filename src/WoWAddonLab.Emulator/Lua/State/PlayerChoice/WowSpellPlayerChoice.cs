namespace WoWAddonLab.Emulator.Lua;

public static class WowSpellPlayerChoice
{
    public const string TaleboundTextureKit = "talebound";
    public static IReadOnlyList<int> DefaultSpellIds { get; } = [1459, 21562, 6673];

    public static WowPlayerChoiceInfo Build(
        IWowSpellProvider? spells,
        string textureKit,
        IReadOnlyList<int> spellIds)
    {
        var choice = new WowPlayerChoiceInfo
        {
            ObjectGuid = "Creature-0-0-0-0-0-0000000000",
            ChoiceId = 1,
            UiTextureKit = textureKit,
            HideWarboardHeader = true
        };
        for (var index = 0; index < spellIds.Count; index++)
        {
            var spell = spells?.Find(spellIds[index]);
            var option = new WowPlayerChoiceOptionInfo
            {
                Id = index + 1,
                Header = spell?.Name ?? $"Spell #{spellIds[index]}",
                ChoiceArtId = spell?.IconId ?? 0,
                SpellId = spellIds[index],
                Rarity = index % 4,
                MaxStacks = 1
            };
            option.Buttons.Add(new WowPlayerChoiceButtonInfo { Id = index + 1, Text = "Choose" });
            choice.Options.Add(option);
        }
        return choice;
    }
}
