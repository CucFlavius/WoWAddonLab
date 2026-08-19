using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WoWAddonLab.Emulator.UI;

public static class WowTextMarkup
{
    private static readonly Regex PrecedingNumber = new(
        @"[-+]?\d[\d,]*(?:\.\d+)?",
        RegexOptions.CultureInvariant);

    public static IReadOnlyList<WowTextSpan> ParseColorSpans(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return [];

        var spans = new List<WowTextSpan>();
        var text = new StringBuilder();
        var visibleText = new StringBuilder();
        uint? color = null;

        void Flush()
        {
            if (text.Length == 0)
                return;
            spans.Add(new WowTextSpan(text.ToString(), color));
            text.Clear();
        }

        for (var index = 0; index < value.Length;)
        {
            if (value[index] != '|' || index + 1 >= value.Length)
            {
                var character = value[index++];
                text.Append(character);
                visibleText.Append(character);
                continue;
            }

            var command = value[index + 1];
            if (command == '|')
            {
                text.Append('|');
                visibleText.Append('|');
                index += 2;
                continue;
            }

            if ((command == 'c' || command == 'C') &&
                index + 10 <= value.Length &&
                uint.TryParse(
                    value.AsSpan(index + 2, 8),
                    NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture,
                    out var argb))
            {
                Flush();
                color = argb;
                index += 10;
                continue;
            }

            if (command == 'r' || command == 'R')
            {
                Flush();
                color = null;
                index += 2;
                continue;
            }

            if (command == 'n' || command == 'N')
            {
                text.Append('\n');
                visibleText.Append('\n');
                index += 2;
                continue;
            }

            if ((command == '4' || command == '7') &&
                TryResolvePlural(value, index, visibleText, out var plural, out var pluralEnd))
            {
                text.Append(plural);
                visibleText.Append(plural);
                index = pluralEnd;
                continue;
            }

            if (command == 'h')
            {
                index += 2;
                continue;
            }

            if (command == 'H')
            {
                var labelStart = value.IndexOf("|h", index + 2, StringComparison.Ordinal);
                index = labelStart >= 0 ? labelStart + 2 : value.Length;
                continue;
            }

            if ((command == 'T' || command == 'A') &&
                value.IndexOf(command == 'T' ? "|t" : "|a", index + 2, StringComparison.Ordinal)
                    is var inlineEnd && inlineEnd >= 0)
            {
                index = inlineEnd + 2;
                continue;
            }

            if ((command == 'c' || command == 'C') &&
                index + 3 < value.Length &&
                (value[index + 2] == 'n' || value[index + 2] == 'N'))
            {
                var nameEnd = value.IndexOf(':', index + 3);
                index = nameEnd >= 0 ? nameEnd + 1 : value.Length;
                continue;
            }

            text.Append('|');
            visibleText.Append('|');
            index++;
        }

        Flush();
        return spans;
    }

    public static string PlainText(string? value) =>
        string.Concat(ParseColorSpans(value).Select(span => span.Text));

    public static string ProcessStoredGrammar(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value ?? string.Empty;

        var output = new StringBuilder(value.Length);
        for (var index = 0; index < value.Length;)
        {
            if (value[index] == '|' &&
                index + 1 < value.Length &&
                value[index + 1] is '4' or '7')
            {
                var visible = new StringBuilder(PlainText(output.ToString()));
                if (TryResolvePlural(
                        value,
                        index,
                        visible,
                        out var selected,
                        out var nextIndex))
                {
                    output.Append(selected);
                    index = nextIndex;
                    continue;
                }
            }

            output.Append(value[index++]);
        }
        return output.ToString();
    }

    private static bool TryResolvePlural(
        string value,
        int markerStart,
        StringBuilder visibleText,
        out string selected,
        out int nextIndex)
    {
        selected = string.Empty;
        nextIndex = markerStart;
        var end = value.IndexOf(';', markerStart + 2);
        if (end < 0)
            return false;

        var choices = value[(markerStart + 2)..end].Split(':');
        if (choices.Length is < 2 or > 3)
            return false;

        var matches = PrecedingNumber.Matches(visibleText.ToString());
        if (matches.Count == 0)
            return false;

        var numericText = matches[^1].Value.Replace(",", string.Empty, StringComparison.Ordinal);
        if (!double.TryParse(
                numericText,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var number))
        {
            return false;
        }

        var absolute = Math.Abs(number);
        if (choices.Length == 2)
        {
            selected = Math.Abs(absolute - 1) < double.Epsilon ? choices[0] : choices[1];
        }
        else
        {
            var integer = (long)Math.Truncate(absolute);
            var modulo10 = integer % 10;
            var modulo100 = integer % 100;
            selected = modulo10 == 1 && modulo100 != 11
                ? choices[0]
                : modulo10 is >= 2 and <= 4 && modulo100 is < 12 or > 14
                    ? choices[1]
                    : choices[2];
        }

        nextIndex = end + 1;
        return true;
    }
}
