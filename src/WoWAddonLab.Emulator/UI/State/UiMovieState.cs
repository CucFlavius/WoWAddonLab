using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed class UiMovieState
{
    public bool SubtitlesEnabled { get; set; }
    public int? RequestedMovieId { get; set; }
    public bool Looping { get; set; }
    public bool Playing { get; set; }
    public int ReturnCode { get; set; }
}
