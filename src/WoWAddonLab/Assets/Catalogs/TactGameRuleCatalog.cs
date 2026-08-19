using DBCD.Providers;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactGameRuleCatalog : TactCatalog, IWowGameRuleProvider
{
    private readonly IReadOnlyDictionary<int, WowGameRule> _rules;

    private TactGameRuleCatalog(IReadOnlyDictionary<int, WowGameRule> rules)
    {
        _rules = rules;
    }

    public int Count => _rules.Count;

    public bool TryGetRule(int id, out WowGameRule rule) =>
        _rules.TryGetValue(id, out rule);

    public static TactGameRuleCatalog Load(TactAssetSource tact, string build)
    {
        var database = tact.Database;
        var results = new Dictionary<int, WowGameRule>();
        foreach (var row in database.Load("Cfg_GameRules", build).Values)
        {
            var id = Integer(row, "ID");
            results[id] = new WowGameRule(
                id,
                Integer(row, "RuleValue"),
                Integer(row, "RuleType"));
        }
        return new TactGameRuleCatalog(results);
    }


}
