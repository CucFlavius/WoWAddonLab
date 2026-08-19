using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowModelSequenceBlendState
{
    public int SequenceIndex { get; init; }
    public uint SequenceDurationMilliseconds { get; init; }
    public double SequenceInitialElapsedMilliseconds { get; init; }
    public double SequenceElapsedMilliseconds { get; set; }
    public double SequencePlaybackClockMilliseconds { get; set; }
    public float SequencePlaybackSpeed { get; init; } = 1;
    public uint SequenceRepeatCount { get; init; } = 1;
    public bool SequencePlaying { get; set; }
    public bool SequenceLoops { get; init; }
    public uint TransitionDurationMilliseconds { get; init; }
    public uint TransitionEndOffsetMilliseconds { get; init; }
    public double TransitionElapsedMilliseconds { get; set; }
}
