using System.Numerics;
using System.Text;
using WoWAddonLab.Assets;
using WoWAddonLab.Emulator.Lua;
using WoWAddonLab.Emulator.UI;
using ImGuiNET;

namespace WoWAddonLab.Rendering;

public sealed class WowUiRenderer : IDisposable
{
    private readonly TextureCache _textures;
    private readonly AddonFontCache _fonts;
    private UiSystem? _lastUi;
    private IReadOnlyList<UiObject> _lastRenderOrder = [];
    private IWowMapProvider? _lastMapProvider;
    private int? _activeFrameBufferOwnerId;

    public WowUiRenderer(TextureCache textures, AddonFontCache fonts)
    {
        _textures = textures;
        _fonts = fonts;
    }

    public int DrawnObjects { get; private set; }

    public void Render(
        ImDrawListPtr drawList,
        UiSystem ui,
        Vector2 origin,
        float scale,
        IWowMapProvider? mapProvider = null)
    {
        _lastUi = ui;
        _lastRenderOrder = ui.VisualRenderOrder().ToArray();
        _lastMapProvider = mapProvider;
        DrawnObjects = 0;
        DrawnObjects = RenderObjects(
            drawList,
            ui,
            _lastRenderOrder,
            origin,
            scale,
            null,
            mapProvider);
    }

    public bool RenderSubtree(
        ImDrawListPtr drawList,
        UiSystem ui,
        int rootId,
        Vector2 viewportOrigin,
        Vector2 viewportSize,
        float padding = 12)
    {
        if (ui.Find(rootId) is not { } root ||
            viewportSize.X <= 1 ||
            viewportSize.Y <= 1)
        {
            return false;
        }

        var subtreeIds = CollectSubtreeIds(ui, root);
        var sourceOrder = ReferenceEquals(_lastUi, ui)
            ? _lastRenderOrder
            : ui.VisualRenderOrder().ToArray();
        var renderOrder = sourceOrder
            .Where(value => subtreeIds.Contains(value.Id))
            .ToArray();
        var drawable = renderOrder
            .Where(IsDrawableObject)
            .ToArray();
        if (drawable.Length == 0)
            return false;

        UiRect? logicalBounds = null;
        var rootBounds = ui.ResolveBounds(root.Id);
        if (rootBounds.Width > float.Epsilon && rootBounds.Height > float.Epsilon)
            logicalBounds = rootBounds;
        foreach (var value in drawable)
        {
            var bounds = ui.ResolveBounds(value.Id);
            if (bounds.Width <= float.Epsilon || bounds.Height <= float.Epsilon)
                continue;
            logicalBounds = logicalBounds is null
                ? bounds
                : Union(logicalBounds.Value, bounds);
        }

        if (logicalBounds is not { } contentBounds ||
            contentBounds.Width <= float.Epsilon ||
            contentBounds.Height <= float.Epsilon)
        {
            return false;
        }

        var available = Vector2.Max(
            Vector2.One,
            viewportSize - new Vector2(padding * 2));
        var scale = Math.Clamp(
            MathF.Min(
                available.X / contentBounds.Width,
                available.Y / contentBounds.Height),
            .01f,
            4);
        var renderedSize = new Vector2(
            contentBounds.Width * scale,
            contentBounds.Height * scale);
        var renderedOrigin =
            viewportOrigin +
            Vector2.Max(Vector2.Zero, (viewportSize - renderedSize) * .5f);
        var origin = new Vector2(
            renderedOrigin.X - contentBounds.Left * scale,
            renderedOrigin.Y - (ui.LogicalHeight - contentBounds.Top) * scale);

        drawList.PushClipRect(
            viewportOrigin,
            viewportOrigin + viewportSize,
            true);
        RenderObjects(
            drawList,
            ui,
            renderOrder,
            origin,
            scale,
            rootId,
            _lastMapProvider);
        drawList.PopClipRect();
        return true;
    }

    private int RenderObjects(
        ImDrawListPtr drawList,
        UiSystem ui,
        IReadOnlyList<UiObject> renderOrder,
        Vector2 origin,
        float scale,
        int? subtreeRootId,
        IWowMapProvider? mapProvider)
    {
        return RenderBatchEntries(
            drawList,
            ui,
            UiRenderBatchPlan.Build(ui, renderOrder),
            origin,
            scale,
            subtreeRootId,
            mapProvider);
    }

    private int RenderBatchEntries(
        ImDrawListPtr drawList,
        UiSystem ui,
        IReadOnlyList<UiRenderBatchEntry> entries,
        Vector2 origin,
        float scale,
        int? subtreeRootId,
        IWowMapProvider? mapProvider)
    {
        var drawnObjects = 0;
        foreach (var entry in entries)
        {
            if (entry is UiFrameBufferBatchEntry frameBuffer)
            {
                WowImGuiController.BeginFrameBuffer(
                    drawList,
                    frameBuffer.Frame.Id,
                    frameBuffer.Frame.Alpha);
                var previousOwner = _activeFrameBufferOwnerId;
                _activeFrameBufferOwnerId = frameBuffer.Frame.Id;
                try
                {
                    drawnObjects += RenderBatchEntries(
                        drawList,
                        ui,
                        frameBuffer.Entries,
                        origin,
                        scale,
                        subtreeRootId,
                        mapProvider);
                }
                finally
                {
                    _activeFrameBufferOwnerId = previousOwner;
                }
                WowImGuiController.EndFrameBuffer(
                    drawList,
                    frameBuffer.Frame.Id,
                    frameBuffer.Frame.Alpha);
                continue;
            }

            var value = ((UiRenderObjectEntry)entry).Value;
            if (!IsDrawableObject(value))
                continue;

            var frameAlphaGradient = ResolveFrameAlphaGradient(
                ui,
                value,
                origin,
                scale);
            var firstGradientCommand = -1;
            if (frameAlphaGradient is not null)
            {
                drawList.AddDrawCmd();
                firstGradientCommand = drawList.CmdBuffer.Size - 1;
            }

            var clip = ResolveClip(ui, value, subtreeRootId);
            if (clip is { } clipBounds)
            {
                drawList.PushClipRect(
                    ToScreen(new Vector2(clipBounds.Left, clipBounds.Top), ui, origin, scale),
                    ToScreen(new Vector2(clipBounds.Right, clipBounds.Bottom), ui, origin, scale),
                    true);
            }

            if (value.Blob is not null)
                RenderBlob(drawList, ui, value, origin, scale, mapProvider);
            else if (value.Cooldown is not null)
                RenderCooldown(drawList, ui, value, origin, scale);
            else if (value.Texture is not null)
                RenderTexture(drawList, ui, value, origin, scale);
            else if (value.Font is not null)
                RenderFont(drawList, ui, value, origin, scale);
            else if (value.Line is not null)
                RenderLine(drawList, ui, value, origin, scale);

            if (clip is not null)
                drawList.PopClipRect();
            if (frameAlphaGradient is { } gradient)
            {
                ApplyFrameAlphaGradient(
                    drawList,
                    firstGradientCommand,
                    gradient);
                drawList.AddDrawCmd();
            }
            drawnObjects++;
        }
        return drawnObjects;
    }

    private static UiFrameAlphaGradientParameters? ResolveFrameAlphaGradient(
        UiSystem ui,
        UiObject value,
        Vector2 origin,
        float scale)
    {
        if (UiFrameAlphaGradient.Resolve(ui, value) is not { } resolved)
            return null;

        var bounds = ui.ResolveBounds(resolved.Owner.Id);
        var upperLeft = ToScreen(
            new Vector2(bounds.Left, bounds.Top),
            ui,
            origin,
            scale);
        var lowerRight = ToScreen(
            new Vector2(bounds.Right, bounds.Bottom),
            ui,
            origin,
            scale);
        return new UiFrameAlphaGradientParameters(
            new Vector4(
                resolved.LeadingEdge.X * scale,
                resolved.LeadingEdge.Y * scale,
                resolved.TrailingEdge.X * scale,
                resolved.TrailingEdge.Y * scale),
            new Vector4(
                upperLeft.X,
                upperLeft.Y,
                lowerRight.X,
                lowerRight.Y));
    }

    private static void ApplyFrameAlphaGradient(
        ImDrawListPtr drawList,
        int firstCommand,
        UiFrameAlphaGradientParameters gradient)
    {
        for (var index = Math.Max(0, firstCommand);
             index < drawList.CmdBuffer.Size;
             index++)
        {
            var command = drawList.CmdBuffer[index];
            if (command.ElemCount == 0 || command.UserCallback != nint.Zero)
                continue;
            command.TextureId = WowImGuiController.FrameAlphaGradientTextureId(
                command.TextureId,
                gradient);
        }
    }

    internal static bool IsDrawableObject(UiObject value) =>
        value.Blob is not null ||
        value.Cooldown is not null ||
        value.Font is not null &&
        value.ObjectType.EndsWith("EditBox", StringComparison.OrdinalIgnoreCase) ||
        value.IsRegion &&
        !value.ObjectType.Equals("MaskTexture", StringComparison.OrdinalIgnoreCase);

    public void Dispose()
    {
    }

