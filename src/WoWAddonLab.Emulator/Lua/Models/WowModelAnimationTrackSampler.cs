using System.Numerics;

namespace WoWAddonLab.Emulator.Lua;

public static class WowModelAnimationTrackSampler
{
    public static bool TrySampleBlended(
        WowModelAnimationTrack<Quaternion>? track,
        int sequenceIndex,
        double sequenceTimeMilliseconds,
        int secondarySequenceIndex,
        double secondarySequenceTimeMilliseconds,
        float secondaryWeight,
        double globalSequenceTimeMilliseconds,
        IReadOnlyList<uint> globalSequenceDurationsMilliseconds,
        Quaternion defaultValue,
        out Quaternion value)
    {
        var hasPrimary = TrySample(
            track,
            sequenceIndex,
            sequenceTimeMilliseconds,
            globalSequenceTimeMilliseconds,
            globalSequenceDurationsMilliseconds,
            out var primary);
        if (!hasPrimary)
            primary = defaultValue;

        if (track is null || track.GlobalSequenceIndex >= 0 ||
            secondarySequenceIndex < 0 || secondaryWeight <= 0)
        {
            value = primary;
            return hasPrimary;
        }

        var hasSecondary = TrySample(
            track,
            secondarySequenceIndex,
            secondarySequenceTimeMilliseconds,
            globalSequenceTimeMilliseconds,
            globalSequenceDurationsMilliseconds,
            out var secondary);
        if (!hasSecondary)
            secondary = defaultValue;
        value = Quaternion.Slerp(
            primary,
            secondary,
            Math.Clamp(secondaryWeight, 0, 1));
        return hasPrimary || hasSecondary;
    }

    public static bool TrySampleBlended(
        WowModelAnimationTrack<Vector3>? track,
        int sequenceIndex,
        double sequenceTimeMilliseconds,
        int secondarySequenceIndex,
        double secondarySequenceTimeMilliseconds,
        float secondaryWeight,
        double globalSequenceTimeMilliseconds,
        IReadOnlyList<uint> globalSequenceDurationsMilliseconds,
        Vector3 defaultValue,
        out Vector3 value)
    {
        var hasPrimary = TrySample(
            track,
            sequenceIndex,
            sequenceTimeMilliseconds,
            globalSequenceTimeMilliseconds,
            globalSequenceDurationsMilliseconds,
            out var primary);
        if (!hasPrimary)
            primary = defaultValue;

        if (track is null || track.GlobalSequenceIndex >= 0 ||
            secondarySequenceIndex < 0 || secondaryWeight <= 0)
        {
            value = primary;
            return hasPrimary;
        }

        var hasSecondary = TrySample(
            track,
            secondarySequenceIndex,
            secondarySequenceTimeMilliseconds,
            globalSequenceTimeMilliseconds,
            globalSequenceDurationsMilliseconds,
            out var secondary);
        if (!hasSecondary)
            secondary = defaultValue;
        value = Vector3.Lerp(
            primary,
            secondary,
            Math.Clamp(secondaryWeight, 0, 1));
        return hasPrimary || hasSecondary;
    }

    public static bool TrySampleBlended(
        WowModelAnimationTrack<float>? track,
        int sequenceIndex,
        double sequenceTimeMilliseconds,
        int secondarySequenceIndex,
        double secondarySequenceTimeMilliseconds,
        float secondaryWeight,
        double globalSequenceTimeMilliseconds,
        IReadOnlyList<uint> globalSequenceDurationsMilliseconds,
        float defaultValue,
        out float value)
    {
        var hasPrimary = TrySample(
            track,
            sequenceIndex,
            sequenceTimeMilliseconds,
            globalSequenceTimeMilliseconds,
            globalSequenceDurationsMilliseconds,
            out var primary);
        if (!hasPrimary)
            primary = defaultValue;

        if (track is null || track.GlobalSequenceIndex >= 0 ||
            secondarySequenceIndex < 0 || secondaryWeight <= 0)
        {
            value = primary;
            return hasPrimary;
        }

        var hasSecondary = TrySample(
            track,
            secondarySequenceIndex,
            secondarySequenceTimeMilliseconds,
            globalSequenceTimeMilliseconds,
            globalSequenceDurationsMilliseconds,
            out var secondary);
        if (!hasSecondary)
            secondary = defaultValue;
        value = primary + (secondary - primary) *
            Math.Clamp(secondaryWeight, 0, 1);
        return hasPrimary || hasSecondary;
    }

