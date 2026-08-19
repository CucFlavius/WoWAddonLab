namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCovenantState
{
    public int ActiveCovenantId { get; set; }

    public IList<int> CovenantIds { get; } = [1, 2, 3, 4];

    public IDictionary<int, WowCovenantDataState> CovenantDataById { get; } =
        new Dictionary<int, WowCovenantDataState>();

    public bool PreviewActive { get; set; }

    public int PreviewCloseFromUiRequests { get; internal set; }
}
