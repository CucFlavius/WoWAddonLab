namespace WoWAddonLab.Emulator.Lua;

public sealed class WowNavigationState
{
    private const float MinimumValidScreenPositionSquared = 0.00000023841858f;
    private bool? _hasValidScreenPositionOverride;

    public double Distance { get; set; }
    public int? FrameId { get; set; }
    public string? NearestPartyMemberToken { get; set; }
    public int TargetState { get; set; }
    public float ScreenPositionX { get; set; }
    public float ScreenPositionY { get; set; }
    public bool HasValidScreenPosition
    {
        get => _hasValidScreenPositionOverride ??
               MathF.Abs(
                   ScreenPositionX * ScreenPositionX +
                   ScreenPositionY * ScreenPositionY) >=
               MinimumValidScreenPositionSquared;
        set => _hasValidScreenPositionOverride = value;
    }
    public bool WasClampedToScreen { get; set; }

    public void UseComputedScreenPositionValidity() =>
        _hasValidScreenPositionOverride = null;
}
