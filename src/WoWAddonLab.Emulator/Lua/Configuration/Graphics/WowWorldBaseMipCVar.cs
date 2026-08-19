using System.Globalization;

namespace WoWAddonLab.Emulator.Lua;

public static class WowWorldBaseMipCVar
{
    public const uint Maximum = 2;

    public static uint Resolve(string value)
    {
        var parsed = WowGraphicsCVarIntegerParser.ParseStrtol(value);
        return Math.Min(unchecked((uint)parsed), Maximum);
    }
}
