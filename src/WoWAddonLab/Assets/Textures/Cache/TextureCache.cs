using System.Numerics;
using WoWAddonLab.Emulator.Diagnostics;
using WoWAddonLab.Emulator.UI;
using Silk.NET.OpenGL;

namespace WoWAddonLab.Assets;

public sealed class TextureCache : IDisposable
{
    private readonly GL _gl;
    private readonly LocalAddonAssetSource _localAssets;
    private readonly EmulatorLog _log;
    private readonly DecodedImageCache? _decodedImages;
    private readonly Dictionary<string, uint> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Vector2> _textureSizes = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _missing = new(StringComparer.OrdinalIgnoreCase);
    private readonly float _maximumSupportedAnisotropy;
    private uint _whiteTexture;
    private uint _transparentTexture;

    public TextureCache(
        GL gl,
        string addonRoot,
        EmulatorLog log,
        string? imageCacheDirectory = null)
        : this(gl, [addonRoot], log, imageCacheDirectory)
    {
    }

    public TextureCache(
        GL gl,
        IEnumerable<string> addonRoots,
        EmulatorLog log,
        string? imageCacheDirectory = null)
    {
        _gl = gl;
        _localAssets = new LocalAddonAssetSource(addonRoots);
        _log = log;
        _decodedImages = string.IsNullOrWhiteSpace(imageCacheDirectory)
            ? null
            : new DecodedImageCache(imageCacheDirectory);
        var maximumSupportedAnisotropy = 1f;
        if (_gl.IsExtensionPresent("GL_EXT_texture_filter_anisotropic"))
        {
            _gl.GetFloat((GLEnum)0x84FF, out maximumSupportedAnisotropy);
            maximumSupportedAnisotropy = Math.Max(1, maximumSupportedAnisotropy);
        }
        _maximumSupportedAnisotropy = maximumSupportedAnisotropy;
        _whiteTexture = Upload(
            [255, 255, 255, 255],
            1,
            1,
            UiTextureSamplerState.Resolve(
                UiTextureFilterMode.Nearest,
                UiTextureWrapMode.Clamp,
                UiTextureWrapMode.Clamp));
        _transparentTexture = Upload(
            [0, 0, 0, 0],
            1,
            1,
            UiTextureSamplerState.Resolve(
                UiTextureFilterMode.Nearest,
                UiTextureWrapMode.Clamp,
                UiTextureWrapMode.Clamp));
    }

    public TactAssetSource? Tact { get; set; }
    public uint WhiteTexture => _whiteTexture;
    public int LoadedCount => _textures.Count;
    public int MissingCount => _missing.Count;

    public uint Resolve(UiTextureState texture) =>
        Resolve(
            texture,
            UiTextureSamplerState.Resolve(
                texture.FilterMode,
                texture.WrapHorizontal,
                texture.WrapVertical));

    public uint Resolve(
        UiTextureState texture,
        UiTextureSamplerState sampler) =>
        Resolve(texture, sampler, WowTextureMipResidency.FullResolution);

    internal uint ResolveSolidWhite(UiTextureSamplerState sampler)
    {
        var key = TextureKey(
            "solid:ffffffff",
            sampler,
            WowTextureMipResidency.FullResolution);
        if (_textures.TryGetValue(key, out var handle))
            return handle;

        handle = Upload([255, 255, 255, 255], 1, 1, sampler);
        _textures[key] = handle;
        _textureSizes[key] = Vector2.One;
        return handle;
    }

