using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal static class WowTextureMipSelector
{
    public static int ResolveLeadingMipLevel(
        int width,
        int height,
        int mipCount,
        WowTextureMipResidency residency)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (mipCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(mipCount));
        if (mipCount == 1 || residency.LoadPriority == 0)
            return 0;

        uint maximumWidth = uint.MaxValue;
        uint maximumHeight = uint.MaxValue;
        var currentWidth = (uint)width;
        var currentHeight = (uint)height;
        if (residency.WorldBaseMip == 0 || residency.BypassWorldBaseMip)
        {
            var cappedPriority = Math.Min(residency.LoadPriority, (byte)12);
            maximumWidth = 1u << cappedPriority;
            maximumHeight = maximumWidth;
        }
        else if (currentWidth >= 64 && currentHeight >= 64)
        {
            maximumWidth = currentWidth >> (int)residency.WorldBaseMip;
            maximumHeight = currentHeight >> (int)residency.WorldBaseMip;
        }

        var leadingMip = 0;
        while (currentWidth > maximumWidth || currentHeight > maximumHeight)
        {
            currentWidth = Math.Max(currentWidth >> 1, 1);
            currentHeight = Math.Max(currentHeight >> 1, 1);
            leadingMip++;
        }

        if (leadingMip >= mipCount)
        {
            throw new InvalidDataException(
                "Texture mip residency selects a level absent from the decoded chain.");
        }
        return leadingMip;
    }
}
