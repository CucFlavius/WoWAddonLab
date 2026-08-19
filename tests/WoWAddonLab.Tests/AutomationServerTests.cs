using System.Net.Http.Json;
using System.Text.Json;
using WoWAddonLab.Automation;

namespace WoWAddonLab.Tests;

public sealed class AutomationServerTests
{
    [Fact]
    public async Task DesktopViewportCaptureIsAvailableAsAnAgentReadablePng()
    {
        using var session = new EmulatorSession();
        var png = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
        await using var server = new AutomationServer(
            session,
            43122,
            new FixedViewportCaptureProvider(new ViewportCapture(png, 1280, 720)));
        await server.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };
        using var response = await client.GetAsync("/v1/viewport.png");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("1280", response.Headers.GetValues("X-Addon-Viewport-Width").Single());
        Assert.Equal("720", response.Headers.GetValues("X-Addon-Viewport-Height").Single());
        Assert.Equal(png, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task HeadlessViewportCaptureReportsThatNoFramebufferExists()
    {
        using var session = new EmulatorSession();
        await using var server = new AutomationServer(session, 43123);
        await server.StartAsync();

        using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };
        using var response = await client.GetAsync("/v1/viewport.png");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(System.Net.HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("desktop renderer", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task LuaEvaluationFailuresReturnTracebacksAndEnterTheStructuredLog()
    {
        using var session = new EmulatorSession();
        await using var server = new AutomationServer(session, 43121);
        await server.StartAsync();
        using var cancellation = new CancellationTokenSource();
        var pump = Task.Run(async () =>
        {
            while (!cancellation.IsCancellationRequested)
            {
                session.Tick(1.0 / 60.0);
                await Task.Delay(1, cancellation.Token)
                    .ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        });

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };
            var response = await client.PostAsJsonAsync(
                "/v1/lua/eval",
                new { code = "error('traversal failure')" });
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            var log = await client.GetFromJsonAsync<JsonElement>("/v1/log?limit=20");

            Assert.Equal(System.Net.HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Contains("traversal failure", body.GetProperty("error").GetString());
            Assert.Contains(
                log.EnumerateArray(),
                entry => entry.GetProperty("message").GetString()!
                    .Contains("traversal failure", StringComparison.Ordinal));
        }
        finally
        {
            cancellation.Cancel();
            await pump;
        }
    }

    [Fact]
    public async Task StatusTreeAndLuaEvaluationAreAvailableToCodingAgents()
    {
        const string addonPath = @"E:\Personal\wowAddon_Dungeon\Dungeonmire";
        if (!Directory.Exists(addonPath))
            return;

        using var session = new EmulatorSession();
        session.Load(addonPath);
        await using var server = new AutomationServer(session, 43120);
        await server.StartAsync();
        using var cancellation = new CancellationTokenSource();
        var pump = Task.Run(async () =>
        {
            while (!cancellation.IsCancellationRequested)
            {
                session.Tick(1.0 / 60.0);
                await Task.Delay(1, cancellation.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
        });

        try
        {
            using var client = new HttpClient { BaseAddress = new Uri(server.BaseUrl) };
            var status = await client.GetFromJsonAsync<JsonElement>("/v1/status");
            var compatibility = await client.GetFromJsonAsync<JsonElement>("/v1/compatibility");
            var response = await client.PostAsJsonAsync("/v1/lua/eval", new { code = "DM.halted" });
            var evaluation = await response.Content.ReadFromJsonAsync<JsonElement>();
            var tree = await client.GetFromJsonAsync<JsonElement>("/v1/ui/tree");
            var mouse = await client.PostAsJsonAsync(
                "/v1/input/mouse",
                new { x = 800, y = 450 });
            var mouseResult = await mouse.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(status.GetProperty("loaded").GetBoolean());
            Assert.Equal(0, compatibility.GetProperty("userFailures").GetArrayLength());
            Assert.Equal("false", evaluation.GetProperty("result").GetString());
            Assert.True(tree.GetArrayLength() > 1_000);
            Assert.True(mouseResult.GetProperty("accepted").GetBoolean());
        }
        finally
        {
            cancellation.Cancel();
            await pump;
        }
    }

    private sealed class FixedViewportCaptureProvider(ViewportCapture capture)
        : IViewportCaptureProvider
    {
        public Task<ViewportCapture> CaptureAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(capture);
    }
}
