namespace WoWAddonLab.Tests;

public sealed class DungeonmireCompatibilityTests
{
    [Fact]
    public void CurrentDungeonmireAddonLoads()
    {
        const string addonPath = @"E:\Personal\wowAddon_Dungeon\Dungeonmire";
        if (!Directory.Exists(addonPath))
            return;

        using var session = new EmulatorSession();
        session.Load(addonPath);
        for (var index = 0; index < 30; index++)
            session.Tick(1.0 / 60.0);

        Assert.True(session.Lua.IsLoaded);
        Assert.Null(session.LastError);
        Assert.True(session.Ui.Objects.Count > 100);
        Assert.Equal("false", session.Lua.Evaluate("tostring(DM.halted)"));
        Assert.Equal("nil", session.Lua.Evaluate("tostring(DM.lastError)"));
        Assert.Equal("true", session.Lua.Evaluate("tostring(DM.Game ~= nil and DM.Renderer ~= nil)"));
    }
}
