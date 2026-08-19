using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class LegacyLfgContractTests
{
    [Fact]
    public void LegacyLfgGlobalsPreserveNativeArityAndEmptyState()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:16:false:false:false:false:false::0:true:false:false:false:false:" +
            "0:0:0:0:6:false:0:0:true:true:false:0:false:false:false:false",
            session.Lua.Evaluate(
                "(function() " +
                "local server={GetLFGInfoServer(1)}; " +
                "local role={GetLFGRoleUpdate()}; " +
                "local ready,bg=GetLFGReadyCheckUpdate(); " +
                "return table.concat({" +
                "select('#',GetLFGProposal()),#server," +
                "tostring(server[1]),tostring(server[2]),tostring(server[3])," +
                "tostring(server[4]),tostring(server[5]),server[6],server[7]," +
                "tostring(server[8]==nil),tostring(server[9]),tostring(server[10])," +
                "tostring(server[11]),tostring(server[12]),server[13],server[14]," +
                "server[15],server[16],#role,tostring(role[1]),role[2],role[3]," +
                "tostring(role[4]==nil),tostring(role[5]==nil),tostring(role[6])," +
                "select('#',GetPartyLFGID()),tostring(IsPartyLFG())," +
                "tostring(IsAllowedToUserTeleport()),tostring(ready),tostring(bg)},':') " +
                "end)()"));
    }

    [Fact]
    public void LegacyLfgGlobalsExposeRecoveredProposalQueueAndCategoryState()
    {
        using var session = new EmulatorSession();
        var lfg = session.Lua.LfgInfo;
        lfg.CurrentProposal = new WowLfgProposalState(
            101,
            2,
            3,
            "Test Dungeon",
            444,
            "TANK",
            true,
            4,
            2,
            5,
            true,
            false,
            2,
            true);
        lfg.ServerInfoByDungeonId[101] = new WowLfgServerInfoState(
            101,
            2,
            true,
            true,
            false,
            true,
            false,
            "note",
            3,
            true,
            true,
            false,
            true,
            9);
        lfg.RoleUpdate = new WowLfgRoleUpdateState(
            true,
            3,
            5,
            2,
            101,
            true);
        lfg.ReadyCheckInProgress = true;
        lfg.PartyLfgDungeonId = 0x100005;
        lfg.PartyLfgSecondaryDungeonId = 0x200006;
        lfg.IsAllowedToUserTeleport = true;
        lfg.DungeonCategoryById[101] = 2;

        Assert.Equal(
            "15:true:101:2:3:Test Dungeon:444:TANK:true:4:2:5:true:false:2:true",
            session.Lua.Evaluate(
                "(function() local p={GetLFGProposal()}; " +
                "for i=1,#p do p[i]=tostring(p[i]) end; " +
                "return #p..':'..table.concat(p,':') end)()"));
        Assert.Equal(
            "16:true:true:false:true:false:note:3:2:true:true:false:true:0:0:0:9",
            session.Lua.Evaluate(
                "(function() local v={GetLFGInfoServer('2.9','101.9')}; " +
                "for i=1,#v do v[i]=tostring(v[i]) end; " +
                "return #v..':'..table.concat(v,':') end)()"));
        Assert.Equal(
            "6:true:3:5:2:101:true:true:false:2:2:5:6:true:true",
            session.Lua.Evaluate(
                "(function() local r={GetLFGRoleUpdate()}; local ready,bg=GetLFGReadyCheckUpdate(); " +
                "local primary,secondary=GetPartyLFGID(); " +
                "return table.concat({#r,tostring(r[1]),r[2],r[3],r[4],r[5]," +
                "tostring(r[6]),tostring(ready),tostring(bg)," +
                "GetLFGCategoryForID('101.9'),GetLFGCategoryForID(101)," +
                "primary,secondary,tostring(IsPartyLFG())," +
                "tostring(IsAllowedToUserTeleport())},':') end)()"));
        Assert.Equal(
            "16:false:false:false",
            session.Lua.Evaluate(
                "(function() return table.concat({" +
                "select('#',GetLFGInfoServer(2,{}))," +
                "tostring(pcall(GetLFGInfoServer))," +
                "tostring(pcall(GetLFGInfoServer,0))," +
                "tostring(pcall(GetLFGCategoryForID,{}))},':') end)()"));

        lfg.PartyLfgSecondaryDungeonId = 0x200000;
        Assert.Equal(
            "2:true",
            session.Lua.Evaluate(
                "(function() local _,secondary=GetPartyLFGID(); " +
                "return select('#',GetPartyLFGID())..':'..tostring(secondary==nil) end)()"));
    }

    [Fact]
    public void RaidInfoRequestIsAsynchronousAndSavedCountsAreStateBacked()
    {
        using var session = new EmulatorSession();
        session.Lua.Instance.SavedInstanceCount = 3;
        session.Lua.Instance.SavedWorldBossCount = 2;

        Assert.Equal(
            "0:0:3:2",
            session.Lua.Evaluate(
                "(function() local count=0; local listener=CreateFrame('Frame'); " +
                "listener:RegisterEvent('UPDATE_INSTANCE_INFO'); " +
                "listener:SetScript('OnEvent',function() count=count+1 end); " +
                "local returns=select('#',RequestRaidInfo()); " +
                "return table.concat({returns,count,GetNumSavedInstances()," +
                "GetNumSavedWorldBosses()},':') end)()"));
        Assert.Equal(1, session.Lua.Instance.RaidInfoRequestCount);
    }
}
