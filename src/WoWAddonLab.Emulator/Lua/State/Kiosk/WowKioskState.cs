namespace WoWAddonLab.Emulator.Lua;

public sealed class WowKioskState
{
    public bool IsEnabled { get; set; }

    public bool IsCompetitiveModeEnabled { get; set; }

    public bool IsHousingResetPending { get; set; }

    public bool IsGodModeRequested { get; set; }

    public int? CharacterTemplateSetIndex { get; set; }

    public WowKioskSessionState SessionState { get; set; }
}
