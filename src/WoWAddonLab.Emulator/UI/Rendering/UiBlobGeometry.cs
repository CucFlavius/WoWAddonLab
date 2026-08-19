using System.Numerics;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Emulator.UI;

public static class UiBlobGeometry
{
    private const float Epsilon = 2.3841858e-7f;

    public static IReadOnlyList<UiBlobMesh> Build(
        UiSystem ui,
        UiObject owner,
        IWowMapProvider? mapProvider)
    {
        if (owner.Blob is not { } state || mapProvider is null)
            return [];

        var bounds = ui.ResolveBounds(owner.Id);
        if (bounds.Width < .001f || bounds.Height < .001f)
            return [];

        var meshes = new List<UiBlobMesh>();
        foreach (var area in state.Areas)
        {
            if (!area.IsVisible || !IsSelected(owner, state, area.BlobId))
                continue;
            if (BuildMesh(state, area, bounds, mapProvider) is { } mesh)
                meshes.Add(mesh);
        }

        if (state.MergingEnabled)
            ApplyMergeThreshold(meshes, state.MergeThreshold);
        return meshes;
    }

    public static bool TryHitTest(
        UiSystem ui,
        UiObject owner,
        IWowMapProvider? mapProvider,
        float normalizedX,
        float normalizedY,
        out int blobId,
        out IReadOnlyList<int> objectiveIndices,
        out string? tooltipText)
    {
        blobId = 0;
        objectiveIndices = [];
        tooltipText = null;
        if (normalizedX is < 0 or > 1 || normalizedY is < 0 or > 1)
            return false;

        var bounds = ui.ResolveBounds(owner.Id);
        var point = new Vector2(
            bounds.Left + bounds.Width * normalizedX,
            bounds.Bottom + bounds.Height * (1 - normalizedY));
        var hits = Build(ui, owner, mapProvider)
            .Where(mesh => mesh.IsVisible && PointInPolygon(point, mesh.Boundary))
            .ToArray();
        if (hits.Length == 0)
            return false;

        var selectedBlobId = hits[0].BlobId;
        blobId = selectedBlobId;
        objectiveIndices = hits
            .Where(mesh => mesh.BlobId == selectedBlobId)
            .Select(mesh => mesh.ObjectiveIndex)
            .Take(24)
            .ToArray();
        tooltipText = hits[0].TooltipText;
        return true;
    }

    private static UiBlobMesh? BuildMesh(
        UiBlobState state,
        UiBlobArea area,
        UiRect bounds,
        IWowMapProvider mapProvider)
    {
        var worldBoundary = state.SmoothingEnabled
            ? ResampleClosedCatmullRom(
                area.WorldBoundary,
                state.NumSplinePoints)
            : RemoveConsecutiveDuplicates(area.WorldBoundary);
        if (worldBoundary.Count < 3)
            return null;

        var boundary = new List<Vector2>(worldBoundary.Count);
        foreach (var worldPoint in worldBoundary)
        {
            if (!mapProvider.TryProjectWorldPosition(
                    state.MapId,
                    area.WorldMapId,
                    worldPoint.X,
                    worldPoint.Y,
                    out var projected))
            {
                return null;
            }
            boundary.Add(new Vector2(
                bounds.Left + bounds.Width * (float)projected.X,
                bounds.Bottom + bounds.Height * (1 - (float)projected.Y)));
        }
        boundary = RemoveConsecutiveDuplicates(boundary);
        if (boundary.Count < 3)
            return null;

        var centroid = Vector2.Zero;
        foreach (var point in boundary)
            centroid += point;
        centroid /= boundary.Count;

        var fillVertices = new List<Vector2>(boundary.Count + 1);
        fillVertices.AddRange(boundary);
        fillVertices.Add(centroid);
        var fillUvs = Enumerable
            .Repeat(Vector2.One, boundary.Count)
            .Append(Vector2.Zero)
            .ToArray();
        var fillIndices = new ushort[boundary.Count * 3];
        for (var index = 0; index < boundary.Count; index++)
        {
            fillIndices[index * 3] = (ushort)index;
            fillIndices[index * 3 + 1] =
                (ushort)((index + 1) % boundary.Count);
            fillIndices[index * 3 + 2] = (ushort)boundary.Count;
        }

        BuildBorder(
            boundary,
            centroid,
            state.BorderScalar * .01f,
            out var borderVertices,
            out var borderUvs,
            out var borderIndices);

        var minimumX = boundary.Min(point => point.X);
        var minimumY = boundary.Min(point => point.Y);
        var maximumX = boundary.Max(point => point.X);
        var maximumY = boundary.Max(point => point.Y);
        return new UiBlobMesh
        {
            BlobId = area.BlobId,
            ObjectiveIndex = area.ObjectiveIndex,
            MergeGroupId = area.MergeGroupId,
            TooltipText = area.TooltipText,
            Boundary = boundary,
            FillVertices = fillVertices,
            FillUvs = fillUvs,
            FillIndices = fillIndices,
            BorderVertices = borderVertices,
            BorderUvs = borderUvs,
            BorderIndices = borderIndices,
            Bounds = new UiRect(
                minimumX,
                minimumY,
                maximumX - minimumX,
                maximumY - minimumY)
        };
    }

