namespace WoWAddonLab.Emulator.UI;

public static class UiTextAlphaGradient
{
    public static bool IsActive(ushort storedStart, ushort storedLength) =>
        unchecked((short)storedStart) >= 0 &&
        unchecked((short)storedLength) > 0;

    public static bool ContainsQuad(
        ushort storedStart,
        ushort storedLength,
        int drawableQuadCount)
    {
        if (!IsActive(storedStart, storedLength))
            return false;
        return unchecked((short)storedStart) < drawableQuadCount;
    }

    public static UiTextQuadAlpha ResolveQuadAlpha(
        ushort storedStart,
        ushort storedLength,
        int quadIndex,
        byte baseAlpha)
    {
        if (!IsActive(storedStart, storedLength))
            return new UiTextQuadAlpha(baseAlpha, baseAlpha);

        var start = unchecked((short)storedStart);
        var length = unchecked((short)storedLength);
        if (quadIndex < start)
            return new UiTextQuadAlpha(baseAlpha, baseAlpha);

        var decrement = baseAlpha / length;
        var completedQuads = Math.Min(quadIndex - start, length);
        var leading = Decrement(baseAlpha, decrement, completedQuads);
        var trailing = quadIndex < start + length
            ? Decrement(leading, decrement, 1)
            : leading;
        return new UiTextQuadAlpha(leading, trailing);
    }

    private static byte Decrement(byte alpha, int decrement, int count)
    {
        for (var index = 0; index < count && alpha != 0; index++)
            alpha = alpha > decrement ? (byte)(alpha - decrement) : (byte)0;
        return alpha;
    }
}
