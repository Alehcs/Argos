using Argos.Engine.Share;

namespace Argos.Tests;

/// <summary>The FFmpeg 8 DLLs the tests run on: OBS's own, or the installed Argos's copy of them.</summary>
static class TestFfmpeg
{
    /// <summary>The folder with the FFmpeg 8 DLLs (OBS's, else the installed Argos's), or null.</summary>
    public static string? Folder => new[]
        {
            @"C:\Program Files\obs-studio\bin\64bit",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Argos", "current"),
        }
        .FirstOrDefault(d => File.Exists(Path.Combine(d, "avcodec-62.dll")));

    public static void Load()
    {
        string? dir = Folder;
        Assert.Needs(dir != null, "the FFmpeg 8 DLLs from OBS");
        ClipTranscoder.Load(dir!);
    }
}
