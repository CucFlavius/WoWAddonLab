using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiTextureState
{
    public string? Asset { get; set; }
    public string? LegacyMaskAsset { get; set; }
    public string? AtlasName { get; set; }
    public float? AtlasWidth { get; set; }
    public float? AtlasHeight { get; set; }
    public float? IntrinsicWidth { get; set; }
    public float? IntrinsicHeight { get; set; }
    public uint? FileDataId { get; set; }
    public string? PortraitUnitToken { get; set; }
    public bool PortraitDisableMasking { get; set; }
    public bool IsColor { get; set; }
    public Vector4 Color { get; set; } = Vector4.One;
    public Vector4 VertexColor { get; set; } = Vector4.One;
    public string BlendMode { get; set; } = "BLEND";
    public string WrapHorizontal { get; set; } = "CLAMP";
    public string WrapVertical { get; set; } = "CLAMP";
    public string FilterMode { get; set; } = "LINEAR";
    public bool BlockingLoadRequested { get; set; }
    public bool SnapToPixelGrid { get; set; } = true;
    public float TexelSnappingBias { get; set; } = 0.3f;
    public bool HorizontallyTiled { get; set; }
    public bool VerticallyTiled { get; set; }
    public bool IsColorSelectWheel { get; set; }
    public UiTextureSliceData? SliceData { get; set; }
    public float Desaturation { get; set; }
    public float Rotation { get; set; }
    public Vector2 RotationPoint { get; set; } = new(0.5f, 0.5f);
    public Vector2[] Uv { get; } =
    [
        new(0, 0),
        new(0, 1),
        new(1, 0),
        new(1, 1)
    ];
    public Vector2[] LocalUv { get; } =
    [
        new(0, 0),
        new(0, 1),
        new(1, 0),
        new(1, 1)
    ];
    public float? AtlasLeft { get; private set; }
    public float? AtlasRight { get; private set; }
    public float? AtlasTop { get; private set; }
    public float? AtlasBottom { get; private set; }
    public Vector2[] VertexOffsets { get; } = new Vector2[4];
    public (string Orientation, Vector4 Minimum, Vector4 Maximum)? Gradient { get; set; }

    public void SetAtlasRegion(float left, float right, float top, float bottom)
    {
        AtlasLeft = left;
        AtlasRight = right;
        AtlasTop = top;
        AtlasBottom = bottom;
        ResolveUv();
    }

    public void ClearAtlasRegion()
    {
        AtlasLeft = null;
        AtlasRight = null;
        AtlasTop = null;
        AtlasBottom = null;
        ResolveUv();
    }

    public void ResetTexCoord()
    {
        LocalUv[0] = new Vector2(0, 0);
        LocalUv[1] = new Vector2(0, 1);
        LocalUv[2] = new Vector2(1, 0);
        LocalUv[3] = new Vector2(1, 1);
        ResolveUv();
    }

    public void ResolveUv()
    {
        if (AtlasLeft is { } left &&
            AtlasRight is { } right &&
            AtlasTop is { } top &&
            AtlasBottom is { } bottom)
        {
            var width = right - left;
            var height = bottom - top;
            for (var index = 0; index < Uv.Length; index++)
            {
                Uv[index] = new Vector2(
                    left + LocalUv[index].X * width,
                    top + LocalUv[index].Y * height);
            }
            return;
        }

        for (var index = 0; index < Uv.Length; index++)
            Uv[index] = LocalUv[index];
    }
}
