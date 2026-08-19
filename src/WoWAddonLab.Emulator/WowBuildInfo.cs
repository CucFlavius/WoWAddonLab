namespace WoWAddonLab.Emulator;

public readonly record struct WowBuildInfo(
    string Version,
    string Build,
    string Date,
    int InterfaceVersion)
{
    public static WowBuildInfo Unknown { get; } = new("0.0.0", "0", string.Empty, 0);

    public static WowBuildInfo FromVersion(string? value)
    {
        if (!System.Version.TryParse(value, out var parsed) || parsed.Build < 0)
            return Unknown;

        var interfaceVersion = checked(
            (parsed.Major * 10_000) + (parsed.Minor * 100) + parsed.Build);
        return new(
            $"{parsed.Major}.{parsed.Minor}.{parsed.Build}",
            parsed.Revision >= 0 ? parsed.Revision.ToString() : "0",
            string.Empty,
            interfaceVersion);
    }
}
