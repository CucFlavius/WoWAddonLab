using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Assets;

public sealed class TactDyeColorCatalog : TactCatalog, IWowDyeColorProvider
{
    private readonly IReadOnlyDictionary<int, int> _gradientTextureIndices;

    private TactDyeColorCatalog(IReadOnlyDictionary<int, int> gradientTextureIndices)
    {
        _gradientTextureIndices = gradientTextureIndices;
    }

    public int Count => _gradientTextureIndices.Count;

    public static TactDyeColorCatalog Load(TactAssetSource tact, string build)
    {
        var results = new Dictionary<int, int>();
        foreach (var row in tact.Database.Load("DyeColor", build).Values)
        {
            var id = Convert.ToInt32(Field(row, "ID") ?? 0);
            if (id == 0)
                continue;

            results[id] = Convert.ToInt32(Field(row, "GradientTextureIndex") ?? 0);
        }

        return new TactDyeColorCatalog(results);
    }

    public bool TryGetGradientTextureIndex(int dyeColorId, out int gradientTextureIndex) =>
        _gradientTextureIndices.TryGetValue(dyeColorId, out gradientTextureIndex);

}
