namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCovenantDataState
{
    public int Id { get; init; }
    public string? TextureKit { get; init; }
    public int CelebrationSoundKit { get; init; }
    public int AnimaChannelSelectSoundKit { get; init; }
    public int AnimaChannelActiveSoundKit { get; init; }
    public int AnimaGemsFullSoundKit { get; init; }
    public int AnimaNewGemSoundKit { get; init; }
    public int AnimaReinforceSelectSoundKit { get; init; }
    public int UpgradeTabSelectSoundKitId { get; init; }
    public int ReservoirFullSoundKitId { get; init; }
    public int BeginResearchSoundKitId { get; init; }
    public int RenownFanfareSoundKitId { get; init; }
    public int FactionId { get; init; }
    public string? Name { get; init; }
    public IReadOnlyList<int> SoulbindIds { get; init; } = [];
}
