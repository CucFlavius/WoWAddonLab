using WoWAddonLab.Emulator.Addons;

namespace WoWAddonLab.Configuration;

public static class AddonManifestContextFactory
{
    public static AddonManifestContext For(
        WowInstallation installation,
        string textLocale = "enUS")
    {
        var code = installation.Product.ProductCode;
        if (!code.Contains("classic", StringComparison.OrdinalIgnoreCase) &&
            !code.Contains("anniversary", StringComparison.OrdinalIgnoreCase))
        {
            return new(
                "mainline",
                "standard",
                "Mainline",
                "Standard",
                textLocale,
                IsPublicTestProduct(code));
        }

        var major = int.TryParse(installation.Version?.Split('.')[0], out var parsed) ? parsed : 0;
        var game = code.Contains("era", StringComparison.OrdinalIgnoreCase) ||
                   code.Contains("anniversary", StringComparison.OrdinalIgnoreCase)
            ? "vanilla"
            : major switch
            {
                <= 1 => "vanilla",
                2 => "tbc",
                3 => "wrath",
                4 => "cata",
                5 => "mists",
                _ => "classic"
            };
        return new(
            "classic",
            game,
            "Classic",
            char.ToUpperInvariant(game[0]) + game[1..],
            textLocale,
            IsPublicTestProduct(code));
    }

    public static bool IsPublicTestProduct(string productCode) =>
        productCode.Equals("wowt", StringComparison.OrdinalIgnoreCase) ||
        productCode.Equals("wowxptr", StringComparison.OrdinalIgnoreCase) ||
        productCode.Equals("wow_beta", StringComparison.OrdinalIgnoreCase) ||
        productCode.Equals("wow_classic_ptr", StringComparison.OrdinalIgnoreCase) ||
        productCode.Equals("wow_classic_era_ptr", StringComparison.OrdinalIgnoreCase) ||
        productCode.Equals("wow_classic_beta", StringComparison.OrdinalIgnoreCase);
}
