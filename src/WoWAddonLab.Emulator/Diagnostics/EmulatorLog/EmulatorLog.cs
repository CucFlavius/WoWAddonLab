using System.Collections.Concurrent;

namespace WoWAddonLab.Emulator.Diagnostics;

public sealed class EmulatorLog
{
    private readonly ConcurrentQueue<EmulatorLogEntry> _entries = new();
    private long _sequence;

    public event Action<EmulatorLogEntry>? EntryAdded;

    public IReadOnlyList<EmulatorLogEntry> Snapshot(int maximum = 500)
    {
        var entries = _entries.ToArray();
        return entries.Length <= maximum ? entries : entries[^maximum..];
    }

    public void Write(
        EmulatorLogLevel level,
        string category,
        string message,
        string? details = null)
    {
        var entry = new EmulatorLogEntry(
            Interlocked.Increment(ref _sequence),
            DateTimeOffset.Now,
            level,
            category,
            message,
            details);

        _entries.Enqueue(entry);
        while (_entries.Count > 5_000)
            _entries.TryDequeue(out _);

        EntryAdded?.Invoke(entry);
    }

    public void Info(string category, string message) =>
        Write(EmulatorLogLevel.Information, category, message);

    public void Warn(string category, string message, string? details = null) =>
        Write(EmulatorLogLevel.Warning, category, message, details);

    public void Error(string category, string message, string? details = null) =>
        Write(EmulatorLogLevel.Error, category, message, details);

    public void Clear()
    {
        while (_entries.TryDequeue(out _))
        {
        }
    }
}
