using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiModelLightState
{
    public bool Omnidirectional { get; set; }
    public Vector3 Point { get; set; }
    public float AmbientIntensity { get; set; }
    public Vector3? AmbientColor { get; set; }
    public float DiffuseIntensity { get; set; }
    public Vector3? DiffuseColor { get; set; }
}
