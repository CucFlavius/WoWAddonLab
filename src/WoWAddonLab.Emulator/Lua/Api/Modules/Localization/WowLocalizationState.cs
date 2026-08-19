using System.Globalization;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowLocalizationState
{
    private static readonly string?[] LocaleNames =
    [
        "enUS", "koKR", "frFR", "deDE", "zhCN", "zhTW",
        "esES", "esMX", "ruRU", null, "ptBR", "itIT"
    ];

    public bool EuropeanNumbers { get; set; }
    public int CurrentRegion { get; set; } = 3;
    public WowClientLocale CurrentLocale { get; set; } = WowClientLocale.EnUS;
    public WowClientLocale OsLocale { get; set; } = ResolveOsLocale(
        CultureInfo.CurrentUICulture.Name);
    public List<string> AvailableLocales { get; } = ["enUS"];
    public Dictionary<string, int> LocaleIds { get; } =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["enUS"] = 0,
            ["koKR"] = 1,
            ["frFR"] = 2,
            ["deDE"] = 3,
            ["zhCN"] = 4,
            ["zhTW"] = 5,
            ["esES"] = 6,
            ["esMX"] = 7,
            ["ruRU"] = 8,
            ["ptBR"] = 10,
            ["itIT"] = 11
        };

    internal static string? LocaleName(WowClientLocale locale)
    {
        var index = (int)locale;
        return index >= 0 && index < LocaleNames.Length
            ? LocaleNames[index]
            : null;
    }

    public static WowClientLocale ResolveOsLocale(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
            return WowClientLocale.EnUS;

        var parts = cultureName.Split(['-', '_'], StringSplitOptions.RemoveEmptyEntries);
        var language = parts[0].ToLowerInvariant();
        var region = parts.Length > 1 ? parts[^1].ToUpperInvariant() : string.Empty;
        var exactName = language + region;
        for (var index = 0; index < LocaleNames.Length; index++)
        {
            if (string.Equals(
                    LocaleNames[index],
                    exactName,
                    StringComparison.OrdinalIgnoreCase))
            {
                return (WowClientLocale)index;
            }
        }

        var languageFallback = language switch
        {
            "de" => WowClientLocale.DeDE,
            "en" => WowClientLocale.EnUS,
            "es" => WowClientLocale.EsMX,
            "fr" => WowClientLocale.FrFR,
            "it" => WowClientLocale.ItIT,
            "ko" => WowClientLocale.KoKR,
            "pt" => WowClientLocale.PtBR,
            "ru" => WowClientLocale.RuRU,
            "zh" => WowClientLocale.ZhCN,
            _ => (WowClientLocale?)null
        };
        if (languageFallback is { } byLanguage)
            return byLanguage;

        return region switch
        {
            "BR" or "PT" => WowClientLocale.PtBR,
            "CN" => WowClientLocale.ZhCN,
            "DE" => WowClientLocale.DeDE,
            "ES" => WowClientLocale.EsES,
            "FR" => WowClientLocale.FrFR,
            "IT" => WowClientLocale.ItIT,
            "KR" => WowClientLocale.KoKR,
            "MX" => WowClientLocale.EsMX,
            "RU" => WowClientLocale.RuRU,
            "TW" => WowClientLocale.ZhTW,
            "AU" or "CA" or "GB" or "NZ" or "US" => WowClientLocale.EnUS,
            _ => WowClientLocale.EnUS
        };
    }
}
