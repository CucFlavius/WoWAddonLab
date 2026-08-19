using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiCooldownState
{
    public bool HideCountdownNumbers { get; set; }
    public int CountdownAbbreviationThresholdMilliseconds { get; set; } = 120_000;
    public int CountdownMillisecondsThreshold { get; set; }
    public int MinimumCountdownDurationMilliseconds { get; set; } = 2_000;
    public string? CountdownFontName { get; set; } = "SystemFont_Shadow_Large_Outline";
    public int? CountdownFontStringId { get; set; }
    public int CountdownFormatterReference { get; set; }
    public bool UseAuraDisplayTime { get; set; }
    public int StartTimeMilliseconds { get; set; }
    public int DisplayDurationMilliseconds { get; set; }
    public float ModRate { get; set; } = 1;
    public bool ZeroDurationDisplay { get; set; }
    public bool UsesUnixClock { get; set; }
    public bool DrawSwipe { get; set; } = true;
    public bool DrawEdge { get; set; } = true;
    public bool DrawBling { get; set; } = true;
    public bool Reverse { get; set; }
    public bool Paused { get; set; }
    public int PausedElapsedMilliseconds { get; set; }
    public int ElapsedDisplayMilliseconds { get; set; }
    public bool CompletionBlingActive { get; set; }
    public float Rotation { get; set; }
    public Vector4 SwipeColor { get; set; }
    public Vector4 EdgeColor { get; set; }
    public Vector4 BlingColor { get; set; }
    public float EdgeScale { get; set; } = MathF.Sqrt(2);
    public string? SwipeTextureAsset { get; set; }
    public uint? SwipeTextureFileDataId { get; set; }
    public string? EdgeTextureAsset { get; set; }
    public uint? EdgeTextureFileDataId { get; set; }
    public string? BlingTextureAsset { get; set; }
    public uint? BlingTextureFileDataId { get; set; }
    public Vector2 TextureCoordinateLow { get; set; } = Vector2.Zero;
    public Vector2 TextureCoordinateHigh { get; set; } = Vector2.One;
}
