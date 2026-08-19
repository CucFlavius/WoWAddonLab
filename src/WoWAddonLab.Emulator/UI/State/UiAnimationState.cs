using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiAnimationState
{
    public int Order { get; set; } = 1;
    public double Duration { get; set; }
    public double StartDelay { get; set; }
    public double EndDelay { get; set; }
    public string Smoothing { get; set; } = "NONE";
    public int PlaybackState { get; set; }
    public double Elapsed { get; set; }
    public double Progress { get; set; }
    public double SmoothProgress { get; set; }
    public bool ManuallyStopped { get; set; }
    public float FromAlpha { get; set; }
    public float ToAlpha { get; set; }
    public float Radians { get; set; }
    public float Degrees
    {
        get => Radians * 57.29578f;
        set => Radians = value * .017453292f;
    }
    public Vector2 Offset { get; set; }
    public Vector2 Scale { get; set; } = Vector2.One;
    public Vector2 ScaleFrom { get; set; } = Vector2.One;
    public Vector2 ScaleTo { get; set; } = Vector2.One;
    public bool HasScaleRange { get; set; }
    public string OriginPoint { get; set; } = "CENTER";
    public Vector2 OriginOffset { get; set; }
    public Vector4 StartColor { get; set; } = Vector4.One;
    public Vector4 EndColor { get; set; } = Vector4.One;
    public UiAnimationTargetMode TargetMode { get; set; }
    public string? TargetNameOrKey { get; set; }
    public int? TargetId { get; set; }
    public uint FlipBookRows { get; set; }
    public uint FlipBookColumns { get; set; }
    public uint FlipBookFrames { get; set; }
    public uint FlipBookFrameWidth { get; set; }
    public uint FlipBookFrameHeight { get; set; }
    public string PathCurveType { get; set; } = "NONE";
    public Vector2[]? InitialTargetLocalUv { get; set; }
}
