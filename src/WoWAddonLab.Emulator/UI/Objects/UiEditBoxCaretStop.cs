using System.Numerics;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Emulator.UI;

public readonly record struct UiEditBoxCaretStop(
    int RawUtf16Position,
    float X,
    float Bottom,
    float Top);
