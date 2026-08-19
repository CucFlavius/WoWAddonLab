using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class InstalledAddonApiContractTests
{
    [Fact]
    public void RealmAndAutoCompleteApisExposeConnectedRealmState()
    {
        using var session = new EmulatorSession();
        session.Lua.Client.RealmName = "Khaz Modan";
        session.Lua.AutoComplete.RealmNames.Add("KhazModan");
        session.Lua.AutoComplete.RealmNames.Add("Dun Modr");

        Assert.Equal(
            "KhazModan:KhazModan:Dun Modr:true:false:1",
            session.Lua.Evaluate(
                "local realms=C_AutoComplete.GetAutoCompleteRealms(); " +
                "return table.concat({GetNormalizedRealmName(),realms[1],realms[2]," +
                "tostring(C_AutoComplete.IsRecognizedName('KhazModan'))," +
                "tostring(C_AutoComplete.IsRecognizedName('Other'))," +
                "GetCVar('loadDeprecationFallbacks')},':')"));

        session.Lua.Client.IsPlayerInWorld = false;
        Assert.Equal("nil", session.Lua.Evaluate("tostring(GetNormalizedRealmName())"));
    }

    [Fact]
    public void PetAndHousingBasicModeApisExposeSimulationState()
    {
        using var session = new EmulatorSession();
        session.Lua.PetInfo.TalentTreeName = "Ferocity";
        session.Lua.PetInfo.SpellIdsByActionId[3] = 123;
        session.Lua.PetInfo.PassiveActionIds.Add(3);

        Assert.Equal(
            "Ferocity:123:true:0:false:true",
            session.Lua.Evaluate(
                "C_HousingBasicMode.SetGridSnapEnabled(true); " +
                "return table.concat({C_PetInfo.GetPetTalentTree()," +
                "C_PetInfo.GetSpellForPetAction(3)," +
                "tostring(C_PetInfo.IsPetActionPassive(3))," +
                "#C_PetInfo.GetPetTamersForMap(84)," +
                "tostring(C_HousingBasicMode.IsPlacingNewDecor())," +
                "tostring(C_HousingBasicMode.IsGridSnapEnabled())},':')"));
    }

    [Fact]
    public void AreaTimeAndDifficultyGlobalsExposeUsableDefaultState()
    {
        using var session = new EmulatorSession();
        session.Lua.Maps.AreaNameOverrides[7288] = "Assault on Violet Hold";
        session.Tick(0.25);

        Assert.Equal(
            "Assault on Violet Hold:1:14:3:0.25",
            session.Lua.Evaluate(
                "return table.concat({" +
                "C_Map.GetAreaInfo(7288)," +
                "GetDungeonDifficultyID()," +
                "GetRaidDifficultyID()," +
                "GetLegacyRaidDifficultyID()," +
                "GetTimePreciseSec()" +
                "},':')"));
        Assert.Equal("nil", session.Lua.Evaluate("tostring(C_Map.GetAreaInfo(999999))"));
    }

    [Fact]
    public void TimePlayedRequestDeliversItsServerResponseOnTheNextTick()
    {
        using var session = new EmulatorSession();
        session.Lua.PlayerScript.TotalTimePlayedSeconds = 3600;
        session.Lua.PlayerScript.LevelTimePlayedSeconds = 120;
        session.Lua.Evaluate(
            "local f=CreateFrame('Frame'); f:RegisterEvent('TIME_PLAYED_MSG'); " +
            "f:SetScript('OnEvent',function(_,_,total,level) " +
            "TIME_PLAYED_RESULT=total..':'..level end); RequestTimePlayed()");

        Assert.Equal("nil", session.Lua.Evaluate("tostring(TIME_PLAYED_RESULT)"));
        session.Tick(1.0 / 60.0);

        Assert.Equal("3600:120", session.Lua.Evaluate("TIME_PLAYED_RESULT"));
        Assert.Equal(1, session.Lua.PlayerScript.TimePlayedRequestCount);
    }

    [Fact]
    public void FriendshipReputationFallsBackToClientFactionData()
    {
        using var session = new EmulatorSession();
        session.FactionProvider = new TestFactionProvider();

        Assert.Equal(
            "0:Ordinary:0:0",
            session.Lua.Evaluate(
                "local info=C_GossipInfo.GetFriendshipReputation(1375); " +
                "local ranks=C_GossipInfo.GetFriendshipReputationRanks(1375); " +
                "return table.concat({info.friendshipFactionID,info.name," +
                "ranks.currentLevel,ranks.maxLevel},':')"));
        Assert.Equal(
            "1:0:0",
            session.Lua.Evaluate(
                "local info=C_GossipInfo.GetFriendshipReputation(999); " +
                "return table.concat({" +
                "select('#',C_GossipInfo.GetFriendshipReputation(999))," +
                "info.friendshipFactionID," +
                "select('#',C_GossipInfo.GetFriendshipReputation(0))},':')"));
    }

    [Fact]
    public void PvpLifetimeStatsExposeTheNativeTwoValueContract()
    {
        using var session = new EmulatorSession();
        session.Lua.PlayerScript.LifetimeHonorableKills = 1234;
        session.Lua.PlayerScript.LifetimeMaxPvpRank = 9;

        Assert.Equal(
            "2:1234:9",
            session.Lua.Evaluate(
                "local kills,rank=GetPVPLifetimeStats(); " +
                "return select('#',GetPVPLifetimeStats())..':'..kills..':'..rank"));

        session.Lua.PlayerScript.LifetimeMaxPvpRank = 4;
        Assert.Equal("0", session.Lua.Evaluate("return select(2,GetPVPLifetimeStats())"));
    }

    [Fact]
    public void QuestCompletionSeparatesCharacterAndAccountState()
    {
        using var session = new EmulatorSession();
        session.Lua.QuestLog.CompletedQuestIds.Add(100);
        session.Lua.QuestLog.CompletedQuestIds.Add(50);
        session.Lua.QuestLog.CompletedOnAccountQuestIds.Add(200);

        Assert.Equal(
            "true:false:false:true:50:100",
            session.Lua.Evaluate(
                "local completed=C_QuestLog.GetAllCompletedQuestIDs(); " +
                "return table.concat({" +
                "tostring(C_QuestLog.IsQuestFlaggedCompleted(100))," +
                "tostring(C_QuestLog.IsQuestFlaggedCompleted(200))," +
                "tostring(C_QuestLog.IsQuestFlaggedCompletedOnAccount(100))," +
                "tostring(C_QuestLog.IsQuestFlaggedCompletedOnAccount(200))," +
                "completed[1],completed[2]" +
                "},':')"));
    }

    [Fact]
    public void QuestTitlesPreferClientTaskDataAndFallBackToQuestCacheState()
    {
        using var session = new EmulatorSession();
        session.QuestProvider = new TestQuestProvider();
        session.Lua.QuestLog.QuestTitles[200] = "Cached quest";

        Assert.Equal(
            "Client task:Cached quest:nil:1",
            session.Lua.Evaluate(
                "return table.concat({" +
                "C_QuestLog.GetTitleForQuestID(100)," +
                "C_QuestLog.GetTitleForQuestID(200)," +
                "tostring(C_QuestLog.GetTitleForQuestID(300))," +
                "select('#',C_QuestLog.GetTitleForQuestID(300))" +
                "},':')"));
    }

    [Fact]
    public void MissingAchievementCriteriaReturnNoValues()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "0:0:false",
            session.Lua.Evaluate(
                "return table.concat({" +
                "select('#',GetAchievementCriteriaInfoByID(6185,34643))," +
                "select('#',GetAchievementCriteriaInfo(6185,3,true))," +
                "tostring(pcall(GetAchievementCriteriaInfoByID,-1,1))" +
                "},':')"));
    }

    [Fact]
    public void ArtifactAppearanceInfoUsesSimulationStateAndReturnsNothingWhenUnknown()
    {
        using var session = new EmulatorSession();
        session.Lua.Artifact.AppearanceInfoById[77] =
            new WowArtifactAppearanceInfoState(
                4,
                77,
                "Ashbringer",
                2,
                true,
                null,
                10,
                11,
                0.1,
                0.2,
                0.3,
                0.8,
                0.9,
                true);

        Assert.Equal(
            "14:4:77:Ashbringer:2:true:nil:10:11:0.1:0.2:0.3:0.8:0.9:true:0",
            session.Lua.Evaluate(
                "local set,id,name,index,unlocked,failure,camera,altCamera," +
                "red,green,blue,opacity,saturation,obtainable=" +
                "C_ArtifactUI.GetAppearanceInfoByID(77); " +
                "return table.concat({" +
                "select('#',C_ArtifactUI.GetAppearanceInfoByID(77))," +
                "set,id,name,index,tostring(unlocked),tostring(failure)," +
                "camera,altCamera,red,green,blue,opacity,saturation," +
                "tostring(obtainable)," +
                "select('#',C_ArtifactUI.GetAppearanceInfoByID(999))},':')"));
    }

    [Fact]
    public void TransmogSetApisCombineClientDefinitionsWithSimulationState()
    {
        using var session = new EmulatorSession();
        session.TransmogSetProvider = new TestTransmogSetProvider();
        session.Lua.TransmogSets.CollectedSetIds.Add(10);
        session.Lua.TransmogSets.FavoriteSetIds.Add(10);

        Assert.Equal(
            "2:10:Tier One:nil:Raids:Season One:3:120000:7:1:false:Alliance:true:true:false:true:false:2:101:102:1:11:10",
            session.Lua.Evaluate(
                "local sets=C_TransmogSets.GetAllSets(); local base=sets[1]; " +
                "local sources=C_TransmogSets.GetAllSourceIDs(10); " +
                "local variants=C_TransmogSets.GetVariantSets(10); " +
                "return table.concat({#sets,base.setID,base.name,tostring(base.baseSetID)," +
                "base.description,base.label,base.expansionID,base.patchID,base.uiOrder," +
                "base.classMask,tostring(base.hiddenUntilCollected),base.requiredFaction," +
                "tostring(base.collected),tostring(base.favorite),tostring(base.limitedTimeSet)," +
                "tostring(base.validForCharacter),tostring(base.grantAsPrecedingVariant)," +
                "#sources,sources[1],sources[2],#variants,variants[1].setID," +
                "C_TransmogSets.GetBaseSetID(11)},':')"));
        Assert.Equal(
            "0:0:0",
            session.Lua.Evaluate(
                "return table.concat({" +
                "select('#',C_TransmogSets.GetSetInfo(999))," +
                "select('#',C_TransmogSets.GetAllSourceIDs(999))," +
                "select('#',C_TransmogSets.GetVariantSets(999))},':')"));

        session.Lua.Evaluate("C_TransmogSets.SetIsFavorite(11, true)");
        Assert.Equal(
            "true:true",
            session.Lua.Evaluate(
                "local favorite,group=C_TransmogSets.GetIsFavorite(11); " +
                "return tostring(favorite)..':'..tostring(group)"));
    }

    private sealed class TestFactionProvider : IWowFactionProvider
    {
        public bool TryGetFriendshipReputation(
            int factionId,
            out WowGossipFriendshipReputationState reputation)
        {
            if (factionId == 1375)
            {
                reputation = new WowGossipFriendshipReputationState(
                    0,
                    0,
                    0,
                    "Ordinary",
                    string.Empty,
                    0,
                    string.Empty,
                    0,
                    null,
                    false,
                    null);
                return true;
            }

            reputation = null!;
            return false;
        }

        public bool TryGetFriendshipRanks(
            int factionId,
            out WowGossipFriendshipRanksState ranks)
        {
            if (factionId == 1375)
            {
                ranks = new WowGossipFriendshipRanksState(0, 0);
                return true;
            }

            ranks = null!;
            return false;
        }
    }

    private sealed class TestQuestProvider : IWowQuestProvider
    {
        public bool TryGetTitle(int questId, out string title)
        {
            title = questId == 100 ? "Client task" : string.Empty;
            return questId == 100;
        }
    }

    private sealed class TestTransmogSetProvider : IWowTransmogSetProvider
    {
        private static readonly WowTransmogSetDefinition BaseSet = new(
            10,
            "Tier One",
            null,
            "Raids",
            "Season One",
            3,
            120000,
            7,
            1,
            false,
            "Alliance",
            false,
            false);

        private static readonly WowTransmogSetDefinition VariantSet = new(
            11,
            "Tier One Heroic",
            10,
            "Raids",
            "Heroic",
            3,
            120000,
            8,
            1,
            false,
            "Alliance",
            false,
            false);

        public IReadOnlyList<WowTransmogSetDefinition> Sets { get; } =
            [BaseSet, VariantSet];

        public bool TryGetSet(int setId, out WowTransmogSetDefinition definition)
        {
            definition = setId switch
            {
                10 => BaseSet,
                11 => VariantSet,
                _ => null!
            };
            return definition is not null;
        }

        public IReadOnlyList<int> GetSourceIds(int setId) =>
            setId == 10 ? [101, 102] : setId == 11 ? [103] : [];

        public IReadOnlyList<WowTransmogSetDefinition> GetVariantSets(int setId) =>
            setId == 10 ? [VariantSet] : [];

        public IReadOnlyList<int> GetSetIdsContainingSource(int sourceId) =>
            sourceId switch
            {
                101 or 102 => [10],
                103 => [11],
                _ => []
            };
    }
}
