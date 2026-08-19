using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiMessageFrameMessage
{
    public required string Text { get; init; }
    public Vector3? Color { get; init; }
    public byte? Alpha { get; init; }
    public uint MessageId { get; init; }
}
