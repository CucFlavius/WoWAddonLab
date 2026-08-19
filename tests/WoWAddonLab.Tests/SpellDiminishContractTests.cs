using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class SpellDiminishContractTests
{
    [Fact]
    public void RegistersExactSurfaceEnumsAndNativeEmptyContracts()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "4:1:0:1:nil:true:" +
            "0:1:2:3:4:5:6:7:8:0:7:" +
            "0:1:2:3:0:2",
            session.Lua.Evaluate(
                "local count=0; for _ in pairs(C_SpellDiminish) do count=count+1 end;" +
                "local all=C_SpellDiminish.GetAllSpellDiminishCategories();" +
                "local info=C_SpellDiminish.GetSpellDiminishCategoryInfo(0);" +
                "return table.concat({" +
                "count,select('#',C_SpellDiminish.GetAllSpellDiminishCategories())," +
                "#all,select('#',C_SpellDiminish.GetSpellDiminishCategoryInfo(0))," +
                "tostring(info),tostring(C_SpellDiminish.IsSystemSupported())," +
                "Enum.SpellDiminishCategory.Root," +
                "Enum.SpellDiminishCategory.Taunt," +
                "Enum.SpellDiminishCategory.Stun," +
                "Enum.SpellDiminishCategory.AoEKnockback," +
                "Enum.SpellDiminishCategory.Incapacitate," +
                "Enum.SpellDiminishCategory.Disorient," +
                "Enum.SpellDiminishCategory.Silence," +
                "Enum.SpellDiminishCategory.Disarm," +
                "Enum.SpellDiminishCategoryMeta.NumValues," +
                "Enum.SpellDiminishCategoryMeta.MinValue," +
                "Enum.SpellDiminishCategoryMeta.MaxValue," +
                "Enum.SpellDiminishRuleset.None," +
                "Enum.SpellDiminishRuleset.PvE," +
                "Enum.SpellDiminishRuleset.PvP," +
                "Enum.SpellDiminishRulesetMeta.NumValues," +
                "Enum.SpellDiminishRulesetMeta.MinValue," +
                "Enum.SpellDiminishRulesetMeta.MaxValue},':')"));
    }

    [Fact]
    public void UsesGeneratedNumericEnumContracts()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:true:true:false:false:false:false:false:false",
            session.Lua.Evaluate(
                "local function ok(fn,...) return pcall(fn,...) end;" +
                "return table.concat({" +
                "tostring(ok(C_SpellDiminish.GetAllSpellDiminishCategories))," +
                "tostring(ok(C_SpellDiminish.GetAllSpellDiminishCategories,nil))," +
                "tostring(ok(C_SpellDiminish.GetAllSpellDiminishCategories,'2'))," +
                "tostring(ok(C_SpellDiminish.GetSpellDiminishCategoryInfo,'7.9'))," +
                "tostring(ok(C_SpellDiminish.GetAllSpellDiminishCategories,3))," +
                "tostring(ok(C_SpellDiminish.GetAllSpellDiminishCategories,{}))," +
                "tostring(ok(C_SpellDiminish.GetSpellDiminishCategoryInfo))," +
                "tostring(ok(C_SpellDiminish.GetSpellDiminishCategoryInfo,-1))," +
                "tostring(ok(C_SpellDiminish.GetSpellDiminishCategoryInfo,8))," +
                "tostring(ok(C_SpellDiminish.ShouldTrackSpellDiminishCategory,0))" +
                "},':')"));
    }

    [Fact]
    public void ProjectsCategoryInfoAndStaticRulesetFiltering()
    {
        using var session = new EmulatorSession();
        foreach (var category in Enum.GetValues<WowSpellDiminishCategory>())
        {
            session.Lua.SpellDiminish.Categories[category] =
                new WowSpellDiminishCategoryInfo(
                    category,
                    category == WowSpellDiminishCategory.Disarm
                        ? null
                        : category.ToString(),
                    category == WowSpellDiminishCategory.Silence
                        ? null
                        : 100 + (int)category);
        }

        Assert.Equal(
            "8:0:7:4:0:2:4:5:2:Stun:102:nil:nil",
            session.Lua.Evaluate(
                "local all=C_SpellDiminish.GetAllSpellDiminishCategories();" +
                "local pvp=C_SpellDiminish.GetAllSpellDiminishCategories(" +
                "Enum.SpellDiminishRuleset.PvP);" +
                "local stun=C_SpellDiminish.GetSpellDiminishCategoryInfo(2);" +
                "local disarm=C_SpellDiminish.GetSpellDiminishCategoryInfo(7);" +
                "local silence=C_SpellDiminish.GetSpellDiminishCategoryInfo(6);" +
                "return table.concat({" +
                "#all,all[1].category,all[8].category," +
                "#pvp,pvp[1].category,pvp[2].category," +
                "pvp[3].category,pvp[4].category," +
                "stun.category,stun.name,stun.icon," +
                "tostring(disarm.name),tostring(silence.icon)},':')"));
    }

    [Fact]
    public void ShouldTrackAppliesNativePvpBaseAndRuntimeFilters()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:true:true:true:false:false:false:false",
            session.Lua.Evaluate(
                "local f=C_SpellDiminish.ShouldTrackSpellDiminishCategory;" +
                "local p=Enum.SpellDiminishRuleset.PvP;" +
                "return table.concat({" +
                "tostring(f(1,Enum.SpellDiminishRuleset.None))," +
                "tostring(f(7,Enum.SpellDiminishRuleset.PvE))," +
                "tostring(f(0,p)),tostring(f(2,p)),tostring(f(1,p))," +
                "tostring(f(3,p)),tostring(f(6,p)),tostring(f(7,p))},':')"));

        session.Lua.SpellDiminish.PvpRuntimeFilterEnabled = true;
        session.Lua.SpellDiminish.PvpTrackedCategories.Add(
            WowSpellDiminishCategory.Stun);

        Assert.Equal(
            "false:true:false:false",
            session.Lua.Evaluate(
                "local f=C_SpellDiminish.ShouldTrackSpellDiminishCategory;" +
                "local p=Enum.SpellDiminishRuleset.PvP;" +
                "return table.concat({" +
                "tostring(f(0,p)),tostring(f(2,p))," +
                "tostring(f(4,p)),tostring(f(5,p))},':')"));
    }
}
