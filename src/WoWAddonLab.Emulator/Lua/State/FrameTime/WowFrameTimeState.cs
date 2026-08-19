namespace WoWAddonLab.Emulator.Lua;

public sealed class WowFrameTimeState
{
    private const int FrameRateHistoryLength = 60;
    private readonly float[] _frameDurationMilliseconds = new float[FrameRateHistoryLength];
    private double _tickMillisecondsAccumulator;
    private int _frameRateHistoryIndex;

    public uint TickMilliseconds { get; private set; }
    public uint SessionStartMilliseconds { get; set; }
    public uint DebugProfileStartMilliseconds { get; set; }
    public float TickTimeSeconds { get; private set; }
    public float FixedTimeStepSeconds { get; set; }

    public double TimeSeconds => TickMilliseconds * 0.001;

    public double SessionTimeSeconds =>
        unchecked((int)(TickMilliseconds - SessionStartMilliseconds)) * 0.001;

    public float FrameRate
    {
        get
        {
            var totalMilliseconds = 0f;
            foreach (var sample in _frameDurationMilliseconds)
                totalMilliseconds += sample;

            var averageMilliseconds = totalMilliseconds * (1f / FrameRateHistoryLength);
            return averageMilliseconds >= 0.001f
                ? 1000f / averageMilliseconds
                : 1000f;
        }
    }

    internal void Advance(double deltaSeconds)
    {
        TickTimeSeconds = (float)deltaSeconds;
        _tickMillisecondsAccumulator += deltaSeconds * 1000.0;
        TickMilliseconds = unchecked((uint)(ulong)_tickMillisecondsAccumulator);

        _frameDurationMilliseconds[_frameRateHistoryIndex] =
            (int)(TickTimeSeconds * 1000f);
        _frameRateHistoryIndex++;
        if (_frameRateHistoryIndex == FrameRateHistoryLength)
            _frameRateHistoryIndex = 0;
    }
}
