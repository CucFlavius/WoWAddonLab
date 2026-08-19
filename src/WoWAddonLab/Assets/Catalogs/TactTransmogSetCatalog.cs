using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactTransmogSetCatalog : TactCatalog,
    IWowTransmogSetProvider,
    IWowTransmogAppearanceProvider
{
    private const int CacheVersion = 1;
    private readonly IReadOnlyDictionary<int, WowTransmogSetDefinition> _setsById;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<int>> _sourceIdsBySetId;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<WowTransmogSetDefinition>>
        _variantsByBaseSetId;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<int>> _setIdsBySourceId;
    private readonly IReadOnlyDictionary<int, WowAppearanceSourceDefinition> _sourcesById;
    private readonly IReadOnlyDictionary<(int ItemId, int ItemModId), WowAppearanceSourceDefinition>
        _sourcesByItemAndMod;
    private readonly IReadOnlyDictionary<int, WowAppearanceSourceDefinition> _sourcesByItem;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<WowAppearanceSourceDefinition>>
        _sourcesByCategory;
    private readonly IReadOnlyDictionary<int, IReadOnlyList<WowAppearanceSourceDefinition>>
        _sourcesByVisual;

    private TactTransmogSetCatalog(
        IReadOnlyList<WowTransmogSetDefinition> sets,
        IReadOnlyDictionary<int, IReadOnlyList<int>> sourceIdsBySetId,
        IReadOnlyDictionary<int, WowAppearanceSourceDefinition> sourcesById)
    {
        Sets = sets;
        _setsById = sets.ToDictionary(value => value.SetId);
        _sourceIdsBySetId = sourceIdsBySetId;
        _sourcesById = sourcesById;
        _sourcesByItemAndMod = sourcesById.Values
            .GroupBy(value => (value.ItemId, value.ItemModId))
            .ToDictionary(group => group.Key, group => group.First());
        _sourcesByItem = sourcesById.Values
            .GroupBy(value => value.ItemId)
            .ToDictionary(group => group.Key, group => group.First());
        _sourcesByCategory = sourcesById.Values
            .Where(value => value.CategoryId > 0)
            .GroupBy(value => value.CategoryId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<WowAppearanceSourceDefinition>)group
                    .OrderBy(value => value.UiOrder)
                    .ThenBy(value => value.VisualId)
                    .ThenBy(value => value.SourceId)
                    .ToArray());
        _sourcesByVisual = sourcesById.Values
            .GroupBy(value => value.VisualId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<WowAppearanceSourceDefinition>)group
                    .OrderBy(value => value.SourceId)
                    .ToArray());
        _variantsByBaseSetId = sets
            .Where(value => value.BaseSetId is not null)
            .GroupBy(value => value.BaseSetId!.Value)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<WowTransmogSetDefinition>)group
                    .OrderBy(value => value.UiOrder)
                    .ThenBy(value => value.SetId)
                    .ToArray());
        _setIdsBySourceId = sourceIdsBySetId
            .SelectMany(pair => pair.Value.Select(sourceId => (sourceId, setId: pair.Key)))
            .GroupBy(value => value.sourceId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<int>)group
                    .Select(value => value.setId)
                    .Distinct()
                    .ToArray());
    }

    public IReadOnlyList<WowTransmogSetDefinition> Sets { get; }
    public int Count => _sourcesById.Count;

    public static TactTransmogSetCatalog Load(
        TactAssetSource tact,
        string build,
        string? cacheDirectory = null)
    {
        var cacheIdentity = tact.CatalogCacheIdentity(build);
        if (TactCatalogCache.TryRead(
                cacheDirectory,
                cacheIdentity,
                "transmog",
                CacheVersion,
                ReadCache,
                out TactTransmogSetCatalog? cached))
        {
            return cached!;
        }

        var database = tact.Database;
        var labels = database.Load("ItemNameDescription", build).Values
            .ToDictionary(
                row => Integer(row, "ID"),
                row => Text(row, "Description_lang", "Description"));
        var descriptions = database.Load("TransmogSetGroup", build).Values
            .ToDictionary(
                row => Integer(row, "ID"),
                row => Text(row, "Name_lang", "Name"));
        var factionGroups = database.Load("FactionGroup", build).Values
            .Select(row => new FactionGroup(
                Integer(row, "MaskID"),
                Text(row, "InternalName")))
            .Where(value => !string.IsNullOrEmpty(value.InternalName))
            .ToArray();

        var sets = database.Load("TransmogSet", build).Values
            .Select(row =>
            {
                var flags = Integer(row, "Flags");
                var parentSetId = Integer(row, "ParentTransmogSetID");
                var labelId = Integer(row, "ItemNameDescriptionID");
                var groupId = Integer(row, "TransmogSetGroupID");
                return new
                {
                    Flags = flags,
                    Definition = new WowTransmogSetDefinition(
                        Integer(row, "ID"),
                        Text(row, "Name_lang", "Name"),
                        parentSetId > 0 ? parentSetId : null,
                        OptionalText(descriptions, groupId),
                        OptionalText(labels, labelId),
                        Integer(row, "ExpansionID"),
                        Integer(row, "PatchIntroduced"),
                        Integer(row, "UiOrder"),
                        Integer(row, "ClassMask"),
                        (flags & 0x2) != 0,
                        RequiredFaction(flags, factionGroups),
                        (flags & 0x20) != 0,
                        (flags & 0x200) != 0)
                };
            })
            .Where(value => (value.Flags & 0x1) == 0)
            .Select(value => value.Definition)
            .OrderBy(value => value.SetId)
            .ToArray();

        var knownSetIds = sets.Select(value => value.SetId).ToHashSet();
        var sourceIds = database.Load("TransmogSetItem", build).Values
            .Select(row => new
            {
                SetId = Integer(row, "TransmogSetID"),
                SourceId = Integer(row, "ItemModifiedAppearanceID")
            })
            .Where(value => knownSetIds.Contains(value.SetId) && value.SourceId > 0)
            .GroupBy(value => value.SetId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<int>)group
                    .Select(value => value.SourceId)
                    .ToArray());

        var items = database.Load("Item", build).Values
            .ToDictionary(
                row => Integer(row, "ID"),
                row => new ItemDefinition(
                    Integer(row, "ClassID"),
                    Integer(row, "SubclassID"),
                    Integer(row, "InventoryType"),
                    Integer(row, "IconFileDataID")));
        var sparseItems = database.Load("ItemSparse", build).Values
            .ToDictionary(
                row => Integer(row, "ID"),
                row => new SparseItemDefinition(
                    Text(row, "Display_lang", "Display"),
                    Integer(row, "OverallQualityID"),
                    Integer(row, "AllowableClass"),
                    Integer(row, "RequiredTransmogHoliday")));
        var appearanceOrders = database.Load("ItemAppearance", build).Values
            .ToDictionary(
                row => Integer(row, "ID"),
                row => Integer(row, "UiOrder"));
        var appearances = database.Load("ItemModifiedAppearance", build).Values
            .Select(row =>
            {
                var sourceId = Integer(row, "ID");
                var itemId = Integer(row, "ItemID");
                items.TryGetValue(itemId, out var item);
                sparseItems.TryGetValue(itemId, out var sparse);
                var inventoryType = item?.InventoryType ?? 0;
                var sourceType = Integer(row, "TransmogSourceTypeEnum");
                var visualId = Integer(row, "ItemAppearanceID");
                return new WowAppearanceSourceDefinition(
                    visualId,
                    sourceId,
                    itemId,
                    Integer(row, "ItemAppearanceModifierID"),
                    item?.SubclassId ?? 0,
                    item?.IconFileDataId ?? 0,
                    inventoryType + 1,
                    TransmogCategory(item, inventoryType),
                    appearanceOrders.GetValueOrDefault(visualId),
                    InventorySlot(inventoryType),
                    sourceType is >= 0 and < 7 ? sourceType + 1 : null,
                    string.IsNullOrEmpty(sparse?.Name) ? null : sparse.Name,
                    sparse is null ? null : sparse.Quality,
                    sparse?.AllowableClassMask ?? 0,
                    sparse?.RequiredTransmogHolidayId ?? 0,
                    null,
                    null);
            })
            .Where(value => value.SourceId > 0)
            .ToDictionary(value => value.SourceId);

        var catalog = new TactTransmogSetCatalog(sets, sourceIds, appearances);
        TactCatalogCache.Write(
            cacheDirectory,
            cacheIdentity,
            "transmog",
            CacheVersion,
            writer => WriteCache(writer, catalog));
        return catalog;
    }

    private static TactTransmogSetCatalog ReadCache(BinaryReader reader)
    {
        var setCount = TactCatalogCache.ReadCount(reader, 100_000);
        var sets = new WowTransmogSetDefinition[setCount];
        for (var index = 0; index < setCount; index++)
        {
            sets[index] = new WowTransmogSetDefinition(
                reader.ReadInt32(),
                reader.ReadString(),
                TactCatalogCache.ReadNullableInt32(reader),
                TactCatalogCache.ReadNullableString(reader),
                TactCatalogCache.ReadNullableString(reader),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadBoolean(),
                TactCatalogCache.ReadNullableString(reader),
                reader.ReadBoolean(),
                reader.ReadBoolean());
        }

        var setSourceCount = TactCatalogCache.ReadCount(reader, 100_000);
        var sourceIds = new Dictionary<int, IReadOnlyList<int>>(setSourceCount);
        for (var index = 0; index < setSourceCount; index++)
        {
            var setId = reader.ReadInt32();
            var sourceCount = TactCatalogCache.ReadCount(reader, 1_000_000);
            var values = new int[sourceCount];
            for (var sourceIndex = 0; sourceIndex < sourceCount; sourceIndex++)
                values[sourceIndex] = reader.ReadInt32();
            sourceIds.Add(setId, values);
        }

        var appearanceCount = TactCatalogCache.ReadCount(reader, 1_000_000);
        var appearances = new Dictionary<int, WowAppearanceSourceDefinition>(appearanceCount);
        for (var index = 0; index < appearanceCount; index++)
        {
            var appearance = new WowAppearanceSourceDefinition(
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                reader.ReadInt32(),
                TactCatalogCache.ReadNullableInt32(reader),
                TactCatalogCache.ReadNullableInt32(reader),
                TactCatalogCache.ReadNullableString(reader),
                TactCatalogCache.ReadNullableInt32(reader),
                reader.ReadInt32(),
                reader.ReadInt32(),
                TactCatalogCache.ReadNullableBoolean(reader),
                TactCatalogCache.ReadNullableBoolean(reader));
            appearances.Add(appearance.SourceId, appearance);
        }

        return new TactTransmogSetCatalog(sets, sourceIds, appearances);
    }

    private static void WriteCache(BinaryWriter writer, TactTransmogSetCatalog catalog)
    {
        writer.Write(catalog.Sets.Count);
        foreach (var set in catalog.Sets)
        {
            writer.Write(set.SetId);
            writer.Write(set.Name);
            TactCatalogCache.WriteNullableInt32(writer, set.BaseSetId);
            TactCatalogCache.WriteNullableString(writer, set.Description);
            TactCatalogCache.WriteNullableString(writer, set.Label);
            writer.Write(set.ExpansionId);
            writer.Write(set.PatchId);
            writer.Write(set.UiOrder);
            writer.Write(set.ClassMask);
            writer.Write(set.HiddenUntilCollected);
            TactCatalogCache.WriteNullableString(writer, set.RequiredFaction);
            writer.Write(set.LimitedTimeSet);
            writer.Write(set.GrantAsPrecedingVariant);
        }

        writer.Write(catalog._sourceIdsBySetId.Count);
        foreach (var (setId, sourceIds) in catalog._sourceIdsBySetId.OrderBy(value => value.Key))
        {
            writer.Write(setId);
            writer.Write(sourceIds.Count);
            foreach (var sourceId in sourceIds)
                writer.Write(sourceId);
        }

        writer.Write(catalog._sourcesById.Count);
        foreach (var source in catalog._sourcesById.Values.OrderBy(value => value.SourceId))
        {
            writer.Write(source.VisualId);
            writer.Write(source.SourceId);
            writer.Write(source.ItemId);
            writer.Write(source.ItemModId);
            writer.Write(source.ItemSubclass);
            writer.Write(source.IconFileDataId);
            writer.Write(source.InventoryType);
            writer.Write(source.CategoryId);
            writer.Write(source.UiOrder);
            TactCatalogCache.WriteNullableInt32(writer, source.InventorySlot);
            TactCatalogCache.WriteNullableInt32(writer, source.SourceType);
            TactCatalogCache.WriteNullableString(writer, source.Name);
            TactCatalogCache.WriteNullableInt32(writer, source.Quality);
            writer.Write(source.AllowableClassMask);
            writer.Write(source.RequiredTransmogHolidayId);
            TactCatalogCache.WriteNullableBoolean(writer, source.MeetsTransmogPlayerCondition);
            TactCatalogCache.WriteNullableBoolean(writer, source.IsHideVisual);
        }
    }

    public bool TryGetSet(int setId, out WowTransmogSetDefinition definition) =>
        _setsById.TryGetValue(setId, out definition!);

    public IReadOnlyList<int> GetSourceIds(int setId) =>
        _sourceIdsBySetId.TryGetValue(setId, out var sourceIds) ? sourceIds : [];

    public IReadOnlyList<WowTransmogSetDefinition> GetVariantSets(int setId) =>
        _variantsByBaseSetId.TryGetValue(setId, out var variants) ? variants : [];

    public IReadOnlyList<int> GetSetIdsContainingSource(int sourceId) =>
        _setIdsBySourceId.TryGetValue(sourceId, out var setIds) ? setIds : [];

    public bool TryGetSource(
        int sourceId,
        out WowAppearanceSourceDefinition definition) =>
        _sourcesById.TryGetValue(sourceId, out definition!);

    public bool TryGetSourceForItem(
        int itemId,
        int? itemModId,
        out WowAppearanceSourceDefinition definition) =>
        itemModId is { } modifier
            ? _sourcesByItemAndMod.TryGetValue((itemId, modifier), out definition!)
            : _sourcesByItem.TryGetValue(itemId, out definition!);

    public IReadOnlyList<WowAppearanceSourceDefinition> GetSourcesByCategory(int categoryId) =>
        _sourcesByCategory.TryGetValue(categoryId, out var definitions) ? definitions : [];

    public IReadOnlyList<WowAppearanceSourceDefinition> GetSourcesByVisual(int visualId) =>
        _sourcesByVisual.TryGetValue(visualId, out var definitions) ? definitions : [];

    private static int TransmogCategory(ItemDefinition? item, int inventoryType)
    {
        if (item is null)
            return 0;

        if (item.ClassId == 2)
        {
            return item.SubclassId switch
            {
                19 => 12,
                0 => 13,
                7 => 14,
                4 => 15,
                15 => 16,
                13 => 17,
                1 => 20,
                8 => 21,
                5 => 22,
                10 => 23,
                6 => 24,
                2 => 25,
                3 => 26,
                18 => 27,
                9 => 28,
                _ => 0
            };
        }

        if (item.ClassId == 4 && item.SubclassId == 6)
            return 18;
        if (item.ClassId == 4 && item.SubclassId == 0 && inventoryType == 23)
            return 19;

        return inventoryType switch
        {
            1 => 1,
            3 => 2,
            16 => 3,
            5 or 20 => 4,
            4 => 5,
            19 => 6,
            9 => 7,
            10 => 8,
            6 => 9,
            7 => 10,
            8 => 11,
            _ => 0
        };
    }

    private static int? InventorySlot(int inventoryType) => inventoryType switch
    {
        1 => 1,
        3 => 3,
        4 => 4,
        5 or 20 => 5,
        6 => 6,
        7 => 7,
        8 => 8,
        9 => 9,
        10 => 10,
        16 => 15,
        13 or 15 or 17 or 21 or 25 or 26 => 16,
        14 or 22 or 23 => 17,
        19 => 19,
        _ => null
    };

    private static string? OptionalText(
        IReadOnlyDictionary<int, string> values,
        int id) =>
        id > 0 && values.TryGetValue(id, out var value) && !string.IsNullOrEmpty(value)
            ? value
            : null;

    private static string? RequiredFaction(
        int flags,
        IReadOnlyList<FactionGroup> factionGroups)
    {
        var selector = (flags & 0x8) != 0
            ? 0
            : (flags & 0x4) == 0 ? 2 : 1;
        if (selector == 2)
            return null;

        var factionMask = selector == 1 ? 3 : 5;
        return factionGroups.FirstOrDefault(
            value => ((1 << value.MaskId) & factionMask) != 0)?.InternalName;
    }

    private sealed record FactionGroup(int MaskId, string InternalName);
    private sealed record ItemDefinition(
        int ClassId,
        int SubclassId,
        int InventoryType,
        int IconFileDataId);
    private sealed record SparseItemDefinition(
        string Name,
        int Quality,
        int AllowableClassMask,
        int RequiredTransmogHolidayId);
}
