using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace WoWAddonLab.Emulator.Diagnostics;

public static class StartupTimeline
{
    private static readonly object Gate = new();
    private static readonly List<StartupSpanRecord> Records = [];
    private static readonly AsyncLocal<StartupSpan?> Current = new();
    private static readonly Dictionary<string, string> Facts = [];

    private const int MaxRecords = 20_000;

    private static readonly Dictionary<string, long> LastLogStamp = [];

    private static long _origin;
    private static int _nextId;
    private static EmulatorLog? _attachedLog;
    private static Action<EmulatorLogEntry>? _logHandler;
    private static double _readyMs = -1;

    public static bool Enabled { get; private set; }

    public static double PreInstrumentationMs { get; private set; }

    public static void Start()
    {
        lock (Gate)
        {
            Records.Clear();
            Facts.Clear();
            _nextId = 0;
            _readyMs = -1;
            _origin = Stopwatch.GetTimestamp();
            LastLogStamp.Clear();
            Enabled = true;
        }

        try
        {
            using var process = Process.GetCurrentProcess();
            PreInstrumentationMs = Math.Max(0, (DateTime.Now - process.StartTime).TotalMilliseconds);
        }
        catch (Exception)
        {
            PreInstrumentationMs = 0;
        }
    }

    public static void Fact(string name, string value)
    {
        if (!Enabled)
            return;
        lock (Gate)
            Facts[name] = value;
    }

    public static StartupSpan Begin(string name, string category = "startup", string? detail = null) =>
        BeginCore(name, category, detail, Current.Value, background: false);

    public static StartupSpan BeginBackground(string name, string category = "startup", string? detail = null) =>
        BeginCore(name, category, detail, parent: null, background: true);

    private static StartupSpan BeginCore(
        string name,
        string category,
        string? detail,
        StartupSpan? parent,
        bool background)
    {
        if (!Enabled)
            return StartupSpan.Disabled;

        int id;
        lock (Gate)
            id = ++_nextId;

        var span = new StartupSpan(
            id,
            parent,
            name,
            category,
            detail,
            background || parent?.Background == true,
            Stopwatch.GetTimestamp());

        Current.Value = span;
        return span;
    }

    internal static void Close(StartupSpan span)
    {
        if (!Enabled)
            return;

        var finished = Stopwatch.GetTimestamp();
        Add(new StartupSpanRecord(
            span.Id,
            span.ParentId,
            span.Depth,
            span.Name,
            span.Category,
            Elapsed(_origin, span.StartedAt),
            Elapsed(span.StartedAt, finished),
            span.ThreadId,
            span.Background,
            Instant: false,
            span.Detail));

        Current.Value = span.Parent;
    }

    public static void Mark(
        string name,
        string category = "startup",
        string? detail = null,
        double intervalMs = 0)
    {
        if (!Enabled)
            return;

        var parent = Current.Value;
        int id;
        lock (Gate)
            id = ++_nextId;

        Add(new StartupSpanRecord(
            id,
            parent?.Id,
            parent is null ? 0 : parent.Depth + 1,
            name,
            category,
            Elapsed(_origin, Stopwatch.GetTimestamp()),
            intervalMs,
            Environment.CurrentManagedThreadId,
            parent?.Background ?? false,
            Instant: true,
            detail));
    }

    public static void AttachLog(EmulatorLog log, params string[] categories)
    {
        if (!Enabled || ReferenceEquals(_attachedLog, log))
            return;

        DetachLog();
        var filter = categories.Length == 0
            ? null
            : categories.ToHashSet(StringComparer.OrdinalIgnoreCase);

        _logHandler = entry =>
        {
            if (filter is not null && !filter.Contains(entry.Category))
                return;
            if (entry.Category.Equals("lua", StringComparison.OrdinalIgnoreCase))
                return;

            var now = Stopwatch.GetTimestamp();
            double interval;
            lock (Gate)
            {
                interval = LastLogStamp.TryGetValue(entry.Category, out var previous)
                    ? Elapsed(previous, now)
                    : 0;
                LastLogStamp[entry.Category] = now;
            }

            Mark(entry.Message, $"log:{entry.Category}", detail: null, intervalMs: interval);
        };
        _attachedLog = log;
        log.EntryAdded += _logHandler;
    }

