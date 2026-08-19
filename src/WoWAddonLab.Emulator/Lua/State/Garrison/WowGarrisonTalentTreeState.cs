namespace WoWAddonLab.Emulator.Lua;

public sealed class WowGarrisonTalentTreeState
{
    public int TreeId { get; init; }
    public string? Title { get; init; }
    public string TextureKit { get; init; } = string.Empty;
    public IReadOnlyList<WowGarrisonTalentState> Talents { get; init; } = [];
    public bool IsClassAgnostic { get; init; }
    public bool IsThemed { get; init; }
    public int FeatureType { get; init; }
    public int FeatureSubtype { get; init; }
}
