using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactPaperDollItemFrameCatalog :
    TactCatalog,
    IWowInventorySlotProvider
{
    private TactPaperDollItemFrameCatalog(
        IReadOnlyDictionary<string, WowInventorySlotInfo> inventorySlots)
    {
        InventorySlots = inventorySlots;
    }

    public IReadOnlyDictionary<string, WowInventorySlotInfo> InventorySlots {
        get;
    }

    public static TactPaperDollItemFrameCatalog Load(
        TactAssetSource tact,
        string build)
    {
        var slots = new Dictionary<string, WowInventorySlotInfo>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var row in tact.Database.Load(
                     "PaperDollItemFrame",
                     build).Values)
        {
            var name = Text(row, "ItemButtonName");
            var slotId = Integer(row, "SlotNumber");
            if (string.IsNullOrWhiteSpace(name) || slotId == 18)
                continue;

            var textureFileId = Integer(row, "SlotIconFileID");
            slots[name] = new WowInventorySlotInfo(
                slotId,
                textureFileId > 0 ? textureFileId : null);
        }

        return new TactPaperDollItemFrameCatalog(slots);
    }
}
