namespace Argos.Tests;

/// <summary>The logo in the window's sidebar: the mark and the name as drawn, on a clear background.</summary>
public static class LogoTests
{
    static string Assets
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Argos.sln")))
                dir = dir.Parent;
            return Path.Combine(dir!.FullName, "src", "Argos", "Ui", "Assets");
        }
    }

    [Test]
    public static void The_sidebar_logo_is_the_mark_and_the_light_name_on_a_clear_background()
    {
        string path = Path.Combine(Assets, "logo.png");
        Assert.True(File.Exists(path), path);
        using var image = new System.Drawing.Bitmap(path);
        Assert.True(image.Height >= 120, $"drawn large enough to stay sharp at 200% and more ({image.Height} px)");
        Assert.Equal(0, (int)image.GetPixel(0, 0).A, "clear background");
        bool gold = false, coral = false, cream = false;
        for (int x = 0; x < image.Width; x += 2)
            for (int y = 0; y < image.Height; y += 2)
            {
                var p = image.GetPixel(x, y);
                if (p.A < 250)
                    continue;
                // The mark on the left, its gold and its coral middle; the name after it, light for the dark sidebar.
                bool mark = x < image.Width / 4, name = x > image.Width / 3;
                gold |= mark && p.R > 220 && p.G is > 160 and < 215 && p.B < 110;
                coral |= mark && p.R > 220 && p.G is > 70 and < 130 && p.B is > 60 and < 130;
                cream |= name && p.R > 235 && p.G > 215 && p.B > 185;
            }
        Assert.True(gold, "the mark's gold");
        Assert.True(coral, "the mark's coral middle");
        Assert.True(cream, "the light name");
    }
}
