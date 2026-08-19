using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

internal readonly record struct WowTextureMipResidency(
    byte LoadPriority,
    uint WorldBaseMip,
    bool BypassWorldBaseMip = false)
{
    public static WowTextureMipResidency FullResolution => new(0, 0);

    public static WowTextureMipResidency ForModel(
        bool noMip,
        WowCVarState cvars) =>
        new(
            noMip ? (byte)0 : (byte)12,
            ReadWorldBaseMip(cvars));

    public static uint ReadWorldBaseMip(WowCVarState cvars)
    {
        if (!cvars.TryGet("worldBaseMip", out var entry))
            return 0;

        return WowWorldBaseMipCVar.Resolve(entry.Value);
    }
}
