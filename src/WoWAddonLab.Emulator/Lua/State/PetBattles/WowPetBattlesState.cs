namespace WoWAddonLab.Emulator.Lua;

public sealed class WowPetBattlesState
{
    public IDictionary<(int PetOwner, int Slot), WowPetBattlePet> Pets
        { get; } =
        new Dictionary<(int, int), WowPetBattlePet>();

    public IDictionary<int, int> ActivePets { get; } =
        new Dictionary<int, int>();

    public IDictionary<int, int> PetCounts { get; } =
        new Dictionary<int, int>();

    public ISet<int> SwappablePetIndices { get; } = new HashSet<int>();

    public WowPetbattleState BattleState { get; set; } =
        WowPetbattleState.WaitingForFrontPets;

    public WowPetBattleSelectedAction? SelectedAction { get; set; }

    public WowPetBattleMatchmakingState? PvpMatchmaking { get; set; }

    public int ForfeitPenalty { get; set; }

    public int TrappablePetCount { get; set; }

    public bool CanActivePetSwapOut { get; set; }

    public bool IsInBattle { get; set; }

    public bool IsPlayerNpc { get; set; }

    public bool IsSkipAvailable { get; set; }

    public bool IsTrapAvailable { get; set; }

    public bool IsWaitingOnOpponent { get; set; }

    public bool ShouldShowPetSelect { get; set; }
    public bool IsWildBattle { get; set; }
}
