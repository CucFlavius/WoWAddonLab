namespace WoWAddonLab.Emulator.Lua;

public sealed record WowLootJournalItemState(
    int ItemId,
    int? IconFileDataId,
    int InventoryTypeIndex);
