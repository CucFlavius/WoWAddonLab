using DBCD.Providers;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactGlobalStringCatalog : TactCatalog, IWowGlobalStringProvider
{
    private TactGlobalStringCatalog(IReadOnlyDictionary<string, string> strings)
    {
        Strings = strings;
    }

    public IReadOnlyDictionary<string, string> Strings { get; }
    public int Count => Strings.Count;

    public static TactGlobalStringCatalog Load(TactAssetSource tact, string build)
    {
        var database = tact.Database;
        var strings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var row in database.Load("GlobalStrings", build).Values)
        {
            if ((Integer(row, "Flags") & 1) == 0)
                continue;
            var name = Text(row, "BaseTag");
            if (string.IsNullOrWhiteSpace(name))
                continue;
            strings[name] = WowGlobalStringText.DecodeDatabaseEscapes(Text(row, "TagText_lang"));
        }
        return new TactGlobalStringCatalog(strings);
    }



}
