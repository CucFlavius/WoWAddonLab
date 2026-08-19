using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGameUiState
{
    public IList<WowDisplayMonitorInfo> Monitors { get; } =
        [new("Primary Display", true)];

    public IList<WowMultisampleOption> MultisampleOptions { get; } = [];
    public bool DoesAnyDisplayHaveNotch { get; set; }
    public bool ShouldUiParentAvoidNotch { get; set; }
    public WowNotchSafeRegion TopLeftNotchSafeRegion { get; set; } = new(0, 0, 0, 0);
    public WowNotchSafeRegion TopRightNotchSafeRegion { get; set; } = new(0, 0, 0, 0);
    public float ConsoleFontHeightPixels { get; set; }
    public int ReloadRequestCount { get; set; }
}
