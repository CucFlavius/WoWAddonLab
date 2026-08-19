namespace WoWAddonLab.Tests;

public sealed class BlizzardStartupDefaultsTests
{
    [Fact]
    public void ClientOwnedUiOpacityCVarsUseBinaryDefaults()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0.5:0.5:0.65:0.65",
            session.Lua.Evaluate(
                "return table.concat({" +
                "C_CVar.GetCVar('partyBackgroundOpacity')," +
                "C_CVar.GetCVarDefault('partyBackgroundOpacity')," +
                "C_CVar.GetCVar('spellActivationOverlayOpacity')," +
                "C_CVar.GetCVarDefault('spellActivationOverlayOpacity')}, ':')"));
    }

    [Fact]
    public void MissingDelvesCompanionUsesNativeTraitTreeFallback()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "1:0:0",
            session.Lua.Evaluate(
                "return table.concat({" +
                "select('#', C_DelvesUI.GetTraitTreeForCompanion())," +
                "C_DelvesUI.GetTraitTreeForCompanion()," +
                "select('#', C_DelvesUI.GetPlayerCompanionPDEID())}, ':')"));
    }

    [Fact]
    public void ReagentBagUsesItsNativeInventorySlot()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "24:3:false",
            session.Lua.Evaluate(
                "local slot,texture,relic=GetInventorySlotInfo('ReagentBag0Slot');" +
                "return table.concat({slot,select('#',GetInventorySlotInfo('ReagentBag0Slot')),tostring(relic)},':')"));
    }

    [Fact]
    public void DevelopmentProfileEnablesAccountGatedMicroMenuFeatures()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:true",
            session.Lua.Evaluate(
                "return table.concat({" +
                "tostring((HasCompletedAnyAchievement() or IsInGuild()) and CanShowAchievementUI())," +
                "tostring(C_Housing.IsHousingServiceEnabled())," +
                "tostring(C_StorePublic.IsEnabled())},':')"));
    }
}
