using System.Globalization;

namespace WoWAddonLab.Emulator.Lua;

internal static class WowGraphicsCVarFloatParser
{
    private const double MinimumNormalFloat = 1.1754943508222875E-38;

    public static float ParseFiniteFloat(string value)
    {
        var text = value.AsSpan();
        var start = 0;
        while (start < text.Length &&
               WowGraphicsCVarIntegerParser.IsAsciiWhitespace(text[start]))
        {
            start++;
        }

        var numberStart = start;
        var negative = false;
        if (start < text.Length && text[start] is '+' or '-')
        {
            negative = text[start] == '-';
            start++;
        }

        if (StartsWithIgnoreCase(text[start..], "inf") ||
            StartsWithIgnoreCase(text[start..], "nan"))
        {
            return 0;
        }

        double parsed;
        if (start + 1 < text.Length &&
            text[start] == '0' &&
            text[start + 1] is 'x' or 'X')
        {
            parsed = ParseHexFloat(text, start + 2, negative);
        }
        else
        {
            var end = ScanDecimalFloat(text, start);
            if (end == start ||
                !double.TryParse(
                    text[numberStart..end],
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out parsed))
            {
                return 0;
            }
        }

        if (!double.IsFinite(parsed))
            return 0;

        var magnitude = Math.Abs(parsed);
        if (magnitude > float.MaxValue)
            return parsed < 0 ? -float.MaxValue : float.MaxValue;
        if (magnitude < MinimumNormalFloat)
            return 0;
        return (float)parsed;
    }

    public static int TruncateToNativeInt(float value)
    {
        if (value >= 2147483648.0f || value < -2147483648.0f)
            return int.MinValue;
        return (int)value;
    }

    private static int ScanDecimalFloat(ReadOnlySpan<char> text, int start)
    {
        var index = start;
        var hasDigits = false;
        while (index < text.Length && text[index] is >= '0' and <= '9')
        {
            hasDigits = true;
            index++;
        }
        if (index < text.Length && text[index] == '.')
        {
            index++;
            while (index < text.Length && text[index] is >= '0' and <= '9')
            {
                hasDigits = true;
                index++;
            }
        }
        if (!hasDigits)
            return start;

        if (index < text.Length && text[index] is 'e' or 'E')
        {
            var exponentEnd = index + 1;
            if (exponentEnd < text.Length && text[exponentEnd] is '+' or '-')
                exponentEnd++;
            var exponentStart = exponentEnd;
            while (exponentEnd < text.Length &&
                   text[exponentEnd] is >= '0' and <= '9')
            {
                exponentEnd++;
            }
            if (exponentEnd > exponentStart)
                index = exponentEnd;
        }
        return index;
    }

    private static double ParseHexFloat(
        ReadOnlySpan<char> text,
        int start,
        bool negative)
    {
        var index = start;
        var digits = 0;
        var fractionalDigits = 0;
        var afterPoint = false;
        double mantissa = 0;
        while (index < text.Length)
        {
            if (!afterPoint && text[index] == '.')
            {
                afterPoint = true;
                index++;
                continue;
            }

            var digit = HexDigit(text[index]);
            if (digit < 0)
                break;
            mantissa = mantissa * 16 + digit;
            digits++;
            if (afterPoint)
                fractionalDigits++;
            index++;
        }
        if (digits == 0)
            return 0;

        var exponent = 0;
        if (index < text.Length && text[index] is 'p' or 'P')
        {
            var exponentIndex = index + 1;
            var exponentNegative = false;
            if (exponentIndex < text.Length && text[exponentIndex] is '+' or '-')
            {
                exponentNegative = text[exponentIndex] == '-';
                exponentIndex++;
            }
            var exponentStart = exponentIndex;
            while (exponentIndex < text.Length &&
                   text[exponentIndex] is >= '0' and <= '9')
            {
                exponent = Math.Min(
                    exponent * 10 + text[exponentIndex] - '0',
                    100000);
                exponentIndex++;
            }
            if (exponentIndex > exponentStart && exponentNegative)
                exponent = -exponent;
        }

        var result = Math.ScaleB(mantissa, exponent - 4 * fractionalDigits);
        return negative ? -result : result;
    }

    private static int HexDigit(char value) => value switch
    {
        >= '0' and <= '9' => value - '0',
        >= 'a' and <= 'f' => value - 'a' + 10,
        >= 'A' and <= 'F' => value - 'A' + 10,
        _ => -1
    };

    private static bool StartsWithIgnoreCase(
        ReadOnlySpan<char> value,
        ReadOnlySpan<char> prefix) =>
        value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
}
