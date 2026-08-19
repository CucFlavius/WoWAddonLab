using System.Security.Cryptography;
using System.Text;
using WoWAddonLab.Addons;
using WoWAddonLab.Configuration;

namespace WoWAddonLab.Configuration;

public static class SavedVariablesProfiles
{
    public static IReadOnlyList<SavedVariablesProfile> Discover(WowInstallation installation)
    {
        var profiles = new List<SavedVariablesProfile>
        {
            new("clean", "Emulator-only product data", null, null, null, null, null)
        };
        var accountRoot = Path.Combine(installation.ProductPath, "WTF", "Account");
        if (!Directory.Exists(accountRoot))
            return profiles;

        foreach (var accountPath in Directory.EnumerateDirectories(accountRoot).OrderBy(Path.GetFileName))
        {
            var account = Path.GetFileName(accountPath);
            var accountSaved = Path.Combine(accountPath, "SavedVariables");
            profiles.Add(new SavedVariablesProfile(
                $"account:{account}",
                $"Account · {account}",
                account,
                null,
                null,
                Directory.Exists(accountSaved) ? accountSaved : null,
                null));

            foreach (var realmPath in Directory.EnumerateDirectories(accountPath)
                         .Where(path => !Path.GetFileName(path).Equals("SavedVariables", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(Path.GetFileName))
            {
                var realm = Path.GetFileName(realmPath);
                foreach (var characterPath in Directory.EnumerateDirectories(realmPath).OrderBy(Path.GetFileName))
                {
                    var characterSaved = Path.Combine(characterPath, "SavedVariables");
                    if (!Directory.Exists(characterSaved))
                        continue;
                    var character = Path.GetFileName(characterPath);
                    profiles.Add(new SavedVariablesProfile(
                        $"character:{account}:{realm}:{character}",
                        $"Character · {character} — {realm} ({account})",
                        account,
                        realm,
                        character,
                        Directory.Exists(accountSaved) ? accountSaved : null,
                        characterSaved));
                }
            }
        }
        return profiles;
    }

    public static string EmulatorDirectory(
        string settingsRoot,
        WowInstallation installation,
        SavedVariablesProfile profile)
    {
        var productKey = StableKey($"{installation.Product.ProductCode}|{installation.ProductPath}");
        var profileKey = StableKey(profile.Id);
        return Path.Combine(settingsRoot, "SavedVariables", productKey, profileKey);
    }

    public static int ImportCopies(
        SavedVariablesProfile profile,
        IEnumerable<InstalledAddon> addons,
        string destinationDirectory)
    {
        Directory.CreateDirectory(destinationDirectory);
        var imported = 0;
        foreach (var addon in addons.Where(value => value.Manifest?.SavedVariables.Count > 0))
        {
            imported += CopyScope(
                profile.AccountSourceDirectory,
                addon.Manifest!.Name,
                addon.Manifest.AccountSavedVariables,
                Path.Combine(destinationDirectory, "Account"));
            imported += CopyScope(
                profile.CharacterSourceDirectory,
                addon.Manifest.Name,
                addon.Manifest.CharacterSavedVariables,
                Path.Combine(destinationDirectory, "Character"));
        }
        return imported;
    }

    private static int CopyScope(
        string? sourceDirectory,
        string addonName,
        IReadOnlyList<string> declaredVariables,
        string destinationDirectory)
    {
        if (sourceDirectory is null || declaredVariables.Count == 0)
            return 0;
        var source = Path.Combine(sourceDirectory, $"{addonName}.lua");
        if (!File.Exists(source))
            return 0;

        Directory.CreateDirectory(destinationDirectory);
        var destination = Path.Combine(destinationDirectory, $"{addonName}.lua");
        var temporary = destination + ".tmp";
        var output =
            $"-- Imported read-only copy from World of Warcraft by WoW Addon Lab.{Environment.NewLine}" +
            $"-- Source: {source}{Environment.NewLine}" +
            File.ReadAllText(source);
        File.WriteAllText(temporary, output, new UTF8Encoding(false));
        File.Move(temporary, destination, true);
        return 1;
    }

    private static string StableKey(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16].ToLowerInvariant();
}
