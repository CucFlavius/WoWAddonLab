using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiControlPointState
{
    public Vector2 Offset { get; set; }
    public int Order { get; set; } = -1;
    public float NormalizedTime { get; set; }
}
