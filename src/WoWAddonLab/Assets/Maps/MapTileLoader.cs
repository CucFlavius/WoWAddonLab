using System.Collections.Concurrent;

namespace WoWAddonLab.Assets;

internal sealed class MapTileLoader : IDisposable
{
    private readonly BlockingCollection<uint> _requests = new(new ConcurrentQueue<uint>());
    private readonly ConcurrentQueue<(uint FileDataId, DecodedTextureImage Image)> _decoded = new();
    private readonly HashSet<uint> _queued = [];
    private readonly TactAssetSource _tact;
    private readonly Thread _worker;
    private volatile bool _disposed;

    public MapTileLoader(TactAssetSource tact)
    {
        _tact = tact;
        _worker = new Thread(Run)
        {
            IsBackground = true,
            Name = "map-tile-loader",
            Priority = ThreadPriority.BelowNormal
        };
        _worker.Start();
    }

    public int Pending => _requests.Count;

    public void Request(uint fileDataId)
    {
        if (_disposed || fileDataId == 0)
            return;
        lock (_queued)
        {
            if (!_queued.Add(fileDataId))
                return;
        }
        _requests.Add(fileDataId);
    }

    public bool TryTakeDecoded(out uint fileDataId, out DecodedTextureImage image)
    {
        if (_decoded.TryDequeue(out var entry))
        {
            fileDataId = entry.FileDataId;
            image = entry.Image;
            return true;
        }
        fileDataId = 0;
        image = null!;
        return false;
    }

    public void Forget(uint fileDataId)
    {
        lock (_queued)
            _queued.Remove(fileDataId);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _requests.CompleteAdding();
        _worker.Join(TimeSpan.FromSeconds(2));
        _requests.Dispose();
    }

    private void Run()
    {
        try
        {
            foreach (var fileDataId in _requests.GetConsumingEnumerable())
            {
                if (_disposed)
                    return;
                try
                {
                    var bytes = _tact.Read(fileDataId);
                    if (bytes is null)
                        continue;
                    _decoded.Enqueue((fileDataId, TextureImageDecoder.Decode(bytes)));
                }
                catch (Exception)
                {
                    Forget(fileDataId);
                }
            }
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }
}
