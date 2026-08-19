using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WoWAddonLab.Assets;

internal sealed record DecodedTextureMipLevel(
    byte[] Pixels,
    int Width,
    int Height);
