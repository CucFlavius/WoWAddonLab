using WoWAddonLab.Emulator.Lua;

namespace WoWAddonLab.Tests;

public sealed class SoundContractTests
{
    [Fact]
    public void PlaySoundUsesNativeDefaultsTruthinessAndTwoValueSuccessShape()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "true:number:2:true:number",
            session.Lua.Evaluate(
                "local ok,handle=C_Sound.PlaySound(123);" +
                "local globalOk,globalHandle=PlaySound(124,'music',0,'','7');" +
                "return table.concat({tostring(ok),type(handle)," +
                "select('#',C_Sound.PlaySound(125,nil,false,false,nil,'ignored'))," +
                "tostring(globalOk),type(globalHandle)},':')"));

        Assert.Equal(3, session.Lua.Sound.PlaybackRequests.Count);
        var first = session.Lua.Sound.PlaybackRequests[0];
        Assert.Equal(WowSoundSourceKind.SoundKit, first.SourceKind);
        Assert.Equal(123, first.SoundKitId);
        Assert.Equal(3, first.UiSoundSubType);
        Assert.Equal("SFX", first.Channel);
        Assert.False(first.ForceNoDuplicates);
        Assert.False(first.RunFinishCallback);
        Assert.Null(first.OverridePriority);

        var second = session.Lua.Sound.PlaybackRequests[1];
        Assert.Equal(7, second.UiSoundSubType);
        Assert.Equal("Music", second.Channel);
        Assert.True(second.ForceNoDuplicates);
        Assert.True(second.RunFinishCallback);
        Assert.Equal(7, second.OverridePriority);
    }

    [Fact]
    public void PlaySoundValidatesGeneratedArgumentsAndMayReturnNothing()
    {
        using var session = new EmulatorSession();

        Assert.Equal(
            "false:false:false",
            session.Lua.Evaluate(
                "return tostring(pcall(C_Sound.PlaySound,{}))..':'.." +
                "tostring(pcall(C_Sound.PlaySound,1,7))..':'.." +
                "tostring(pcall(C_Sound.PlaySound,1,nil,nil,nil,{}))"));

        session.Lua.Sound.UnavailableSoundKitIds.Add(77);
        Assert.Equal(
            "0:0",
            session.Lua.Evaluate(
                "return select('#',C_Sound.PlaySound(0))..':'.." +
                "select('#',C_Sound.PlaySound(77))"));

        session.Lua.Sound.PlaybackSuppressed = true;
        Assert.Equal(
            "0",
            session.Lua.Evaluate("return select('#',C_Sound.PlaySound(123))"));
    }

    [Fact]
    public void PlaySoundFileResolvesNumericOrPathSourcesAndUsesLegacyChannelRules()
    {
        using var session = new EmulatorSession();
        session.Lua.Sound.ResolveFileDataId = path =>
            path.Equals("Sound\\Known.ogg", StringComparison.OrdinalIgnoreCase)
                ? 456u
                : 0u;

        Assert.Equal(
            "true:number:true:number:0:false:false",
            session.Lua.Evaluate(
                "local a,ha=PlaySoundFile('321','Ambience');" +
                "local b,hb=PlaySoundFile('Sound\\\\Known.ogg','music');" +
                "return table.concat({tostring(a),type(ha),tostring(b),type(hb)," +
                "select('#',PlaySoundFile('Sound\\\\Missing.ogg'))," +
                "tostring(pcall(PlaySoundFile,false))," +
                "tostring(pcall(PlaySoundFile,{}))},':')"));

        Assert.Equal(2, session.Lua.Sound.PlaybackRequests.Count);
        var numeric = session.Lua.Sound.PlaybackRequests[0];
        Assert.Equal(WowSoundSourceKind.FileDataId, numeric.SourceKind);
        Assert.Equal(321u, numeric.FileDataId);
        Assert.Equal("Ambience", numeric.Channel);

        var path = session.Lua.Sound.PlaybackRequests[1];
        Assert.Equal(WowSoundSourceKind.FilePath, path.SourceKind);
        Assert.Equal("Sound\\Known.ogg", path.FilePath);
        Assert.Equal(456u, path.FileDataId);
        Assert.Equal("Music", path.Channel);

        Assert.Equal(
            "SFX:SFX",
            session.Lua.Evaluate(
                "PlaySoundFile(777,'unknown'); PlaySoundFile(778,false); return 'SFX:SFX'"));
        Assert.All(
            session.Lua.Sound.PlaybackRequests.Skip(2),
            playback => Assert.Equal("SFX", playback.Channel));
    }

    [Fact]
    public void StopSoundReturnsNothingAndUpdatesPlayingAndScaledVolumeState()
    {
        using var session = new EmulatorSession();
        session.Lua.Evaluate("return C_Sound.PlaySound(9001)");
        var playback = Assert.Single(session.Lua.Sound.PlaybackRequests);
        playback.ScaledVolume = 0.5f;

        Assert.Equal(
            "true:0.5:0:false:0",
            session.Lua.Evaluate(
                $"local h={playback.Handle}; " +
                "local before=C_Sound.IsPlaying(h);" +
                "local volume=C_Sound.GetSoundScaledVolume(h);" +
                "local count=select('#',StopSound(tostring(h),'12.9'));" +
                "return table.concat({tostring(before),tostring(volume),count," +
                "tostring(C_Sound.IsPlaying(h)),C_Sound.GetSoundScaledVolume(h)},':')"));

        var stop = Assert.Single(session.Lua.Sound.StopRequests);
        Assert.Equal(playback.Handle, stop.Handle);
        Assert.Equal(12, stop.FadeoutMilliseconds);

        Assert.Equal(
            "0:false:false:false",
            session.Lua.Evaluate(
                "return select('#',StopSound(404,{}))..':'.." +
                "tostring(pcall(StopSound,{}))..':'.." +
                "tostring(pcall(C_Sound.IsPlaying,-1))..':'.." +
                "tostring(pcall(C_Sound.GetSoundScaledVolume,{}))"));
        Assert.Null(session.Lua.Sound.StopRequests[^1].FadeoutMilliseconds);
    }

    [Fact]
    public void MuteSoundFileTracksResolvedFilesAndReturnsNothing()
    {
        using var session = new EmulatorSession();
        session.Lua.Sound.ResolveFileDataId = path =>
            path == "Sound\\Known.ogg" ? 456u : 0u;

        Assert.Equal(
            "0:0:0:true:true:true:true",
            session.Lua.Evaluate(
                "local muteCount=select('#',MuteSoundFile('321'));" +
                "local pathCount=select('#',MuteSoundFile('Sound\\\\Known.ogg'));" +
                "local unmuteCount=select('#',UnmuteSoundFile(321));" +
                "return table.concat({muteCount,pathCount,unmuteCount," +
                "tostring(pcall(MuteSoundFile,0)==false)," +
                "tostring(pcall(MuteSoundFile,'Sound\\\\Missing.ogg')==false)," +
                "tostring(pcall(MuteSoundFile,false)==false)," +
                "tostring(pcall(UnmuteSoundFile,{})==false)},':')"));

        Assert.DoesNotContain(321u, session.Lua.Sound.MutedFileDataIds);
        Assert.Contains(456u, session.Lua.Sound.MutedFileDataIds);
    }
}