    public static void Freeze()
    {
        DetachLog();
        Enabled = false;
    }

    public static void DetachLog()
    {
        if (_attachedLog is not null && _logHandler is not null)
            _attachedLog.EntryAdded -= _logHandler;
        _attachedLog = null;
        _logHandler = null;
    }

    public static void Ready(string name = "first frame with addons presented")
    {
        if (!Enabled || _readyMs >= 0)
            return;
        Mark(name, "milestone");
        _readyMs = Elapsed(_origin, Stopwatch.GetTimestamp());
    }

    public static bool IsReady => _readyMs >= 0;

    public static double ReadyMs => _readyMs;

    public static double TotalMs => (_readyMs >= 0 ? _readyMs : Elapsed(_origin, Stopwatch.GetTimestamp()))
                                    + PreInstrumentationMs;

    public static IReadOnlyList<StartupSpanRecord> Snapshot()
    {
        lock (Gate)
            return Records.OrderBy(value => value.StartMs).ToArray();
    }

    private static void Add(StartupSpanRecord record)
    {
        lock (Gate)
        {
            if (Records.Count >= MaxRecords)
                return;
            Records.Add(record);
        }
    }

    private static double Elapsed(long from, long to) =>
        (to - from) * 1000d / Stopwatch.Frequency;

    private static IReadOnlyDictionary<string, string> FactSnapshot()
    {
        lock (Gate)
            return new Dictionary<string, string>(Facts);
    }

    public static string ToJson() =>
        JsonSerializer.Serialize(
            new
            {
                preInstrumentationMs = PreInstrumentationMs,
                readyMs = _readyMs,
                totalMs = TotalMs,
                facts = FactSnapshot(),
                spans = Snapshot()
            },
            new JsonSerializerOptions { WriteIndented = true });

