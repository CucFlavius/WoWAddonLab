using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactItemClassCatalog : TactCatalog, IWowItemClassProvider, IWowItemProvider
{
    private const int CacheVersion = 1;
    private TactItemClassCatalog(
        IReadOnlyDictionary<int, string> classes,
        IReadOnlyDictionary<(int ClassId, int SubClassId), WowItemSubClassData>
            subClasses,
        IReadOnlyDictionary<int, WowItemData> items)
    {
        Classes = classes;
        SubClasses = subClasses;
        Items = items;
    }

    public IReadOnlyDictionary<int, string> Classes { get; }
    public IReadOnlyDictionary<(int ClassId, int SubClassId), WowItemSubClassData>
        SubClasses { get; }
    public IReadOnlyDictionary<int, WowItemData> Items { get; }

    public static TactItemClassCatalog Load(
        TactAssetSource tact,
        string build,
        string? cacheDirectory = null)
    {
        var cacheIdentity = tact.CatalogCacheIdentity(build);
        if (TactCatalogCache.TryRead(
                cacheDirectory,
                cacheIdentity,
                "items",
                CacheVersion,
                ReadCache,
                out TactItemClassCatalog? cached))
        {
            return cached!;
        }

        var classes = new Dictionary<int, string>();
        foreach (var row in tact.Database.Load("ItemClass", build).Values)
        {
            var id = Integer(row, "ClassID");
            var name = Text(row, "ClassName_lang", "ClassName", "Name_lang", "Name");
            if (!string.IsNullOrWhiteSpace(name))
                classes[id] = name;
        }

        var subClasses = new Dictionary<
            (int ClassId, int SubClassId),
            WowItemSubClassData>();
        foreach (var row in tact.Database.Load("ItemSubClass", build).Values)
        {
            var classId = Integer(row, "ClassID");
            var subClassId = Integer(row, "SubClassID");
            var name = Text(
                row,
                "VerboseName_lang",
                "VerboseName",
                "DisplayName_lang",
                "DisplayName");
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var flags = Integer(row, "Flags");
            subClasses[(classId, subClassId)] = new WowItemSubClassData(
                name,
                (flags & 0x200) != 0);
        }

        var items = new Dictionary<int, WowItemData>();
        foreach (var row in tact.Database.Load("Item", build).Values)
        {
            var id = Integer(row, "ID");
            var classId = Integer(row, "ClassID");
            var subClassId = Integer(row, "SubclassID");
            var inventoryType = Integer(row, "InventoryType");
            classes.TryGetValue(classId, out var itemType);
            subClasses.TryGetValue((classId, subClassId), out var itemSubClass);
            items[id] = new WowItemData
            {
                ItemId = id,
                ItemType = itemType,
                ItemSubType = itemSubClass?.Name,
                EquipLocation = InventoryTypeName(inventoryType),
                TextureFileId = Integer(row, "IconFileDataID"),
                ClassId = classId,
                SubClassId = subClassId
            };
        }

        foreach (var row in tact.Database.Load("ItemSparse", build).Values)
        {
            var id = Integer(row, "ID");
            if (!items.TryGetValue(id, out var item))
                continue;
            item.Name = Text(row, "Display_lang", "Display");
            item.Description = Text(row, "Description_lang", "Description");
            item.Quality = (byte)Math.Clamp(Integer(row, "OverallQualityID"), 0, byte.MaxValue);
            item.ItemLevel = Integer(row, "ItemLevel");
            item.MinimumLevel = Integer(row, "RequiredLevel");
            item.StackCount = Math.Max(1, Integer(row, "Stackable"));
            item.SellPrice = Integer(row, "SellPrice");
            item.BindType = Integer(row, "Bonding");
            item.ExpansionId = Integer(row, "ExpansionID");
            var setId = Integer(row, "ItemSet");
            item.SetId = setId > 0 ? setId : null;
            if (!string.IsNullOrEmpty(item.Name))
                item.Link = $"|Hitem:{id}|h[{item.Name}]|h";
        }

        var catalog = new TactItemClassCatalog(classes, subClasses, items);
        TactCatalogCache.Write(
            cacheDirectory,
            cacheIdentity,
            "items",
            CacheVersion,
            writer => WriteCache(writer, catalog));
        return catalog;
    }

    private static TactItemClassCatalog ReadCache(BinaryReader reader)
    {
        var classCount = TactCatalogCache.ReadCount(reader, 1_000);
        var classes = new Dictionary<int, string>(classCount);
        for (var index = 0; index < classCount; index++)
            classes.Add(reader.ReadInt32(), reader.ReadString());

        var subClassCount = TactCatalogCache.ReadCount(reader, 10_000);
        var subClasses = new Dictionary<(int, int), WowItemSubClassData>(subClassCount);
        for (var index = 0; index < subClassCount; index++)
        {
            var classId = reader.ReadInt32();
            var subClassId = reader.ReadInt32();
            subClasses.Add(
                (classId, subClassId),
                new WowItemSubClassData(
                    TactCatalogCache.ReadNullableString(reader),
                    reader.ReadBoolean()));
        }

        var itemCount = TactCatalogCache.ReadCount(reader, 1_000_000);
        var items = new Dictionary<int, WowItemData>(itemCount);
        for (var index = 0; index < itemCount; index++)
        {
            var item = new WowItemData
            {
                ItemId = reader.ReadInt32(),
                Name = TactCatalogCache.ReadNullableString(reader),
                Link = TactCatalogCache.ReadNullableString(reader),
                Quality = reader.ReadByte(),
                ItemLevel = reader.ReadInt32(),
                MinimumLevel = reader.ReadInt32(),
                ItemType = TactCatalogCache.ReadNullableString(reader),
                ItemSubType = TactCatalogCache.ReadNullableString(reader),
                StackCount = reader.ReadInt32(),
                EquipLocation = TactCatalogCache.ReadNullableString(reader),
                TextureFileId = reader.ReadInt32(),
                SellPrice = reader.ReadInt32(),
                ClassId = reader.ReadInt32(),
                SubClassId = reader.ReadInt32(),
                BindType = reader.ReadInt32(),
                ExpansionId = reader.ReadInt32(),
                SetId = TactCatalogCache.ReadNullableInt32(reader),
                Description = TactCatalogCache.ReadNullableString(reader)
            };
            items.Add(item.ItemId, item);
        }

        return new TactItemClassCatalog(classes, subClasses, items);
    }

    private static void WriteCache(BinaryWriter writer, TactItemClassCatalog catalog)
    {
        writer.Write(catalog.Classes.Count);
        foreach (var (id, name) in catalog.Classes.OrderBy(value => value.Key))
        {
            writer.Write(id);
            writer.Write(name);
        }

        writer.Write(catalog.SubClasses.Count);
        foreach (var (key, subClass) in catalog.SubClasses
                     .OrderBy(value => value.Key.ClassId)
                     .ThenBy(value => value.Key.SubClassId))
        {
            writer.Write(key.ClassId);
            writer.Write(key.SubClassId);
            TactCatalogCache.WriteNullableString(writer, subClass.Name);
            writer.Write(subClass.UsesInventoryType);
        }

        writer.Write(catalog.Items.Count);
        foreach (var item in catalog.Items.Values.OrderBy(value => value.ItemId))
        {
            writer.Write(item.ItemId);
            TactCatalogCache.WriteNullableString(writer, item.Name);
            TactCatalogCache.WriteNullableString(writer, item.Link);
            writer.Write(item.Quality);
            writer.Write(item.ItemLevel);
            writer.Write(item.MinimumLevel);
            TactCatalogCache.WriteNullableString(writer, item.ItemType);
            TactCatalogCache.WriteNullableString(writer, item.ItemSubType);
            writer.Write(item.StackCount);
            TactCatalogCache.WriteNullableString(writer, item.EquipLocation);
            writer.Write(item.TextureFileId);
            writer.Write(item.SellPrice);
            writer.Write(item.ClassId);
            writer.Write(item.SubClassId);
            writer.Write(item.BindType);
            writer.Write(item.ExpansionId);
            TactCatalogCache.WriteNullableInt32(writer, item.SetId);
            TactCatalogCache.WriteNullableString(writer, item.Description);
        }
    }

    private static string? InventoryTypeName(int inventoryType) => inventoryType switch
    {
        1 => "INVTYPE_HEAD", 2 => "INVTYPE_NECK", 3 => "INVTYPE_SHOULDER",
        4 => "INVTYPE_BODY", 5 => "INVTYPE_CHEST", 6 => "INVTYPE_WAIST",
        7 => "INVTYPE_LEGS", 8 => "INVTYPE_FEET", 9 => "INVTYPE_WRIST",
        10 => "INVTYPE_HAND", 11 => "INVTYPE_FINGER", 12 => "INVTYPE_TRINKET",
        13 => "INVTYPE_WEAPON", 14 => "INVTYPE_SHIELD", 15 => "INVTYPE_RANGED",
        16 => "INVTYPE_CLOAK", 17 => "INVTYPE_2HWEAPON", 18 => "INVTYPE_BAG",
        19 => "INVTYPE_TABARD", 20 => "INVTYPE_ROBE", 21 => "INVTYPE_WEAPONMAINHAND",
        22 => "INVTYPE_WEAPONOFFHAND", 23 => "INVTYPE_HOLDABLE", 24 => "INVTYPE_AMMO",
        25 => "INVTYPE_THROWN", 26 => "INVTYPE_RANGEDRIGHT", 27 => "INVTYPE_QUIVER",
        28 => "INVTYPE_RELIC",
        _ => null
    };
}
