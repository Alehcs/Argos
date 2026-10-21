using Argos.Engine;
using Argos.Settings;

namespace Argos.Tests;

public static class EncoderCatalogTests
{
    static EncoderPlan PlanFor(VideoCodec c) =>
        EncoderPresets.Plan(c, QualityPreset.Ultra, 2560, 1440, 60, 22, 40, nvencAvailable: true);

    [Test]
    public static void Amd_only_machine_gets_amf_av1()
    {
        var pick = EncoderCatalog.Pick(VideoCodec.Av1, ["av1_texture_amf", "h265_texture_amf", "h264_texture_amf", "obs_x264"]);
        Assert.Equal("av1_texture_amf", pick.Id, "id");
        Assert.Equal(EncoderVendor.Amd, pick.Vendor, "vendor");
        Assert.Equal(VideoCodec.Av1, pick.Codec, "codec");
    }

    [Test]
    public static void Wanted_codec_beats_vendor_order()
    {
        var pick = EncoderCatalog.Pick(VideoCodec.Av1, ["obs_nvenc_hevc_tex", "obs_qsv11_av1"]);
        Assert.Equal("obs_qsv11_av1", pick.Id, "id");
        Assert.Equal(EncoderVendor.Intel, pick.Vendor, "vendor");
    }

    [Test]
    public static void Missing_codec_falls_back_to_the_next_one()
    {
        var pick = EncoderCatalog.Pick(VideoCodec.Av1, ["obs_nvenc_hevc_tex", "obs_nvenc_h264_tex"]);
        Assert.Equal("obs_nvenc_hevc_tex", pick.Id, "hevc next");
        var h264Only = EncoderCatalog.Pick(VideoCodec.Hevc, ["obs_qsv11_v2"]);
        Assert.Equal("obs_qsv11_v2", h264Only.Id, "h264 qsv");
        Assert.Equal(VideoCodec.H264, h264Only.Codec, "codec");
    }

    [Test]
    public static void No_hardware_encoder_uses_x264()
    {
        var pick = EncoderCatalog.Pick(VideoCodec.Av1, ["obs_x264", "ffmpeg_aac"]);
        Assert.Equal("obs_x264", pick.Id, "id");
        Assert.Equal(EncoderVendor.Software, pick.Vendor, "vendor");
    }

    [Test]
    public static void Software_encoding_is_limited_to_1080p30()
    {
        Assert.Equal(((1920, 1080), 30), EncoderCatalog.LimitForSoftware((2560, 1440), 60), "1440p60");
        Assert.Equal(((1280, 720), 24), EncoderCatalog.LimitForSoftware((1280, 720), 24), "small stays");
        Assert.Equal(((2580, 1080), 30), EncoderCatalog.LimitForSoftware((3440, 1440), 60), "ultrawide keeps aspect");
    }

    [Test]
    public static void Nvidia_settings_use_cqvbr()
    {
        var s = EncoderCatalog.Settings(PlanFor(VideoCodec.Av1), new EncoderChoice("obs_nvenc_av1_tex", EncoderVendor.Nvidia, VideoCodec.Av1));
        Assert.Equal("CQVBR", (string)s["rate_control"], "rc");
        Assert.Equal(22L, (long)s["target_quality"], "cq");
        Assert.Equal(60000L, (long)s["max_bitrate"], "max");
        Assert.Equal(false, (bool)s["lookahead"], "no cuda lookahead");
    }

    [Test]
    public static void Amd_settings_use_qvbr_quality_level()
    {
        var s = EncoderCatalog.Settings(PlanFor(VideoCodec.Hevc), new EncoderChoice("h265_texture_amf", EncoderVendor.Amd, VideoCodec.Hevc));
        Assert.Equal("QVBR", (string)s["rate_control"], "rc");
        Assert.Equal(18L, (long)s["cqp"], "quality level on the 51 scale");
        Assert.Equal("quality", (string)s["preset"], "preset");
    }

    [Test]
    public static void Intel_settings_use_icq()
    {
        var s = EncoderCatalog.Settings(PlanFor(VideoCodec.H264), new EncoderChoice("obs_qsv11_v2", EncoderVendor.Intel, VideoCodec.H264));
        Assert.Equal("ICQ", (string)s["rate_control"], "rc");
        Assert.Equal(18L, (long)s["icq_quality"], "quality");
        Assert.Equal("TU4", (string)s["target_usage"], "usage");
    }

    [Test]
    public static void Software_settings_use_crf_veryfast()
    {
        var plan = EncoderPresets.Plan(VideoCodec.H264, QualityPreset.Ultra, 1920, 1080, 30, 22, 40, nvencAvailable: false);
        var s = EncoderCatalog.Settings(plan, new EncoderChoice("obs_x264", EncoderVendor.Software, VideoCodec.H264));
        Assert.Equal("CRF", (string)s["rate_control"], "rc");
        Assert.Equal("veryfast", (string)s["preset"], "preset");
    }

    /// <summary>The Performance mode spares the graphics card on every vendor and keeps the same quality target.</summary>
    [Test]
    public static void A_light_plan_spares_the_graphics_card_on_every_vendor()
    {
        var normal = EncoderPresets.Plan(VideoCodec.Av1, QualityPreset.Balanced, 1920, 1080, 60, 22, 40, nvencAvailable: true);
        var light = EncoderPresets.Plan(VideoCodec.Av1, QualityPreset.Balanced, 1920, 1080, 60, 22, 40, nvencAvailable: true, light: true);
        Assert.True(!normal.Light && light.Light, "the plan says which");
        Assert.Equal((normal.Cq, normal.MaxBitrateKbps), (light.Cq, light.MaxBitrateKbps), "the same quality target and ceiling");

        var nvidia = new EncoderChoice("obs_nvenc_av1_tex", EncoderVendor.Nvidia, VideoCodec.Av1);
        var n = EncoderCatalog.Settings(normal, nvidia);
        var l = EncoderCatalog.Settings(light, nvidia);
        Assert.Equal(("qres", true), ((string)n["multipass"], (bool)n["adaptive_quantization"]), "NVIDIA, Normal");
        Assert.Equal(("disabled", false), ((string)l["multipass"], (bool)l["adaptive_quantization"]), "NVIDIA, Performance");

        var amd = new EncoderChoice("av1_texture_amf", EncoderVendor.Amd, VideoCodec.Av1);
        Assert.Equal(("quality", "balanced"), ((string)EncoderCatalog.Settings(normal, amd)["preset"], (string)EncoderCatalog.Settings(light, amd)["preset"]), "AMD");
        var intel = new EncoderChoice("obs_qsv11_av1", EncoderVendor.Intel, VideoCodec.Av1);
        Assert.Equal(("TU4", "TU7"), ((string)EncoderCatalog.Settings(normal, intel)["target_usage"], (string)EncoderCatalog.Settings(light, intel)["target_usage"]), "Intel");

        var cpuLight = EncoderPresets.Plan(VideoCodec.H264, QualityPreset.Balanced, 1920, 1080, 30, 22, 40, nvencAvailable: false, light: true);
        Assert.Equal("superfast", (string)EncoderCatalog.Settings(cpuLight, new EncoderChoice("obs_x264", EncoderVendor.Software, VideoCodec.H264))["preset"], "x264");
    }
}
