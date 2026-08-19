namespace WoWAddonLab.Emulator.Lua;

public sealed class WowVideoOptionsState
{
    public string CurrentGraphicsApi { get; set; } = "D3D12";
    public IList<string> GraphicsApis { get; } =
        new List<string> { "D3D11", "D3D12", "Auto" };
    public double MinimumRenderScale { get; set; } = (double)(1f / 3f);
    public double MaximumRenderScale { get; set; } = 2;
    public float CameraFovDefault { get; set; } = 90;
    public float CameraFovMinimum { get; set; } = 50;
    public float CameraFovMaximum { get; set; } = 90;
    public bool AdvancedAntiAliasingAvailable { get; set; }
    public bool UpscalingAntiAliasingAvailable { get; set; }
    public bool SpellVisualDensitySystemSupported { get; set; } = true;
    public IList<WowGraphicsAdapterState> Adapters { get; } =
        new List<WowGraphicsAdapterState>();
    public IList<(uint Width, uint Height)> GameWindowSizes { get; } =
        new List<(uint Width, uint Height)>();
    public (uint Width, uint Height)? DefaultGameWindowSize { get; set; }
    public (uint Width, uint Height)? RequestedGameWindowSize { get; set; }
}
