using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class SpellContractTests
{
    [Fact]
    public void RegistersExactSurfaceEnumsConstantsAndNativeEmptyArities()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "63:false:2:0:0:7:7:0:0:0:2:0:0:400:1:nil:" +
            "0:1:2:3:0:2:133",
            session.Lua.Evaluate(
                "local count=0; for _ in pairs(C_Spell) do count=count+1 end;" +
                "local health,powers=C_Spell.GetAuraStatChanges(7);" +
                "local icon,original=C_Spell.GetSpellTexture(7);" +
                "return table.concat({" +
                "count,tostring(C_Spell.DoesSpellExist(7))," +
                "select('#',C_Spell.GetAuraStatChanges(7)),health,#powers," +
                "C_Spell.GetBaseSpell(7),C_Spell.GetOverrideSpell(7)," +
                "select('#',C_Spell.GetSpellIDForSpellIdentifier(7))," +
                "select('#',C_Spell.GetSpellInfo(7))," +
                "select('#',C_Spell.GetSpellTexture(0))," +
                "select('#',C_Spell.GetSpellTexture(7)),icon,original," +
                "C_Spell.GetSpellQueueWindow()," +
                "select('#',C_Spell.IsSpellInRange(7))," +
                "tostring(C_Spell.IsSpellInRange(7))," +
                "Enum.SpellAuraVisibilityType.RaidInCombat," +
                "Enum.SpellAuraVisibilityType.RaidOutOfCombat," +
                "Enum.SpellAuraVisibilityType.EnemyTarget," +
                "Enum.SpellAuraVisibilityTypeMeta.NumValues," +
                "Enum.SpellAuraVisibilityTypeMeta.MinValue," +
                "Enum.SpellAuraVisibilityTypeMeta.MaxValue," +
                "Constants.SpellCooldownConsts.GLOBAL_RECOVERY_CATEGORY},':')"));
    }

    [Fact]
    public void SpellIdentifierMatchesNativeNumberLinkAndNameParsing()
    {
        using var session = new EmulatorSession();
        session.Lua.Spells.Add(123, "Fireball");

        Assert.Equal(
            "true:true:true:true:true:false:" +
            "false:false:true:true:false:true:false:false",
            session.Lua.Evaluate(
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "return table.concat({" +
                "tostring(C_Spell.DoesSpellExist(123.9))," +
                "tostring(C_Spell.DoesSpellExist('Fireball'))," +
                "tostring(C_Spell.DoesSpellExist('|Hspell:123:0|h[x]|h'))," +
                "tostring(C_Spell.DoesSpellExist('prefix spell:0x7Btail'))," +
                "tostring(C_Spell.DoesSpellExist('123'))," +
                "tostring(C_Spell.DoesSpellExist('Missing'))," +
                "tostring(ok(C_Spell.DoesSpellExist))," +
                "tostring(ok(C_Spell.DoesSpellExist,{}))," +
                "tostring(ok(C_Spell.GetAuraStatChanges,'123'))," +
                "tostring(ok(C_Spell.CancelSpellByID,'123'))," +
                "tostring(ok(C_Spell.EnableSpellRangeCheck,123,1))," +
                "tostring(ok(C_Spell.GetVisibilityInfo,123,'0'))," +
                "tostring(ok(C_Spell.GetSpellCooldownDuration,123,1))," +
                "tostring(ok(C_Spell.GetSpellLink,123,{}))},':')"));
    }

    [Fact]
    public void ProjectsRecoveredSpellInfoCooldownChargeAndCostShapes()
    {
        using var session = new EmulatorSession();
        var spell = session.Lua.Spells.Add(123, "Fireball");
        spell.Description = "Throws fire.";
        spell.Subtext = "Rank";
        spell.IconId = 11;
        spell.OriginalIconId = 12;
        spell.CastTimeMilliseconds = 1500;
        spell.MinRange = 1.25f;
        spell.MaxRange = 40.5f;
        spell.Cooldown = new WowActionCooldownInfo(
            1.5,
            6,
            true,
            true,
            0.75,
            133,
            0.25,
            false);
        spell.Charges = new WowActionChargeInfo(2, 3, 4, 5, 0.5, true);
        spell.LossOfControlCooldownInfo =
            new WowActionLossOfControlInfo(6, 7, 0.8, true, true);
        spell.PowerCosts =
        [
            new WowSpellPowerCostInfo(
                0,
                "Mana",
                10,
                2,
                5,
                1,
                77,
                true)
        ];

        Assert.Equal(
            "Fireball:11:12:1500:1.25:40.5:123:" +
            "1.5:6:true:true:0.75:133:0.25:false:" +
            "2:3:4:5:0.5:true:" +
            "6:7:0.8:true:true:" +
            "0:Mana:10:2:5:1:77:true:" +
            "Throws fire.:Rank",
            session.Lua.Evaluate(
                "local i=C_Spell.GetSpellInfo('Fireball');" +
                "local c=C_Spell.GetSpellCooldown(123);" +
                "local q=C_Spell.GetSpellCharges(123);" +
                "local l=C_Spell.GetSpellLossOfControlCooldownInfo(123);" +
                "local p=C_Spell.GetSpellPowerCost(123)[1];" +
                "return table.concat({" +
                "i.name,i.iconID,i.originalIconID,i.castTime," +
                "i.minRange,i.maxRange,i.spellID," +
                "c.startTime,c.duration,tostring(c.isEnabled)," +
                "tostring(c.isActive),c.modRate,c.activeCategory," +
                "c.timeUntilEndOfStartRecovery,tostring(c.isOnGCD)," +
                "q.currentCharges,q.maxCharges,q.cooldownStartTime," +
                "q.cooldownDuration,q.chargeModRate,tostring(q.isActive)," +
                "l.startTime,l.duration,l.modRate,tostring(l.isActive)," +
                "tostring(l.shouldReplaceNormalCooldown)," +
                "p.type,p.name,p.cost,p.minCost,p.costPercent,p.costPerSec," +
                "p.requiredAuraID,tostring(p.hasRequiredAura)," +
                "C_Spell.GetSpellDescription(123)," +
                "C_Spell.GetSpellSubtext(123)},':')"));
    }

    [Fact]
    public void ClientSpellProviderSuppliesLazyStaticSpellInfo()
    {
        using var session = new EmulatorSession();
        session.SpellProvider = new TestSpellProvider(
            new WowSpellStaticInfo(61849, "Guild Battle Standard", 135961, 135961, 0, 0, 0),
            new WowSpellStaticInfo(133, "Fireball", 135812, 135812, 2000, 0, 40));

        Assert.Empty(session.Lua.Spells.Definitions);
        Assert.Equal(
            "Guild Battle Standard:61849:Fireball:135812:2000:0:40:133:nil",
            session.Lua.Evaluate(
                "local guild=C_Spell.GetSpellInfo(61849);" +
                "local fire=C_Spell.GetSpellInfo('Fireball');" +
                "return table.concat({guild.name,guild.spellID,fire.name,fire.iconID," +
                "fire.castTime,fire.minRange,fire.maxRange,fire.spellID," +
                "tostring(C_Spell.GetSpellInfo(999999) or nil)},':')"));
        Assert.Equal(2, session.Lua.Spells.Definitions.Count);
    }

    [Fact]
    public void ProjectsAuraDebuffVisibilityAndArrayContracts()
    {
        using var session = new EmulatorSession();
        var spell = session.Lua.Spells.Add(123, "Fireball");
        spell.AuraStatChanges = new WowSpellAuraStatChanges(
            -50,
            [new WowSpellAuraPowerChange(3, 25)]);
        spell.DeadlyDebuffInfo =
            new WowSpellDeadlyDebuffInfo(7, "Move", 800, 4, 99);
        spell.ItemModifiedAppearancesApplied = [8, 9];
        spell.Visibility[WowSpellAuraVisibilityType.EnemyTarget] =
            new WowSpellVisibilityInfo(true, false, true);

        Assert.Equal(
            "-50:1:3:25:800:4:7:Move:99:2:8:9:true:false:true:" +
            "0:0:0",
            session.Lua.Evaluate(
                "local h,p=C_Spell.GetAuraStatChanges(123);" +
                "local d=C_Spell.GetDeadlyDebuffInfo(123);" +
                "local a=C_Spell.GetItemModifiedAppearancesApplied(123);" +
                "local custom,mine,spec=C_Spell.GetVisibilityInfo(" +
                "123,Enum.SpellAuraVisibilityType.EnemyTarget);" +
                "return table.concat({" +
                "h,#p,p[1].powerType,p[1].amount," +
                "d.criticalTimeRemainingMs,d.criticalStacks,d.priority," +
                "d.warningText,d.soundKitID,#a,a[1],a[2]," +
                "tostring(custom),tostring(mine),tostring(spec)," +
                "select('#',C_Spell.GetDeadlyDebuffInfo(999))," +
                "select('#',C_Spell.GetVisibilityInfo(999,0))," +
                "select('#',C_Spell.GetSpellPowerCost(999))},':')"));
    }

    [Fact]
    public void DisplayCountNilableRangeAndSchoolStringsMatchRecoveredRules()
    {
        using var session = new EmulatorSession();
        var consumable = session.Lua.Spells.Add(1, "Potion");
        consumable.IsConsumable = true;
        consumable.UseCount = 12;
        var charged = session.Lua.Spells.Add(2, "Charged");
        charged.Charges = new WowActionChargeInfo(CurrentCharges: 3);
        charged.IsInRange = false;
        var unknownRange = session.Lua.Spells.Add(3, "Unknown Range");

        Assert.Equal(
            "12:*:over:3::false:nil:Physical:Shadowflame:Cosmic:Unknown",
            session.Lua.Evaluate(
                "return table.concat({" +
                "C_Spell.GetSpellDisplayCount(1)," +
                "C_Spell.GetSpellDisplayCount(1,5)," +
                "C_Spell.GetSpellDisplayCount(1,5,'over')," +
                "C_Spell.GetSpellDisplayCount(2)," +
                "C_Spell.GetSpellDisplayCount(999)," +
                "tostring(C_Spell.IsSpellInRange(2))," +
                "tostring(C_Spell.IsSpellInRange(3))," +
                "C_Spell.GetSchoolString(1)," +
                "C_Spell.GetSchoolString(36)," +
                "C_Spell.GetSchoolString(106)," +
                "C_Spell.GetSchoolString(0)},':')"));
    }

    [Fact]
    public void MutationsAndBooleanQueriesUseRepresentedSpellState()
    {
        using var session = new EmulatorSession();
        var spell = session.Lua.Spells.Add(123, "Fireball");
        spell.AutoCastAllowed = true;
        spell.IsUsable = true;
        spell.HasInsufficientPower = true;
        spell.IsHarmful = true;
        spell.IsImportant = true;
        spell.HasRange = true;
        session.Lua.Spells.ActiveCastSpellId = 123;
        session.Lua.Spells.RangedAutoAttackSpellId = 123;
        session.Lua.Spells.TargetSpellIsEnchanting = true;
        session.Lua.Spells.TargetSpellJumpsUpgradeTrack = true;
        session.Lua.Spells.TargetSpellReplacesBonusTree = true;

        Assert.Equal(
            "true:true:true:true:true:true:true:true:true:false",
            session.Lua.Evaluate(
                "C_Spell.EnableSpellRangeCheck(123,true);" +
                "C_Spell.PickupSpell(123);" +
                "C_Spell.RequestLoadSpellData(123);" +
                "C_Spell.SetSpellAutoCastEnabled(123,true);" +
                "local allowed,enabled=C_Spell.GetSpellAutoCast(123);" +
                "local usable,power=C_Spell.IsSpellUsable(123);" +
                "C_Spell.ToggleSpellAutoCast(123);" +
                "local _,toggled=C_Spell.GetSpellAutoCast(123);" +
                "C_Spell.CancelSpellByID(123);" +
                "return table.concat({" +
                "tostring(allowed),tostring(enabled),tostring(usable)," +
                "tostring(power),tostring(C_Spell.IsSpellHarmful(123))," +
                "tostring(C_Spell.IsSpellImportant(123))," +
                "tostring(C_Spell.SpellHasRange(123))," +
                "tostring(C_Spell.IsRangedAutoAttackSpell(123))," +
                "tostring(C_Spell.TargetSpellIsEnchanting())," +
                "tostring(toggled)},':')"));

        Assert.Contains(123, session.Lua.Spells.RangeCheckedSpellIds);
        Assert.Contains(123, session.Lua.Spells.RequestedLoadSpellIds);
        Assert.Equal(123, session.Lua.Spells.PickedUpSpellId);
        Assert.Equal(123, session.Lua.Spells.LastCancelledSpellId);
        Assert.Null(session.Lua.Spells.ActiveCastSpellId);
    }

    private sealed class TestSpellProvider(params WowSpellStaticInfo[] spells)
        : IWowSpellProvider
    {
        private readonly IReadOnlyDictionary<int, WowSpellStaticInfo> _spells =
            spells.ToDictionary(value => value.Id);

        public int Count => _spells.Count;

        public WowSpellStaticInfo? Find(int id) =>
            _spells.GetValueOrDefault(id);

        public int FindIdByName(string name) =>
            _spells.Values.FirstOrDefault(value =>
                value.Name.Equals(name, StringComparison.OrdinalIgnoreCase))?.Id ?? 0;
    }
}