    public static string ToMarkdown(string title = "Startup Timing")
    {
        var records = Snapshot();
        var facts = FactSnapshot();
        var builder = new StringBuilder();

        builder.Append("# ").AppendLine(title).AppendLine();
        builder.AppendLine("Every measured step from process start until the emulator first presents the");
        builder.AppendLine("loaded Blizzard UI and addons. Generated by `--startup-report`; do not hand-edit.");
        builder.AppendLine();

        builder.AppendLine("## Run").AppendLine();
        builder.AppendLine("| Property | Value |");
        builder.AppendLine("| --- | --- |");
        AppendFact(builder, "Generated", DateTimeOffset.Now.ToString("u", CultureInfo.InvariantCulture));
        foreach (var (key, value) in facts.OrderBy(value => value.Key, StringComparer.Ordinal))
            AppendFact(builder, key, value);
        AppendFact(builder, "Machine", $"{Environment.MachineName}, {Environment.ProcessorCount} logical cores");
        AppendFact(builder, "OS", Environment.OSVersion.VersionString);
        AppendFact(builder, "Runtime", Environment.Version.ToString());
        builder.AppendLine();

        builder.AppendLine("## Totals").AppendLine();
        builder.AppendLine("| Segment | ms |");
        builder.AppendLine("| --- | ---: |");
        AppendRow(builder, "Process start to first instrumented line (host, JIT, Main entry)", PreInstrumentationMs);
        AppendRow(builder, "Instrumented startup to first presented frame", Math.Max(0, _readyMs));
        AppendRow(builder, "**Total wall clock**", TotalMs);
        builder.AppendLine();

        var roots = records.Where(value => value.ParentId is null && !value.Instant).ToArray();
        if (roots.Length > 0)
        {
            builder.AppendLine("## Top-level phases").AppendLine();
            builder.AppendLine("| Phase | Start ms | Duration ms | Share | Thread |");
            builder.AppendLine("| --- | ---: | ---: | ---: | --- |");
            var total = Math.Max(1, _readyMs);
            foreach (var record in roots)
            {
                builder
                    .Append("| ").Append(Escape(record.Name))
                    .Append(" | ").Append(Format(record.StartMs))
                    .Append(" | ").Append(Format(record.DurationMs))
                    .Append(" | ").Append(Format(record.DurationMs / total * 100)).Append(" %")
                    .Append(" | ").Append(record.Background ? $"worker {record.ThreadId}" : $"main {record.ThreadId}")
                    .AppendLine(" |");
            }
            builder.AppendLine();
        }

        var childDuration = records
            .Where(value => value.ParentId is not null && !value.Instant)
            .GroupBy(value => value.ParentId!.Value)
            .ToDictionary(group => group.Key, group => group.Sum(value => value.DurationMs));

        builder.AppendLine("## Full timeline").AppendLine();
        builder.AppendLine("Indentation shows nesting. `Self` is the duration minus its direct children, so a");
        builder.AppendLine("large self value is unaccounted work inside that step. Worker rows run concurrently");
        builder.AppendLine("with the main thread, so their durations overlap rather than add.");
        builder.AppendLine();
        builder.AppendLine("| Start ms | Duration ms | Self ms | Step |");
        builder.AppendLine("| ---: | ---: | ---: | --- |");

        foreach (var record in records.Where(value => !value.Instant))
        {
            var self = record.DurationMs
                       - (childDuration.TryGetValue(record.Id, out var children) ? children : 0);
            var label = $"{new string(' ', record.Depth * 2)}**{record.Name}**";
            if (record.Detail is not null)
                label += $" ({record.Detail})";
            if (record.Background)
                label += " _[worker]_";

            builder
                .Append("| ").Append(Format(record.StartMs))
                .Append(" | ").Append(Format(record.DurationMs))
                .Append(" | ").Append(Format(Math.Max(0, self)))
                .Append(" | ").Append(Escape(label))
                .AppendLine(" |");
        }
        builder.AppendLine();

        var costly = records
            .Where(value => !value.Instant)
            .Select(value => (value.Name,
                Self: value.DurationMs - (childDuration.TryGetValue(value.Id, out var children) ? children : 0)))
            .Where(value => value.Self > 1)
            .OrderByDescending(value => value.Self)
            .Take(25)
            .ToArray();
        if (costly.Length > 0)
        {
            builder.AppendLine("## Most expensive steps by self time").AppendLine();
            builder.AppendLine("| Self ms | Step |");
            builder.AppendLine("| ---: | --- |");
            foreach (var (name, self) in costly)
                builder.Append("| ").Append(Format(self)).Append(" | ").Append(Escape(name)).AppendLine(" |");
            builder.AppendLine();
        }

        var loaderMarks = records
            .Where(value => value.Instant && value.Category.Equals("log:loader", StringComparison.Ordinal))
            .Where(value => value.DurationMs >= 1)
            .OrderByDescending(value => value.DurationMs)
            .Take(40)
            .ToArray();
        if (loaderMarks.Length > 0)
        {
            builder.AppendLine("## Slowest module and XML load steps").AppendLine();
            builder.AppendLine("Interval between consecutive loader log entries, which approximates the cost of");
            builder.AppendLine("the module or XML file named on each row.").AppendLine();
            builder.AppendLine("| Interval ms | Entry |");
            builder.AppendLine("| ---: | --- |");
            foreach (var mark in loaderMarks)
            {
                builder
                    .Append("| ").Append(Format(mark.DurationMs))
                    .Append(" | ").Append(Escape(Truncate(mark.Name, 120)))
                    .AppendLine(" |");
            }
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static void AppendFact(StringBuilder builder, string key, string value) =>
        builder.Append("| ").Append(Escape(key)).Append(" | ").Append(Escape(value)).AppendLine(" |");

    private static void AppendRow(StringBuilder builder, string label, double milliseconds) =>
        builder.Append("| ").Append(Escape(label)).Append(" | ").Append(Format(milliseconds)).AppendLine(" |");

    private static string Format(double value) =>
        value.ToString(value >= 100 ? "N0" : "N1", CultureInfo.InvariantCulture);

    private static string Truncate(string value, int maximum) =>
        value.Length <= maximum ? value : value[..maximum] + "...";

    private static string Escape(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal);
}
