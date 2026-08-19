namespace WoWAddonLab.Tests;

public sealed class MovieFrameContractTests
{
    [Fact]
    public void ConstructorDefaultsAndInheritedFrameResetPreserveMovieOwnedState()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate(
            "local movie=CreateFrame('MovieFrame','MovieFrameResetTarget',UIParent); " +
            "movie:EnableSubtitles(false)");

        var movieFrame = session.Ui.Find("MovieFrameResetTarget")!;
        Assert.NotNull(movieFrame.Movie);
        Assert.False(movieFrame.Movie.SubtitlesEnabled);
        Assert.Null(movieFrame.Movie.RequestedMovieId);
        Assert.False(movieFrame.Movie.Looping);
        Assert.False(movieFrame.Movie.Playing);
        Assert.Equal(0, movieFrame.Movie.ReturnCode);

        movieFrame.Movie.SubtitlesEnabled = true;
        movieFrame.Movie.RequestedMovieId = 173;
        movieFrame.Movie.Looping = true;
        movieFrame.Movie.Playing = true;
        movieFrame.Movie.ReturnCode = 9;

        session.Lua.Evaluate(
            "local movie=MovieFrameResetTarget; " +
            "movie:SetPoint('CENTER',UIParent,'CENTER',12,34); " +
            "movie:SetAlpha(.25); movie:SetMovable(true); movie:SetResizable(true); " +
            "movie:EnableMouse(true); movie:EnableMouseWheel(true); " +
            "movie:EnableKeyboard(true); " +
            "movie:SetScript('OnMovieFinished',function() end); " +
            "movie:SetToDefaults()");

        Assert.Equal(
            "0:1:false:false:false:false:false:false",
            session.Lua.Evaluate(
                "local movie=MovieFrameResetTarget; " +
                "return table.concat({movie:GetNumPoints(),movie:GetAlpha()," +
                "tostring(movie:IsMovable()),tostring(movie:IsResizable())," +
                "tostring(movie:IsMouseClickEnabled())," +
                "tostring(movie:IsMouseMotionEnabled())," +
                "tostring(movie:IsMouseWheelEnabled())," +
                "tostring(movie:IsKeyboardEnabled())},':')"));
        Assert.Equal(
            "true",
            session.Lua.Evaluate(
                "return tostring(MovieFrameResetTarget:GetScript('OnMovieFinished')==nil)"));

        Assert.True(movieFrame.Movie.SubtitlesEnabled);
        Assert.Equal(173, movieFrame.Movie.RequestedMovieId);
        Assert.True(movieFrame.Movie.Looping);
        Assert.True(movieFrame.Movie.Playing);
        Assert.Equal(9, movieFrame.Movie.ReturnCode);
    }
}
