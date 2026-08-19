using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiAnimationGroupState
{
    public string Looping { get; set; } = "NONE";
    public float AnimationSpeedMultiplier { get; set; } = 1;
    public bool SetToFinalAlpha { get; set; }
    public bool PendingFinish { get; set; }
    public bool Playing { get; set; }
    public bool Paused { get; set; }
    public bool Finished { get; set; }
    public bool Reverse { get; set; }
    public double Elapsed { get; set; }
    public int CurrentOrderIndex { get; set; }
}
