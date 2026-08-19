namespace WoWAddonLab.Emulator.Lua;

public sealed class WowCinematicState
{
    public IDictionary<int, WowCinematicPlaybackState> ActiveByType { get; } =
        new Dictionary<int, WowCinematicPlaybackState>();

    public bool InCinematic => ActiveByType.Count > 0;
    public int CurrentMovieId { get; private set; }
    public string? CurrentSummary { get; set; }
    public bool MouseOverrideDisabled { get; set; }
    public bool OpeningCinematicRequested { get; set; }
    public int? LastFinishedType { get; private set; }
    public bool LastFinishWasUserCanceled { get; private set; }
    public bool LastFinishHadError { get; private set; }

    public void Start(int movieType, int movieId, bool canCancel)
    {
        if (movieId == 0)
            return;

        ActiveByType[movieType] =
            new WowCinematicPlaybackState(movieId, canCancel);
        CurrentMovieId = movieId;
    }

    public void Finish(int movieType, bool userCanceled, bool didError)
    {
        ActiveByType.Remove(movieType);
        CurrentMovieId = 0;
        CurrentSummary = null;
        LastFinishedType = movieType;
        LastFinishWasUserCanceled = userCanceled;
        LastFinishHadError = didError;
    }
}
