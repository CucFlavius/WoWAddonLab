using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WoWAddonLab.Assets;

internal sealed record DecodedTextureImage(
    IReadOnlyList<DecodedTextureMipLevel> MipLevels)
{
    public DecodedTextureMipLevel BaseLevel => MipLevels[0];
}
