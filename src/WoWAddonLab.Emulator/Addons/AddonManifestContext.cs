namespace WoWAddonLab.Emulator.Addons;

public sealed record AddonManifestContext(
    string Family,
    string GameType,
    string FamilyDirectory,
    string GameDirectory,
    string TextLocale,
    bool IsPublicTestClient)
{
    public static AddonManifestContext Mainline { get; } = new(
        "mainline",
        "standard",
        "Mainline",
        "Standard",
        "enUS",
        false);

    public bool MatchesGameType(string value) =>
        value.Equals(Family, StringComparison.OrdinalIgnoreCase) ||
        value.Equals(GameType, StringComparison.OrdinalIgnoreCase);

    public bool MatchesTextLocale(string value) =>
        value.Equals(TextLocale, StringComparison.OrdinalIgnoreCase);
}