    private void RenderBlob(
        ImDrawListPtr drawList,
        UiSystem ui,
        UiObject value,
        Vector2 origin,
        float scale,
        IWowMapProvider? mapProvider)
    {
        var blob = value.Blob!;
        if (mapProvider is null ||
            blob.FillTexture is null &&
            blob.FillTextureFileDataId is null ||
            blob.BorderTexture is null &&
            blob.BorderTextureFileDataId is null)
        {
            return;
        }

        var meshes = UiBlobGeometry.Build(ui, value, mapProvider);
        if (meshes.Count == 0)
            return;

        var fillState = new UiTextureState
        {
            Asset = blob.FillTexture,
            FileDataId = blob.FillTextureFileDataId,
            BlendMode = "BLEND"
        };
        var borderState = new UiTextureState
        {
            Asset = blob.BorderTexture,
            FileDataId = blob.BorderTextureFileDataId,
            BlendMode = "BLEND"
        };
        var fillTexture = WowImGuiController.TextureId(
            _textures.Resolve(fillState),
            "BLEND",
            0);
        var borderTexture = WowImGuiController.TextureId(
            _textures.Resolve(borderState),
            "BLEND",
            0);
        var effectiveAlpha = EffectiveAlpha(ui, value);
        var fillColor = ImGui.ColorConvertFloat4ToU32(new Vector4(
            1,
            1,
            1,
            effectiveAlpha * blob.FillAlpha / 255f));
        var borderColor = ImGui.ColorConvertFloat4ToU32(new Vector4(
            1,
            1,
            1,
            effectiveAlpha * blob.BorderAlpha / 255f));

        foreach (var mesh in meshes.Where(mesh => mesh.IsVisible))
        {
            RenderIndexedTriangles(
                drawList,
                fillTexture,
                mesh.FillVertices,
                mesh.FillUvs,
                mesh.FillIndices,
                triangleStrip: false,
                fillColor,
                ui,
                origin,
                scale);
            RenderIndexedTriangles(
                drawList,
                borderTexture,
                mesh.BorderVertices,
                mesh.BorderUvs,
                mesh.BorderIndices,
                triangleStrip: true,
                borderColor,
                ui,
                origin,
                scale);
        }
    }

    private static void RenderIndexedTriangles(
        ImDrawListPtr drawList,
        nint textureId,
        IReadOnlyList<Vector2> vertices,
        IReadOnlyList<Vector2> uvs,
        IReadOnlyList<ushort> indices,
        bool triangleStrip,
        uint color,
        UiSystem ui,
        Vector2 origin,
        float scale)
    {
        var step = triangleStrip ? 1 : 3;
        for (var index = 0; index + 2 < indices.Count; index += step)
        {
            var first = indices[index];
            var second = indices[index + 1];
            var third = indices[index + 2];
            if (triangleStrip && (index & 1) != 0)
                (first, second) = (second, first);
            if (first >= vertices.Count ||
                second >= vertices.Count ||
                third >= vertices.Count)
            {
                continue;
            }

            var firstPosition = ToScreen(
                vertices[first],
                ui,
                origin,
                scale);
            var secondPosition = ToScreen(
                vertices[second],
                ui,
                origin,
                scale);
            var thirdPosition = ToScreen(
                vertices[third],
                ui,
                origin,
                scale);
            drawList.AddImageQuad(
                textureId,
                firstPosition,
                secondPosition,
                thirdPosition,
                thirdPosition,
                uvs[first],
                uvs[second],
                uvs[third],
                uvs[third],
                color);
        }
    }

    private void RenderCooldown(
        ImDrawListPtr drawList,
        UiSystem ui,
        UiObject value,
        Vector2 origin,
        float scale)
    {
        var cooldown = value.Cooldown!;
        if (cooldown.DrawSwipe &&
            (cooldown.SwipeTextureAsset is not null ||
             cooldown.SwipeTextureFileDataId is not null))
        {
            RenderCooldownSwipe(drawList, ui, value, origin, scale);
        }

        if (cooldown.CompletionBlingActive)
        {
            if (cooldown.DrawBling &&
                (cooldown.BlingTextureAsset is not null ||
                 cooldown.BlingTextureFileDataId is not null) &&
                ui.ResolveCooldownBlingQuad(value) is { } blingQuad)
            {
                var blingColor = cooldown.BlingColor;
                blingColor.W *=
                    ui.ResolveCooldownBlingAlpha(value) *
                    EffectiveAlpha(ui, value);
                RenderCooldownQuad(
                    drawList,
                    blingQuad,
                    cooldown.BlingTextureAsset,
                    cooldown.BlingTextureFileDataId,
                    blingColor,
                    ui,
                    origin,
                    scale);
            }
            return;
        }

        if (cooldown.DrawEdge &&
            (cooldown.EdgeTextureAsset is not null ||
             cooldown.EdgeTextureFileDataId is not null) &&
            ui.ResolveCooldownEdgeQuad(value) is { } edgeQuad)
        {
            var edgeColor = cooldown.EdgeColor;
            edgeColor.W *= EffectiveAlpha(ui, value);
            RenderCooldownQuad(
                drawList,
                edgeQuad,
                cooldown.EdgeTextureAsset,
                cooldown.EdgeTextureFileDataId,
                edgeColor,
                ui,
                origin,
                scale);
        }
    }

    private void RenderCooldownSwipe(
        ImDrawListPtr drawList,
        UiSystem ui,
        UiObject value,
        Vector2 origin,
        float scale)
    {
        var cooldown = value.Cooldown!;
        var vertices = ui.ResolveCooldownSwipeVertices(value);
        if (vertices.Count < 3)
            return;

        var texture = new UiTextureState
        {
            Asset = cooldown.SwipeTextureAsset,
            FileDataId = cooldown.SwipeTextureFileDataId,
            BlendMode = "BLEND"
        };
        var handle = CooldownTextureHandle(texture);
        var tint = cooldown.SwipeColor;
        tint.W *= EffectiveAlpha(ui, value);
        var color = ImGui.ColorConvertFloat4ToU32(
            Vector4.Clamp(tint, Vector4.Zero, Vector4.One));
        var center = ToScreen(vertices[0].Position, ui, origin, scale);
        for (var index = 1; index + 1 < vertices.Count; index++)
        {
            drawList.AddImageQuad(
                WowImGuiController.TextureId(handle, "BLEND", 0),
                center,
                ToScreen(vertices[index].Position, ui, origin, scale),
                ToScreen(vertices[index + 1].Position, ui, origin, scale),
                center,
                vertices[0].Uv,
                vertices[index].Uv,
                vertices[index + 1].Uv,
                vertices[0].Uv,
                color);
        }
    }

