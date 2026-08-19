using WoWAddonLab.Emulator.Diagnostics;

namespace WoWAddonLab.Diagnostics;

public static class StartupReportWriter
{
    public static void Write(CommandLineOptions options, string title)
    {
        if (!StartupTimeline.Enabled)
            return;
        StartupTimeline.DetachLog();
        TryWrite(options.StartupReportPath, () => StartupTimeline.ToMarkdown(title));
        TryWrite(options.StartupJsonPath, StartupTimeline.ToJson);
        StartupTimeline.Freeze();
    }

    private static void TryWrite(string? path, Func<string> render)
    {
        if (path is null)
            return;
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);
            File.WriteAllText(path, render());
            Console.WriteLine($"Startup report written to {path}");
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Startup report failed: {exception.Message}");
        }
    }
}
