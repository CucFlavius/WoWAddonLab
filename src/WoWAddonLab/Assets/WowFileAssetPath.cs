namespace WoWAddonLab.Assets;

internal static class WowFileAssetPath
{
    public static string WithDefaultBlpExtension(string path)
        => WithExtension(path, ".blp");

    public static string WithDefaultTgaExtension(string path)
        => WithExtension(path, ".tga");

    private static string WithExtension(string path, string extensionValue)
    {
        var normalized = path.Replace('\\', '/');
        var separator = normalized.LastIndexOf('/');
        var fileNameStart = separator + 1;
        var extension = normalized.LastIndexOf('.');
        if (extension <= fileNameStart)
            extension = normalized.Length;
        return $"{normalized[..extension]}{extensionValue}";
    }
}
