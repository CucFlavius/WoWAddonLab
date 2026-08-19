using System.Globalization;
using System.Runtime.InteropServices;
using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

internal sealed class WowDurationTextBindingState
{
    public bool Enabled { get; set; }
    public int? FontStringId { get; set; }
    public int DurationReference;
    public string TextFormat { get; set; } = string.Empty;
    public List<WowDurationTextFormatComponent> Components { get; private set; } = [];
    public string? ExpiredText { get; set; }
    public string? ZeroDurationText { get; set; }
    public DurationTimeModifier TimeModifier { get; set; }
    public float UpdateInterval { get; set; }
    public float UpdateElapsed { get; set; }

    public void ReplaceComponents(
        LuaRuntime runtime,
        List<WowDurationTextFormatComponent> components)
    {
        foreach (var component in Components)
            component.Formatter.Release(runtime);
        Components = components;
    }

    public void Reset(LuaRuntime runtime)
    {
        Enabled = false;
        FontStringId = null;
        runtime.ReleaseReference(DurationReference);
        DurationReference = 0;
        TextFormat = string.Empty;
        ReplaceComponents(runtime, []);
        ExpiredText = null;
        ZeroDurationText = null;
        TimeModifier = DurationTimeModifier.RealTime;
        UpdateInterval = 0;
        UpdateElapsed = 0;
    }

    public void ReleaseReferences(LuaRuntime runtime) => Reset(runtime);
}
