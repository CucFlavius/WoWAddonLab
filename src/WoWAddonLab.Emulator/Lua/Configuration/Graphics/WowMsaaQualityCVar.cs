namespace WoWAddonLab.Emulator.Lua;

public static class WowMsaaQualityCVar
{
    public static bool TryResolve(
        string? value,
        out byte colorSampleExponent,
        out byte extraCoverageExponent)
    {
        colorSampleExponent = 0;
        extraCoverageExponent = 0;
        if (string.IsNullOrEmpty(value))
            return false;

        var text = value.AsSpan();
        var start = SkipAsciiWhitespace(text, 0);
        if (!TryReadUnsignedDecimal(
                text[start..],
                out var first,
                out var consumed))
        {
            return false;
        }

        colorSampleExponent = unchecked((byte)first);
        var separatorIndex = start + consumed;
        if (separatorIndex < text.Length)
        {
            var secondStart = SkipAsciiWhitespace(text, separatorIndex + 1);
            if (TryReadUnsignedDecimal(
                    text[secondStart..],
                    out var second,
                    out _))
            {
                extraCoverageExponent = unchecked((byte)second);
            }
        }

        return colorSampleExponent <= 4;
    }

    private static bool TryReadUnsignedDecimal(
        ReadOnlySpan<char> value,
        out uint parsed,
        out int consumed)
    {
        parsed = 0;
        consumed = 0;
        var negative = false;
        if (!value.IsEmpty && value[0] is '+' or '-')
        {
            negative = value[0] == '-';
            consumed++;
        }

        var digitStart = consumed;
        while (consumed < value.Length &&
               value[consumed] is >= '0' and <= '9')
        {
            parsed = unchecked(parsed * 10u + (uint)(value[consumed] - '0'));
            consumed++;
        }
        if (consumed == digitStart)
            return false;
        if (negative)
            parsed = unchecked(0u - parsed);
        return true;
    }

    private static int SkipAsciiWhitespace(ReadOnlySpan<char> value, int start)
    {
        while (start < value.Length &&
               WowGraphicsCVarIntegerParser.IsAsciiWhitespace(value[start]))
        {
            start++;
        }
        return start;
    }
}
