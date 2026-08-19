using WoWAddonLab.Emulator.Diagnostics;

namespace WoWAddonLab.Tests;

public sealed class EmulatorLogTests
{
    [Fact]
    public void LogEntriesRetainTechnicalDetails()
    {
        var log = new EmulatorLog();

        log.Warn("assets", "Texture failed.", "Decoder stack and header data.");

        var entry = Assert.Single(log.Snapshot());
        Assert.Equal("assets", entry.Category);
        Assert.Equal("Texture failed.", entry.Message);
        Assert.Equal("Decoder stack and header data.", entry.Details);
    }

    [Fact]
    public void ClearRemovesExistingEntries()
    {
        var log = new EmulatorLog();
        log.Info("runtime", "Ready.");

        log.Clear();

        Assert.Empty(log.Snapshot());
    }
}