    internal uint Resolve(
        UiTextureState texture,
        UiTextureSamplerState sampler,
        WowTextureMipResidency residency)
    {
        if (texture.FileDataId == 0)
            return _transparentTexture;

        var assetKey = texture.FileDataId is { } id
            ? $"fdid:{id}"
            : texture.Asset ?? string.Empty;
        if (texture.IsColor)
            return _whiteTexture;
        if (assetKey.Length == 0)
            return _transparentTexture;
        var key = TextureKey(assetKey, sampler, residency);
        if (_textures.TryGetValue(key, out var handle))
        {
            ApplyIntrinsicSize(texture, key);
            return handle;
        }
        if (_missing.Contains(assetKey))
            return MissingFallback(texture.BlendMode);

        try
        {
            var bytes = _localAssets.Read(texture.Asset, defaultToBlp: true) ??
                        Tact?.Read(texture.Asset, texture.FileDataId);
            if (bytes is null)
            {
                _missing.Add(assetKey);
                _log.Warn("assets", $"Texture was not found: {assetKey}");
                return MissingFallback(texture.BlendMode);
            }

            DecodedTextureImage decoded;
            if (_decodedImages is null ||
                !_decodedImages.TryRead(bytes, out decoded))
            {
                decoded = TextureImageDecoder.Decode(bytes);
                _decodedImages?.Write(bytes, decoded);
            }
            var leadingMip = WowTextureMipSelector.ResolveLeadingMipLevel(
                decoded.BaseLevel.Width,
                decoded.BaseLevel.Height,
                decoded.MipLevels.Count,
                residency);
            var residentImage = leadingMip == 0
                ? decoded
                : new DecodedTextureImage(decoded.MipLevels.Skip(leadingMip).ToArray());
            handle = Upload(
                residentImage,
                sampler);
            _textures[key] = handle;
            _textureSizes[key] = new Vector2(
                residentImage.BaseLevel.Width,
                residentImage.BaseLevel.Height);
            ApplyIntrinsicSize(texture, key);
            return handle;
        }
        catch (Exception exception)
        {
            _missing.Add(assetKey);
            _log.Warn(
                "assets",
                $"Failed to load {assetKey}: {exception.Message}",
                exception.ToString());
            return MissingFallback(texture.BlendMode);
        }
    }

    public void RetryMissing() => _missing.Clear();

    public byte[]? ReadAsset(string asset) =>
        _localAssets.Read(asset, defaultToBlp: true) ?? Tact?.Read(asset, null);

    public bool TryGetSourcePixelSize(
        UiTextureState texture,
        out Vector2 size)
    {
        var assetKey = texture.FileDataId is { } id
            ? $"fdid:{id}"
            : texture.Asset ?? string.Empty;
        if (assetKey.Length == 0 ||
            !_textureSizes.TryGetValue(
                TextureKey(
                    assetKey,
                    UiTextureSamplerState.Resolve(
                        texture.FilterMode,
                        texture.WrapHorizontal,
                        texture.WrapVertical),
                    WowTextureMipResidency.FullResolution),
                out var fullSize))
        {
            size = default;
            return false;
        }

        var upperLeft = texture.Uv[0] * fullSize;
        var lowerLeft = texture.Uv[1] * fullSize;
        var upperRight = texture.Uv[2] * fullSize;
        var lowerRight = texture.Uv[3] * fullSize;
        size = new Vector2(
            Math.Max(
                Vector2.Distance(upperLeft, upperRight),
                Vector2.Distance(lowerLeft, lowerRight)),
            Math.Max(
                Vector2.Distance(upperLeft, lowerLeft),
                Vector2.Distance(upperRight, lowerRight)));
        return size.X > 0 && size.Y > 0;
    }

    public void Dispose()
    {
        foreach (var texture in _textures.Values)
            _gl.DeleteTexture(texture);
        _textures.Clear();
        _textureSizes.Clear();
        if (_whiteTexture != 0)
            _gl.DeleteTexture(_whiteTexture);
        _whiteTexture = 0;
        if (_transparentTexture != 0)
            _gl.DeleteTexture(_transparentTexture);
        _transparentTexture = 0;
    }

    private void ApplyIntrinsicSize(UiTextureState texture, string key)
    {
        if (texture.AtlasName is not null ||
            !_textureSizes.TryGetValue(key, out var size))
        {
            return;
        }

        texture.IntrinsicWidth ??= size.X;
        texture.IntrinsicHeight ??= size.Y;
    }

