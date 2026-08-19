using DBCD.Providers;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactAccountStoreCatalog : TactCatalog, IWowAccountStoreProvider
{
    private TactAccountStoreCatalog(
        IReadOnlyList<WowAccountStoreCategoryDefinition> categories,
        IReadOnlyList<WowAccountStoreItemDefinition> items)
    {
        Categories = categories;
        Items = items;
    }

    public IReadOnlyList<WowAccountStoreCategoryDefinition> Categories { get; }
    public IReadOnlyList<WowAccountStoreItemDefinition> Items { get; }

    public static TactAccountStoreCatalog Load(TactAssetSource tact, string build)
    {
        var database = tact.Database;
        var categories = database.Load("AccountStoreCategory", build).Values
            .Select(row => new WowAccountStoreCategoryDefinition(
                Integer(row, "ID"),
                Integer(row, "StoreFrontID"),
                Integer(row, "OrderIndex"),
                Text(row, "Name_lang"),
                Integer(row, "Field_11_0_7_57361_005"),
                Unsigned(row, "Icon")))
            .OrderBy(value => value.StoreFrontId)
            .ThenBy(value => value.OrderIndex)
            .ThenBy(value => value.Id)
            .ToArray();
        var items = database.Load("AccountStoreItem", build).Values
            .Select(row => new WowAccountStoreItemDefinition(
                Integer(row, "ID"),
                Integer(row, "StoreFrontID"),
                Integer(row, "AccountStoreCategoryID"),
                Integer(row, "OrderIndex"),
                Text(row, "Name_lang"),
                Text(row, "Description_lang"),
                Integer(row, "Price"),
                Integer(row, "CurrencyTypesID"),
                Integer(row, "SpellID"),
                Integer(row, "TransmogSetID"),
                Integer(row, "CreatureDisplayInfoID"),
                Integer(row, "UiModelSceneID"),
                Unsigned(row, "Icon"),
                Integer(row, "Field_11_0_7_57361_010"),
                Integer(row, "RefundDuration") == 0))
            .OrderBy(value => value.StoreFrontId)
            .ThenBy(value => value.CategoryId)
            .ThenBy(value => value.OrderIndex)
            .ThenBy(value => value.Id)
            .ToArray();
        return new TactAccountStoreCatalog(categories, items);
    }




}
