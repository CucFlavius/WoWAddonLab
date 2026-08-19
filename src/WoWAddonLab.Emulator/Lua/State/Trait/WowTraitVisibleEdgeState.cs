namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTraitVisibleEdgeState(
    int TargetNode,
    int Type,
    int VisualStyle,
    bool IsActive);