    private void RenderCooldownQuad(
        ImDrawListPtr drawList,
        UiCooldownQuad quad,
        string? asset,
        uint? fileDataId,
        Vector4 tint,
        UiSystem ui,
        Vector2 origin,
        float scale)
    {
        var texture = new UiTextureState
        {
            Asset = asset,
            FileDataId = fileDataId,
            BlendMode = "BLEND"
        };
        var handle = CooldownTextureHandle(texture);
        var color = ImGui.ColorConvertFloat4ToU32(
            Vector4.Clamp(tint, Vector4.Zero, Vector4.One));
        drawList.AddImageQuad(
            WowImGuiController.TextureId(handle, "BLEND", 0),
            ToScreen(quad.UpperLeft, ui, origin, scale),
            ToScreen(quad.UpperRight, ui, origin, scale),
            ToScreen(quad.LowerRight, ui, origin, scale),
            ToScreen(quad.LowerLeft, ui, origin, scale),
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1),
            color);
    }

    private uint CooldownTextureHandle(UiTextureState texture) =>
        texture.FileDataId == 0 ||
        texture.FileDataId is null && string.IsNullOrEmpty(texture.Asset)
            ? _textures.WhiteTexture
            : _textures.Resolve(texture);

    private static HashSet<int> CollectSubtreeIds(UiSystem ui, UiObject root)
    {
        var result = new HashSet<int>();
        var pending = new Stack<int>();
        pending.Push(root.Id);
        while (pending.TryPop(out var id))
        {
            if (!result.Add(id) || ui.Find(id) is not { } value)
                continue;
            foreach (var childId in value.Children)
                pending.Push(childId);
        }
        return result;
    }

    private void RenderTexture(
        ImDrawListPtr drawList,
        UiSystem ui,
        UiObject value,
        Vector2 origin,
        float scale)
    {
        var texture = value.Texture!;
        if (texture.IsColorSelectWheel)
        {
            RenderColorSelectWheel(drawList, ui, value, texture, origin, scale);
            return;
        }
        var hadIntrinsicSize =
            texture.IntrinsicWidth.HasValue && texture.IntrinsicHeight.HasValue;
        var handle = _textures.Resolve(texture);
        if (!hadIntrinsicSize &&
            texture.IntrinsicWidth.HasValue &&
            texture.IntrinsicHeight.HasValue)
        {
            ui.InvalidateLayout();
        }
        var horizontallyTiled =
            texture.HorizontallyTiled ||
            texture.WrapHorizontal.Equals("REPEAT", StringComparison.OrdinalIgnoreCase);
        var verticallyTiled =
            texture.VerticallyTiled ||
            texture.WrapVertical.Equals("REPEAT", StringComparison.OrdinalIgnoreCase);
        var statusBarFill = ResolveStatusBarTextureFill(
            ui,
            value,
            ui.ResolveBounds(value.Id),
            !horizontallyTiled && !verticallyTiled);
        var bounds = statusBarFill?.Bounds ?? ui.ResolveBounds(value.Id);
        var alpha = EffectiveAlpha(ui, value);
        var tint = texture.IsColor ? texture.Color * texture.VertexColor : texture.VertexColor;
        tint.W *= alpha;
        var color = ImGui.ColorConvertFloat4ToU32(Vector4.Clamp(tint, Vector4.Zero, Vector4.One));
        var effectiveScale = ui.LayoutScale(value);

        var quad = TextureQuad(ui, value, bounds, effectiveScale);
        var upperLeft = quad.UpperLeft;
        var lowerLeft = quad.LowerLeft;
        var upperRight = quad.UpperRight;
        var lowerRight = quad.LowerRight;
        var hasAnimationRotation = HasAnimationRotation(ui, value);
        var ul = ToScreen(upperLeft, ui, origin, scale);
        var ll = ToScreen(lowerLeft, ui, origin, scale);
        var ur = ToScreen(upperRight, ui, origin, scale);
        var lr = ToScreen(lowerRight, ui, origin, scale);
        ul = SnapTexturePosition(ul, texture);
        ll = SnapTexturePosition(ll, texture);
        ur = SnapTexturePosition(ur, texture);
        lr = SnapTexturePosition(lr, texture);

        if (texture.Gradient is { } gradient)
        {
            var minimum = gradient.Minimum * texture.VertexColor;
            var maximum = gradient.Maximum * texture.VertexColor;
            minimum.W *= alpha;
            maximum.W *= alpha;
            var min = ImGui.ColorConvertFloat4ToU32(Vector4.Clamp(minimum, Vector4.Zero, Vector4.One));
            var max = ImGui.ColorConvertFloat4ToU32(Vector4.Clamp(maximum, Vector4.Zero, Vector4.One));
            AddGradientQuad(
                drawList,
                WowImGuiController.TextureId(
                    _textures.WhiteTexture,
                    texture.BlendMode,
                    texture.Desaturation),
                ul,
                ur,
                lr,
                ll,
                gradient.Orientation.Equals("VERTICAL", StringComparison.OrdinalIgnoreCase)
                    ? max
                    : min,
                max,
                gradient.Orientation.Equals("VERTICAL", StringComparison.OrdinalIgnoreCase)
                    ? min
                    : max,
                min);
            return;
        }

        IReadOnlyList<Vector2> uv = statusBarFill?.NormalizedUv is { } fillUv
            ? fillUv
                .Select(point => TextureUv(texture, point.X, point.Y))
                .ToArray()
            : texture.Uv;
        var maskBindings =
            new List<WowImGuiController.TextureMaskDrawBinding>(3);
        if ((value.MaskTextureIds.Count > 0 || texture.LegacyMaskAsset is not null) &&
            !texture.PortraitDisableMasking)
        {
            var masks = value.MaskTextureIds
                .Select(ui.Find)
                .Where(mask =>
                    mask?.Texture is not null &&
                    ui.IsVisible(mask))
                .Select(mask => new UiTextureMask(
                    mask!.Texture!,
                    TextureQuad(
                        ui,
                        mask,
                        ui.ResolveBounds(mask.Id),
                        ui.LayoutScale(mask))))
                .ToList();
            if (texture.LegacyMaskAsset is { } legacyMaskAsset)
            {
                masks.Add(new UiTextureMask(
                    new UiTextureState { Asset = legacyMaskAsset },
                    quad));
            }
            if (masks.Count > 0)
            {
                foreach (var mask in masks.Take(3))
                {
                    var upperLeftUv = MaskUvAt(mask, quad.UpperLeft);
                    var upperRightUv = MaskUvAt(mask, quad.UpperRight);
                    var lowerLeftUv = MaskUvAt(mask, quad.LowerLeft);
                    if (!UiTextureMaskShaderTransform.TryResolve(
                            ul,
                            ur,
                            ll,
                            upperLeftUv,
                            upperRightUv,
                            lowerLeftUv,
                            out var transform))
                    {
                        continue;
                    }
                    maskBindings.Add(
                        new WowImGuiController.TextureMaskDrawBinding(
                            _textures.Resolve(mask.Texture),
                            transform));
                }
            }
        }
        var textureId = WowImGuiController.TextureId(
            handle,
            texture.BlendMode,
            texture.Desaturation);
        if (maskBindings.Count > 0)
        {
            textureId = WowImGuiController.TextureMaskTextureId(
                textureId,
                maskBindings);
        }
        var sourceLogicalWidth =
            texture.AtlasWidth ?? texture.IntrinsicWidth;
        var sourceLogicalHeight =
            texture.AtlasHeight ?? texture.IntrinsicHeight;
        if (texture.SliceData is { } slice &&
            sourceLogicalWidth is > 0 &&
            sourceLogicalHeight is > 0)
        {
            var logicalSourceSize = TextureSourceLogicalSize(
                texture,
                sourceLogicalWidth.Value,
                sourceLogicalHeight.Value);
            var sourcePixelSize =
                _textures.TryGetSourcePixelSize(texture, out var resolvedPixelSize)
                    ? resolvedPixelSize
                    : logicalSourceSize;
            var density = new Vector2(
                sourcePixelSize.X / Math.Max(logicalSourceSize.X, 1e-6f),
                sourcePixelSize.Y / Math.Max(logicalSourceSize.Y, 1e-6f));
            var quadSize = TextureQuadSize(quad);
            var sliceParameters = UiTextureSliceShaderParameters.Resolve(
                new Vector2(
                    quadSize.X * density.X / Math.Max(effectiveScale, 1e-6f),
                    quadSize.Y * density.Y / Math.Max(effectiveScale, 1e-6f)),
                sourcePixelSize,
                new UiInsets(
                    Math.Max(0, slice.MarginLeft) * density.X,
                    Math.Max(0, slice.MarginRight) * density.X,
                    Math.Max(0, slice.MarginTop) * density.Y,
                    Math.Max(0, slice.MarginBottom) * density.Y),
                slice.Mode);
            RenderSlicedTexture(
                drawList,
                textureId,
                value,
                texture,
                sliceParameters,
                bounds,
                effectiveScale,
                hasAnimationRotation,
                ui,
                origin,
                scale,
                color);
            return;
        }
        if ((horizontallyTiled || verticallyTiled) &&
            texture.AtlasWidth is > 0 &&
            texture.AtlasHeight is > 0)
        {
            RenderTiledTexture(
                drawList,
                textureId,
                value,
                texture,
                bounds,
                effectiveScale,
                horizontallyTiled,
                verticallyTiled,
                hasAnimationRotation,
                ui,
                origin,
                scale,
                color);
            return;
        }

        drawList.AddImageQuad(
            textureId,
            ul,
            ur,
            lr,
            ll,
            uv[0],
            uv[2],
            uv[3],
            uv[1],
            color);
    }

    private static Vector2 MaskUvAt(UiTextureMask mask, Vector2 subjectPoint) =>
        UiTextureMaskShaderTransform.InterpolateUv(
            mask.Texture.Uv,
            UiTextureMaskShaderTransform.ProjectIntoQuad(
                mask.Quad.UpperLeft,
                mask.Quad.UpperRight,
                mask.Quad.LowerLeft,
                subjectPoint));

    private void RenderColorSelectWheel(
        ImDrawListPtr drawList,
        UiSystem ui,
        UiObject value,
        UiTextureState texture,
        Vector2 origin,
        float scale)
    {
        const int segmentCount = 128;
        var bounds = ui.ResolveBounds(value.Id);
        var quad = TextureQuad(ui, value, bounds, ui.LayoutScale(value));
        var center = MapColorWheelPoint(quad, Vector2.Zero);
        var centerColor = texture.VertexColor;
        centerColor.W *= EffectiveAlpha(ui, value);
        var centerPacked = ImGui.ColorConvertFloat4ToU32(
            Vector4.Clamp(centerColor, Vector4.Zero, Vector4.One));
        var textureId = WowImGuiController.TextureId(
            _textures.WhiteTexture,
            texture.BlendMode,
            texture.Desaturation);

        for (var index = 0; index < segmentCount; index++)
        {
            var hue0 = index * (360f / segmentCount);
            var hue1 = (index + 1) * (360f / segmentCount);
            var angle0 = hue0 * MathF.PI / 180;
            var angle1 = hue1 * MathF.PI / 180;
            var point0 = MapColorWheelPoint(
                quad,
                new Vector2(-MathF.Cos(angle0), -MathF.Sin(angle0)));
            var point1 = MapColorWheelPoint(
                quad,
                new Vector2(-MathF.Cos(angle1), -MathF.Sin(angle1)));
            var color0 = ColorWheelEdgeColor(hue0) * texture.VertexColor;
            var color1 = ColorWheelEdgeColor(hue1 == 360 ? 0 : hue1) *
                         texture.VertexColor;
            var alpha = EffectiveAlpha(ui, value);
            color0.W *= alpha;
            color1.W *= alpha;
            AddGradientQuad(
                drawList,
                textureId,
                ToScreen(center, ui, origin, scale),
                ToScreen(point0, ui, origin, scale),
                ToScreen(point1, ui, origin, scale),
                ToScreen(center, ui, origin, scale),
                centerPacked,
                ImGui.ColorConvertFloat4ToU32(
                    Vector4.Clamp(color0, Vector4.Zero, Vector4.One)),
                ImGui.ColorConvertFloat4ToU32(
                    Vector4.Clamp(color1, Vector4.Zero, Vector4.One)),
                centerPacked);
        }
    }

    private static Vector2 MapColorWheelPoint(
        UiTextureQuad quad,
        Vector2 normalized)
    {
        var horizontal = (normalized.X + 1) * 0.5f;
        var vertical = (normalized.Y + 1) * 0.5f;
        var left = Vector2.Lerp(quad.LowerLeft, quad.UpperLeft, vertical);
        var right = Vector2.Lerp(quad.LowerRight, quad.UpperRight, vertical);
        return Vector2.Lerp(left, right, horizontal);
    }

    private static Vector4 ColorWheelEdgeColor(float hue)
    {
        var scaled = hue / 60;
        var sector = Math.Min((int)scaled, 5);
        var fraction = scaled - sector;
        var q = 1 - fraction;
        var t = fraction;
        var rgb = sector switch
        {
            0 => new Vector3(1, t, 0),
            1 => new Vector3(q, 1, 0),
            2 => new Vector3(0, 1, t),
            3 => new Vector3(0, q, 1),
            4 => new Vector3(t, 0, 1),
            _ => new Vector3(1, 0, q)
        };
        rgb = new Vector3(
            MathF.Truncate(rgb.X * 255) / 255,
            MathF.Truncate(rgb.Y * 255) / 255,
            MathF.Truncate(rgb.Z * 255) / 255);
        return new Vector4(rgb, 1);
    }

    private static void RenderSlicedTexture(
        ImDrawListPtr drawList,
        nint textureId,
        UiObject value,
        UiTextureState texture,
        UiTextureSliceShaderParameters slice,
        UiRect bounds,
        float effectiveScale,
        bool hasAnimationRotation,
        UiSystem ui,
        Vector2 origin,
        float scale,
        uint color)
    {
        var horizontal =
            new[] { 0f, slice.DestinationLeft, slice.DestinationRight, 1f };
        var vertical =
            new[] { 0f, slice.DestinationTop, slice.DestinationBottom, 1f };
        var sourceHorizontal =
            new[] { 0f, slice.SourceLeft, slice.SourceRight, 1f };
        var sourceVertical =
            new[] { 0f, slice.SourceTop, slice.SourceBottom, 1f };

        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 3; column++)
            {
                RenderSlicePiece(
                    drawList,
                    textureId,
                    texture,
                    bounds,
                    horizontal[column],
                    horizontal[column + 1],
                    vertical[row],
                    vertical[row + 1],
                    sourceHorizontal[column],
                    sourceHorizontal[column + 1],
                    sourceVertical[row],
                    sourceVertical[row + 1],
                    slice.Mode == UiTextureSliceMode.Tiled && column == 1,
                    slice.Mode == UiTextureSliceMode.Tiled && row == 1,
                    slice.CenterRepeat.X,
                    slice.CenterRepeat.Y,
                    slice.HalfTexelX,
                    slice.HalfTexelY,
                    effectiveScale,
                    ui,
                    value,
                    hasAnimationRotation,
                    origin,
                    scale,
                    color);
            }
        }
    }

    private static void RenderSlicePiece(
        ImDrawListPtr drawList,
        nint textureId,
        UiTextureState texture,
        UiRect bounds,
        float destinationLeft,
        float destinationRight,
        float destinationTop,
        float destinationBottom,
        float sourceLeft,
        float sourceRight,
        float sourceTop,
        float sourceBottom,
        bool tileHorizontal,
        bool tileVertical,
        float repeatHorizontal,
        float repeatVertical,
        float halfTexelHorizontal,
        float halfTexelVertical,
        float effectiveScale,
        UiSystem ui,
        UiObject value,
        bool hasAnimationRotation,
        Vector2 origin,
        float scale,
        uint color)
    {
        var left = bounds.Left + destinationLeft * bounds.Width;
        var right = bounds.Left + destinationRight * bounds.Width;
        var top = bounds.Top - destinationTop * bounds.Height;
        var bottom = bounds.Top - destinationBottom * bounds.Height;
        var width = right - left;
        var height = top - bottom;
        if (width <= 0 || height <= 0)
            return;

        var tileWidth =
            tileHorizontal && repeatHorizontal > 0
                ? width / repeatHorizontal
                : width;
        var tileHeight =
            tileVertical && repeatVertical > 0
                ? height / repeatVertical
                : height;
        if (!float.IsFinite(tileWidth) ||
            !float.IsFinite(tileHeight) ||
            tileWidth <= 0 ||
            tileHeight <= 0)
        {
            return;
        }
        var sampledSourceLeft =
            tileHorizontal ? sourceLeft + halfTexelHorizontal : sourceLeft;
        var sampledSourceRight =
            tileHorizontal ? sourceRight - halfTexelHorizontal : sourceRight;
        var sampledSourceTop =
            tileVertical ? sourceTop + halfTexelVertical : sourceTop;
        var sampledSourceBottom =
            tileVertical ? sourceBottom - halfTexelVertical : sourceBottom;

        for (var y = 0f; y < height; y += tileHeight)
        {
            var drawnHeight = Math.Min(tileHeight, height - y);
            var sourceBottomFraction = drawnHeight / tileHeight;
            for (var x = 0f; x < width; x += tileWidth)
            {
                var drawnWidth = Math.Min(tileWidth, width - x);
                var sourceRightFraction = drawnWidth / tileWidth;
                var x0 = left + x;
                var x1 = x0 + drawnWidth;
                var y0 = top - y;
                var y1 = y0 - drawnHeight;
                var u1 = float.Lerp(
                    sampledSourceLeft,
                    sampledSourceRight,
                    sourceRightFraction);
                var v1 = float.Lerp(
                    sampledSourceTop,
                    sampledSourceBottom,
                    sourceBottomFraction);
                var upperLeft = TextureVertex(
                    texture,
                    bounds,
                    (x0 - bounds.Left) / bounds.Width,
                    (bounds.Top - y0) / bounds.Height,
                    effectiveScale);
                var upperRight = TextureVertex(
                    texture,
                    bounds,
                    (x1 - bounds.Left) / bounds.Width,
                    (bounds.Top - y0) / bounds.Height,
                    effectiveScale);
                var lowerRight = TextureVertex(
                    texture,
                    bounds,
                    (x1 - bounds.Left) / bounds.Width,
                    (bounds.Top - y1) / bounds.Height,
                    effectiveScale);
                var lowerLeft = TextureVertex(
                    texture,
                    bounds,
                    (x0 - bounds.Left) / bounds.Width,
                    (bounds.Top - y1) / bounds.Height,
                    effectiveScale);
                if (hasAnimationRotation)
                {
                    upperLeft = ApplyAnimationRotations(ui, value, upperLeft);
                    upperRight = ApplyAnimationRotations(ui, value, upperRight);
                    lowerRight = ApplyAnimationRotations(ui, value, lowerRight);
                    lowerLeft = ApplyAnimationRotations(ui, value, lowerLeft);
                }

                drawList.AddImageQuad(
                    textureId,
                    SnapTexturePosition(
                        ToScreen(upperLeft, ui, origin, scale),
                        texture),
                    SnapTexturePosition(
                        ToScreen(upperRight, ui, origin, scale),
                        texture),
                    SnapTexturePosition(
                        ToScreen(lowerRight, ui, origin, scale),
                        texture),
                    SnapTexturePosition(
                        ToScreen(lowerLeft, ui, origin, scale),
                        texture),
                    TextureUv(texture, sampledSourceLeft, sampledSourceTop),
                    TextureUv(texture, u1, sampledSourceTop),
                    TextureUv(texture, u1, v1),
                    TextureUv(texture, sampledSourceLeft, v1),
                    color);
            }
        }
    }

    private static void RenderTiledTexture(
        ImDrawListPtr drawList,
        nint textureId,
        UiObject value,
        UiTextureState texture,
        UiRect bounds,
        float effectiveScale,
        bool horizontallyTiled,
        bool verticallyTiled,
        bool hasAnimationRotation,
        UiSystem ui,
        Vector2 origin,
        float scale,
        uint color)
    {
        var tileWidth = horizontallyTiled
            ? texture.AtlasWidth!.Value * effectiveScale
            : bounds.Width;
        var tileHeight = verticallyTiled
            ? texture.AtlasHeight!.Value * effectiveScale
            : bounds.Height;
        if (tileWidth <= 0 || tileHeight <= 0 || bounds.Width <= 0 || bounds.Height <= 0)
            return;

        for (var topOffset = 0f; topOffset < bounds.Height; topOffset += tileHeight)
        {
            var drawnHeight = Math.Min(tileHeight, bounds.Height - topOffset);
            var v1 = drawnHeight / tileHeight;
            for (var leftOffset = 0f; leftOffset < bounds.Width; leftOffset += tileWidth)
            {
                var drawnWidth = Math.Min(tileWidth, bounds.Width - leftOffset);
                var u1 = drawnWidth / tileWidth;

                var normalizedLeft = leftOffset / bounds.Width;
                var normalizedRight = (leftOffset + drawnWidth) / bounds.Width;
                var normalizedTop = topOffset / bounds.Height;
                var normalizedBottom = (topOffset + drawnHeight) / bounds.Height;
                var upperLeft = TextureVertex(
                    texture,
                    bounds,
                    normalizedLeft,
                    normalizedTop,
                    effectiveScale);
                var upperRight = TextureVertex(
                    texture,
                    bounds,
                    normalizedRight,
                    normalizedTop,
                    effectiveScale);
                var lowerRight = TextureVertex(
                    texture,
                    bounds,
                    normalizedRight,
                    normalizedBottom,
                    effectiveScale);
                var lowerLeft = TextureVertex(
                    texture,
                    bounds,
                    normalizedLeft,
                    normalizedBottom,
                    effectiveScale);
                if (hasAnimationRotation)
                {
                    upperLeft = ApplyAnimationRotations(ui, value, upperLeft);
                    upperRight = ApplyAnimationRotations(ui, value, upperRight);
                    lowerRight = ApplyAnimationRotations(ui, value, lowerRight);
                    lowerLeft = ApplyAnimationRotations(ui, value, lowerLeft);
                }

                drawList.AddImageQuad(
                    textureId,
                    SnapTexturePosition(
                        ToScreen(upperLeft, ui, origin, scale),
                        texture),
                    SnapTexturePosition(
                        ToScreen(upperRight, ui, origin, scale),
                        texture),
                    SnapTexturePosition(
                        ToScreen(lowerRight, ui, origin, scale),
                        texture),
                    SnapTexturePosition(
                        ToScreen(lowerLeft, ui, origin, scale),
                        texture),
                    TextureUv(texture, 0, 0),
                    TextureUv(texture, u1, 0),
                    TextureUv(texture, u1, v1),
                    TextureUv(texture, 0, v1),
                    color);
            }
        }
    }

    private static Vector2 TextureVertex(
        UiTextureState texture,
        UiRect bounds,
        float horizontal,
        float vertical,
        float effectiveScale)
    {
        var upperLeft = new Vector2(bounds.Left, bounds.Top) +
                        texture.VertexOffsets[0] * effectiveScale;
        var lowerLeft = new Vector2(bounds.Left, bounds.Bottom) +
                        texture.VertexOffsets[1] * effectiveScale;
        var upperRight = new Vector2(bounds.Right, bounds.Top) +
                         texture.VertexOffsets[2] * effectiveScale;
        var lowerRight = new Vector2(bounds.Right, bounds.Bottom) +
                         texture.VertexOffsets[3] * effectiveScale;
        var top = Vector2.Lerp(upperLeft, upperRight, horizontal);
        var bottom = Vector2.Lerp(lowerLeft, lowerRight, horizontal);
        var result = Vector2.Lerp(top, bottom, vertical);
        return MathF.Abs(texture.Rotation) > 0.00001f
            ? Rotate(result, TextureRotationCenter(texture, bounds), texture.Rotation)
            : result;
    }

    private static Vector2 SnapTexturePosition(
        Vector2 position,
        UiTextureState texture)
    {
        if (texture.TexelSnappingBias <= 0 && !texture.SnapToPixelGrid)
            return position;

        var framebufferScale = ImGui.GetIO().DisplayFramebufferScale;
        var scaleX = Math.Max(framebufferScale.X, 1e-6f);
        var scaleY = Math.Max(framebufferScale.Y, 1e-6f);
        return new Vector2(
            MathF.Round(position.X * scaleX) / scaleX,
            MathF.Round(position.Y * scaleY) / scaleY);
    }

    private static unsafe void AddGradientQuad(
        ImDrawListPtr drawList,
        nint textureId,
        Vector2 upperLeft,
        Vector2 upperRight,
        Vector2 lowerRight,
        Vector2 lowerLeft,
        uint upperLeftColor,
        uint upperRightColor,
        uint lowerRightColor,
        uint lowerLeftColor)
    {
        var firstVertex = drawList.VtxBuffer.Size;
        drawList.AddImageQuad(
            textureId,
            upperLeft,
            upperRight,
            lowerRight,
            lowerLeft,
            Vector2.Zero,
            Vector2.Zero,
            Vector2.Zero,
            Vector2.Zero,
            uint.MaxValue);
        var vertices = (ImDrawVert*)drawList.VtxBuffer.Data;
        vertices[firstVertex].col = upperLeftColor;
        vertices[firstVertex + 1].col = upperRightColor;
        vertices[firstVertex + 2].col = lowerRightColor;
        vertices[firstVertex + 3].col = lowerLeftColor;
    }

    private static UiTextureQuad TextureQuad(
        UiSystem ui,
        UiObject value,
        UiRect bounds,
        float effectiveScale)
    {
        var texture = value.Texture!;
        var upperLeft = new Vector2(bounds.Left, bounds.Top) +
                        texture.VertexOffsets[0] * effectiveScale;
        var lowerLeft = new Vector2(bounds.Left, bounds.Bottom) +
                        texture.VertexOffsets[1] * effectiveScale;
        var upperRight = new Vector2(bounds.Right, bounds.Top) +
                         texture.VertexOffsets[2] * effectiveScale;
        var lowerRight = new Vector2(bounds.Right, bounds.Bottom) +
                         texture.VertexOffsets[3] * effectiveScale;
        if (MathF.Abs(texture.Rotation) > 0.00001f)
        {
            var rotationCenter = TextureRotationCenter(texture, bounds);
            upperLeft = Rotate(upperLeft, rotationCenter, texture.Rotation);
            lowerLeft = Rotate(lowerLeft, rotationCenter, texture.Rotation);
            upperRight = Rotate(upperRight, rotationCenter, texture.Rotation);
            lowerRight = Rotate(lowerRight, rotationCenter, texture.Rotation);
        }
        if (HasAnimationRotation(ui, value))
        {
            upperLeft = ApplyAnimationRotations(ui, value, upperLeft);
            lowerLeft = ApplyAnimationRotations(ui, value, lowerLeft);
            upperRight = ApplyAnimationRotations(ui, value, upperRight);
            lowerRight = ApplyAnimationRotations(ui, value, lowerRight);
        }
        return new UiTextureQuad(upperLeft, lowerLeft, upperRight, lowerRight);
    }

    private static Vector2 TextureQuadSize(UiTextureQuad quad) =>
        new(
            Math.Max(
                Vector2.Distance(quad.UpperLeft, quad.UpperRight),
                Vector2.Distance(quad.LowerLeft, quad.LowerRight)),
            Math.Max(
                Vector2.Distance(quad.UpperLeft, quad.LowerLeft),
                Vector2.Distance(quad.UpperRight, quad.LowerRight)));

    private static Vector2 TextureSourceLogicalSize(
        UiTextureState texture,
        float fullWidth,
        float fullHeight)
    {
        var fullSize = new Vector2(fullWidth, fullHeight);
        var upperLeft = texture.LocalUv[0] * fullSize;
        var lowerLeft = texture.LocalUv[1] * fullSize;
        var upperRight = texture.LocalUv[2] * fullSize;
        var lowerRight = texture.LocalUv[3] * fullSize;
        return new Vector2(
            Math.Max(
                Vector2.Distance(upperLeft, upperRight),
                Vector2.Distance(lowerLeft, lowerRight)),
            Math.Max(
                Vector2.Distance(upperLeft, lowerLeft),
                Vector2.Distance(upperRight, lowerRight)));
    }

    private static Vector2 TextureRotationCenter(UiTextureState texture, UiRect bounds) =>
        new(
            bounds.Left + bounds.Width * texture.RotationPoint.X,
            bounds.Bottom + bounds.Height * texture.RotationPoint.Y);

    private static Vector2 TextureUv(UiTextureState texture, float horizontal, float vertical)
    {
        var top = Vector2.Lerp(texture.Uv[0], texture.Uv[2], horizontal);
        var bottom = Vector2.Lerp(texture.Uv[1], texture.Uv[3], horizontal);
        return Vector2.Lerp(top, bottom, vertical);
    }

    private void RenderFont(
        ImDrawListPtr drawList,
        UiSystem ui,
        UiObject value,
        Vector2 origin,
        float scale)
    {
        var font = value.Font!;
        var isEditBox = value.ObjectType.EndsWith(
            "EditBox",
            StringComparison.OrdinalIgnoreCase);
        if (isEditBox)
            value.EditBoxCaretStops.Clear();
        var renderText = isEditBox && value.EditBoxPassword
            ? new string('*', Encoding.UTF8.GetByteCount(value.TextValue))
            : font.Text;
        var hasFocusedCaret = isEditBox && ui.FocusedObjectId == value.Id;
        if (string.IsNullOrEmpty(renderText) && !hasFocusedCaret)
            return;

        var effectiveScale = ui.LayoutScale(value);
        var framebufferScale = ImGui.GetIO().DisplayFramebufferScale;
        var framebufferScaleX = Math.Max(framebufferScale.X, 1e-6f);
        var framebufferScaleY = Math.Max(framebufferScale.Y, 1e-6f);
        var physicalPixelsPerUiUnit = effectiveScale * scale * framebufferScaleY;
        var rasterFontSize = UiTextLineMetrics.ResolvePhysicalRasterHeight(
            font.FontSize,
            font.TextScale,
            physicalPixelsPerUiUnit);
        var fontSize = UiTextLineMetrics.ResolvePhysicalRenderHeight(
                           font.FontSize,
                           font.TextScale,
                           physicalPixelsPerUiUnit,
                           value.FontSmoothScaling) /
                       framebufferScaleY *
                       value.FontAnimationFontSizeScale;
        if (!(rasterFontSize > 0) || !(fontSize > 0))
            return;
        var continuationIndent = font.IndentedWordWrap
            ? UiTextLineMetrics.IndentedWordWrapPixels / framebufferScaleX
            : 0;
        var renderFont = _fonts.Select(font.FontPath, rasterFontSize);
        var imguiFont = renderFont.Font;
        var glyphFontSize = renderFont.GlyphSize * fontSize / rasterFontSize;
        var textBounds = ui.ResolveTextBounds(value);
        var widthConstrained = UiSystem.IsWidthConstrained(value);
        var heightConstrained = UiSystem.IsHeightConstrained(value);
        var physicalSpacing =
            font.Spacing * effectiveScale * scale * framebufferScaleY;
        var lineAdvance =
            fontSize +
            UiTextLineMetrics.QuantizePhysicalSpacing(physicalSpacing) /
            framebufferScaleY;
        if (!isEditBox &&
            (widthConstrained || heightConstrained || font.MaximumLines > 0))
        {
            var availableWidth = widthConstrained
                ? textBounds.Width * scale
                : float.PositiveInfinity;
            var availableHeight = textBounds.Height * scale;
            var fittingHeight = font.MaximumLines > 0
                ? font.MaximumLines * lineAdvance
                : Math.Max(lineAdvance, availableHeight);
            renderText = UiDisplayTextFitter.Resolve(
                renderText,
                candidate =>
                {
                    var candidateLines = SplitColorLines(
                        candidate,
                        value.FontFixedColor);
                    if (font.WordWrap && widthConstrained && availableWidth > 0)
                    {
                        candidateLines = WrapColorLines(
                            candidateLines,
                            imguiFont,
                            glyphFontSize,
                            availableWidth,
                            continuationIndent,
                            font.NonSpaceWrap);
                    }
                    if (candidateLines.Count == 0)
                        return true;

                    var candidateWidth = candidateLines
                        .Select((line, index) =>
                            imguiFont.CalcTextSizeA(
                                glyphFontSize,
                                float.MaxValue,
                                0,
                                string.Concat(line.Select(span => span.Text))).X +
                            (index > 0 ? continuationIndent : 0))
                        .DefaultIfEmpty(0)
                        .Max();
                    var candidateHeight =
                        fontSize +
                        Math.Max(0, candidateLines.Count - 1) * lineAdvance;
                    return (!widthConstrained ||
                            candidateWidth <= availableWidth + 0.001f) &&
                           (!heightConstrained && font.MaximumLines == 0 ||
                            candidateHeight <= fittingHeight + 0.001f);
                }).Text;
        }

        var lines = SplitColorLines(renderText, value.FontFixedColor);
        if (font.WordWrap && widthConstrained && textBounds.Width > 0)
        {
            lines = WrapColorLines(
                lines,
                imguiFont,
                glyphFontSize,
                textBounds.Width * scale,
                continuationIndent,
                font.NonSpaceWrap);
        }
        if (font.MaximumLines > 0 && lines.Count > font.MaximumLines)
            lines.RemoveRange(font.MaximumLines, lines.Count - font.MaximumLines);
        if (lines.Count == 0)
            return;

        var widths = lines
            .Select(line => imguiFont.CalcTextSizeA(
                glyphFontSize,
                float.MaxValue,
                0,
                string.Concat(line.Select(span => span.Text))).X)
            .ToArray();
        var maximumWidth = widths
            .Select((width, index) =>
                width + (index > 0 ? continuationIndent : 0))
            .DefaultIfEmpty(0)
            .Max();
        var totalHeight = fontSize + Math.Max(0, lines.Count - 1) * lineAdvance;
        var topLeft = ui.ResolveTextTopLeft(
            value,
            maximumWidth / scale,
            totalHeight * value.FontAnimationVertexScale / scale);
        var blockPosition = ToScreen(topLeft, ui, origin, scale);
        blockPosition.X += UiTextLineMetrics.ResolvePhysicalHorizontalAnchorOffset(
            font.JustifyHorizontal,
            maximumWidth);
        blockPosition = UiTextLineMetrics.SnapTopDownPhysicalOrigin(
            blockPosition,
            value.FontSmoothScaling,
            framebufferScale);
        var textPosition = blockPosition;
        if (isEditBox &&
            !value.MultiLine &&
            font.JustifyHorizontal.Equals("LEFT", StringComparison.OrdinalIgnoreCase))
        {
            var displayText = WowTextMarkup.PlainText(renderText)
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n');
            var availableWidth = Math.Max(0, textBounds.Width * scale - 1);
            var cursor = Math.Clamp(
                EditBoxDisplayOffset(value, value.CursorPosition),
                0,
                displayText.Length);
            var displayStart = Math.Clamp(
                value.EditBoxDisplayStart,
                0,
                displayText.Length);
            if (maximumWidth <= availableWidth)
            {
                displayStart = 0;
            }
            else
            {
                if (cursor < displayStart)
                    displayStart = cursor;
                while (displayStart < cursor &&
                       MeasureTextRange(
                           imguiFont,
                           glyphFontSize,
                           displayText,
                           displayStart,
                           cursor) > availableWidth)
                {
                    displayStart = NextUtf16Boundary(displayText, displayStart);
                }
            }

            value.EditBoxDisplayStart = displayStart;
            textPosition.X -= MeasureTextPrefix(
                imguiFont,
                glyphFontSize,
                displayText,
                displayStart);
        }
        else if (isEditBox)
        {
            value.EditBoxDisplayStart = 0;
        }

        var outlineRadius = font.FontFlags.Contains("THICKOUTLINE", StringComparison.OrdinalIgnoreCase)
            ? 2
            : font.FontFlags.Contains("OUTLINE", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        var effectiveAlpha = EffectiveAlpha(ui, value);
        var shadowColorValue = font.ShadowColor * value.VertexColor;
        shadowColorValue.W *= effectiveAlpha;
        shadowColorValue.W = UiTextShadow.BoundAlpha(
            shadowColorValue.W,
            font.Color.W * value.VertexColor.W * effectiveAlpha);
        var hasShadow = UiTextShadow.IsVisible(
            font.ShadowOffset,
            shadowColorValue);
        var shadowColor = hasShadow
            ? ImGui.ColorConvertFloat4ToU32(
                Vector4.Clamp(shadowColorValue, Vector4.Zero, Vector4.One))
            : 0;
        var shadowOffset = new Vector2(
            font.ShadowOffset.X,
            -font.ShadowOffset.Y) * effectiveScale * scale;
        if (isEditBox)
        {
            var clipUpperLeft = ToScreen(
                new Vector2(textBounds.Left, textBounds.Top),
                ui,
                origin,
                scale);
            var clipLowerRight = ToScreen(
                new Vector2(textBounds.Right, textBounds.Bottom),
                ui,
                origin,
                scale);
            drawList.PushClipRect(clipUpperLeft, clipLowerRight, true);
            RenderEditBoxSelectionAndCursor(
                drawList,
                ui,
                value,
                renderText,
                lines,
                widths,
                textPosition,
                lineAdvance,
                fontSize,
                glyphFontSize,
                imguiFont,
                effectiveAlpha,
                origin,
                scale,
                framebufferScale);
        }

        var textFirstVertex = drawList.VtxBuffer.Size;
        var gradientQuadOffset = 0;
        for (var lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            var horizontalOffset = UiTextLineMetrics.ResolvePhysicalLineOffset(
                font.JustifyHorizontal,
                widths[lineIndex] * framebufferScaleX,
                value.FontSmoothScaling,
                lineIndex > 0 && font.IndentedWordWrap) /
                framebufferScaleX;
            var cursor = textPosition + new Vector2(horizontalOffset, lineIndex * lineAdvance);
            foreach (var span in lines[lineIndex])
            {
                if (span.Text.Length == 0)
                    continue;

                var colorValue = span.Argb is { } argb
                    ? ArgbToColor(argb)
                    : font.Color;
                colorValue *= value.VertexColor;
                colorValue.W *= effectiveAlpha;
                var color = ImGui.ColorConvertFloat4ToU32(
                    Vector4.Clamp(colorValue, Vector4.Zero, Vector4.One));
                if (hasShadow)
                {
                    AddFontText(
                        drawList,
                        imguiFont,
                        glyphFontSize,
                        cursor + shadowOffset,
                        shadowColor,
                        span.Text,
                        value,
                        gradientQuadOffset);
                }
                if (outlineRadius > 0)
                {
                    var outlineColor = ImGui.ColorConvertFloat4ToU32(
                        new Vector4(0, 0, 0, Math.Clamp(colorValue.W, 0, 1)));
                    for (var offsetY = -outlineRadius; offsetY <= outlineRadius; offsetY++)
                    {
                        for (var offsetX = -outlineRadius; offsetX <= outlineRadius; offsetX++)
                        {
                            if (offsetX == 0 && offsetY == 0)
                                continue;
                            AddFontText(
                                drawList,
                                imguiFont,
                                glyphFontSize,
                                cursor + new Vector2(
                                    offsetX / framebufferScaleX,
                                    offsetY / framebufferScaleY),
                                outlineColor,
                                span.Text,
                                value,
                                gradientQuadOffset);
                        }
                    }
                }

                gradientQuadOffset += AddFontText(
                    drawList,
                    imguiFont,
                    glyphFontSize,
                    cursor,
                    color,
                    span.Text,
                    value,
                    gradientQuadOffset);
                cursor.X += imguiFont.CalcTextSizeA(
                    glyphFontSize,
                    float.MaxValue,
                    0,
                    span.Text).X;
            }

        }

        if (drawList.VtxBuffer.Size > textFirstVertex &&
            UiTextBlockRotation.IsActive(value.FontRotation))
        {
            RotateFontVertices(
                drawList,
                textFirstVertex,
                blockPosition,
                value.FontRotation);
        }

        if (drawList.VtxBuffer.Size > textFirstVertex &&
            MathF.Abs(value.FontAnimationVertexScale - 1) >= float.Epsilon)
        {
            ScaleFontVertices(
                drawList,
                textFirstVertex,
                blockPosition,
                value.FontAnimationVertexScale);
        }

        if (isEditBox)
            drawList.PopClipRect();
    }

    private static void RenderEditBoxSelectionAndCursor(
        ImDrawListPtr drawList,
        UiSystem ui,
        UiObject value,
        string renderText,
        IReadOnlyList<List<WowTextSpan>> lines,
        IReadOnlyList<float> widths,
        Vector2 position,
        float lineAdvance,
        float lineHeight,
        float glyphFontSize,
        ImFontPtr font,
        float effectiveAlpha,
        Vector2 origin,
        float scale,
        Vector2 framebufferScale)
    {
        var framebufferScaleX = Math.Max(framebufferScale.X, 1e-6f);
        var lineTexts = lines
            .Select(line => string.Concat(line.Select(span => span.Text)))
            .ToArray();
        var plainDisplayText = WowTextMarkup.PlainText(renderText)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        var lineStarts = ResolveRenderedLineStarts(plainDisplayText, lineTexts);
        PopulateEditBoxCaretStops(
            ui,
            value,
            lineTexts,
            lineStarts,
            widths,
            position,
            lineAdvance,
            lineHeight,
            glyphFontSize,
            font,
            origin,
            scale,
            framebufferScale);
        var selectionStart = EditBoxDisplayOffset(
            value,
            Math.Min(value.EditBoxHighlightStart, value.EditBoxHighlightEnd));
        var selectionEnd = EditBoxDisplayOffset(
            value,
            Math.Max(value.EditBoxHighlightStart, value.EditBoxHighlightEnd));

        if (!value.EditBoxPassword && selectionStart != selectionEnd)
        {
            var highlightColor = value.EditBoxHighlightColor;
            highlightColor.W *= effectiveAlpha;
            var packedHighlight = ImGui.ColorConvertFloat4ToU32(
                Vector4.Clamp(highlightColor, Vector4.Zero, Vector4.One));
            for (var index = 0; index < lineTexts.Length; index++)
            {
                var lineStart = lineStarts[index];
                var lineEnd = lineStart + lineTexts[index].Length;
                var start = Math.Max(selectionStart, lineStart);
                var end = Math.Min(selectionEnd, lineEnd);
                if (end <= start)
                    continue;

                var relativeStart = Math.Clamp(start - lineStart, 0, lineTexts[index].Length);
                var relativeEnd = Math.Clamp(end - lineStart, relativeStart, lineTexts[index].Length);
                var horizontalOffset = UiTextLineMetrics.ResolvePhysicalLineOffset(
                    value.Font!.JustifyHorizontal,
                    widths[index] * framebufferScaleX,
                    value.FontSmoothScaling,
                    index > 0 && value.Font.IndentedWordWrap) /
                    framebufferScaleX;
                var x1 = position.X + horizontalOffset + MeasureTextPrefix(
                    font,
                    glyphFontSize,
                    lineTexts[index],
                    relativeStart);
                var x2 = position.X + horizontalOffset + MeasureTextPrefix(
                    font,
                    glyphFontSize,
                    lineTexts[index],
                    relativeEnd);
                var y1 = position.Y + index * lineAdvance;
                drawList.AddRectFilled(
                    new Vector2(x1, y1),
                    new Vector2(x2, y1 + lineHeight),
                    packedHighlight);
            }
        }

        if (ui.FocusedObjectId != value.Id || !IsEditBoxCaretVisible(value))
            return;

        var cursor = EditBoxDisplayOffset(value, value.CursorPosition);
        var cursorLine = Math.Max(0, lineTexts.Length - 1);
        for (var index = 0; index < lineTexts.Length; index++)
        {
            var lineStart = lineStarts[index];
            var lineEnd = lineStart + lineTexts[index].Length;
            if (cursor <= lineEnd || index == lineTexts.Length - 1)
            {
                cursorLine = index;
                break;
            }
        }

        var cursorText = lineTexts[cursorLine];
        var cursorWithinLine = Math.Clamp(
            cursor - lineStarts[cursorLine],
            0,
            cursorText.Length);
        var cursorHorizontalOffset = UiTextLineMetrics.ResolvePhysicalLineOffset(
            value.Font!.JustifyHorizontal,
            widths[cursorLine] * framebufferScaleX,
            value.FontSmoothScaling,
            cursorLine > 0 && value.Font.IndentedWordWrap) /
            framebufferScaleX;
        var cursorX = MathF.Round((
                position.X +
                cursorHorizontalOffset +
                MeasureTextPrefix(font, glyphFontSize, cursorText, cursorWithinLine)) *
            framebufferScaleX) / framebufferScaleX;
        var cursorTop = position.Y + cursorLine * lineAdvance;
        var cursorColor = ImGui.ColorConvertFloat4ToU32(
            new Vector4(1, 1, 1, Math.Clamp(effectiveAlpha, 0, 1)));
        drawList.AddRectFilled(
            new Vector2(cursorX, cursorTop),
            new Vector2(
                cursorX + 1 / framebufferScaleX,
                cursorTop + lineHeight),
            cursorColor);
    }

    private static void PopulateEditBoxCaretStops(
        UiSystem ui,
        UiObject value,
        IReadOnlyList<string> lineTexts,
        IReadOnlyList<int> lineStarts,
        IReadOnlyList<float> widths,
        Vector2 position,
        float lineAdvance,
        float lineHeight,
        float glyphFontSize,
        ImFontPtr font,
        Vector2 origin,
        float scale,
        Vector2 framebufferScale)
    {
        value.EditBoxCaretStops.Clear();
        if (lineTexts.Count == 0 || scale <= 0)
            return;
        var framebufferScaleX = Math.Max(framebufferScale.X, 1e-6f);

        var rawPositionByDisplayOffset = new SortedDictionary<int, int>();
        for (var rawPosition = 0;
             rawPosition <= value.TextValue.Length;
             rawPosition = NextUtf16Boundary(value.TextValue, rawPosition))
        {
            rawPositionByDisplayOffset[
                EditBoxDisplayOffset(value, rawPosition)] = rawPosition;
            if (rawPosition == value.TextValue.Length)
                break;
        }

        foreach (var (displayOffset, rawPosition) in rawPositionByDisplayOffset)
        {
            for (var lineIndex = 0; lineIndex < lineTexts.Count; lineIndex++)
            {
                var lineStart = lineStarts[lineIndex];
                var lineEnd = lineStart + lineTexts[lineIndex].Length;
                if (displayOffset < lineStart || displayOffset > lineEnd)
                    continue;

                var relativeOffset = Math.Clamp(
                    displayOffset - lineStart,
                    0,
                    lineTexts[lineIndex].Length);
                var horizontalOffset = UiTextLineMetrics.ResolvePhysicalLineOffset(
                    value.Font!.JustifyHorizontal,
                    widths[lineIndex] * framebufferScaleX,
                    value.FontSmoothScaling,
                    lineIndex > 0 && value.Font.IndentedWordWrap) /
                    framebufferScaleX;
                var screenX =
                    position.X +
                    horizontalOffset +
                    MeasureTextPrefix(
                        font,
                        glyphFontSize,
                        lineTexts[lineIndex],
                        relativeOffset);
                var screenTop = position.Y + lineIndex * lineAdvance;
                var screenBottom = screenTop + lineHeight;
                value.EditBoxCaretStops.Add(
                    new UiEditBoxCaretStop(
                        rawPosition,
                        (screenX - origin.X) / scale,
                        ui.LogicalHeight - (screenBottom - origin.Y) / scale,
                        ui.LogicalHeight - (screenTop - origin.Y) / scale));
            }
        }
    }

    private static int[] ResolveRenderedLineStarts(
        string displayText,
        IReadOnlyList<string> lines)
    {
        var result = new int[lines.Count];
        var searchStart = 0;
        for (var index = 0; index < lines.Count; index++)
        {
            while (searchStart < displayText.Length && displayText[searchStart] == '\n')
                searchStart++;

            if (lines[index].Length == 0)
            {
                result[index] = searchStart;
                continue;
            }

            var match = displayText.IndexOf(
                lines[index],
                searchStart,
                StringComparison.Ordinal);
            result[index] = match >= 0 ? match : searchStart;
            searchStart = Math.Min(
                displayText.Length,
                result[index] + lines[index].Length);
        }
        return result;
    }

    private static int EditBoxDisplayOffset(UiObject value, int rawUtf16Position)
    {
        rawUtf16Position = Math.Clamp(rawUtf16Position, 0, value.TextValue.Length);
        var prefix = value.TextValue[..rawUtf16Position];
        if (value.EditBoxPassword)
            return Encoding.UTF8.GetByteCount(prefix);
        return WowTextMarkup.PlainText(prefix)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Length;
    }

    private static float MeasureTextPrefix(
        ImFontPtr font,
        float fontSize,
        string text,
        int length) =>
        length <= 0
            ? 0
            : font.CalcTextSizeA(
                fontSize,
                float.MaxValue,
                0,
                text[..Math.Min(length, text.Length)]).X;

    private static float MeasureTextRange(
        ImFontPtr font,
        float fontSize,
        string text,
        int start,
        int end)
    {
        start = Math.Clamp(start, 0, text.Length);
        end = Math.Clamp(end, start, text.Length);
        return end == start
            ? 0
            : font.CalcTextSizeA(
                fontSize,
                float.MaxValue,
                0,
                text[start..end]).X;
    }

    private static int NextUtf16Boundary(string value, int position)
    {
        position = Math.Clamp(position, 0, value.Length);
        if (position >= value.Length)
            return value.Length;
        if (char.IsHighSurrogate(value[position]) &&
            position + 1 < value.Length &&
            char.IsLowSurrogate(value[position + 1]))
        {
            return position + 2;
        }
        return position + 1;
    }

    private static bool IsEditBoxCaretVisible(UiObject value)
    {
        if (value.EditBoxBlinkSpeed <= 0)
            return true;
        var phase = ImGui.GetTime() % (value.EditBoxBlinkSpeed * 2);
        return phase < value.EditBoxBlinkSpeed;
    }

    private static unsafe int AddFontText(
        ImDrawListPtr drawList,
        ImFontPtr font,
        float fontSize,
        Vector2 position,
        uint color,
        string text,
        UiObject value,
        int gradientQuadOffset)
    {
        var firstVertex = drawList.VtxBuffer.Size;
        drawList.AddText(font, fontSize, position, color, text);
        var vertexCount = drawList.VtxBuffer.Size - firstVertex;
        var quadCount = vertexCount / 4;
        if (!UiTextAlphaGradient.IsActive(
                value.FontAlphaGradientStart,
                value.FontAlphaGradientLength))
        {
            return quadCount;
        }

        var vertices = (ImDrawVert*)drawList.VtxBuffer.Data;
        var baseAlpha = (byte)(color >> 24);
        for (var quadIndex = 0; quadIndex < quadCount; quadIndex++)
        {
            var firstQuadVertex = firstVertex + quadIndex * 4;
            var minimumX = float.MaxValue;
            var maximumX = float.MinValue;
            for (var localIndex = 0; localIndex < 4; localIndex++)
            {
                var x = vertices[firstQuadVertex + localIndex].pos.X;
                minimumX = Math.Min(minimumX, x);
                maximumX = Math.Max(maximumX, x);
            }

            var midpointX = (minimumX + maximumX) * 0.5f;
            var alpha = UiTextAlphaGradient.ResolveQuadAlpha(
                value.FontAlphaGradientStart,
                value.FontAlphaGradientLength,
                gradientQuadOffset + quadIndex,
                baseAlpha);
            for (var localIndex = 0; localIndex < 4; localIndex++)
            {
                var index = firstQuadVertex + localIndex;
                var vertex = vertices[index];
                var vertexAlpha = vertex.pos.X <= midpointX
                    ? alpha.Leading
                    : alpha.Trailing;
                vertex.col = vertex.col & 0x00FF_FFFFu | (uint)vertexAlpha << 24;
                vertices[index] = vertex;
            }
        }
        return quadCount;
    }

    private static unsafe void RotateFontVertices(
        ImDrawListPtr drawList,
        int firstVertex,
        Vector2 textBlockOrigin,
        float radians)
    {
        var vertices = (ImDrawVert*)drawList.VtxBuffer.Data;
        for (var index = firstVertex; index < drawList.VtxBuffer.Size; index++)
        {
            var vertex = vertices[index];
            vertex.pos = UiTextBlockRotation.RotateScreenPoint(
                vertex.pos,
                textBlockOrigin,
                radians);
            vertices[index] = vertex;
        }
    }

    private static unsafe void ScaleFontVertices(
        ImDrawListPtr drawList,
        int firstVertex,
        Vector2 textBlockOrigin,
        float scale)
    {
        var vertices = (ImDrawVert*)drawList.VtxBuffer.Data;
        for (var index = firstVertex; index < drawList.VtxBuffer.Size; index++)
        {
            var vertex = vertices[index];
            vertex.pos = textBlockOrigin + (vertex.pos - textBlockOrigin) * scale;
            vertices[index] = vertex;
        }
    }

    private void RenderLine(
        ImDrawListPtr drawList,
        UiSystem ui,
        UiObject value,
        Vector2 origin,
        float scale)
    {
        var line = value.Line!;
        if (line.Start is null || line.End is null)
            return;

        var quad = ui.ResolveLineQuad(value);
        var startPositive = ToScreen(quad[0], ui, origin, scale);
        var startNegative = ToScreen(quad[1], ui, origin, scale);
        var endPositive = ToScreen(quad[2], ui, origin, scale);
        var endNegative = ToScreen(quad[3], ui, origin, scale);

        var texture = line.Texture;
        var tint = texture.IsColor
            ? texture.Color * texture.VertexColor
            : texture.VertexColor;
        tint.W *= EffectiveAlpha(ui, value);
        var color = ImGui.ColorConvertFloat4ToU32(
            Vector4.Clamp(tint, Vector4.Zero, Vector4.One));
        var handle = _textures.Resolve(texture);
        drawList.AddImageQuad(
            WowImGuiController.TextureId(
                handle,
                texture.BlendMode,
                texture.Desaturation),
            startPositive,
            endPositive,
            endNegative,
            startNegative,
            texture.Uv[0],
            texture.Uv[2],
            texture.Uv[3],
            texture.Uv[1],
            color);
    }

    private static List<List<WowTextSpan>> SplitColorLines(string text, bool fixedColor)
    {
        var lines = new List<List<WowTextSpan>> { new() };
        foreach (var span in WowTextMarkup.ParseColorSpans(text))
        {
            var normalized = span.Text
                .Replace("\r\n", "\n", StringComparison.Ordinal)
                .Replace('\r', '\n');
            var parts = normalized.Split('\n');
            for (var index = 0; index < parts.Length; index++)
            {
                if (parts[index].Length > 0)
                    lines[^1].Add(new WowTextSpan(parts[index], fixedColor ? null : span.Argb));
                if (index + 1 < parts.Length)
                    lines.Add(new List<WowTextSpan>());
            }
        }
        return lines;
    }

    private static List<List<WowTextSpan>> WrapColorLines(
        IReadOnlyList<List<WowTextSpan>> source,
        ImFontPtr font,
        float fontSize,
        float maximumWidth,
        float continuationIndent,
        bool nonSpaceWrap)
    {
        if (maximumWidth <= 0)
            return source.Select(line => line.ToList()).ToList();

        var result = new List<List<WowTextSpan>>();
        foreach (var sourceLine in source)
        {
            var line = new List<WowTextSpan>();
            var lineWidth = 0f;
            float CurrentMaximumWidth() =>
                Math.Max(
                    0,
                    maximumWidth -
                    (result.Count > 0 ? continuationIndent : 0));

            void StartLine()
            {
                result.Add(line);
                line = [];
                lineWidth = 0;
            }

            void Append(string text, uint? argb)
            {
                if (text.Length == 0)
                    return;
                if (line.Count > 0 && line[^1].Argb == argb)
                    line[^1] = line[^1] with { Text = line[^1].Text + text };
                else
                    line.Add(new WowTextSpan(text, argb));
                lineWidth += font.CalcTextSizeA(fontSize, float.MaxValue, 0, text).X;
            }

            foreach (var span in sourceLine)
            {
                foreach (var token in TextTokens(span.Text))
                {
                    var whitespace = token.All(char.IsWhiteSpace);
                    if (whitespace && line.Count == 0)
                        continue;

                    var tokenWidth = font.CalcTextSizeA(
                        fontSize,
                        float.MaxValue,
                        0,
                        token).X;
                    if (line.Count > 0 &&
                        lineWidth + tokenWidth > CurrentMaximumWidth())
                    {
                        StartLine();
                        if (whitespace)
                            continue;
                    }

                    if (nonSpaceWrap &&
                        !whitespace &&
                        tokenWidth > CurrentMaximumWidth())
                    {
                        foreach (var character in token)
                        {
                            var text = character.ToString();
                            var characterWidth = font.CalcTextSizeA(
                                fontSize,
                                float.MaxValue,
                                0,
                                text).X;
                            if (line.Count > 0 &&
                                lineWidth + characterWidth >
                                CurrentMaximumWidth())
                                StartLine();
                            Append(text, span.Argb);
                        }
                    }
                    else
                    {
                        Append(token, span.Argb);
                    }
                }
            }

            result.Add(line);
        }
        return result;
    }

    private static IEnumerable<string> TextTokens(string text)
    {
        if (text.Length == 0)
            yield break;
        var start = 0;
        var whitespace = char.IsWhiteSpace(text[0]);
        for (var index = 1; index < text.Length; index++)
        {
            var nextWhitespace = char.IsWhiteSpace(text[index]);
            if (nextWhitespace == whitespace)
                continue;
            yield return text[start..index];
            start = index;
            whitespace = nextWhitespace;
        }
        yield return text[start..];
    }

    private static Vector4 ArgbToColor(uint argb) =>
        new(
            ((argb >> 16) & 0xff) / 255f,
            ((argb >> 8) & 0xff) / 255f,
            (argb & 0xff) / 255f,
            ((argb >> 24) & 0xff) / 255f);

    private static UiRect? ResolveClip(
        UiSystem ui,
        UiObject value,
        int? subtreeRootId)
    {
        if (subtreeRootId == value.Id)
            return null;

        UiRect? result = null;
        var child = value;
        var parentId = value.ParentId;
        while (parentId is { } id && ui.Find(id) is { } parent)
        {
            if (parent.ClipsChildren || parent.ScrollChildId == child.Id)
            {
                var next = ui.ResolveBounds(parent.Id);
                result = result is null ? next : Intersect(result.Value, next);
            }
            if (parent.Id == subtreeRootId)
                break;
            child = parent;
            parentId = parent.ParentId;
        }
        return result;
    }

    private static UiRect Intersect(UiRect left, UiRect right)
    {
        var x1 = Math.Max(left.Left, right.Left);
        var y1 = Math.Max(left.Bottom, right.Bottom);
        var x2 = Math.Min(left.Right, right.Right);
        var y2 = Math.Min(left.Top, right.Top);
        return new UiRect(x1, y1, Math.Max(0, x2 - x1), Math.Max(0, y2 - y1));
    }

    private static UiRect Union(UiRect left, UiRect right)
    {
        var x1 = Math.Min(left.Left, right.Left);
        var y1 = Math.Min(left.Bottom, right.Bottom);
        var x2 = Math.Max(left.Right, right.Right);
        var y2 = Math.Max(left.Top, right.Top);
        return new UiRect(x1, y1, x2 - x1, y2 - y1);
    }

    private static UiStatusBarFillResult? ResolveStatusBarTextureFill(
        UiSystem ui,
        UiObject texture,
        UiRect bounds,
        bool cropTexture)
    {
        if (texture.ParentId is not { } parentId ||
            ui.Find(parentId) is not { StatusBar: { } statusBar } parent ||
            statusBar.TextureId != texture.Id)
        {
            return null;
        }

        var range = statusBar.Maximum - statusBar.Minimum;
        var progress = parent.ObjectType.Equals(
            "StatusBar",
            StringComparison.OrdinalIgnoreCase)
            ? Math.Clamp(statusBar.DisplayNormalizedValue, 0, 1)
            : Math.Abs(range) < double.Epsilon
                ? statusBar.FillStyle == 1 ? 1 : 0
                : Math.Clamp((statusBar.Value - statusBar.Minimum) / range, 0, 1);
        return UiStatusBarFillGeometry.Resolve(
            bounds,
            progress,
            statusBar.Orientation,
            statusBar.FillStyle,
            statusBar.RotatesTexture,
            cropTexture,
            ui.NativeCoordinateUnitsPerLogicalUnit);
    }

    private static Vector2 Rotate(Vector2 point, Vector2 center, float radians)
    {
        var offset = point - center;
        var cosine = MathF.Cos(radians);
        var sine = MathF.Sin(radians);
        return center + new Vector2(
            offset.X * cosine - offset.Y * sine,
            offset.X * sine + offset.Y * cosine);
    }

    private static bool HasAnimationRotation(UiSystem ui, UiObject value)
    {
        for (UiObject? current = value;
             current is not null;
             current = current.ParentId is { } parentId ? ui.Find(parentId) : null)
        {
            if (MathF.Abs(current.AnimationRotation) > .00001f)
                return true;
        }
        return false;
    }

    private static Vector2 ApplyAnimationRotations(
        UiSystem ui,
        UiObject value,
        Vector2 point)
    {
        var hierarchy = new Stack<UiObject>();
        for (UiObject? current = value;
             current is not null;
             current = current.ParentId is { } parentId ? ui.Find(parentId) : null)
            hierarchy.Push(current);

        while (hierarchy.TryPop(out var current))
        {
            if (MathF.Abs(current.AnimationRotation) <= .00001f)
                continue;
            var bounds = ui.ResolveBounds(current.Id);
            var origin = ui.ResolveTransformOrigin(
                current,
                bounds,
                current.AnimationRotationOriginPoint,
                current.AnimationRotationOriginOffset);
            point = Rotate(point, origin, current.AnimationRotation);
        }
        return point;
    }

    private float EffectiveAlpha(UiSystem ui, UiObject value) =>
        ui.RenderAlpha(value, _activeFrameBufferOwnerId);

    private static Vector2 ToScreen(Vector2 logical, UiSystem ui, Vector2 origin, float scale) =>
        new(
            origin.X + logical.X * scale,
            origin.Y + (ui.LogicalHeight - logical.Y) * scale);
}
