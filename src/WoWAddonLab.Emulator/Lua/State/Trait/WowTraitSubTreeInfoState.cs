namespace WoWAddonLab.Emulator.Lua;

public sealed record WowTraitSubTreeInfoState(
    int Id,
    string? Name,
    string? Description,
    int IconElementId,
    int? TraitCurrencyId,
    bool IsActive,
    IReadOnlyList<int> SubTreeSelectionNodeIds,
    int PosX,
    int PosY);