    private static void BuildBorder(
        IReadOnlyList<Vector2> boundary,
        Vector2 centroid,
        float borderOffset,
        out IReadOnlyList<Vector2> vertices,
        out IReadOnlyList<Vector2> uvs,
        out IReadOnlyList<ushort> indices)
    {
        var count = boundary.Count;
        var offsetStarts = new Vector2[count];
        var offsetEnds = new Vector2[count];
        for (var index = 0; index < count; index++)
        {
            var start = boundary[index];
            var end = boundary[(index + 1) % count];
            var delta = end - start;
            var normal = new Vector2(-delta.Y, delta.X);
            if (normal.LengthSquared() > Epsilon)
                normal = Vector2.Normalize(normal);
            normal *= borderOffset;
            if (Vector2.Dot(centroid - start, normal) > 0)
                normal = -normal;
            offsetStarts[index] = start + normal;
            offsetEnds[index] = end + normal;
        }

        var resultVertices = new Vector2[count * 2];
        var resultUvs = new Vector2[count * 2];
        for (var index = 0; index < count; index++)
        {
            resultVertices[index * 2] = boundary[index];
            resultUvs[index * 2] = Vector2.Zero;
            resultVertices[index * 2 + 1] = TryIntersectInfiniteLines(
                offsetStarts[index],
                offsetEnds[index],
                offsetStarts[(index + 1) % count],
                offsetEnds[(index + 1) % count],
                out var intersection)
                ? intersection
                : offsetStarts[index];
            resultUvs[index * 2 + 1] = Vector2.One;
        }

        var resultIndices = new ushort[count * 2 + 2];
        for (var index = 0; index < count * 2; index++)
            resultIndices[index] = (ushort)index;
        resultIndices[^2] = 0;
        resultIndices[^1] = 1;
        vertices = resultVertices;
        uvs = resultUvs;
        indices = resultIndices;
    }

    private static IReadOnlyList<Vector2> ResampleClosedCatmullRom(
        IReadOnlyList<Vector2> source,
        int sampleCount)
    {
        var points = RemoveConsecutiveDuplicates(source);
        if (points.Count < 3 || sampleCount <= 0)
            return points;

        var lengths = new float[points.Count];
        var totalLength = 0f;
        for (var index = 0; index < points.Count; index++)
        {
            lengths[index] = Vector2.Distance(
                points[index],
                points[(index + 1) % points.Count]);
            totalLength += lengths[index];
        }
        if (totalLength <= Epsilon)
            return [];

        var result = new List<Vector2>(sampleCount);
        for (var sample = 0; sample < sampleCount; sample++)
        {
            var target = totalLength * sample / sampleCount;
            var accumulated = 0f;
            var segment = 0;
            while (segment + 1 < lengths.Length &&
                   target >= accumulated + lengths[segment])
            {
                accumulated += lengths[segment++];
            }
            var local = lengths[segment] > Epsilon
                ? (target - accumulated) / lengths[segment]
                : 0;
            var p0 = points[(segment - 1 + points.Count) % points.Count];
            var p1 = points[segment];
            var p2 = points[(segment + 1) % points.Count];
            var p3 = points[(segment + 2) % points.Count];
            var sampled = CatmullRom(p0, p1, p2, p3, local);
            sampled = new Vector2(
                MathF.Truncate(sampled.X),
                MathF.Truncate(sampled.Y));
            if (result.Count == 0 ||
                Vector2.DistanceSquared(result[^1], sampled) >= Epsilon)
            {
                result.Add(sampled);
            }
        }
        return RemoveCollinearPoints(result);
    }

    private static Vector2 CatmullRom(
        Vector2 p0,
        Vector2 p1,
        Vector2 p2,
        Vector2 p3,
        float t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        return
            p0 * (-.5f * t3 + t2 - .5f * t) +
            p1 * (1.5f * t3 - 2.5f * t2 + 1) +
            p2 * (-1.5f * t3 + 2 * t2 + .5f * t) +
            p3 * (.5f * t3 - .5f * t2);
    }

