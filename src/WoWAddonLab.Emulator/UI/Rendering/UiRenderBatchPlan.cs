namespace WoWAddonLab.Emulator.UI;

public static class UiRenderBatchPlan
{
    public static IReadOnlyList<UiRenderBatchEntry> Build(
        UiSystem ui,
        IReadOnlyList<UiObject> renderOrder)
    {
        if (renderOrder.Count == 0)
            return [];

        var orderIndices = new Dictionary<int, int>(renderOrder.Count);
        for (var index = 0; index < renderOrder.Count; index++)
            orderIndices.TryAdd(renderOrder[index].Id, index);

        var builders = renderOrder
            .Where(value => value.IsFrameBuffer && !value.IsRegion)
            .DistinctBy(value => value.Id)
            .ToDictionary(
                value => value.Id,
                value => new BatchBuilder(value, orderIndices[value.Id]));
        if (builders.Count == 0)
        {
            return renderOrder
                .Select<UiObject, UiRenderBatchEntry>(
                    value => new UiRenderObjectEntry(value))
                .ToArray();
        }

        foreach (var builder in builders.Values)
        {
            builder.Parent = FindNearestFrameBuffer(
                ui,
                builder.Frame.ParentId,
                builders);
        }

        var rootObjects = new List<PositionedObject>();
        for (var index = 0; index < renderOrder.Count; index++)
        {
            var value = renderOrder[index];
            if (builders.ContainsKey(value.Id))
                continue;

            var owner = FindNearestFrameBuffer(
                ui,
                value.ParentId,
                builders);
            if (owner is null)
                rootObjects.Add(new PositionedObject(index, value));
            else
                owner.AddObject(index, value);
        }

        var rootBatches = builders.Values
            .Where(value => value.Parent is null)
            .ToArray();
        foreach (var root in rootBatches)
            root.ResolvePosition();

        return Merge(rootObjects, rootBatches);
    }

    private static BatchBuilder? FindNearestFrameBuffer(
        UiSystem ui,
        int? parentId,
        IReadOnlyDictionary<int, BatchBuilder> builders)
    {
        while (parentId is { } id && ui.Find(id) is { } parent)
        {
            if (builders.TryGetValue(id, out var owner))
                return owner;
            parentId = parent.ParentId;
        }

        return null;
    }

    private static IReadOnlyList<UiRenderBatchEntry> Merge(
        IReadOnlyList<PositionedObject> objects,
        IReadOnlyList<BatchBuilder> batches)
    {
        return objects
            .Select(value => new PositionedEntry(
                value.Position,
                value.Position,
                new UiRenderObjectEntry(value.Value)))
            .Concat(batches.Select(value => new PositionedEntry(
                value.Position,
                value.Frame.Id,
                value.Build())))
            .OrderBy(value => value.Position)
            .ThenBy(value => value.TieBreaker)
            .Select(value => value.Entry)
            .ToArray();
    }

    private sealed class BatchBuilder
    {
        private readonly List<PositionedObject> _objects = [];
        private readonly List<BatchBuilder> _children = [];
        private BatchBuilder? _parent;
        private bool _positionResolved;

        public BatchBuilder(UiObject frame, int position)
        {
            Frame = frame;
            Position = position;
        }

        public UiObject Frame { get; }
        public int Position { get; private set; }

        public BatchBuilder? Parent
        {
            get => _parent;
            set
            {
                if (ReferenceEquals(_parent, value))
                    return;
                _parent = value;
                value?._children.Add(this);
            }
        }

        public void AddObject(int position, UiObject value)
        {
            _objects.Add(new PositionedObject(position, value));
            Position = Math.Min(Position, position);
        }

        public int ResolvePosition()
        {
            if (_positionResolved)
                return Position;
            _positionResolved = true;
            foreach (var child in _children)
                Position = Math.Min(Position, child.ResolvePosition());
            return Position;
        }

        public UiFrameBufferBatchEntry Build() =>
            new(Frame, Merge(_objects, _children));
    }

    private sealed record PositionedObject(int Position, UiObject Value);

    private sealed record PositionedEntry(
        int Position,
        int TieBreaker,
        UiRenderBatchEntry Entry);
}
