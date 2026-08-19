using System.Globalization;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowDurationFormatterReference
{
    public WowDurationFormatterReference()
    {
    }

    public WowDurationFormatterReference(
        IWowNumericFormatterState state,
        int reference)
    {
        State = state;
        Reference = reference;
    }

    public IWowNumericFormatterState? State { get; private set; }
    public int Reference { get; private set; }

    public void Set(
        LuaRuntime runtime,
        IWowNumericFormatterState state,
        int reference)
    {
        Release(runtime);
        State = state;
        Reference = reference;
    }

    public void Release(LuaRuntime runtime)
    {
        runtime.ReleaseReference(Reference);
        Reference = 0;
        State = null;
    }
}
