namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTraitTreeInfoState(
    int Id,
    IReadOnlyList<WowTraitTreeGateState> Gates,
    bool HideSingleRankNumbers,
    int? RootNodeId,
    string UiTextureKit,
    string? TitleText);
