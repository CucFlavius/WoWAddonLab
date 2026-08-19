using System.Collections.Concurrent;

namespace WoWAddonLab.Emulator.Diagnostics;

public sealed record EmulatorLogEntry(
    long Sequence,
    DateTimeOffset Timestamp,
    EmulatorLogLevel Level,
    string Category,
    string Message,
    string? Details = null);
