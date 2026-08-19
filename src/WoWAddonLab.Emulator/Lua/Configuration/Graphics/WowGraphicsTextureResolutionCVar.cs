using System.Globalization;

namespace WoWAddonLab.Emulator.Lua;

public static class WowGraphicsTextureResolutionCVar
{
    public static bool TryResolve(string value, out int worldBaseMip)
    {
        var parsed = WowGraphicsCVarFloatParser.ParseFiniteFloat(value);
        var uiOrdinal = WowGraphicsCVarFloatParser.TruncateToNativeInt(parsed);

        if (uiOrdinal <= 0)
        {
            worldBaseMip = 2;
            return true;
        }
        if (uiOrdinal == 1)
        {
            worldBaseMip = 1;
            return true;
        }
        if (uiOrdinal == 2)
        {
            worldBaseMip = 0;
            return true;
        }

        worldBaseMip = 0;
        return false;
    }
}
