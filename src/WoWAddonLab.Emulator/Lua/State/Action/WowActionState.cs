namespace WoWAddonLab.Emulator.Lua;

public sealed class WowActionState
{
    public const int MaximumActionCount = 228;

    public Dictionary<int, WowActionSlot> Slots { get; } = [];
    public HashSet<int> RangeCheckedSlots { get; } = [];
    public Dictionary<int, WowActionUiRegistration> UiRegistrations { get; } = [];
    public Dictionary<int, int> BonusBarIndexBySlot { get; } = [];
    public Dictionary<int, IReadOnlyList<int>> PetActionPetBarIndices { get; } = [];
    public Dictionary<int, WowPossessActionState> PossessActions { get; } = [];
    public Dictionary<int, WowPetActionState> PetActions { get; } = [];

    public int? LastUsedSlot { get; set; }
    public int ActionBarPage { get; set; }
    public int BonusBarIndex { get; set; } = -1;
    public int? OverrideBarSkin { get; set; }
    public bool HasAssistedCombatActionButtons { get; set; }
    public bool HasBonusActionBar { get; set; }
    public bool HasExtraActionBar { get; set; }
    public bool HasOverrideActionBar { get; set; }
    public bool HasTempShapeshiftActionBar { get; set; }
    public bool HasVehicleActionBar { get; set; }
    public bool IsPossessBarVisible { get; set; }
    public bool HasPetActionBar { get; set; }
    public bool ShouldOverrideBarShowHealthBar { get; set; }
    public bool ShouldOverrideBarShowManaBar { get; set; }
    public byte ActionBarToggleMask { get; set; }
}
