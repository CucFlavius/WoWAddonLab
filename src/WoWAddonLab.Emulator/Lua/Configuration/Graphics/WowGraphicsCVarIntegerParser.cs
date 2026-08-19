namespace WoWAddonLab.Emulator.Lua;

internal static class WowGraphicsCVarIntegerParser
{
    public static int ParseStrtol(string value)
    {
        var text = value.AsSpan();
        var radix = text.Length >= 2 &&
                    text[0] == '0' &&
                    text[1] is 'x' or 'X'
            ? 16
            : 10;
        var index = 0;
        while (index < text.Length && IsAsciiWhitespace(text[index]))
            index++;

        var negative = false;
        if (index < text.Length && text[index] is '+' or '-')
        {
            negative = text[index] == '-';
            index++;
        }
        if (radix == 16 &&
            index + 1 < text.Length &&
            text[index] == '0' &&
            text[index + 1] is 'x' or 'X')
        {
            index += 2;
        }

        ulong magnitude = 0;
        var hasDigits = false;
        var overflowed = false;
        while (index < text.Length)
        {
            var digit = text[index] switch
            {
                >= '0' and <= '9' => text[index] - '0',
                >= 'a' and <= 'f' => text[index] - 'a' + 10,
                >= 'A' and <= 'F' => text[index] - 'A' + 10,
                _ => -1
            };
            if (digit < 0 || digit >= radix)
                break;

            hasDigits = true;
            if (magnitude > (ulong.MaxValue - (uint)digit) / (uint)radix)
                overflowed = true;
            else if (!overflowed)
                magnitude = magnitude * (uint)radix + (uint)digit;
            index++;
        }
        if (!hasDigits)
            return 0;
        if (overflowed)
            return negative ? int.MinValue : int.MaxValue;
        if (negative)
            return magnitude >= 0x80000000UL
                ? int.MinValue
                : -(int)magnitude;
        return magnitude > int.MaxValue
            ? int.MaxValue
            : (int)magnitude;
    }

    public static bool IsAsciiWhitespace(char value) =>
        value is ' ' or '\t' or '\n' or '\v' or '\f' or '\r';
}
