using System.Diagnostics;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowAddOnProfilerState
{
    public const int MetricCount = 12;

    private readonly Dictionary<string, double[]> _addOnMetrics =
        new(StringComparer.Ordinal);
    private readonly double[] _applicationMetrics = new double[MetricCount];
    private readonly double[] _overallMetrics = new double[MetricCount];
    private readonly Dictionary<WowAddOnPerformanceMessageType, WowAddOnPerformanceMessage>
        _shownPerformanceMessages = [];
    private readonly List<WowAddOnProfilerCallMeasurement> _activeMeasurements = [];
    private int _ticksPerSecond = Stopwatch.Frequency > int.MaxValue
        ? int.MaxValue
        : (int)Stopwatch.Frequency;

    public bool Enabled { get; set; } = true;

    public int TicksPerSecond
    {
        get => _ticksPerSecond;
        set => _ticksPerSecond = Math.Max(1, value);
    }

    public ulong AllocatedBytes { get; set; }
    public ulong DeallocatedBytes { get; set; }
    public WowAddOnPerformanceMessage? PendingPerformanceMessage { get; set; }

    public IReadOnlyDictionary<
        WowAddOnPerformanceMessageType,
        WowAddOnPerformanceMessage> ShownPerformanceMessages =>
        _shownPerformanceMessages;

    public void SetAddOnMetric(
        string addOnName,
        WowAddOnProfilerMetric metric,
        double value) =>
        GetOrCreateMetrics(addOnName)[ValidateMetric(metric)] = value;

    public void SetApplicationMetric(
        WowAddOnProfilerMetric metric,
        double value) =>
        _applicationMetrics[ValidateMetric(metric)] = value;

    public void SetOverallMetric(
        WowAddOnProfilerMetric metric,
        double value) =>
        _overallMetrics[ValidateMetric(metric)] = value;

    public double GetAddOnMetric(
        string addOnName,
        WowAddOnProfilerMetric metric) =>
        _addOnMetrics.TryGetValue(addOnName, out var metrics)
            ? metrics[ValidateMetric(metric)]
            : 0;

    public double GetApplicationMetric(WowAddOnProfilerMetric metric) =>
        _applicationMetrics[ValidateMetric(metric)];

    public double GetOverallMetric(WowAddOnProfilerMetric metric) =>
        _overallMetrics[ValidateMetric(metric)];

    public IReadOnlyList<WowAddOnProfilerResult> GetTopAddOns(
        WowAddOnProfilerMetric metric,
        int count)
    {
        var metricIndex = ValidateMetric(metric);
        var maximumCount = count < 0 ? int.MaxValue : count;
        if (maximumCount == 0)
            return [];

        return _addOnMetrics
            .Select(pair => new WowAddOnProfilerResult(
                pair.Key,
                pair.Value[metricIndex]))
            .Where(result => result.MetricValue > 0)
            .OrderByDescending(result => result.MetricValue)
            .Take(maximumCount)
            .ToArray();
    }

    internal void RecordPerformanceMessageShown(
        WowAddOnPerformanceMessage message)
    {
        _shownPerformanceMessages[message.Type] = message;
        if (PendingPerformanceMessage == message)
            PendingPerformanceMessage = null;
    }

    internal WowAddOnProfilerCallMeasurement BeginMeasurement()
    {
        var measurement = new WowAddOnProfilerCallMeasurement(
            Stopwatch.GetTimestamp(),
            AllocatedBytes,
            DeallocatedBytes);
        _activeMeasurements.Add(measurement);
        return measurement;
    }

    internal WowAddOnProfilerCallResults EndMeasurement(
        WowAddOnProfilerCallMeasurement measurement)
    {
        _activeMeasurements.Remove(measurement);
        var elapsedTicks = ToNativeTickDelta(
            Stopwatch.GetTimestamp() - measurement.StartedAtTicks);
        return new WowAddOnProfilerCallResults(
            ToMilliseconds(elapsedTicks),
            elapsedTicks,
            unchecked(AllocatedBytes - measurement.StartedAllocatedBytes),
            unchecked(DeallocatedBytes - measurement.StartedDeallocatedBytes),
            measurement.Events.ToArray());
    }

    internal void CancelMeasurement(
        WowAddOnProfilerCallMeasurement measurement) =>
        _activeMeasurements.Remove(measurement);

    internal void AddMeasuredCallEvent(string name)
    {
        if (_activeMeasurements.Count == 0)
            return;

        var timestamp = Stopwatch.GetTimestamp();
        foreach (var measurement in _activeMeasurements)
        {
            var elapsedTicks = ToNativeTickDelta(
                timestamp - measurement.StartedAtTicks);
            measurement.Events.Add(new WowAddOnProfilerCallEvent(
                name,
                unchecked(AllocatedBytes - measurement.StartedAllocatedBytes),
                unchecked(DeallocatedBytes - measurement.StartedDeallocatedBytes),
                ToMilliseconds(elapsedTicks),
                elapsedTicks));
        }
    }

    private double[] GetOrCreateMetrics(string addOnName)
    {
        ArgumentException.ThrowIfNullOrEmpty(addOnName);
        if (_addOnMetrics.TryGetValue(addOnName, out var metrics))
            return metrics;

        metrics = new double[MetricCount];
        _addOnMetrics.Add(addOnName, metrics);
        return metrics;
    }

    private static int ValidateMetric(WowAddOnProfilerMetric metric)
    {
        var value = (int)metric;
        return value is >= 0 and < MetricCount
            ? value
            : throw new ArgumentOutOfRangeException(nameof(metric));
    }

    private static int ToNativeTickDelta(long delta) =>
        unchecked((int)delta);

    private double ToMilliseconds(int elapsedTicks) =>
        1000d / TicksPerSecond * elapsedTicks;
}
