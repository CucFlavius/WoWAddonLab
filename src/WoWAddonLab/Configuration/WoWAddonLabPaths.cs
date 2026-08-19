namespace WoWAddonLab.Configuration;

public static class WoWAddonLabPaths
{
    public static string UserDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WoWAddonLab");

    public static string CacheDirectory { get; } = Path.Combine(
        AppContext.BaseDirectory,
        "cache");

    public static string TactCacheDirectory =>
        CacheDirectory;

    public static string BlizzardUiCacheDirectory =>
        Path.Combine(CacheDirectory, "blizzard-ui");

    public static string FontCacheDirectory =>
        Path.Combine(CacheDirectory, "fonts");

    public static string ImageCacheDirectory =>
        Path.Combine(CacheDirectory, "images");

    public static string DataCacheDirectory =>
        Path.Combine(CacheDirectory, "data");

    public static string LuaCacheDirectory =>
        Path.Combine(CacheDirectory, "lua");

    public static string DefinitionsCacheDirectory =>
        Path.Combine(CacheDirectory, "definitions");
}
