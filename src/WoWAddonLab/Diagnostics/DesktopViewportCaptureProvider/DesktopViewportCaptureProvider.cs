using System.Collections.Concurrent;
using System.Numerics;
using WoWAddonLab.Automation;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace WoWAddonLab.Diagnostics;

internal sealed class DesktopViewportCaptureProvider : IViewportCaptureProvider, IDisposable
{
    private readonly ConcurrentQueue<CaptureRequest> _requests = new();
    private bool _disposed;

    public Task<ViewportCapture> CaptureAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var completion = new TaskCompletionSource<ViewportCapture>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        _requests.Enqueue(new CaptureRequest(completion, cancellationToken));
        return completion.Task;
    }

    public unsafe void ServicePending(
        GL gl,
        Vector2 canvasOrigin,
        Vector2 canvasSize,
        Vector2 framebufferScale,
        Vector2D<int> framebufferSize)
    {
        if (!_requests.TryDequeue(out var first))
            return;

        var requests = new List<CaptureRequest> { first };
        while (_requests.TryDequeue(out var request))
            requests.Add(request);

        var active = requests
            .Where(value => !value.CancellationToken.IsCancellationRequested)
            .ToArray();
        foreach (var request in requests.Except(active))
            request.Completion.TrySetCanceled(request.CancellationToken);
        if (active.Length == 0)
            return;

        try
        {
            var rectangle = ViewportCaptureGeometry.Resolve(
                canvasOrigin,
                canvasSize,
                framebufferScale,
                framebufferSize);
            if (rectangle.Width <= 0 || rectangle.Height <= 0)
                throw new InvalidOperationException("The addon viewport is not currently drawable.");

            var pixels = new byte[checked(rectangle.Width * rectangle.Height * 4)];
            gl.GetInteger(GLEnum.ReadFramebufferBinding, out var previousFramebuffer);
            gl.GetInteger(GLEnum.ReadBuffer, out var previousReadBuffer);
            gl.GetInteger(GLEnum.PackAlignment, out var previousPackAlignment);
            try
            {
                gl.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
                gl.ReadBuffer(GLEnum.Back);
                gl.PixelStore(GLEnum.PackAlignment, 1);
                fixed (byte* pointer = pixels)
                {
                    gl.ReadPixels(
                        rectangle.X,
                        rectangle.OpenGlY,
                        (uint)rectangle.Width,
                        (uint)rectangle.Height,
                        GLEnum.Rgba,
                        GLEnum.UnsignedByte,
                        pointer);
                }
            }
            finally
            {
                gl.PixelStore(GLEnum.PackAlignment, previousPackAlignment);
                gl.ReadBuffer((GLEnum)previousReadBuffer);
                gl.BindFramebuffer(
                    FramebufferTarget.ReadFramebuffer,
                    (uint)previousFramebuffer);
            }

            ViewportCaptureGeometry.FlipRowsInPlace(
                pixels,
                rectangle.Width,
                rectangle.Height);
            using var image = Image.LoadPixelData<Rgba32>(
                pixels,
                rectangle.Width,
                rectangle.Height);
            using var stream = new MemoryStream();
            image.Save(stream, new PngEncoder());
            var capture = new ViewportCapture(
                stream.ToArray(),
                rectangle.Width,
                rectangle.Height);
            foreach (var request in active)
                request.Completion.TrySetResult(capture);
        }
        catch (Exception exception)
        {
            foreach (var request in active)
                request.Completion.TrySetException(exception);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        var exception = new ObjectDisposedException(nameof(DesktopViewportCaptureProvider));
        while (_requests.TryDequeue(out var request))
            request.Completion.TrySetException(exception);
    }

    private sealed record CaptureRequest(
        TaskCompletionSource<ViewportCapture> Completion,
        CancellationToken CancellationToken);
}
