using WoWAddonLab.Emulator.Diagnostics;
using WoWAddonLab.Emulator.Lua;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Tests;

public sealed class LuaBytecodeCacheTests
{
    [Fact]
    public void CachedChunkExecutesInAFreshRuntime()
    {
        var root = Path.Combine(Path.GetTempPath(), $"wow-addon-lab-lua-cache-{Guid.NewGuid():N}");
        var cache = Path.Combine(root, "cache");
        var script = Path.Combine(root, "script.lua");
        Directory.CreateDirectory(root);
        var values = string.Join(',', Enumerable.Range(1, 100_000));
        File.WriteAllText(
            script,
            $"CachedData={{{values}}}; CachedChunkValue = (CachedChunkValue or 0) + 1");
        try
        {
            using (var runtime = CreateRuntime(cache))
            {
                runtime.ExecuteFile(script);
                Assert.Equal("1", runtime.Evaluate("return CachedChunkValue"));
            }

            var cachedFiles = Directory
                .EnumerateFiles(cache, "*.luac", SearchOption.AllDirectories)
                .Count();
            Assert.True(cachedFiles >= 1);

            using var cachedRuntime = CreateRuntime(cache);
            cachedRuntime.ExecuteFile(script);
            Assert.Equal("1", cachedRuntime.Evaluate("return CachedChunkValue"));
            Assert.Equal(
                cachedFiles,
                Directory.EnumerateFiles(cache, "*.luac", SearchOption.AllDirectories).Count());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static LuaRuntime CreateRuntime(string cache) =>
        new(new EmulatorLog(), new UiSystem(), luaCacheDirectory: cache);
}
