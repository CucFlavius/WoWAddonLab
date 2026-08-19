using DBCD.Providers;
using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Assets;

public sealed class TactGlobalColorCatalog : TactCatalog, IWowGlobalColorProvider
{
    private TactGlobalColorCatalog(IReadOnlyList<WowGlobalColor> colors)
    {
        Colors = colors;
    }

    public IReadOnlyList<WowGlobalColor> Colors { get; }
    public int Count => Colors.Count;

    public static TactGlobalColorCatalog Load(TactAssetSource tact, string build)
    {
        var database = tact.Database;
        var results = new List<WowGlobalColor>();

        foreach (var row in database.Load("GlobalColor", build).Values)
        {
            var baseTag = Convert.ToString(Field(row, "LuaConstantName")) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(baseTag))
                continue;

            var argb = unchecked((uint)Convert.ToInt32(Field(row, "Color") ?? 0));
            results.Add(new WowGlobalColor(
                baseTag,
                ((argb >> 16) & 0xff) / 255f,
                ((argb >> 8) & 0xff) / 255f,
                (argb & 0xff) / 255f,
                ((argb >> 24) & 0xff) / 255f));
        }

        return new TactGlobalColorCatalog(results);
    }

}
