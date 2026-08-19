using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowInputState
{
    public HashSet<string> MouseButtonsDown { get; } =
        new(StringComparer.OrdinalIgnoreCase);
    public bool SupportsClipCursor { get; set; } = true;
    public bool ControlDown { get; set; }
    public bool ShiftDown { get; set; }
    public bool AltDown { get; set; }
}
