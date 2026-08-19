using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class DamageMeterContractTests
{
    [Fact]
    public void RegistersExactSurfaceEnumsAndNativeEmptyContracts()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "8:2:false::0:0:0:true:0:0:0:true:" +
            "0:1:2:3:5:0:4:3:0:2:3:0:2:5:0:4:" +
            "9:0:8:11:0:10",
            session.Lua.Evaluate(
                "local count=0; for _ in pairs(C_DamageMeter) do count=count+1 end;" +
                "local available,reason=C_DamageMeter.IsDamageMeterAvailable();" +
                "local combat=C_DamageMeter.GetCombatSessionFromID(1,0);" +
                "local source=C_DamageMeter.GetCombatSessionSourceFromID(1,0);" +
                "return table.concat({" +
                "count,select('#',C_DamageMeter.IsDamageMeterAvailable())," +
                "tostring(available),reason,#C_DamageMeter.GetAvailableCombatSessions()," +
                "#combat.combatSources,combat.maxAmount," +
                "tostring(combat.durationSeconds==nil)," +
                "#source.combatSpells,source.maxAmount,source.totalAmount," +
                "tostring(C_DamageMeter.GetSessionDurationSeconds(0)==nil)," +
                "Enum.DamageMeterCombineSessionType.None," +
                "Enum.DamageMeterCombineSessionType.ChallengeMode," +
                "Enum.DamageMeterCombineSessionType.Arena," +
                "Enum.DamageMeterCombineSessionType.ArenaMultiRound," +
                "Enum.DamageMeterOverrideTypeMeta.NumValues," +
                "Enum.DamageMeterOverrideTypeMeta.MinValue," +
                "Enum.DamageMeterOverrideTypeMeta.MaxValue," +
                "Enum.DamageMeterSessionTypeMeta.NumValues," +
                "Enum.DamageMeterSessionTypeMeta.MinValue," +
                "Enum.DamageMeterSessionTypeMeta.MaxValue," +
                "Enum.DamageMeterSourceDisplayTypeMeta.NumValues," +
                "Enum.DamageMeterSourceDisplayTypeMeta.MinValue," +
                "Enum.DamageMeterSourceDisplayTypeMeta.MaxValue," +
                "Enum.DamageMeterSpellDetailsDisplayTypeMeta.NumValues," +
                "Enum.DamageMeterSpellDetailsDisplayTypeMeta.MinValue," +
                "Enum.DamageMeterSpellDetailsDisplayTypeMeta.MaxValue," +
                "Enum.DamageMeterStorageTypeMeta.NumValues," +
                "Enum.DamageMeterStorageTypeMeta.MinValue," +
                "Enum.DamageMeterStorageTypeMeta.MaxValue," +
                "Enum.DamageMeterTypeMeta.NumValues," +
                "Enum.DamageMeterTypeMeta.MinValue," +
                "Enum.DamageMeterTypeMeta.MaxValue},':')"));
    }

    [Fact]
    public void EnforcesRecoveredUInt32EnumAndOptionalArgumentContracts()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:true:false:false:false:true:true:false:false",
            session.Lua.Evaluate(
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "return table.concat({" +
                "tostring(ok(C_DamageMeter.GetCombatSessionFromID,'1.9','10'))," +
                "tostring(ok(C_DamageMeter.GetCombatSessionFromID,'4294967295',0))," +
                "tostring(ok(C_DamageMeter.GetCombatSessionFromType,'2.9','0'))," +
                "tostring(ok(C_DamageMeter.GetCombatSessionFromID,-1,0))," +
                "tostring(ok(C_DamageMeter.GetCombatSessionFromID,4294967296,0))," +
                "tostring(ok(C_DamageMeter.GetCombatSessionFromID,1,11))," +
                "tostring(ok(C_DamageMeter.GetCombatSessionSourceFromID,1,0,nil,'3.9'))," +
                "tostring(ok(C_DamageMeter.GetCombatSessionSourceFromType,0,0,'Player-1',nil))," +
                "tostring(ok(C_DamageMeter.GetCombatSessionFromType,3,0))," +
                "tostring(ok(C_DamageMeter.GetCombatSessionSourceFromID,1,0,{}))" +
                "},':')"));
    }

    [Fact]
    public void ProjectsRecoveredAvailableSessionAndCombatSessionShapes()
    {
        using var session = new EmulatorSession();
        var meter = session.Lua.DamageMeter;
        meter.IsAvailable = true;
        meter.AvailabilityReason = "Ready";
        meter.AvailableSessions.Add(
            new WowDamageMeterAvailableCombatSession(44, "Boss", 12));
        meter.SessionDurations[WowDamageMeterSessionType.Overall] = 12;
        meter.SessionsById[(44, WowDamageMeterType.DamageDone)] =
            new WowDamageMeterCombatSession(
                [
                    new WowDamageMeterCombatSource(
                        "Player-1",
                        123,
                        "Alpha",
                        "MAGE",
                        456,
                        900,
                        75.5,
                        true,
                        7,
                        11,
                        "elite",
                        WowDamageMeterSourceDisplayType.Ally,
                        "Alliance")
                ],
                900,
                900,
                12);
        meter.SessionsByType[
            (WowDamageMeterSessionType.Overall, WowDamageMeterType.DamageDone)] =
            meter.SessionsById[(44, WowDamageMeterType.DamageDone)];

        Assert.Equal(
            "true:Ready:44:Boss:12:12:1:900:900:12:" +
            "Player-1:123:Alpha:MAGE:456:900:75.5:true:7:11:" +
            "elite:1:Alliance:1",
            session.Lua.Evaluate(
                "local available,reason=C_DamageMeter.IsDamageMeterAvailable();" +
                "local a=C_DamageMeter.GetAvailableCombatSessions()[1];" +
                "local c=C_DamageMeter.GetCombatSessionFromID(44,0);" +
                "local s=c.combatSources[1];" +
                "local byType=C_DamageMeter.GetCombatSessionFromType(0,0);" +
                "return table.concat({" +
                "tostring(available),reason,a.sessionID,a.name,a.durationSeconds," +
                "C_DamageMeter.GetSessionDurationSeconds(0)," +
                "#c.combatSources,c.maxAmount,c.totalAmount,c.durationSeconds," +
                "s.sourceGUID,s.sourceCreatureID,s.name,s.classFilename," +
                "s.specIconID,s.totalAmount,s.amountPerSecond," +
                "tostring(s.isLocalPlayer),s.deathRecapID,s.deathTimeSeconds," +
                "s.classification,s.sourceDisplayType,s.factionGroup," +
                "#byType.combatSources},':')"));
    }

    [Fact]
    public void ProjectsRecoveredCombatSpellShapeAndResetsAllSessions()
    {
        using var session = new EmulatorSession();
        var meter = session.Lua.DamageMeter;
        var source = new WowDamageMeterCombatSessionSource(
            [
                new WowDamageMeterCombatSpell(
                    1234,
                    600,
                    50.25f,
                    "Target",
                    10,
                    true,
                    false,
                    new WowDamageMeterCombatSpellDetails(
                        "Target",
                        "WARRIOR",
                        "boss",
                        false,
                        true,
                        600,
                        789))
            ],
            600,
            600);
        meter.SourcesById[
            (44, WowDamageMeterType.DamageDone, "Player-1", 123)] = source;
        meter.SourcesByType[
            (WowDamageMeterSessionType.Current,
                WowDamageMeterType.DamageDone,
                "Player-1",
                123)] = source;
        meter.AvailableSessions.Add(
            new WowDamageMeterAvailableCombatSession(44, "Boss", 12));
        meter.SessionDurations[WowDamageMeterSessionType.Current] = 12;
        meter.SessionsByType[
            (WowDamageMeterSessionType.Expired, WowDamageMeterType.DamageDone)] =
            new WowDamageMeterCombatSession(
                [
                    new WowDamageMeterCombatSource(
                        null,
                        null,
                        "Should not be visible",
                        null,
                        0,
                        1,
                        1,
                        false,
                        0,
                        0,
                        null,
                        WowDamageMeterSourceDisplayType.None,
                        null)
                ],
                1,
                1,
                1);
        meter.SourcesByType[
            (WowDamageMeterSessionType.Expired,
                WowDamageMeterType.DamageDone,
                null,
                null)] = source;
        meter.SessionDurations[WowDamageMeterSessionType.Expired] = 1;

        Assert.Equal(
            "1:600:600:1234:600:50.25:Target:10:true:false:" +
            "Target:WARRIOR:boss:false:true:600:789:1",
            session.Lua.Evaluate(
                "local s=C_DamageMeter.GetCombatSessionSourceFromID(" +
                "44,0,'Player-1',123);" +
                "local p=s.combatSpells[1]; local d=p.combatSpellDetails;" +
                "local byType=C_DamageMeter.GetCombatSessionSourceFromType(" +
                "1,0,'Player-1',123);" +
                "return table.concat({" +
                "#s.combatSpells,s.maxAmount,s.totalAmount,p.spellID," +
                "p.totalAmount,p.amountPerSecond,p.creatureName," +
                "p.overkillAmount,tostring(p.isAvoidable),tostring(p.isDeadly)," +
                "d.unitName,d.unitClassFilename,d.classification," +
                "tostring(d.isPet),tostring(d.isMob),d.amount,d.specIconID," +
                "#byType.combatSpells},':')"));

        Assert.Equal(
            "0:0:true",
            session.Lua.Evaluate(
                "return #C_DamageMeter.GetCombatSessionFromType(" +
                "2,0).combatSources..':'.." +
                "#C_DamageMeter.GetCombatSessionSourceFromType(" +
                "2,0).combatSpells..':'.." +
                "tostring(C_DamageMeter.GetSessionDurationSeconds(2)==nil)"));

        Assert.Equal(
            "0:true:0",
            session.Lua.Evaluate(
                "C_DamageMeter.ResetAllCombatSessions();" +
                "return #C_DamageMeter.GetAvailableCombatSessions()..':'.." +
                "tostring(C_DamageMeter.GetSessionDurationSeconds(1)==nil)..':'.." +
                "#C_DamageMeter.GetCombatSessionSourceFromID(" +
                "44,0,'Player-1',123).combatSpells"));
        Assert.Equal(1, meter.ResetCount);
    }
}
