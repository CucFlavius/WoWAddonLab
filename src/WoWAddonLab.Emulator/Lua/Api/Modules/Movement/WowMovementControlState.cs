using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowMovementControlState
{
    public bool MovingForward { get; internal set; }
    public bool MovingBackward { get; internal set; }
    public bool TurningLeft { get; internal set; }
    public bool TurningRight { get; internal set; }
    public bool StrafingLeft { get; internal set; }
    public bool StrafingRight { get; internal set; }
    public bool Ascending { get; internal set; }
    public bool StrafeAlsoTurns { get; set; }
}
