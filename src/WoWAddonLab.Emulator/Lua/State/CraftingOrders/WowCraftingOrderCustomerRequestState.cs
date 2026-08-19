namespace WoWAddonLab.Emulator.Lua;

public sealed record WowCraftingOrderCustomerRequestState(
    byte OrderType,
    uint? SelectedSkillLineAbility,
    bool SearchFavorites,
    bool InitialNonPublicSearch,
    WowCraftingOrderSortState PrimarySort,
    WowCraftingOrderSortState SecondarySort,
    bool ForCrafter,
    uint Offset,
    bool HasCallback,
    byte? Profession = null);
