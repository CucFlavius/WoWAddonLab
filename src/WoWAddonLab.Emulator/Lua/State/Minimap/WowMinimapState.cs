namespace WoWAddonLab.Emulator.Lua;

public sealed class WowMinimapState
{
    public bool CanTrackBattlePets { get; set; }
    public bool ShouldUseHybridMinimap { get; set; }
    public int Zoom { get; set; }
    public IList<WowMinimapTrackingState> Tracking { get; } =
        new List<WowMinimapTrackingState>();
}
