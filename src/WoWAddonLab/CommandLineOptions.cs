namespace WoWAddonLab;

public sealed record CommandLineOptions(
    string? AddonPath,
    string? Product,
    IReadOnlyList<string> EnabledAddons,
    string? Profile,
    bool ImportSavedVariables,
    bool? LoadBlizzardUi,
    int? BlizzardUiModuleLimit,
    bool Headless,
    int Port,
    int? Ticks,
    float LogicalWidth,
    float LogicalHeight,
    bool AutoConnectTact,
    string? StartupReportPath = null,
    string? StartupJsonPath = null,
    bool ExitWhenReady = false)
{
    public static bool WantsStartupReport(string[] arguments) =>
        arguments.Any(value => value is "--startup-report" or "--startup-json");

    public static CommandLineOptions Parse(string[] arguments)
    {
        string? addonPath = null;
        string? product = null;
        var enabledAddons = new List<string>();
        string? profile = null;
        var importSavedVariables = false;
        bool? loadBlizzardUi = null;
        int? blizzardUiModuleLimit = null;
        var headless = false;
        var port = 43117;
        int? ticks = null;
        var logicalWidth = 1600f;
        var logicalHeight = 900f;
        var autoConnectTact = true;
        string? startupReportPath = null;
        string? startupJsonPath = null;
        var exitWhenReady = false;

        for (var index = 0; index < arguments.Length; index++)
        {
            switch (arguments[index])
            {
                case "--addon" when index + 1 < arguments.Length:
                    addonPath = arguments[++index];
                    break;
                case "--product" when index + 1 < arguments.Length:
                    product = arguments[++index];
                    break;
                case "--enable" when index + 1 < arguments.Length:
                    enabledAddons.AddRange(arguments[++index].Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                    break;
                case "--profile" when index + 1 < arguments.Length:
                    profile = arguments[++index];
                    break;
                case "--import-saved":
                    importSavedVariables = true;
                    break;
                case "--blizzard-ui":
                    loadBlizzardUi = true;
                    break;
                case "--no-blizzard-ui":
                    loadBlizzardUi = false;
                    break;
                case "--blizzard-ui-limit" when index + 1 < arguments.Length:
                    blizzardUiModuleLimit = Math.Max(0, int.Parse(arguments[++index]));
                    break;
                case "--headless":
                    headless = true;
                    break;
                case "--port" when index + 1 < arguments.Length:
                    port = int.Parse(arguments[++index]);
                    break;
                case "--ticks" when index + 1 < arguments.Length:
                    ticks = int.Parse(arguments[++index]);
                    break;
                case "--width" when index + 1 < arguments.Length:
                    logicalWidth = float.Parse(arguments[++index]);
                    break;
                case "--height" when index + 1 < arguments.Length:
                    logicalHeight = float.Parse(arguments[++index]);
                    break;
                case "--no-tact":
                    autoConnectTact = false;
                    break;
                case "--startup-report" when index + 1 < arguments.Length:
                    startupReportPath = Path.GetFullPath(arguments[++index]);
                    break;
                case "--startup-json" when index + 1 < arguments.Length:
                    startupJsonPath = Path.GetFullPath(arguments[++index]);
                    break;
                case "--exit-when-ready":
                    exitWhenReady = true;
                    break;
            }
        }

        return new CommandLineOptions(
            addonPath is null ? null : Path.GetFullPath(addonPath),
            product,
            enabledAddons.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            profile,
            importSavedVariables,
            loadBlizzardUi,
            blizzardUiModuleLimit,
            headless,
            port,
            ticks,
            logicalWidth,
            logicalHeight,
            autoConnectTact,
            startupReportPath,
            startupJsonPath,
            exitWhenReady);
    }
}
