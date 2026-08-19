namespace WoWAddonLab.Emulator.Lua;

public enum WowPetBattleQueueStatus
{
    None = 0,
    Queued = 1,
    QueuedUpdate = 2,
    AlreadyQueued = 3,
    JoinFailed = 4,
    JoinFailedSlots = 5,
    JoinFailedJournalLock = 6,
    JoinFailedNeutral = 7,
    MatchAccepted = 8,
    MatchDeclined = 9,
    MatchOpponentDeclined = 10,
    ProposalTimedOut = 11,
    Removed = 12,
    RequeuedAfterInternalError = 13,
    RequeuedAfterOpponentRemoved = 14,
    Matchmaking = 15,
    LostConnection = 16,
    Shutdown = 17,
    Suspended = 18,
    Unsuspended = 19,
    InBattle = 20,
    NoBattlingHere = 21
}
