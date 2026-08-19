namespace WoWAddonLab.Emulator.Lua;

public interface IWowGameRuleProvider
{
    bool TryGetRule(int id, out WowGameRule rule);
}
