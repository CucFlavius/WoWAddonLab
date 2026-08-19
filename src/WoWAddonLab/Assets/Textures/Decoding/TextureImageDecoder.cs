using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace WoWAddonLab.Assets;

internal static class TextureImageDecoder
{
    public static DecodedTextureImage Decode(byte[] bytes)
    {
        if (IsBlp(bytes))
            return DecodeBlp(bytes);

        using var image = Image.Load<Rgba32>(bytes);
        return new DecodedTextureImage([CopyLevel(image)]);
    }

    private static DecodedTextureImage DecodeBlp(byte[] bytes)
        => new BLP(bytes).Decode();

    private static DecodedTextureMipLevel CopyLevel(Image<Rgba32> image)
    {
        var pixels = new byte[checked(image.Width * image.Height * 4)];
        image.CopyPixelDataTo(pixels);
        return new DecodedTextureMipLevel(pixels, image.Width, image.Height);
    }

    private static bool IsBlp(ReadOnlySpan<byte> bytes) =>
        bytes.Length >= 4 &&
        bytes[0] == (byte)'B' &&
        bytes[1] == (byte)'L' &&
        bytes[2] == (byte)'P';

}
