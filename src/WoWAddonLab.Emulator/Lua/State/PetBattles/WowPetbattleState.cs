namespace WoWAddonLab.Emulator.Lua;

public enum WowPetbattleState
{
    Created = 0,
    WaitingPreBattle = 1,
    RoundInProgress = 2,
    WaitingForFrontPets = 3,
    CreatedFailed = 4,
    FinalRound = 5,
    Finished = 6
}
