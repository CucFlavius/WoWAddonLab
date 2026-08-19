using System.Text;

namespace WoWAddonLab.Emulator.UI;

public static class UiMeasuredTextWrapping
{
    public static IReadOnlyList<string> Wrap(
        string text,
        float firstMaximumWidth,
        float continuationMaximumWidth,
        bool nonSpaceWrap,
        Func<string, float> measure)
    {
        var result = new List<string>();
        var normalized = text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        foreach (var logicalLine in normalized.Split('\n'))
        {
            WrapLogicalLine(
                logicalLine,
                result.Count == 0 ? firstMaximumWidth : continuationMaximumWidth,
                continuationMaximumWidth,
                nonSpaceWrap,
                measure,
                result);
        }

        if (result.Count == 0)
            result.Add(string.Empty);
        return result;
    }

    private static void WrapLogicalLine(
        string line,
        float firstMaximumWidth,
        float continuationMaximumWidth,
        bool nonSpaceWrap,
        Func<string, float> measure,
        List<string> result)
    {
        if (line.Length == 0)
        {
            result.Add(string.Empty);
            return;
        }

        var runes = line.EnumerateRunes().ToArray();
        var boundaries = new int[runes.Length + 1];
        for (var index = 0; index < runes.Length; index++)
            boundaries[index + 1] = boundaries[index] + runes[index].Utf16SequenceLength;

        var start = 0;
        var maximumWidth = firstMaximumWidth;
        while (start < runes.Length)
        {
            var remaining = line[boundaries[start]..];
            if (!float.IsFinite(maximumWidth) ||
                measure(remaining) <= maximumWidth + 0.001f)
            {
                result.Add(remaining);
                return;
            }

            var fittingEnd = start;
            for (var end = start + 1; end <= runes.Length; end++)
            {
                var candidate = line[boundaries[start]..boundaries[end]];
                if (measure(candidate) > maximumWidth + 0.001f)
                    break;
                fittingEnd = end;
            }

            if (fittingEnd == start)
                fittingEnd = start + 1;

            var whitespaceBreak = -1;
            for (var index = fittingEnd - 1; index >= start; index--)
            {
                if (!Rune.IsWhiteSpace(runes[index]))
                    continue;
                whitespaceBreak = index;
                break;
            }

            if (whitespaceBreak < start)
            {
                if (!nonSpaceWrap)
                {
                    result.Add(remaining);
                    return;
                }

                result.Add(line[boundaries[start]..boundaries[fittingEnd]]);
                start = fittingEnd;
            }
            else
            {
                result.Add(line[boundaries[start]..boundaries[whitespaceBreak]]);
                start = whitespaceBreak + 1;
                while (start < runes.Length && Rune.IsWhiteSpace(runes[start]))
                    start++;
            }

            maximumWidth = continuationMaximumWidth;
        }
    }
}
