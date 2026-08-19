using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Emulator.Lua;

public readonly record struct WowItemLocation(
    WowItemLocationKind Kind,
    int BagId,
    int SlotIndex)
{
    public static WowItemLocation Bag(int bagId, int slotIndex) =>
        new(WowItemLocationKind.Bag, bagId, slotIndex);

    public static WowItemLocation Equipment(int slotIndex) =>
        new(WowItemLocationKind.Equipment, 0, slotIndex);
}
