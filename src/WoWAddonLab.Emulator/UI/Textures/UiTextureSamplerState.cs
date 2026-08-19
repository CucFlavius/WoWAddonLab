using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiTextureSamplerState(
    UiTextureFilterMode Filter,
    UiTextureAddressMode AddressU,
    UiTextureAddressMode AddressV,
    UiTextureBorderColor BorderColor,
    int MaxAnisotropy = 1,
    float MinLod = 0,
    float MaxLod = 16,
    bool ComparisonEnabled = false)
{
    public bool MinLinear => Filter is
        UiTextureFilterMode.Linear or
        UiTextureFilterMode.Bilinear or
        UiTextureFilterMode.Trilinear or
        UiTextureFilterMode.Anisotropic;
    public bool MagLinear => MinLinear;
    public bool MipLinear => Filter is
        UiTextureFilterMode.Trilinear or
        UiTextureFilterMode.Anisotropic;
    public bool UsesMipmaps => MipLinear;

    public UiTextureSamplerState ResolveForAvailableMipLevels(int mipLevelCount)
    {
        if (mipLevelCount == 1 && Filter is
            UiTextureFilterMode.Bilinear or
            UiTextureFilterMode.Trilinear or
            UiTextureFilterMode.Anisotropic)
        {
            return this with
            {
                Filter = UiTextureFilterMode.Linear,
                MaxAnisotropy = 1
            };
        }

        return this;
    }

    public Vector4 BorderRgba =>
        BorderColor switch
        {
            UiTextureBorderColor.TransparentBlack => Vector4.Zero,
            UiTextureBorderColor.OpaqueBlack => new Vector4(0, 0, 0, 1),
            _ => Vector4.One
        };

    public static bool TryAddressTexel(
        int coordinate,
        int extent,
        UiTextureAddressMode mode,
        out int addressed)
    {
        if (extent <= 0)
        {
            addressed = 0;
            return false;
        }

        switch (mode)
        {
            case UiTextureAddressMode.Repeat:
                addressed = PositiveModulo(coordinate, extent);
                return true;
            case UiTextureAddressMode.Mirror:
            {
                var period = checked(extent * 2);
                var mirrored = PositiveModulo(coordinate, period);
                addressed = mirrored < extent
                    ? mirrored
                    : period - 1 - mirrored;
                return true;
            }
            case UiTextureAddressMode.Border when
                coordinate < 0 || coordinate >= extent:
                addressed = 0;
                return false;
            default:
                addressed = Math.Clamp(coordinate, 0, extent - 1);
                return true;
        }
    }

    public static UiTextureSamplerState Resolve(
        UiTextureFilterMode filter,
        UiTextureWrapMode wrapHorizontal,
        UiTextureWrapMode wrapVertical)
    {
        var borderColor = UiTextureBorderColor.White;
        if (TryGetBorderColor(wrapVertical, out var verticalBorder))
            borderColor = verticalBorder;
        if (TryGetBorderColor(wrapHorizontal, out var horizontalBorder))
            borderColor = horizontalBorder;

        return new UiTextureSamplerState(
            filter,
            ResolveAddress(wrapHorizontal),
            ResolveAddress(wrapVertical),
            borderColor);
    }

    public static UiTextureSamplerState Resolve(
        string? filter,
        string? wrapHorizontal,
        string? wrapVertical) =>
        Resolve(
            ParseFilter(filter),
            ParseWrap(wrapHorizontal),
            ParseWrap(wrapVertical));

    public static UiTextureFilterMode ParseFilter(string? value) =>
        value?.ToUpperInvariant() switch
        {
            "NEAREST" => UiTextureFilterMode.Nearest,
            "TRILINEAR" => UiTextureFilterMode.Trilinear,
            _ => UiTextureFilterMode.Linear
        };

    public static UiTextureWrapMode ParseWrap(string? value) =>
        value?.ToUpperInvariant() switch
        {
            "REPEAT" => UiTextureWrapMode.Repeat,
            "CLAMPTOBLACK" => UiTextureWrapMode.ClampToBlack,
            "CLAMPTOBLACKADDITIVE" => UiTextureWrapMode.ClampToBlackAdditive,
            "CLAMPTOWHITE" => UiTextureWrapMode.ClampToWhite,
            "MIRROR" => UiTextureWrapMode.Mirror,
            _ => UiTextureWrapMode.Clamp
        };

    private static UiTextureAddressMode ResolveAddress(UiTextureWrapMode mode) =>
        mode switch
        {
            UiTextureWrapMode.Repeat => UiTextureAddressMode.Repeat,
            UiTextureWrapMode.Mirror => UiTextureAddressMode.Mirror,
            UiTextureWrapMode.ClampToBlack or
            UiTextureWrapMode.ClampToBlackAdditive or
            UiTextureWrapMode.ClampToWhite => UiTextureAddressMode.Border,
            _ => UiTextureAddressMode.Clamp
        };

    private static bool TryGetBorderColor(
        UiTextureWrapMode mode,
        out UiTextureBorderColor color)
    {
        switch (mode)
        {
            case UiTextureWrapMode.ClampToBlack:
                color = UiTextureBorderColor.OpaqueBlack;
                return true;
            case UiTextureWrapMode.ClampToBlackAdditive:
                color = UiTextureBorderColor.TransparentBlack;
                return true;
            case UiTextureWrapMode.ClampToWhite:
                color = UiTextureBorderColor.White;
                return true;
            default:
                color = UiTextureBorderColor.White;
                return false;
        }
    }

    private static int PositiveModulo(int value, int divisor)
    {
        var remainder = value % divisor;
        return remainder < 0 ? remainder + divisor : remainder;
    }
}
