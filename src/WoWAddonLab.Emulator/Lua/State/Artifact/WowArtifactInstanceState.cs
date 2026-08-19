namespace WoWAddonLab.Emulator.Lua;

public sealed record WowArtifactInstanceState(
    string ArtifactGuid,
    WowArtifactInfoState? Info,
    WowArtifactArtInfoState? ArtInfo)
{
    public byte? Tier { get; init; }
}
