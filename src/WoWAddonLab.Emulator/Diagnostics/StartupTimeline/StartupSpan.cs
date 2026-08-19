using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace WoWAddonLab.Emulator.Diagnostics;

public sealed class StartupSpan : IDisposable
{
    internal static readonly StartupSpan Disabled = new();

    private readonly bool _enabled;
    private bool _closed;

    private StartupSpan()
    {
        Name = string.Empty;
        Category = string.Empty;
        _enabled = false;
    }

    internal StartupSpan(
        int id,
        StartupSpan? parent,
        string name,
        string category,
        string? detail,
        bool background,
        long startedAt)
    {
        Id = id;
        Parent = parent;
        ParentId = parent?.Id;
        Depth = parent is null ? 0 : parent.Depth + 1;
        Name = name;
        Category = category;
        Detail = detail;
        Background = background;
        StartedAt = startedAt;
        ThreadId = Environment.CurrentManagedThreadId;
        _enabled = true;
    }

    internal int Id { get; }
    internal int? ParentId { get; }
    internal int Depth { get; }
    internal string Name { get; }
    internal string Category { get; }
    internal string? Detail { get; private set; }
    internal bool Background { get; }
    internal long StartedAt { get; }
    internal int ThreadId { get; }
    internal StartupSpan? Parent { get; }

    public StartupSpan Annotate(string detail)
    {
        if (_enabled)
            Detail = Detail is null ? detail : $"{Detail}, {detail}";
        return this;
    }

    public void Dispose()
    {
        if (!_enabled || _closed)
            return;
        _closed = true;
        StartupTimeline.Close(this);
    }
}