    private static List<Vector2> RemoveConsecutiveDuplicates(
        IReadOnlyList<Vector2> source)
    {
        var result = new List<Vector2>(source.Count);
        foreach (var point in source)
        {
            if (result.Count == 0 ||
                Vector2.DistanceSquared(result[^1], point) >= Epsilon)
            {
                result.Add(point);
            }
        }
        if (result.Count > 1 &&
            Vector2.DistanceSquared(result[0], result[^1]) < Epsilon)
        {
            result.RemoveAt(result.Count - 1);
        }
        return result;
    }

    private static IReadOnlyList<Vector2> RemoveCollinearPoints(
        IReadOnlyList<Vector2> source)
    {
        if (source.Count < 4)
            return source;
        var result = new List<Vector2>(source);
        var changed = true;
        while (changed && result.Count >= 4)
        {
            changed = false;
            for (var index = 0; index < result.Count; index++)
            {
                var previous = result[(index - 1 + result.Count) % result.Count];
                var current = result[index];
                var next = result[(index + 1) % result.Count];
                var first = current - previous;
                var second = next - current;
                var cross = first.X * second.Y - first.Y * second.X;
                if (MathF.Abs(cross) >= .001f)
                    continue;
                result.RemoveAt(index);
                changed = true;
                break;
            }
        }
        return result;
    }

    private static void ApplyMergeThreshold(
        IReadOnlyList<UiBlobMesh> meshes,
        float threshold)
    {
        for (var firstIndex = 0; firstIndex < meshes.Count; firstIndex++)
        {
            var first = meshes[firstIndex];
            if (!first.IsVisible)
                continue;
            for (var secondIndex = firstIndex + 1;
                 secondIndex < meshes.Count;
                 secondIndex++)
            {
                var second = meshes[secondIndex];
                if (!second.IsVisible ||
                    first.MergeGroupId != second.MergeGroupId ||
                    !BoundsOverlap(first.Bounds, second.Bounds))
                {
                    continue;
                }

                var firstArea = first.Bounds.Width * first.Bounds.Height;
                var secondArea = second.Bounds.Width * second.Bounds.Height;
                var smaller = secondArea > firstArea ? first : second;
                var larger = secondArea > firstArea ? second : first;
                var contained = smaller.Boundary.Count(point =>
                    PointInPolygon(point, larger.Boundary));
                if ((float)contained / smaller.Boundary.Count > threshold)
                    smaller.IsVisible = false;
            }
        }
    }

    private static bool BoundsOverlap(UiRect first, UiRect second) =>
        MathF.Max(first.Left, second.Left) <
        MathF.Min(first.Right, second.Right) &&
        MathF.Max(first.Bottom, second.Bottom) <
        MathF.Min(first.Top, second.Top);

    private static bool PointInPolygon(
        Vector2 point,
        IReadOnlyList<Vector2> polygon)
    {
        var inside = false;
        for (int current = 0, previous = polygon.Count - 1;
             current < polygon.Count;
             previous = current++)
        {
            var a = polygon[current];
            var b = polygon[previous];
            if ((a.Y > point.Y) == (b.Y > point.Y))
                continue;
            var crossingX =
                (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X;
            if (point.X < crossingX)
                inside = !inside;
        }
        return inside;
    }

    private static bool TryIntersectInfiniteLines(
        Vector2 firstStart,
        Vector2 firstEnd,
        Vector2 secondStart,
        Vector2 secondEnd,
        out Vector2 intersection)
    {
        var firstDelta = firstStart - firstEnd;
        var secondDelta = secondStart - secondEnd;
        var denominator =
            secondDelta.Y * firstDelta.X -
            firstDelta.Y * secondDelta.X;
        if (MathF.Abs(denominator) < Epsilon)
        {
            intersection = default;
            return false;
        }

        var firstCross =
            firstStart.X * firstEnd.Y -
            firstEnd.X * firstStart.Y;
        var secondCross =
            secondStart.X * secondEnd.Y -
            secondEnd.X * secondStart.Y;
        intersection = new Vector2(
            (firstCross * secondDelta.X -
             secondCross * firstDelta.X) / denominator,
            (firstCross * secondDelta.Y -
             secondCross * firstDelta.Y) / denominator);
        return true;
    }

    private static bool IsSelected(
        UiObject owner,
        UiBlobState state,
        int blobId)
    {
        if (owner.ObjectType.Equals(
                "ScenarioPOIFrame",
                StringComparison.OrdinalIgnoreCase))
        {
            return state.DrawAll;
        }
        return state.DrawnBlobIds.Contains(blobId);
    }
}
