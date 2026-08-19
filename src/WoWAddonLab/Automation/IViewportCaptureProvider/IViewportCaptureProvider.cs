namespace WoWAddonLab.Automation;

public interface IViewportCaptureProvider
{
    Task<ViewportCapture> CaptureAsync(CancellationToken cancellationToken = default);
}
