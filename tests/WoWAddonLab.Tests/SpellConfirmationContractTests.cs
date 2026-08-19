using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class SpellConfirmationContractTests
{
    [Fact]
    public void MissingPromptsReturnOneEmptyTableAndArgumentsAreIgnored()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:1",
            session.Lua.Evaluate(
                "return #GetSpellConfirmationPromptsInfo(false,17)..':'.." +
                "select('#',GetSpellConfirmationPromptsInfo({},'ignored'))"));
    }

    [Fact]
    public void MissingPlayerReturnsZeroValuesInsteadOfAnEmptyTable()
    {
        using var session = new EmulatorSession();
        session.Lua.SpellConfirmation.IsPlayerAvailable = false;

        Assert.Equal(
            "0",
            session.Lua.Evaluate(
                "return select('#',GetSpellConfirmationPromptsInfo())"));
    }

    [Fact]
    public void PromptTableUsesNativeFieldNamesDefaultsAndOptionalCurrencyFields()
    {
        using var session = new EmulatorSession();
        session.Lua.SpellConfirmation.Prompts.Add(new WowSpellConfirmationPrompt
        {
            SpellId = 123,
            ConfirmType = 4,
            Text = "Confirm this",
            DifficultyId = 0,
            DisplayItemId = 456,
            ItemContext = 7,
            TreasureContextLevel = 8
        });

        Assert.Equal(
            "1:123:4:Confirm this:-1:nil:nil:14:456:7:8",
            session.Lua.Evaluate(
                "local prompts=GetSpellConfirmationPromptsInfo(); local p=prompts[1]; " +
                "return table.concat({#prompts,p.spellID,p.confirmType,p.text,p.duration," +
                "tostring(p.currencyID),tostring(p.currencyCost),p.difficultyID," +
                "p.displayItemID,p.itemContext,p.treasureContextLevel},':')"));
    }

    [Fact]
    public void PromptDurationUsesSignedWrappedTickDeltaWithoutExpiryClamping()
    {
        using var session = new EmulatorSession();
        session.Tick(0.1);
        var prompt = new WowSpellConfirmationPrompt
        {
            ExpirationTickMilliseconds = session.Lua.FrameTime.TickMilliseconds + 3_999,
            CurrencyId = 81,
            CurrencyCost = 5
        };
        session.Lua.SpellConfirmation.Prompts.Add(prompt);

        Assert.Equal(
            "3:81:5",
            session.Lua.Evaluate(
                "local p=GetSpellConfirmationPromptsInfo()[1]; " +
                "return p.duration..':'..p.currencyID..':'..p.currencyCost"));

        prompt.ExpirationTickMilliseconds =
            session.Lua.FrameTime.TickMilliseconds - 1_500;
        Assert.Equal(
            "-1",
            session.Lua.Evaluate(
                "return GetSpellConfirmationPromptsInfo()[1].duration"));
    }
}
