namespace WoWAddonLab.Emulator.Lua;

public interface IWowInventorySlotProvider
{
    IReadOnlyDictionary<string, WowInventorySlotInfo> InventorySlots { get; }
}
