namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCameraState
{
    public float Zoom { get; set; } = 2;
    public float MaximumZoom { get; set; } = 39;
    public int? CurrentViewIndex { get; internal set; }
    public Dictionary<int, float> SavedViewZooms { get; } = [];
    public Dictionary<WowCameraMovementDirection, WowCameraMovementState> ActiveMovements { get; } = [];
}
