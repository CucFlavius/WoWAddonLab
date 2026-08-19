using Microsoft.Win32;

namespace WoWAddonLab.Configuration;

public static class WowInstallationDetector
{
    private static readonly WowProduct[] Products =
    [
        new("World of Warcraft", "_retail_", "Wow.exe", "wow"),
        new("World of Warcraft Event", "_event1_", "WoWB.exe", "wowe1"),
        new("World of Warcraft Public Test", "_ptr_", "WoWT.exe", "wowt"),
        new("World of Warcraft Public Test 3", "_xptr_", "WoWT.exe", "wowxptr"),
        new("World of Warcraft Classic", "_classic_", "WowClassic.exe", "wow_classic"),
        new("World of Warcraft Classic PTR", "_classic_ptr_", "WowClassicT.exe", "wow_classic_ptr"),
        new("World of Warcraft Classic Era", "_classic_era_", "WowClassic.exe", "wow_classic_era"),
        new("World of Warcraft Classic Era PTR", "_classic_era_ptr_", "WowClassicT.exe", "wow_classic_era_ptr"),
        new("World of Warcraft Classic Anniversary", "_anniversary_", "WowClassic.exe", "wow_anniversary"),
        new("World of Warcraft Classic Titan", "_classic_titan_", "WowClassic.exe", "wow_classic_titan"),
        new("World of Warcraft Beta", "_beta_", "WoWB.exe", "wow_beta"),
        new("World of Warcraft Classic Beta", "_classic_beta_", "WoWClassicB.exe", "wow_classic_beta"),
        new("World of Warcraft Alpha", "_alpha_", "WoWB.exe", "wowdev"),
        new("World of Warcraft Classic Alpha", "_classic_alpha_", "WowClassicT.exe", "wow_classic_alpha"),
        new("World of Warcraft Submission", "_submission_", "WoWT.exe", "wowz")
    ];

    public static IReadOnlyList<WowInstallation> Detect()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (OperatingSystem.IsWindows())
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                using var localMachine = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, view);
                using var uninstall = localMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
                if (uninstall is null)
                    continue;
                foreach (var product in Products.Select(value => value.UninstallName).Distinct())
                {
                    using var key = uninstall.OpenSubKey(product);
                    if (key?.GetValue("InstallLocation") is string location && Directory.Exists(location))
                        roots.Add(Path.GetFullPath(location.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
                }
            }
        }

        foreach (var common in CommonInstallRoots())
        {
            if (Directory.Exists(common))
                roots.Add(Path.GetFullPath(common));
        }

        var result = new List<WowInstallation>();
        foreach (var root in roots)
        {
            foreach (var product in Products)
            {
                var productPath = Path.Combine(root, product.FolderName);
                if (!File.Exists(Path.Combine(productPath, product.ExecutableName)))
                    continue;
                var resolved = ReadFlavor(productPath) is { } flavor
                    ? product with { ProductCode = flavor }
                    : product;
                result.Add(new WowInstallation(root, productPath, resolved, ReadVersion(root, resolved.ProductCode)));
            }

            var knownFolders = Products
                .Select(value => value.FolderName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var directory in Directory.EnumerateDirectories(root, "_*_"))
            {
                var folder = Path.GetFileName(directory);
                if (knownFolders.Contains(folder))
                    continue;
                var executable = Directory.EnumerateFiles(directory, "Wow*.exe", SearchOption.TopDirectoryOnly)
                    .Select(Path.GetFileName)
                    .FirstOrDefault(value =>
                        !value!.Equals("BlizzardError.exe", StringComparison.OrdinalIgnoreCase));
                if (executable is null)
                    continue;

                var label = folder.Trim('_').Replace('_', ' ');
                var code = ReadFlavor(directory) ?? "wow_" + folder.Trim('_');
                var discovered = new WowProduct(
                    $"World of Warcraft ({label})",
                    folder,
                    executable,
                    code);
                result.Add(new WowInstallation(root, directory, discovered, ReadVersion(root, code)));
            }
        }

        return result
            .DistinctBy(value => (value.ProductPath.ToUpperInvariant(), value.Product.ProductCode))
            .OrderBy(value => value.Product.UninstallName)
            .ThenBy(value => value.ProductPath)
            .ToArray();
    }

    private static IEnumerable<string> CommonInstallRoots()
    {
        foreach (var variable in new[] { "ProgramFiles", "ProgramFiles(x86)", "PUBLIC" })
        {
            var path = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(path))
                yield return Path.Combine(path, "World of Warcraft");
        }
        yield return @"C:\Program Files (x86)\World of Warcraft";
        yield return @"C:\Games\World of Warcraft";
        yield return @"D:\Games\World of Warcraft";
        yield return @"E:\Games\World of Warcraft";
    }

    private static string? ReadFlavor(string productPath)
    {
        var path = Path.Combine(productPath, ".flavor.info");
        if (!File.Exists(path))
            return null;
        var lines = File.ReadAllLines(path);
        return lines.Length >= 2 && !string.IsNullOrWhiteSpace(lines[1]) ? lines[1].Trim() : null;
    }

    private static string? ReadVersion(string root, string productCode)
    {
        var buildInfo = Path.Combine(root, ".build.info");
        if (!File.Exists(buildInfo))
            return null;
        var lines = File.ReadAllLines(buildInfo);
        if (lines.Length < 2)
            return null;
        var headers = lines[0].Split('|').Select(value => value.Split('!')[0]).ToArray();
        var productIndex = Array.FindIndex(headers, value => value.Equals("Product", StringComparison.OrdinalIgnoreCase));
        var versionIndex = Array.FindIndex(headers, value => value.Equals("Version", StringComparison.OrdinalIgnoreCase));
        if (productIndex < 0 || versionIndex < 0)
            return null;
        foreach (var line in lines.Skip(1))
        {
            var values = line.Split('|');
            if (values.Length > Math.Max(productIndex, versionIndex) &&
                values[productIndex].Equals(productCode, StringComparison.OrdinalIgnoreCase))
                return values[versionIndex];
        }
        return null;
    }
}
