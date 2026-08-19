using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowClassTalentsState
{
    public bool CanChangeTalents { get; set; }
    public bool CanAddTalents { get; set; }
    public bool CanCreateNewConfig { get; set; }
    public string? ChangeError { get; set; }
    public bool CanEditTalents { get; set; }
    public int? ActiveConfigId { get; set; }
    public int? ActiveHeroTalentSpec { get; set; }
    public int? CurrentSpecializationId { get; set; }
    public Dictionary<int, IReadOnlyList<int>> ConfigIdsBySpecialization { get; } = [];
    public Dictionary<int, int> LastSelectedSavedConfigIdsBySpecialization { get; } = [];
    public Dictionary<(int ConfigId, int ClassSpecId), WowHeroTalentSpecsState>
        HeroTalentSpecsByConfigAndClassSpec { get; } = [];
    public bool HasStarterBuild { get; set; }
    public bool StarterBuildActive { get; set; }
}
