namespace WoWAddonLab.Emulator.Lua;

public sealed class WowRestrictedActionsState
{
    public IDictionary<int, int> RestrictionStates { get; } =
        new Dictionary<int, int>();

    public int GetState(int restrictionType, bool inCombatLockdown)
    {
        if (RestrictionStates.TryGetValue(restrictionType, out var state))
            return state;
        return restrictionType == 0 && inCombatLockdown ? 2 : 0;
    }
}
