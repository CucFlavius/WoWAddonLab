using System.Numerics;
using ImGuiNET;
using Silk.NET.OpenGL;
using WoWAddonLab.Assets;

namespace WoWAddonLab.Simulation;

public sealed class SimulationMapControl : IDisposable
{
    private const int UploadsPerFrame = 8;
    private const float MinimumTileSize = 2;
    private const float MaximumTileSize = 96;
    private const float ZoomStep = 1.15f;

    private readonly Dictionary<uint, uint> _textures = [];
    private GL? _gl;
    private TactWorldMapCatalog? _catalog;
    private MapTileLoader? _loader;
    private WorldMapEntry? _map;
    private WdtMinimapTiles? _tiles;
    private string _filter = string.Empty;
    private float _tileSize = 8;
    private Vector2 _pan;
    private bool _dragging;
    private Vector2 _surfaceMinimum;
    private Vector2 _surfaceMaximum;
    private int _hoverFrame = -1;

    public Action<WorldMapEntry, int, int>? TilePicked;

    public Func<float>? WheelSource { get; set; }

    public bool WantsWheel(Vector2 mouse) =>
        _hoverFrame == ImGui.GetFrameCount() &&
        mouse.X >= _surfaceMinimum.X &&
        mouse.Y >= _surfaceMinimum.Y &&
        mouse.X < _surfaceMaximum.X &&
        mouse.Y < _surfaceMaximum.Y;

    public void Attach(GL? gl, TactWorldMapCatalog? catalog, TactAssetSource? tact)
    {
        ReleaseTextures();
        _loader?.Dispose();
        _loader = tact is null ? null : new MapTileLoader(tact);
        _gl = gl;
        _catalog = catalog;
        _map = null;
        _tiles = null;
        _pan = Vector2.Zero;
    }

    public void Render(int? selectedMapId, int? selectedX, int? selectedY)
    {
        if (_catalog is null)
        {
            ImGui.TextDisabled("Client data is not loaded.");
            return;
        }
        if (_map is null && selectedMapId is { } restore)
            SelectById(restore);

        DrainUploads();

        ImGui.SetNextItemWidth(-1);
        if (ImGui.BeginCombo("##map-select", _map?.Name ?? "Select a map"))
        {
            ImGui.SetNextItemWidth(-1);
            ImGui.InputTextWithHint("##map-filter", "Filter...", ref _filter, 128);
            foreach (var map in _catalog.Maps
                         .Where(map =>
                             string.IsNullOrWhiteSpace(_filter) ||
                             map.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase) ||
                             map.Directory.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                         .Take(200))
            {
                if (ImGui.Selectable($"{map.Name}##{map.Id}", _map?.Id == map.Id))
                    Select(map);
            }
            ImGui.EndCombo();
        }

        var size = new Vector2(-1, 220);
        if (!ImGui.BeginChild("##map-view", size, ImGuiChildFlags.Border))
        {
            ImGui.EndChild();
            return;
        }

        if (_tiles is null)
        {
            ImGui.TextDisabled(_map is null ? "No map selected." : "No minimap tiles.");
            ImGui.EndChild();
            return;
        }

        var origin = ImGui.GetCursorScreenPos();
        var available = ImGui.GetContentRegionAvail();
        ImGui.InvisibleButton("##map-surface", available);
        var hovered = ImGui.IsItemHovered();
        _surfaceMinimum = origin;
        _surfaceMaximum = origin + available;
        if (hovered)
            _hoverFrame = ImGui.GetFrameCount();
        HandleInput(hovered, origin, available, selectedMapId);

        var drawList = ImGui.GetWindowDrawList();
        drawList.PushClipRect(origin, origin + available, true);
        var tile = new Vector2(_tileSize, _tileSize);
        var requested = 0;
        for (var y = _tiles.MinimumY; y <= _tiles.MaximumY; y++)
        {
            for (var x = _tiles.MinimumX; x <= _tiles.MaximumX; x++)
            {
                var fileDataId = _tiles[x, y];
                if (fileDataId == 0)
                    continue;
                var position = origin + _pan + new Vector2(
                    (x - _tiles.MinimumX) * _tileSize,
                    (y - _tiles.MinimumY) * _tileSize);
                if (position.X + _tileSize < origin.X ||
                    position.Y + _tileSize < origin.Y ||
                    position.X > origin.X + available.X ||
                    position.Y > origin.Y + available.Y)
                {
                    continue;
                }
                if (_textures.TryGetValue(fileDataId, out var handle))
                {
                    drawList.AddImage((nint)handle, position, position + tile);
                    continue;
                }
                drawList.AddRectFilled(position, position + tile, 0xFF1A1A1A);
                if (requested < 64)
                {
                    _loader?.Request(fileDataId);
                    requested++;
                }
            }
        }

        if (selectedMapId == _map!.Id && selectedX is { } sx && selectedY is { } sy)
        {
            var marker = origin + _pan + new Vector2(
                (sx - _tiles.MinimumX) * _tileSize,
                (sy - _tiles.MinimumY) * _tileSize);
            drawList.AddRect(marker, marker + tile, 0xFF40C7EB, 0, ImDrawFlags.None, 2);
        }
        drawList.PopClipRect();
        ImGui.EndChild();

        var pending = _loader?.Pending ?? 0;
        ImGui.TextDisabled(
            pending > 0
                ? $"{_tiles.Count} tiles   loading {pending}"
                : $"{_tiles.Count} tiles");
    }

