using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class LocaleGlobalsContractTests
{
    [Fact]
    public void CurrentAndOsLocaleUseIndependentNativeEnumState()
    {
        using var session = new EmulatorSession();
        session.Lua.Localization.CurrentLocale = WowClientLocale.DeDE;
        session.Lua.Localization.OsLocale = WowClientLocale.ZhTW;

        Assert.Equal(
            "deDE:zhTW",
            session.Lua.Evaluate("return GetLocale()..':'..GetOSLocale()"));
    }

    [Fact]
    public void LocaleQueriesIgnoreArgumentsAndReturnOneOptionalValue()
    {
        using var session = new EmulatorSession();
        session.Lua.Localization.CurrentLocale = WowClientLocale.PtBR;
        session.Lua.Localization.OsLocale = WowClientLocale.ItIT;

        Assert.Equal(
            "1:ptBR:1:itIT",
            session.Lua.Evaluate(
                "local function pack(...) return select('#',...),... end; " +
                "local currentCount,current=pack(GetLocale(false,17)); " +
                "local osCount,os=pack(GetOSLocale({},'ignored')); " +
                "return table.concat({currentCount,current,osCount,os},':')"));
    }

    [Fact]
    public void NullLocaleTableSlotPushesNilAndCVarDoesNotMutateCurrentLocale()
    {
        using var session = new EmulatorSession();
        session.Lua.Localization.CurrentLocale = WowClientLocale.None;
        session.Lua.Localization.OsLocale = WowClientLocale.None;
        session.Lua.CVars.Define("textLocale", "enUS", "deDE");

        Assert.Equal(
            "1:nil:1:nil",
            session.Lua.Evaluate(
                "return select('#',GetLocale())..':'..tostring(GetLocale())..':'.." +
                "select('#',GetOSLocale())..':'..tostring(GetOSLocale())"));
    }

    [Theory]
    [InlineData("en-US", WowClientLocale.EnUS)]
    [InlineData("de-DE", WowClientLocale.DeDE)]
    [InlineData("zh-TW", WowClientLocale.ZhTW)]
    [InlineData("es-AR", WowClientLocale.EsMX)]
    [InlineData("ja-US", WowClientLocale.EnUS)]
    [InlineData("ja-JP", WowClientLocale.EnUS)]
    public void OsCultureFallbackMatchesNativeSupportedLocaleResolution(
        string cultureName,
        WowClientLocale expected)
    {
        Assert.Equal(expected, WowLocalizationState.ResolveOsLocale(cultureName));
    }
}
