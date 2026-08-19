using System.Numerics;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Emulator.UI;

public sealed record UiFrameEventCallback(int Reference, IReadOnlyList<string> Units);
