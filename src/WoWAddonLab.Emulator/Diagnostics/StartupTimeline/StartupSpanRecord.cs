using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace WoWAddonLab.Emulator.Diagnostics;

public sealed record StartupSpanRecord(
    int Id,
    int? ParentId,
    int Depth,
    string Name,
    string Category,
    double StartMs,
    double DurationMs,
    int ThreadId,
    bool Background,
    bool Instant,
    string? Detail);