    public static bool TrySampleFixed16NormalizedBlended(
        WowModelAnimationTrack<float>? track,
        int sequenceIndex,
        double sequenceTimeMilliseconds,
        int secondarySequenceIndex,
        double secondarySequenceTimeMilliseconds,
        float secondaryWeight,
        double globalSequenceTimeMilliseconds,
        IReadOnlyList<uint> globalSequenceDurationsMilliseconds,
        float defaultValue,
        out float value)
    {
        var hasPrimary = TrySampleFixed16Normalized(
            track,
            sequenceIndex,
            sequenceTimeMilliseconds,
            globalSequenceTimeMilliseconds,
            globalSequenceDurationsMilliseconds,
            out var primary);
        if (!hasPrimary)
            primary = defaultValue;

        if (track is null || track.GlobalSequenceIndex >= 0 ||
            secondarySequenceIndex < 0 || secondaryWeight <= 0)
        {
            value = primary;
            return hasPrimary;
        }

        var hasSecondary = TrySampleFixed16Normalized(
            track,
            secondarySequenceIndex,
            secondarySequenceTimeMilliseconds,
            globalSequenceTimeMilliseconds,
            globalSequenceDurationsMilliseconds,
            out var secondary);
        if (!hasSecondary)
            secondary = defaultValue;
        value = primary + (secondary - primary) *
            Math.Clamp(secondaryWeight, 0, 1);
        return hasPrimary || hasSecondary;
    }

    public static bool TrySample(
        WowModelAnimationTrack<Quaternion>? track,
        int sequenceIndex,
        double sequenceTimeMilliseconds,
        double globalSequenceTimeMilliseconds,
        IReadOnlyList<uint> globalSequenceDurationsMilliseconds,
        out Quaternion value) =>
        TrySample(
            track,
            sequenceIndex,
            sequenceTimeMilliseconds,
            globalSequenceTimeMilliseconds,
            globalSequenceDurationsMilliseconds,
            NormalizedLerp,
            static (current, next, amount) =>
                NormalizedLerp(current.Value, next.Value, amount),
            static (current, next, amount) =>
                NormalizedLerp(current.Value, next.Value, amount),
            anyNonzeroUsesLinear: true,
            out value);

    public static bool TrySample(
        WowModelAnimationTrack<Vector3>? track,
        int sequenceIndex,
        double sequenceTimeMilliseconds,
        double globalSequenceTimeMilliseconds,
        IReadOnlyList<uint> globalSequenceDurationsMilliseconds,
        out Vector3 value) =>
        TrySample(
            track,
            sequenceIndex,
            sequenceTimeMilliseconds,
            globalSequenceTimeMilliseconds,
            globalSequenceDurationsMilliseconds,
            Vector3.Lerp,
            EvaluateBezier,
            EvaluateHermite,
            anyNonzeroUsesLinear: false,
            out value);

    public static bool TrySample(
        WowModelAnimationTrack<float>? track,
        int sequenceIndex,
        double sequenceTimeMilliseconds,
        double globalSequenceTimeMilliseconds,
        IReadOnlyList<uint> globalSequenceDurationsMilliseconds,
        out float value) =>
        TrySample(
            track,
            sequenceIndex,
            sequenceTimeMilliseconds,
            globalSequenceTimeMilliseconds,
            globalSequenceDurationsMilliseconds,
            static (first, second, amount) =>
                first + (second - first) * amount,
            EvaluateBezier,
            EvaluateHermite,
            anyNonzeroUsesLinear: false,
            out value);

    public static bool TrySampleFixed16Normalized(
        WowModelAnimationTrack<float>? track,
        int sequenceIndex,
        double sequenceTimeMilliseconds,
        double globalSequenceTimeMilliseconds,
        IReadOnlyList<uint> globalSequenceDurationsMilliseconds,
        out float value) =>
        TrySample(
            track,
            sequenceIndex,
            sequenceTimeMilliseconds,
            globalSequenceTimeMilliseconds,
            globalSequenceDurationsMilliseconds,
            static (first, second, amount) =>
                first + (second - first) * amount,
            EvaluateBezier,
            EvaluateHermite,
            anyNonzeroUsesLinear: true,
            out value);

