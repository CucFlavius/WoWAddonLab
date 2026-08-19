using DBCD.Providers;
using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Assets;

public sealed class TactAzeriteEssenceCatalog : TactCatalog, IWowAzeriteEssenceProvider
{
    private TactAzeriteEssenceCatalog(
        IReadOnlyList<WowAzeriteMilestoneDefinition> milestones)
    {
        Milestones = milestones;
    }

    public IReadOnlyList<WowAzeriteMilestoneDefinition> Milestones { get; }

    public static TactAzeriteEssenceCatalog Load(TactAssetSource tact, string build)
    {
        var database = tact.Database;
        var powers = database.Load("AzeritePower", build).Values
            .ToDictionary(
                row => Integer(row, "ID"),
                row => Integer(row, "SpellID"));
        var milestones = database.Load("AzeriteItemMilestonePower", build).Values
            .Select(row =>
            {
                var powerId = Integer(row, "AzeritePowerID");
                return new WowAzeriteMilestoneDefinition(
                    Integer(row, "ID"),
                    Integer(row, "RequiredLevel"),
                    powerId,
                    Integer(row, "AzeriteEssenceType"),
                    Boolean(row, "IsHeartEssenceUnlock"),
                    powers.GetValueOrDefault(powerId));
            })
            .OrderBy(value => value.Id)
            .ToArray();
        return new TactAzeriteEssenceCatalog(milestones);
    }



}
