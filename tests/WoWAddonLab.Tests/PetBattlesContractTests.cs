using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class PetBattlesContractTests
{
    [Fact]
    public void RegistersExactSurfaceEnumsAndNativeEmptyContracts()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "24:1:0:0:0:false:false:" +
            "0:1:2:3:0:2:7:0:6:22:0:21:12:15:20:21",
            session.Lua.Evaluate(
                "local count=0; for _ in pairs(C_PetBattles) do count=count+1 end;" +
                "return table.concat({" +
                "count,select('#',C_PetBattles.GetBreedQuality(0,1))," +
                "C_PetBattles.GetBreedQuality(0,1)," +
                "select('#',C_PetBattles.GetIcon(0,1))," +
                "select('#',C_PetBattles.GetName(0,1))," +
                "tostring(C_PetBattles.IsPlayerNPC())," +
                "tostring(C_PetBattles.IsWildBattle())," +
                "Enum.PetbattleSlot.Slot_0," +
                "Enum.PetbattleSlot.Slot_1," +
                "Enum.PetbattleSlot.Slot_2," +
                "Enum.PetbattleSlotMeta.NumValues," +
                "Enum.PetbattleSlotMeta.MinValue," +
                "Enum.PetbattleSlotMeta.MaxValue," +
                "Enum.PetbattleStateMeta.NumValues," +
                "Enum.PetbattleStateMeta.MinValue," +
                "Enum.PetbattleStateMeta.MaxValue," +
                "Enum.PetBattleQueueStatusMeta.NumValues," +
                "Enum.PetBattleQueueStatusMeta.MinValue," +
                "Enum.PetBattleQueueStatusMeta.MaxValue," +
                "Enum.PetBattleQueueStatus.Removed," +
                "Enum.PetBattleQueueStatus.Matchmaking," +
                "Enum.PetBattleQueueStatus.InBattle," +
                "Enum.PetBattleQueueStatus.NoBattlingHere},':')"));
    }

    [Fact]
    public void MetadataEnumeratorsRetainNativeReturnShapesWithoutClientRecords()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:true:table:0",
            session.Lua.Evaluate(
                "local target={}; local returned=C_PetBattles.GetAllStates(target); " +
                "local created=C_PetBattles.GetAllStates(); local count=0; " +
                "for _ in pairs(created) do count=count+1 end; " +
                "return table.concat({" +
                "select('#',C_PetBattles.GetAllEffectNames())," +
                "tostring(returned==target),type(created),count},':')"));
    }

    [Fact]
    public void RegistersRecoveredSurfaceAndEnforcesNativeParsers()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "function:function:function:true:true:false:false:false:false",
            session.Lua.Evaluate(
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "return table.concat({" +
                "type(C_PetBattles.GetAbilityInfo)," +
                "type(C_PetBattles.GetNumPets)," +
                "type(C_PetBattles.IsInBattle)," +
                "tostring(ok(C_PetBattles.GetBreedQuality,'2.9','7.9'))," +
                "tostring(ok(C_PetBattles.GetIcon,'0','-2.9'))," +
                "tostring(ok(C_PetBattles.GetName,3,1))," +
                "tostring(ok(C_PetBattles.GetName,-1,1))," +
                "tostring(ok(C_PetBattles.GetName,0,2147483648))," +
                "tostring(ok(C_PetBattles.GetName,0,{}))" +
                "},':')"));
    }

    [Fact]
    public void PetBattleQueriesUseEmptyClientReturnShapes()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:1:3:0:0:0:false:false:false:false:false:false:2:false:0:3:nil:0:0",
            session.Lua.Evaluate(
                "local selectedCount=select('#',C_PetBattles.GetSelectedAction());" +
                "local trap,trapCount=C_PetBattles.IsTrapAvailable();" +
                "local queue,estimate,elapsed=C_PetBattles.GetPVPMatchmakingInfo();" +
                "return table.concat({" +
                "select('#',C_PetBattles.GetAbilityInfo())," +
                "C_PetBattles.GetActivePet()," +
                "C_PetBattles.GetBattleState()," +
                "C_PetBattles.GetForfeitPenalty()," +
                "C_PetBattles.GetNumPets(),selectedCount," +
                "tostring(C_PetBattles.CanActivePetSwapOut())," +
                "tostring(C_PetBattles.CanPetSwapIn(1))," +
                "tostring(C_PetBattles.IsInBattle())," +
                "tostring(C_PetBattles.IsSkipAvailable())," +
                "tostring(C_PetBattles.IsWaitingOnOpponent())," +
                "tostring(C_PetBattles.ShouldShowPetSelect())," +
                "select('#',C_PetBattles.IsTrapAvailable())," +
                "tostring(trap),trapCount," +
                "select('#',C_PetBattles.GetPVPMatchmakingInfo())," +
                "tostring(queue),estimate,elapsed},':')"));
    }

    [Fact]
    public void HealthAndExperienceUseNativeMissingPetFallbacks()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "100:100:2:0:50",
            session.Lua.Evaluate(
                "local xp,maxXp=C_PetBattles.GetXP(0,1);" +
                "return table.concat({" +
                "C_PetBattles.GetHealth(0,1)," +
                "C_PetBattles.GetMaxHealth(0,1)," +
                "select('#',C_PetBattles.GetXP(0,1)),xp,maxXp},':')"));
    }

    [Fact]
    public void ProjectsRecoveredPetFieldsAndBattleFlags()
    {
        using var session = new EmulatorSession();
        session.Lua.PetBattles.Pets[(2, 3)] =
            new WowPetBattlePet(
                4,
                123456,
                "Fluffy",
                "Darkmoon Rabbit");
        session.Lua.PetBattles.Pets[(1, 2)] =
            new WowPetBattlePet(
                2,
                null,
                string.Empty,
                "Missing Icon");
        session.Lua.PetBattles.IsPlayerNpc = true;
        session.Lua.PetBattles.IsWildBattle = true;

        Assert.Equal(
            "4:123456:2:Fluffy:Darkmoon Rabbit:true:true:0",
            session.Lua.Evaluate(
                "local customName,speciesName=C_PetBattles.GetName(2,3);" +
                "return table.concat({" +
                "C_PetBattles.GetBreedQuality(2,3)," +
                "C_PetBattles.GetIcon(2,3)," +
                "select('#',C_PetBattles.GetName(2,3))," +
                "customName,speciesName," +
                "tostring(C_PetBattles.IsPlayerNPC())," +
                "tostring(C_PetBattles.IsWildBattle())," +
                "select('#',C_PetBattles.GetIcon(1,2))},':')"));
    }
}
