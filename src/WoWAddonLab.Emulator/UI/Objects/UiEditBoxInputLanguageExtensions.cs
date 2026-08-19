using System.Numerics;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Emulator.UI;

public static class UiEditBoxInputLanguageExtensions
{
    public static string ToWowName(this UiEditBoxInputLanguage value) =>
        value switch
        {
            UiEditBoxInputLanguage.Roman => "ROMAN",
            UiEditBoxInputLanguage.Korean => "KOREAN",
            UiEditBoxInputLanguage.Chinese => "CHINESE",
            UiEditBoxInputLanguage.Japanese => "JAPANESE",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
}