    private uint MissingFallback(string blendMode) => _transparentTexture;

    private static string TextureKey(
        string assetKey,
        UiTextureSamplerState sampler,
        WowTextureMipResidency residency) =>
        string.Join(
            '|',
            assetKey,
            (byte)sampler.Filter,
            sampler.MaxAnisotropy,
            sampler.AddressU,
            sampler.AddressV,
            sampler.BorderColor,
            sampler.MinLod,
            sampler.MaxLod,
            sampler.ComparisonEnabled,
            residency.LoadPriority,
            residency.WorldBaseMip,
            residency.BypassWorldBaseMip);

    private unsafe uint Upload(
        byte[] pixels,
        int width,
        int height,
        UiTextureSamplerState sampler) =>
        Upload(
            new DecodedTextureImage(
                [new DecodedTextureMipLevel(pixels, width, height)]),
            sampler);

    private unsafe uint Upload(
        DecodedTextureImage image,
        UiTextureSamplerState sampler)
    {
        var effectiveSampler = sampler.ResolveForAvailableMipLevels(image.MipLevels.Count);
        var texture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, texture);
        for (var levelIndex = 0; levelIndex < image.MipLevels.Count; levelIndex++)
        {
            var level = image.MipLevels[levelIndex];
            fixed (byte* pointer = level.Pixels)
            {
                _gl.TexImage2D(
                    TextureTarget.Texture2D,
                    levelIndex,
                    InternalFormat.Rgba8,
                    (uint)level.Width,
                    (uint)level.Height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    pointer);
            }
        }
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMaxLevel,
            image.MipLevels.Count - 1);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)(effectiveSampler.Filter switch
            {
                UiTextureFilterMode.Trilinear or
                UiTextureFilterMode.Anisotropic => TextureMinFilter.LinearMipmapLinear,
                UiTextureFilterMode.Linear or
                UiTextureFilterMode.Bilinear => TextureMinFilter.Linear,
                _ => TextureMinFilter.Nearest
            }));
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)(effectiveSampler.MagLinear
                ? TextureMagFilter.Linear
                : TextureMagFilter.Nearest));
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS,
            (int)ToOpenGlWrapMode(effectiveSampler.AddressU));
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT,
            (int)ToOpenGlWrapMode(effectiveSampler.AddressV));
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinLod,
            effectiveSampler.MinLod);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMaxLod,
            effectiveSampler.MaxLod);
        if (_maximumSupportedAnisotropy > 1)
        {
            _gl.TexParameter(
                TextureTarget.Texture2D,
                (TextureParameterName)0x84FE,
                Math.Min(effectiveSampler.MaxAnisotropy, _maximumSupportedAnisotropy));
        }
        var border = effectiveSampler.BorderColor switch
        {
            UiTextureBorderColor.TransparentBlack => new[] { 0f, 0f, 0f, 0f },
            UiTextureBorderColor.OpaqueBlack => new[] { 0f, 0f, 0f, 1f },
            _ => new[] { 1f, 1f, 1f, 1f }
        };
        fixed (float* borderPointer = border)
        {
            _gl.TexParameter(
                TextureTarget.Texture2D,
                TextureParameterName.TextureBorderColor,
                borderPointer);
        }
        _gl.BindTexture(TextureTarget.Texture2D, 0);
        return texture;
    }

    private static TextureWrapMode ToOpenGlWrapMode(UiTextureAddressMode mode) =>
        mode switch
        {
            UiTextureAddressMode.Repeat => TextureWrapMode.Repeat,
            UiTextureAddressMode.Mirror => TextureWrapMode.MirroredRepeat,
            UiTextureAddressMode.Border => TextureWrapMode.ClampToBorder,
            _ => TextureWrapMode.ClampToEdge
        };
}
