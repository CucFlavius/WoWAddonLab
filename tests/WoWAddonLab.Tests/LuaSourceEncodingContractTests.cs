using System.Text;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class LuaSourceEncodingContractTests
{
    [Fact]
    public void Utf8BomAddonSourceLoadsUsingItsExactByteLength()
    {
        var addon = Path.Combine(Path.GetTempPath(), $"wow-addon-lab-utf8-{Guid.NewGuid():N}");
        Directory.CreateDirectory(addon);
        try
        {
            var name = Path.GetFileName(addon);
            File.WriteAllText(Path.Combine(addon, $"{name}.toc"), "localized.lua\n");
            File.WriteAllText(
                Path.Combine(addon, "localized.lua"),
                "local localized = 'Zażółć gęślą jaźń'\nUTF8_SOURCE_LOADED = #localized > 0\n",
                new UTF8Encoding(true));

            using var session = new EmulatorSession();
            session.Load(addon);

            Assert.Equal("true", session.Lua.Evaluate("tostring(UTF8_SOURCE_LOADED)"));
            Assert.Empty(session.Lua.AddonLoadFailures);
        }
        finally
        {
            Directory.Delete(addon, true);
        }
    }
}
