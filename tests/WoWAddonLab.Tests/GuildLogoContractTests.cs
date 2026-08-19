using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class GuildLogoContractTests
{
    [Fact]
    public void MissingUnitTabardReturnsZeroValues()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:0",
            session.Lua.Evaluate(
                "return select('#',GetGuildLogoInfo())..':'.." +
                "select('#',GetGuildLogoInfo('missing'))"));
    }

    [Fact]
    public void NonStringArgumentUsesPlayerAndReturnsNativeElevenValueShape()
    {
        using var session = new EmulatorSession();
        session.Lua.Guild.DefaultTabardInfo = CreateTabard(123, 9);

        Assert.Equal(
            "11:10:20:30:40:50:60:70:80:90:123:9:11",
            session.Lua.Evaluate(
                "local values={GetGuildLogoInfo({})}; " +
                "return #values..':'..table.concat(values,':')..':'.." +
                "select('#',GetGuildLogoInfo('player'))"));
    }

    [Fact]
    public void StringCoercibleUnitIsResolvedAndZeroFileIdFailsTheNativePredicate()
    {
        using var session = new EmulatorSession();
        session.Lua.Guild.TabardInfoByUnit["17"] = CreateTabard(321, 4);
        session.Lua.Guild.TabardInfoByUnit["18"] = CreateTabard(0, 5);

        Assert.Equal(
            "11:10:321:4:0:0",
            session.Lua.Evaluate(
                "local a,b,c,d,e,f,g,h,i,fileID,style=GetGuildLogoInfo(17); " +
                "return select('#',GetGuildLogoInfo(17))..':'..a..':'.." +
                "tostring(fileID)..':'..style..':'.." +
                "select('#',GetGuildLogoInfo(18))..':'.." +
                "select('#',GetGuildLogoInfo(19))"));
    }

    private static WowClubFinderTabardInfoState CreateTabard(
        int emblemFileId,
        int emblemStyle)
    {
        return new WowClubFinderTabardInfoState
        {
            BackgroundColor = new(10d / 255d, 20d / 255d, 30d / 255d),
            BorderColor = new(40d / 255d, 50d / 255d, 60d / 255d),
            EmblemColor = new(70d / 255d, 80d / 255d, 90d / 255d),
            EmblemFileId = emblemFileId,
            EmblemStyle = emblemStyle
        };
    }
}
