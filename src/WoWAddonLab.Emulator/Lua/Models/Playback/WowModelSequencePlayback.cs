using WoWAddonLab.Emulator.UI;

namespace WoWAddonLab.Emulator.Lua;

public static class WowModelSequencePlayback
{
    public static double ResolveSampleTimeMilliseconds(UiObject model)
    {
        return ResolveSampleTimeMilliseconds(
            model.ModelResolvedSequenceIndex,
            model.ModelResolvedSequenceDurationMilliseconds,
            model.ModelSequenceElapsedMilliseconds,
            model.ModelSequencePlaying,
            model.ModelSequenceLoops,
            model.ModelSequenceRepeatCount);
    }

    public static double ResolveSampleTimeMilliseconds(
        WowModelSequenceBlendState state) =>
        ResolveSampleTimeMilliseconds(
            state.SequenceIndex,
            state.SequenceDurationMilliseconds,
            state.SequenceElapsedMilliseconds,
            state.SequencePlaying,
            state.SequenceLoops,
            state.SequenceRepeatCount);

    public static float ResolveSecondaryPoseWeight(
        WowModelSequenceBlendState? state)
    {
        if (state is null || state.TransitionDurationMilliseconds == 0)
            return 0;

        var transitionEnd = state.TransitionEndOffsetMilliseconds != 0
            ? state.TransitionEndOffsetMilliseconds
            : state.TransitionDurationMilliseconds;
        var remainingTicks =
            transitionEnd -
            Math.Floor(state.TransitionElapsedMilliseconds);
        if (remainingTicks <= 0)
            return 0;

        var normalized = (float)Math.Clamp(
            remainingTicks / state.TransitionDurationMilliseconds,
            0,
            1);
        return normalized * normalized * (3 - 2 * normalized);
    }

    private static double ResolveSampleTimeMilliseconds(
        int sequenceIndex,
        uint duration,
        double elapsed,
        bool playing,
        bool loops,
        uint repeatCount)
    {
        if (sequenceIndex < 0 || duration == 0)
            return 0;

        if (loops || playing && repeatCount > 1)
        {
            elapsed %= duration;
            if (elapsed < 0)
                elapsed += duration;
            return elapsed;
        }

        return Math.Clamp(elapsed, 0, duration);
    }
}
