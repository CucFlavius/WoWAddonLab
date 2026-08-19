using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WoWAddonLab.Emulator;

namespace WoWAddonLab.Automation;

public sealed class AutomationServer : IAsyncDisposable
{
    private readonly EmulatorSession _session;
    private readonly IViewportCaptureProvider? _viewportCapture;
    private readonly WebApplication _application;

    public AutomationServer(
        EmulatorSession session,
        int port = 43117,
        IViewportCaptureProvider? viewportCapture = null)
    {
        _session = session;
        _viewportCapture = viewportCapture;
        Port = port;
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls($"http://127.0.0.1:{port}");
        builder.Logging.ClearProviders();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.WriteIndented = true;
        });
        _application = builder.Build();
        MapRoutes();
    }

    public int Port { get; }
    public string BaseUrl => $"http://127.0.0.1:{Port}";

    public Task StartAsync(CancellationToken cancellationToken = default) =>
        _application.StartAsync(cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _application.StopAsync();
        await _application.DisposeAsync();
    }

    private void MapRoutes()
    {
        _application.MapGet("/", () => Results.Json(new
        {
            name = "WoW Addon Lab Automation API",
            version = 1,
            endpoints = new[]
            {
                "GET /v1/status",
                "GET /v1/compatibility",
                "GET /v1/ui/tree",
                "GET /v1/log?limit=200",
                "GET /v1/viewport.png",
                "POST /v1/lua/eval { code }",
                "POST /v1/reload",
                "POST /v1/events { name, args? }",
                "POST /v1/input/key { key, down, control?, shift?, alt? }",
                "POST /v1/input/text { text }",
                "POST /v1/input/mouse { x?, y?, button?, down?, wheel? }",
                "POST /v1/slash { command }"
            }
        }));

        _application.MapGet("/v1/status", async (CancellationToken token) =>
            Results.Json(await _session.InvokeAsync(value => value.Status(), token)));

        _application.MapGet("/v1/compatibility", async (CancellationToken token) =>
            Results.Json(await _session.InvokeAsync(value => value.CompatibilityReport(), token)));

        _application.MapGet("/v1/ui/tree", async (CancellationToken token) =>
            Results.Json(await _session.InvokeAsync(value => value.Ui.SnapshotTree(), token)));

        _application.MapGet("/v1/log", async (int? limit, CancellationToken token) =>
            Results.Json(await _session.InvokeAsync(value => value.Log.Snapshot(Math.Clamp(limit ?? 200, 1, 5_000)), token)));

        _application.MapGet("/v1/viewport.png", CaptureViewportAsync);

        _application.MapPost("/v1/lua/eval", async (EvalRequest request, CancellationToken token) =>
        {
            var evaluation = await _session.InvokeAsync(value =>
            {
                try
                {
                    return new EvalResponse(value.Lua.Evaluate(request.Code), null);
                }
                catch (Exception exception)
                {
                    value.Log.Error("automation", $"Lua evaluation failed: {exception.Message}");
                    return new EvalResponse(null, exception.Message);
                }
            }, token);
            return evaluation.Error is null
                ? Results.Json(new { result = evaluation.Result })
                : Results.Json(
                    new { error = evaluation.Error },
                    statusCode: StatusCodes.Status500InternalServerError);
        });

        _application.MapPost("/v1/reload", async (CancellationToken token) =>
        {
            await _session.InvokeAsync(value =>
            {
                value.Reload();
                return true;
            }, token);
            return Results.Ok(new { reloaded = true });
        });

        _application.MapPost("/v1/events", async (EventRequest request, CancellationToken token) =>
        {
            await _session.InvokeAsync(value =>
            {
                value.Lua.TriggerEvent(request.Name, request.Args?.Select(ConvertJson).ToArray() ?? []);
                return true;
            }, token);
            return Results.Ok(new { triggered = request.Name });
        });

        _application.MapPost("/v1/input/key", async (KeyRequest request, CancellationToken token) =>
        {
            await _session.InvokeAsync(value =>
            {
                value.Key(request.Key, request.Down, request.Control, request.Shift, request.Alt);
                return true;
            }, token);
            return Results.Ok(new { accepted = true });
        });

        _application.MapPost("/v1/input/text", async (TextInputRequest request, CancellationToken token) =>
        {
            await _session.InvokeAsync(value =>
            {
                value.TextInput(request.Text);
                return true;
            }, token);
            return Results.Ok(new { accepted = true });
        });

        _application.MapPost("/v1/input/mouse", async (MouseRequest request, CancellationToken token) =>
        {
            var result = await _session.InvokeAsync(value =>
            {
                if (request.X is { } x && request.Y is { } y)
                    value.MouseMove(x, y);
                var target = value.Ui.HitTest(
                    value.Ui.CursorPosition,
                    requireClick: true,
                    request.Button ?? "LeftButton");
                if (request.Button is not null && request.Down is { } down)
                    value.MouseButton(request.Button, down);
                if (request.Wheel is { } wheel)
                    value.MouseWheel(wheel);
                return new MouseInputResult(
                    true,
                    target?.Id,
                    target?.Name,
                    target?.ObjectType,
                    target?.Enabled);
            }, token);
            return Results.Ok(result);
        });

        _application.MapPost("/v1/slash", async (SlashRequest request, CancellationToken token) =>
            Results.Json(new
            {
                handled = await _session.InvokeAsync(value => value.Lua.InvokeSlashCommand(request.Command), token)
            }));
    }

    private async Task<IResult> CaptureViewportAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (_viewportCapture is null)
        {
            return Results.Json(
                new { error = "Viewport capture is only available from the desktop renderer." },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        try
        {
            var capture = await _viewportCapture.CaptureAsync(cancellationToken);
            context.Response.Headers["X-Addon-Viewport-Width"] = capture.Width.ToString();
            context.Response.Headers["X-Addon-Viewport-Height"] = capture.Height.ToString();
            context.Response.Headers.CacheControl = "no-store";
            return Results.File(capture.PngBytes, "image/png");
        }
        catch (InvalidOperationException exception)
        {
            return Results.Json(
                new { error = exception.Message },
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static object? ConvertJson(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when value.TryGetInt64(out var integer) => integer,
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.String => value.GetString(),
        _ => value.GetRawText()
    };

    private sealed record EvalRequest(string Code);
    private sealed record EvalResponse(string? Result, string? Error);
    private sealed record EventRequest(string Name, JsonElement[]? Args);
    private sealed record KeyRequest(
        string Key,
        bool Down,
        bool Control = false,
        bool Shift = false,
        bool Alt = false);
    private sealed record TextInputRequest(string Text);
    private sealed record MouseRequest(
        float? X,
        float? Y,
        string? Button,
        bool? Down,
        float? Wheel);
    private sealed record MouseInputResult(
        bool Accepted,
        int? TargetId,
        string? TargetName,
        string? TargetType,
        bool? TargetEnabled);
    private sealed record SlashRequest(string Command);
}
