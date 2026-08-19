namespace WoWAddonLab.Emulator.Lua;

public sealed class WowArtifactState
{
    public bool HasEquippedArtifact { get; set; }
    public bool IsAtForge { get; set; }
    public bool IsEquippedArtifactMaxed { get; set; }
    public bool IsEquippedArtifactDisabled { get; set; } = true;
    public bool IsArtifactDisabled { get; set; } = true;
    public bool IsMaxedByRulesOrEffect { get; set; }
    public int NumObtainedArtifacts { get; set; }
    public int ClearCount { get; internal set; }
    public WowArtifactInstanceState? ViewedArtifact { get; set; }
    public WowArtifactInstanceState? EquippedArtifact { get; set; }
    public IList<byte> PurchasedPowerRanks { get; } = new List<byte>();
    public IDictionary<uint, WowArtifactAppearanceInfoState> AppearanceInfoById { get; } =
        new Dictionary<uint, WowArtifactAppearanceInfoState>();
}