    private void HandleInput(
        bool hovered,
        Vector2 origin,
        Vector2 available,
        int? selectedMapId)
    {
        var io = ImGui.GetIO();
        var wheel = WheelSource?.Invoke() ?? io.MouseWheel;
        if (hovered && MathF.Abs(wheel) > float.Epsilon)
        {
            var anchor = ImGui.GetMousePos() - origin - _pan;
            var previous = _tileSize;
            _tileSize = Math.Clamp(
                _tileSize * MathF.Pow(ZoomStep, wheel),
                MinimumTileSize,
                MaximumTileSize);
            _pan += anchor * (1 - _tileSize / previous);
        }

        if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            _dragging = true;
        if (_dragging && ImGui.IsMouseReleased(ImGuiMouseButton.Right))
            _dragging = false;
        if (_dragging)
            _pan += io.MouseDelta;

        if (!hovered || !ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            return;
        var local = ImGui.GetMousePos() - origin - _pan;
        var tileX = _tiles!.MinimumX + (int)MathF.Floor(local.X / _tileSize);
        var tileY = _tiles.MinimumY + (int)MathF.Floor(local.Y / _tileSize);
        if (_tiles[tileX, tileY] != 0)
            TilePicked?.Invoke(_map!, tileX, tileY);
    }

    private void SelectById(int mapId)
    {
        if (_catalog!.Maps.FirstOrDefault(map => map.Id == mapId) is { } map)
            Select(map);
    }

    private void Select(WorldMapEntry map)
    {
        _map = map;
        _tiles = _catalog!.TilesFor(map);
        _pan = Vector2.Zero;
        _tileSize = 8;
    }

    private void DrainUploads()
    {
        if (_gl is null || _loader is null)
            return;
        for (var index = 0; index < UploadsPerFrame; index++)
        {
            if (!_loader.TryTakeDecoded(out var fileDataId, out var image))
                return;
            _textures[fileDataId] = Upload(image);
        }
    }

    private unsafe uint Upload(DecodedTextureImage image)
    {
        var level = image.BaseLevel;
        var texture = _gl!.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, texture);
        fixed (byte* pixels = level.Pixels)
        {
            _gl.TexImage2D(
                TextureTarget.Texture2D,
                0,
                InternalFormat.Rgba8,
                (uint)level.Width,
                (uint)level.Height,
                0,
                PixelFormat.Rgba,
                PixelType.UnsignedByte,
                pixels);
        }
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapS,
            (int)TextureWrapMode.ClampToEdge);
        _gl.TexParameter(
            TextureTarget.Texture2D,
            TextureParameterName.TextureWrapT,
            (int)TextureWrapMode.ClampToEdge);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
        return texture;
    }

    private void ReleaseTextures()
    {
        if (_gl is not null)
        {
            foreach (var texture in _textures.Values)
                _gl.DeleteTexture(texture);
        }
        _textures.Clear();
    }

    public void Dispose()
    {
        _loader?.Dispose();
        _loader = null;
        ReleaseTextures();
    }
}
