using System.Globalization;
using System.Text;

namespace WoWAddonLab.Emulator.UI;

internal static class EditBoxTextRules
{
    public static string ApplyReplacement(UiObject value, string text)
    {
        if (!IsInsertionValid(value, text, text))
            return string.Empty;
        return EnforceLimits(value, text);
    }

    public static string ApplyInsertion(
        UiObject value,
        string baseText,
        int cursor,
        string insertedText)
    {
        var candidate = baseText.Insert(cursor, insertedText);
        if (!IsInsertionValid(value, insertedText, candidate))
            return EnforceLimits(value, baseText);
        return EnforceLimits(value, candidate);
    }

    private static bool IsInsertionValid(
        UiObject value,
        string insertedText,
        string candidate)
    {
        if (value.EditBoxAlphabeticOnly)
            return insertedText.EnumerateRunes().All(Rune.IsLetter);

        if (!value.Attributes.TryGetValue("Numeric", out var numeric) ||
            numeric is not true)
        {
            return true;
        }

        foreach (var rune in insertedText.EnumerateRunes())
        {
            if (rune.Value is >= '0' and <= '9')
                continue;
            if (value.EditBoxNumericFullRange && rune.Value is '-' or '.')
                continue;
            return false;
        }

        if (!value.EditBoxNumericFullRange)
            return true;
        if (candidate == "-")
            return true;
        return double.TryParse(
                   candidate,
                   NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                   CultureInfo.InvariantCulture,
                   out var parsed) &&
               double.IsFinite(parsed);
    }

    public static string EnforceLimits(UiObject value, string text)
    {
        if (value.MaximumLetters > 0)
            text = TruncateRunes(text, value.MaximumLetters);
        if (value.EditBoxMaximumBytes > 0)
            text = TruncateUtf8(text, value.EditBoxMaximumBytes - 1);
        if (value.EditBoxVisibleTextByteLimit > 0)
            text = TruncateVisibleUtf8(text, value.EditBoxVisibleTextByteLimit);
        return text;
    }

    private static string TruncateRunes(string value, int maximumRunes)
    {
        if (maximumRunes <= 0)
            return string.Empty;
        var result = new StringBuilder(value.Length);
        var count = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            if (count++ >= maximumRunes)
                break;
            result.Append(rune);
        }
        return result.ToString();
    }

    private static string TruncateUtf8(string value, int maximumBytes)
    {
        if (maximumBytes <= 0)
            return string.Empty;
        if (Encoding.UTF8.GetByteCount(value) <= maximumBytes)
            return value;

        var result = new StringBuilder(value.Length);
        var usedBytes = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            if (usedBytes + rune.Utf8SequenceLength > maximumBytes)
                break;
            result.Append(rune);
            usedBytes += rune.Utf8SequenceLength;
        }
        return result.ToString();
    }

    private static string TruncateVisibleUtf8(string value, int maximumBytes)
    {
        if (maximumBytes <= 0 || value.Length == 0)
            return string.Empty;

        var result = new StringBuilder(value.Length);
        var visibleBytes = 0;
        for (var index = 0; index < value.Length;)
        {
            if (value[index] == '|' && index + 1 < value.Length)
            {
                var command = value[index + 1];
                if ((command is 'c' or 'C') &&
                    index + 10 <= value.Length &&
                    uint.TryParse(
                        value.AsSpan(index + 2, 8),
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out _))
                {
                    result.Append(value, index, 10);
                    index += 10;
                    continue;
                }

                if (command is 'r' or 'R' or 'h')
                {
                    result.Append(value, index, 2);
                    index += 2;
                    continue;
                }

                if (command == 'H')
                {
                    var labelStart = value.IndexOf("|h", index + 2, StringComparison.Ordinal);
                    if (labelStart < 0)
                        break;
                    result.Append(value, index, labelStart + 2 - index);
                    index = labelStart + 2;
                    continue;
                }

                if (command is 'T' or 'A')
                {
                    var terminator = command == 'T' ? "|t" : "|a";
                    var inlineEnd = value.IndexOf(terminator, index + 2, StringComparison.Ordinal);
                    if (inlineEnd < 0)
                        break;
                    result.Append(value, index, inlineEnd + 2 - index);
                    index = inlineEnd + 2;
                    continue;
                }

                if ((command is 'c' or 'C') &&
                    index + 3 < value.Length &&
                    value[index + 2] is 'n' or 'N')
                {
                    var nameEnd = value.IndexOf(':', index + 3);
                    if (nameEnd < 0)
                        break;
                    result.Append(value, index, nameEnd + 1 - index);
                    index = nameEnd + 1;
                    continue;
                }

                if (command == '|')
                {
                    if (visibleBytes + 1 > maximumBytes)
                        break;
                    result.Append("||");
                    visibleBytes++;
                    index += 2;
                    continue;
                }

                if (command is 'n' or 'N')
                {
                    if (visibleBytes + 1 > maximumBytes)
                        break;
                    result.Append(value, index, 2);
                    visibleBytes++;
                    index += 2;
                    continue;
                }
            }

            if (!Rune.TryGetRuneAt(value, index, out var rune) ||
                visibleBytes + rune.Utf8SequenceLength > maximumBytes)
            {
                break;
            }

            result.Append(rune);
            visibleBytes += rune.Utf8SequenceLength;
            index += rune.Utf16SequenceLength;
        }

        return result.ToString();
    }
}
