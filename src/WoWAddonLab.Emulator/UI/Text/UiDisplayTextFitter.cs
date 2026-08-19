using System.Text;

namespace WoWAddonLab.Emulator.UI;

public static class UiDisplayTextFitter
{
    public const int NativeBufferCapacity = 0x1FFF;
    public const int NativeTextByteCapacity = NativeBufferCapacity - 3;

    public static UiDisplayTextResult Resolve(
        string processedText,
        Func<string, bool> fits)
    {
        ArgumentNullException.ThrowIfNull(processedText);
        ArgumentNullException.ThrowIfNull(fits);

        if (processedText.Length == 0 || fits(processedText))
            return new UiDisplayTextResult(processedText, false);

        var boundary = Math.Min(
            Encoding.UTF8.GetByteCount(processedText),
            NativeTextByteCapacity);
        while (boundary > 0)
        {
            var prefix = Utf8Prefix(processedText, boundary);
            prefix = BacktrackIncompleteInlineMarkup(prefix);
            prefix = prefix.TrimEnd(' ');
            var candidate = string.Concat(prefix, "...");
            if (fits(candidate))
                return new UiDisplayTextResult(candidate, true);

            var prefixBytes = Encoding.UTF8.GetByteCount(prefix);
            boundary = prefixBytes > 0
                ? PreviousUtf8Boundary(processedText, prefixBytes)
                : PreviousUtf8Boundary(processedText, boundary);
        }

        return new UiDisplayTextResult("...", true);
    }

    private static string Utf8Prefix(string value, int maximumBytes)
    {
        if (maximumBytes <= 0 || value.Length == 0)
            return string.Empty;
        if (Encoding.UTF8.GetByteCount(value) <= maximumBytes)
            return value;

        var consumedBytes = 0;
        var consumedChars = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            var runeBytes = rune.Utf8SequenceLength;
            if (consumedBytes + runeBytes > maximumBytes)
                break;
            consumedBytes += runeBytes;
            consumedChars += rune.Utf16SequenceLength;
        }
        return value[..consumedChars];
    }

    private static int PreviousUtf8Boundary(string value, int byteBoundary)
    {
        if (byteBoundary <= 0)
            return 0;

        var previous = 0;
        var consumed = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            var next = consumed + rune.Utf8SequenceLength;
            if (next >= byteBoundary)
                return previous;
            previous = next;
            consumed = next;
        }
        return previous;
    }

    private static string BacktrackIncompleteInlineMarkup(string value)
    {
        var textureStart = value.LastIndexOf("|T", StringComparison.Ordinal);
        var textureEnd = value.LastIndexOf("|t", StringComparison.Ordinal);
        if (textureStart > textureEnd)
            value = value[..textureStart];

        var atlasStart = value.LastIndexOf("|A", StringComparison.Ordinal);
        var atlasEnd = value.LastIndexOf("|a", StringComparison.Ordinal);
        if (atlasStart > atlasEnd)
            value = value[..atlasStart];

        return value;
    }
}
