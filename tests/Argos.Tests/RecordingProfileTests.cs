using Argos.App;
using Argos.Settings;
using Argos.Ui;

namespace Argos.Tests;

/// <summary>The Normal and Performance modes: what each sets, that every setting stays changeable, and the Status notice.</summary>
public static class RecordingProfileTests
{
    [Test]
    public static void A_mode_sets_its_defaults_and_every_setting_stays_changeable()
    {
        var s = new AppSettings();
        Assert.Equal(RecordingProfile.Normal, s.Video.Profile, "Normal at first");

        RecordingProfiles.Apply(s, RecordingProfile.Performance);
        Assert.Equal((QualityPreset.Balanced, OutputResolution.P1080, 60, "p4", 60),
            (s.Video.Quality, s.Video.Resolution, s.Video.Fps, s.Video.Preset, s.Replay.Seconds), "Performance");
        s.Video.Fps = 30;
        s.Normalize();
        Assert.Equal((RecordingProfile.Performance, 30), (s.Video.Profile, s.Video.Fps), "changing one setting keeps the mode");

        RecordingProfiles.Apply(s, RecordingProfile.Normal);
        var d = new AppSettings();
        Assert.Equal((d.Video.Quality, d.Video.Resolution, d.Video.Fps, d.Video.Preset, d.Replay.Seconds),
            (s.Video.Quality, s.Video.Resolution, s.Video.Fps, s.Video.Preset, s.Replay.Seconds), "Normal: the defaults again");

        s.Video.Profile = (RecordingProfile)7;
        s.Normalize();
        Assert.Equal(RecordingProfile.Normal, s.Video.Profile, "an unknown mode reads as Normal");
    }

    [Test]
    public static void A_new_mode_rebuilds_the_pipeline()
    {
        var after = new AppSettings();
        RecordingProfiles.Apply(after, RecordingProfile.Performance);
        Assert.True(SettingsDiff.NeedsRebuild(new AppSettings(), after), "the encoder works differently");
    }

    [Test]
    public static void The_losing_frames_notice_offers_the_performance_mode_until_it_is_picked()
    {
        var vm = new SettingsViewModel();
        vm.SeedProfile(RecordingProfile.Normal);
        vm.SeedStatus(new StatusDto(null, [], null, null, false, false, AppInfo.Version));
        Assert.True(vm.LosingFramesText.Length == 0 && !vm.SuggestPerformance, "no notice without losses");

        vm.SeedStatus(new StatusDto(null, [], null, null, false, false, AppInfo.Version, LosingFrames: true));
        Assert.True(vm.LosingFramesText.Length > 0 && vm.SuggestPerformance, "the notice and its button");

        vm.UsePerformanceCommand.Execute(null);
        Assert.Equal(RecordingProfile.Performance, vm.Profile, "the button picks Performance");
        Assert.True(vm.IsDirty, "to be saved like any other change");
        Assert.True(!vm.SuggestPerformance && vm.LosingFramesText.Length > 0, "the advice stays, the button goes");
    }
}
