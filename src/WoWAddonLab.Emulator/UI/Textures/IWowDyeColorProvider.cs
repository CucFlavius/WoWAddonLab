namespace WoWAddonLab.Emulator.UI;

public interface IWowDyeColorProvider
{
    bool TryGetGradientTextureIndex(int dyeColorId, out int gradientTextureIndex);
}
