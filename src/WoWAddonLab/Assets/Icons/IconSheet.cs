using System.Numerics;
using System.Reflection;
using Silk.NET.OpenGL;

namespace WoWAddonLab.Assets;

internal sealed class IconSheet : IDisposable
{
    private const string ResourceName = "WoWAddonLab.Assets.Icons1.png";
    private const int Columns = 8;
    private const int Rows = 8;
    private const int MaximumMipLevel = 1;

    private readonly GL _gl;
    private uint _texture;

    public IconSheet(GL gl)
    {
        _gl = gl;
        _texture = Upload(Decode());
    }

    public nint TextureId => (nint)_texture;

    public static Vector2 Uv0(IconSheetIcon icon) =>
        new((int)icon % Columns / (float)Columns, (int)icon / Columns / (float)Rows);

    public static Vector2 Uv1(IconSheetIcon icon) =>
        Uv0(icon) + new Vector2(1f / Columns, 1f / Rows);

    public void Dispose()
    {
        if (_texture != 0)
            _gl.DeleteTexture(_texture);
        _texture = 0;
    }

    private static DecodedTextureImage Decode()
    {
        using var stream =
            typeof(IconSheet).GetTypeInfo().Assembly.GetManifestResourceStream(ResourceName) ??
            throw new InvalidOperationException(
                $"The embedded icon sheet {ResourceName} is missing from the build.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return TextureImageDecoder.Decode(buffer.ToArray());
    }

    private unsafe uint Upload(DecodedTextureImage image)
    {
        var level = image.BaseLevel;
        var texture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, texture);
        fixed (byte* pixels = level.Pixels)
        {
            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba8,
                (uint)level.Width,
                (uint)level.Height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                pixels);
        }
        _gl.GenerateMipmap(TextureTarget.Texture2D);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMaxLevel,
            MaximumMipLevel);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.LinearMipmapLinear);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
        return texture;
    }
}
