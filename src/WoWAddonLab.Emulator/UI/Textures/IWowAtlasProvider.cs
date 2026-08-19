using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public interface IWowAtlasProvider
{
    bool TryGetAtlas(string name, out WowAtlasInfo info);

    IEnumerable<WowAtlasInfo> EnumerateAtlases() => [];
}
