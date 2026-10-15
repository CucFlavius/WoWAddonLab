using WoWAddonLab.Emulator;
using WoWAddonLab.Emulator.Addons;

namespace WoWAddonLab.Tests;

public sealed class Patch1217ContractTests
{
    [Fact]
    public void ImportResolvesRelativeModulesFromTocFileReturnValues()
    {
        var root = Path.Combine(Path.GetTempPath(), $"wow-addon-lab-{Guid.NewGuid():N}");
        var addon = CreateAddon(
            root,
            "ImportProbe",
            "",
            ("utility.lua", "local Utility = { value = 7 }; return Utility"),
            ("nothing.lua", "return nil"),
            (Path.Combine("sub", "helper.lua"), "return import('..utility').value * 2"),
            ("main.lua",
                "local Utility = import('.Utility'); " +
                "ImportProbeResult = Utility.value .. ':' .. tostring(import('.nothing')) .. ':' .. " +
                "import('.sub.helper') .. ':' .. tostring(import('ImportProbe.utility') == Utility) .. ':' .. " +
                "tostring((pcall(function() return import('.missing') end)))"));
        try
        {
            using var session = new EmulatorSession();
            session.Load(addon);

            if (session.Lua.AddonLoadFailures.Count > 0)
                Assert.Fail(string.Join(" | ", session.Lua.AddonLoadFailures.Select(failure => failure.Message)));
            Assert.Equal("7:nil:14:true:false", session.Lua.Evaluate("return ImportProbeResult"));
            Assert.Equal(
                "false",
                session.Lua.Evaluate("return tostring((pcall(import, '.Utility')))"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void BootstrapFilesRunForLoadOnDemandAddonsBeforeTheyLoad()
    {
        var root = Path.Combine(Path.GetTempPath(), $"wow-addon-lab-{Guid.NewGuid():N}");
        var addon = CreateAddon(
            root,
            "BootProbe",
            "## LoadOnDemand: 1\n## BootstrapFiles: bootprobe_bootstrap.lua",
            ("main.lua", "BootProbeMainLoaded = true"),
            ("bootprobe_bootstrap.lua",
                "local AddonName = ...; BootProbeBootstrapped = AddonName; " +
                "function BootProbe_LoadUI() return C_AddOns.LoadAddOn(AddonName) end"));
        try
        {
            using var session = new EmulatorSession();
            session.Load([], null, [], [], [addon]);

            Assert.Equal(
                "BootProbe:nil",
                session.Lua.Evaluate("return tostring(BootProbeBootstrapped)..':'..tostring(BootProbeMainLoaded)"));
            Assert.Equal(
                "true:true",
                session.Lua.Evaluate("return tostring(BootProbe_LoadUI())..':'..tostring(BootProbeMainLoaded)"));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void WoWHackGameTypeAppliesTaleboundGameRules()
    {
        using var session = new EmulatorSession();
        session.ManifestContext = AddonManifestContext.Mainline with { GameType = "wowhack" };
        session.Load([]);

        Assert.Equal(
            "3:true:3:3:1:0:1000:false",
            session.Lua.Evaluate(
                "return C_GameRules.GetActiveGameMode()..':'..tostring(C_GameRules.IsWoWHack())..':'.." +
                "C_GameRules.GetGameRuleAsFloat(Enum.GameRule.ActionButtonTypeOverlayStrategy)..':'.." +
                "C_ActionBar.GetSpellTypeRestrictionForAction(145)..':'.." +
                "C_GameModeExpBar.GetCurrentLevel()..':'..C_GameModeExpBar.GetCurrentXP()..':'.." +
                "C_GameModeExpBar.GetMaxXPForLevel()..':'..tostring(C_DelvesUI.HasActiveTale())"));
    }

    [Fact]
    public void StandardGameTypeKeepsActionBarsUnrestricted()
    {
        using var session = new EmulatorSession();
        session.Load([]);

        Assert.Equal(
            "1:false:0:true",
            session.Lua.Evaluate(
                "return C_GameRules.GetActiveGameMode()..':'..tostring(C_GameRules.IsWoWHack())..':'.." +
                "C_ActionBar.GetSpellTypeRestrictionForAction(145)..':'.." +
                "tostring(C_ActionBar.SpellMatchesActionSpellTypeRestriction(1459, 145))"));
    }

    [Fact]
    public void SimulatedTaleboundChoiceUsesTheTaleboundTextureKit()
    {
        using var session = new EmulatorSession();
        session.Load([]);
        session.ShowPlayerChoice("talebound", [1459, 6673]);

        Assert.Equal(
            "talebound:2:1459:true",
            session.Lua.Evaluate(
                "local info = C_PlayerChoice.GetCurrentPlayerChoiceInfo(); " +
                "return info.uiTextureKit..':'..#info.options..':'..info.options[1].spellID..':'.." +
                "tostring(C_PlayerChoice.IsWaitingForPlayerChoiceResponse())"));
    }

    private static string CreateAddon(
        string root,
        string name,
        string metadata,
        params (string Path, string Content)[] files)
    {
        var addon = Path.Combine(root, name);
        Directory.CreateDirectory(addon);
        var tocFiles = files
            .Where(file => !metadata.Contains(file.Path, StringComparison.OrdinalIgnoreCase))
            .Select(file => file.Path);
        File.WriteAllText(
            Path.Combine(addon, $"{name}.toc"),
            $"## Interface: 120107\n{metadata}\n{string.Join('\n', tocFiles)}");
        foreach (var (path, content) in files)
        {
            var fullPath = Path.Combine(addon, path);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, content);
        }
        return addon;
    }
}
