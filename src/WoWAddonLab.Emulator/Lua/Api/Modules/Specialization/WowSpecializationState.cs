using LuaNET.Lua51;
using static LuaNET.Lua51.Lua;

namespace WoWAddonLab.Emulator.Lua;

public sealed class WowSpecializationState
{
    public WowSpecializationState()
    {
        AddWarriorSpecialization(1, 71, "Arms", "Two-handed weapon master", 132355, "Interface/TalentFrame/Arms");
        AddWarriorSpecialization(2, 72, "Fury", "Dual-wielding berserker", 132347, "Interface/TalentFrame/Fury");
        AddWarriorSpecialization(3, 73, "Protection", "Shield-bearing defender", 132341, "Interface/TalentFrame/Protection", "TANK");
    }

    public int ActiveSpecGroup { get; set; } = 1;
    public int SpecGroupCount { get; set; } = 1;
    public bool IsInitialized { get; set; } = true;
    public bool HasLootSpecializations { get; set; } = true;
    public bool CanUsePvpTalentUi { get; set; } = true;
    public string CanUsePvpTalentUiReason { get; set; } = string.Empty;
    public bool CanUseTalentSpecUi { get; set; } = true;
    public string CanUseTalentSpecUiReason { get; set; } = string.Empty;
    public bool CanUseTalentUi { get; set; } = true;
    public string CanUseTalentUiReason { get; set; } = string.Empty;
    public bool HasUnspentPvpTalentPoints { get; set; }
    public bool HasNewPvpTalentSlot { get; set; }
    public int? PetSpecializationIndex { get; set; }
    public uint? PetNumber { get; set; }
    public IList<int> SelectedPvpTalentIds { get; } = new List<int>();
    public IDictionary<int, IReadOnlyList<int>> MasterySpellIdsBySpecializationIndex
        { get; } = new Dictionary<int, IReadOnlyList<int>>();
    public IDictionary<int, IReadOnlyList<int>> DisplaySpellIdsBySpecializationId
        { get; } = new Dictionary<int, IReadOnlyList<int>>();
    public IDictionary<int, int> ClassIdBySpecializationId { get; } =
        new Dictionary<int, int>();
    public IDictionary<int, IReadOnlyList<int>> SpecializationIdsBySetId { get; } =
        new Dictionary<int, IReadOnlyList<int>>();
    public IDictionary<int, int> PvpTalentSlotUnlockLevel { get; } =
        new Dictionary<int, int>();
    public IDictionary<int, int> PvpTalentUnlockLevel { get; } =
        new Dictionary<int, int>();
    public IDictionary<(string UnitToken, int TalentIndex), int>
        InspectSelectedPvpTalentIds { get; } =
            new Dictionary<(string UnitToken, int TalentIndex), int>();
    public IDictionary<int, WowPvpTalentInfoState> PvpTalentInfoById { get; } =
        new Dictionary<int, WowPvpTalentInfoState>();
    public IDictionary<int, WowPvpTalentSlotInfoState> PvpTalentSlotInfoByIndex
        { get; } = new Dictionary<int, WowPvpTalentSlotInfoState>();
    public ISet<int> LockedPvpTalentIds { get; } = new HashSet<int>();
    public ISet<int> CurrentSpecializationSetIds { get; } = new HashSet<int>();
    public IDictionary<int, WowCurrentSpecializationInfoState>
        CurrentInfoBySpecializationIndex { get; } =
            new Dictionary<int, WowCurrentSpecializationInfoState>();
    public IDictionary<(int ClassId, int Index), WowSpecializationInfoState>
        InfoByClassAndIndex { get; } =
            new Dictionary<(int ClassId, int Index), WowSpecializationInfoState>();
    public IDictionary<int, int> CountsByClassId { get; } =
        new Dictionary<int, int>
        {
            [1] = 3,
            [2] = 3,
            [3] = 3,
            [4] = 3,
            [5] = 3,
            [6] = 3,
            [7] = 3,
            [8] = 3,
            [9] = 3,
            [10] = 3,
            [11] = 4,
            [12] = 3,
            [13] = 3
        };

    private void AddWarriorSpecialization(
        int index,
        int id,
        string name,
        string description,
        int iconFileDataId,
        string background,
        string role = "DAMAGER")
    {
        CurrentInfoBySpecializationIndex[index] = new WowCurrentSpecializationInfoState(
            id, name, description, iconFileDataId, role, 1, 0, background, 0, true);
        InfoByClassAndIndex[(1, index)] = new WowSpecializationInfoState(
            id, name, description, iconFileDataId, role, index == 1, false, null, null);
        ClassIdBySpecializationId[id] = 1;
    }
}
