namespace WoWAddonLab.Emulator.Lua;

public sealed class WowShapeshiftState
{
    public IList<WowShapeshiftFormState> Forms { get; } =
        new List<WowShapeshiftFormState>();
    public int CurrentFormIndex { get; set; }
    public int? CurrentFormIndexExcludingTemporaryForms { get; set; }
    public int? CurrentFormId { get; set; }
}
