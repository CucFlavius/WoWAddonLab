using DBCD.Providers;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactCharacterServiceCatalog : TactCatalog, IWowCharacterServiceProvider
{
    private readonly IReadOnlyDictionary<int, WowCharacterServiceDisplayData> _services;

    private TactCharacterServiceCatalog(
        IReadOnlyDictionary<int, WowCharacterServiceDisplayData> services)
    {
        _services = services;
    }

    public int Count => _services.Count;

    public bool TryGetDisplayData(int boostType, out WowCharacterServiceDisplayData data) =>
        _services.TryGetValue(boostType, out data!);

    public static TactCharacterServiceCatalog Load(TactAssetSource tact, string build)
    {
        var database = tact.Database;
        var textureKits = database.Load("UiTextureKit", build).Values
            .ToDictionary(
                row => Integer(row, "ID"),
                row => Text(row, "KitPrefix"));
        var services = new Dictionary<int, WowCharacterServiceDisplayData>();
        foreach (var row in database.Load("CharacterServiceInfo", build).Values
                     .OrderBy(row => Integer(row, "ID")))
        {
            var boostType = Integer(row, "BoostType");
            services.TryAdd(
                boostType,
                new WowCharacterServiceDisplayData(
                    boostType,
                    Integer(row, "ServiceType"),
                    Integer(row, "BoostLevel"),
                    Integer(row, "Expansion"),
                    string.Empty,
                    string.Empty,
                    Text(row, "FlowTitle_lang"),
                    Integer(row, "Flags"),
                    Integer(row, "ProfessionLevel"),
                    new WowCharacterServicePopupInfo(
                        Text(row, "PopupTitle_lang"),
                        Text(row, "PopupDescription_lang"),
                        ResolveTextureKit(
                            textureKits,
                            Integer(row, "PopupUITextureKitID"))),
                    Unsigned(row, "IconFileDataID"),
                    ResolveOptionalTextureKit(
                        textureKits,
                        Integer(row, "Field_11_0_0_54675_012"))));
        }
        return new TactCharacterServiceCatalog(services);
    }

    private static string ResolveTextureKit(
        IReadOnlyDictionary<int, string> textureKits,
        int textureKitId) =>
        textureKits.GetValueOrDefault(textureKitId) ?? string.Empty;

    private static string? ResolveOptionalTextureKit(
        IReadOnlyDictionary<int, string> textureKits,
        int textureKitId) =>
        textureKits.TryGetValue(textureKitId, out var textureKit)
            ? textureKit
            : null;




}