    private static bool TrySample<T>(
        WowModelAnimationTrack<T>? track,
        int sequenceIndex,
        double sequenceTimeMilliseconds,
        double globalSequenceTimeMilliseconds,
        IReadOnlyList<uint> globalSequenceDurationsMilliseconds,
        Func<T, T, float, T> linear,
        Func<WowModelAnimationTrackKey<T>, WowModelAnimationTrackKey<T>, float, T>
            bezier,
        Func<WowModelAnimationTrackKey<T>, WowModelAnimationTrackKey<T>, float, T>
            hermite,
        bool anyNonzeroUsesLinear,
        out T value)
    {
        value = default!;
        if (track is null || track.Sequences.Count == 0)
            return false;

        var time = sequenceTimeMilliseconds;
        var trackSequenceIndex = sequenceIndex;
        if (track.GlobalSequenceIndex >= 0)
        {
            trackSequenceIndex = 0;
            time = globalSequenceTimeMilliseconds;
            if ((uint)track.GlobalSequenceIndex >=
                (uint)globalSequenceDurationsMilliseconds.Count)
            {
                return false;
            }

            var duration = globalSequenceDurationsMilliseconds[
                track.GlobalSequenceIndex];
            if (duration > 0)
                time %= duration;
        }

        if ((uint)trackSequenceIndex >= (uint)track.Sequences.Count)
            trackSequenceIndex = 0;
        var sequence = track.Sequences[trackSequenceIndex];
        var keyCount = Math.Min(
            sequence.TimestampsMilliseconds.Count,
            sequence.Keys.Count);
        if (keyCount == 0)
            return false;

        time = Math.Max(time, 0);
        var keyIndex = FindKeyIndex(sequence.TimestampsMilliseconds, keyCount, time);
        var current = sequence.Keys[keyIndex];
        value = current.Value;
        if (track.InterpolationType == 0 || keyIndex + 1 >= keyCount)
            return true;

        var firstTimestamp = sequence.TimestampsMilliseconds[keyIndex];
        var secondTimestamp = sequence.TimestampsMilliseconds[keyIndex + 1];
        if (secondTimestamp <= firstTimestamp)
            return true;

        var amount = (float)Math.Clamp(
            (time - firstTimestamp) / (secondTimestamp - firstTimestamp),
            0,
            1);
        var next = sequence.Keys[keyIndex + 1];
        value = anyNonzeroUsesLinear
            ? linear(current.Value, next.Value, amount)
            : track.InterpolationType switch
            {
                1 => linear(current.Value, next.Value, amount),
                2 => bezier(current, next, amount),
                3 => hermite(current, next, amount),
                _ => current.Value
            };
        return true;
    }

    private static int FindKeyIndex(
        IReadOnlyList<uint> timestamps,
        int keyCount,
        double time)
    {
        var low = 0;
        var high = keyCount - 1;
        while (low < high)
        {
            var middle = low + (high - low + 1) / 2;
            if (timestamps[middle] <= time)
                low = middle;
            else
                high = middle - 1;
        }
        return low;
    }

    private static float EvaluateBezier(
        WowModelAnimationTrackKey<float> current,
        WowModelAnimationTrackKey<float> next,
        float amount)
    {
        var inverse = 1 - amount;
        return inverse * inverse * inverse * current.Value +
               3 * inverse * inverse * amount * current.OutTangent +
               3 * inverse * amount * amount * next.InTangent +
               amount * amount * amount * next.Value;
    }

    private static Vector3 EvaluateBezier(
        WowModelAnimationTrackKey<Vector3> current,
        WowModelAnimationTrackKey<Vector3> next,
        float amount)
    {
        var inverse = 1 - amount;
        return inverse * inverse * inverse * current.Value +
               3 * inverse * inverse * amount * current.OutTangent +
               3 * inverse * amount * amount * next.InTangent +
               amount * amount * amount * next.Value;
    }

    private static float EvaluateHermite(
        WowModelAnimationTrackKey<float> current,
        WowModelAnimationTrackKey<float> next,
        float amount)
    {
        var squared = amount * amount;
        var cubed = squared * amount;
        return (2 * cubed - 3 * squared + 1) * current.Value +
               (cubed - 2 * squared + amount) * current.OutTangent +
               (-2 * cubed + 3 * squared) * next.Value +
               (cubed - squared) * next.InTangent;
    }

    private static Vector3 EvaluateHermite(
        WowModelAnimationTrackKey<Vector3> current,
        WowModelAnimationTrackKey<Vector3> next,
        float amount)
    {
        var squared = amount * amount;
        var cubed = squared * amount;
        return (2 * cubed - 3 * squared + 1) * current.Value +
               (cubed - 2 * squared + amount) * current.OutTangent +
               (-2 * cubed + 3 * squared) * next.Value +
               (cubed - squared) * next.InTangent;
    }

    private static Quaternion NormalizedLerp(
        Quaternion first,
        Quaternion second,
        float amount)
    {
        var value = new Quaternion(
            first.X + (second.X - first.X) * amount,
            first.Y + (second.Y - first.Y) * amount,
            first.Z + (second.Z - first.Z) * amount,
            first.W + (second.W - first.W) * amount);
        var lengthSquared =
            (value.Y * value.Y + value.X * value.X) +
            (value.Z * value.Z + value.W * value.W);
        var inverseLength =
            1.021435f - (lengthSquared - 0.95906597f) * 0.532516f;
        if (lengthSquared <= 0.91521198f)
        {
            inverseLength *= 1.021435f -
                (inverseLength * inverseLength * lengthSquared - 0.95906597f) *
                0.532516f;
            if (lengthSquared <= 0.6521197f)
            {
                inverseLength *= 1.021435f -
                    (inverseLength * inverseLength * lengthSquared - 0.95906597f) *
                    0.532516f;
            }
        }

        return value * inverseLength;
    }
}
