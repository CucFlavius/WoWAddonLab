using System.Numerics;

namespace WoWAddonLab.Emulator.UI;

public sealed record UiDurationState(
    double StartTime,
    double Duration,
    double ModRate);
