using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiUnitPositionState
{
    public int UiMapId { get; set; }
    public float PlayerPingScale { get; set; } = 1;
    public bool PlayerPingActive { get; set; }
    public double PlayerPingStartedAt { get; set; }
    public float PlayerPingDuration { get; set; }
    public float PlayerPingFadeDuration { get; set; }
    public bool UnitsFinalized { get; set; }
    public Dictionary<string, UiUnitPositionEntry> Units { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public List<int> UnitTexturePool { get; } = [];
    public int?[] PlayerPingTextureIds { get; } = new int?[3];
    public Dictionary<int, UiUnitPositionPingTexture> PlayerPingTextures { get; } = [];
    public List<string> MouseOverUnits { get; } = [];
}
