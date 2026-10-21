using Argos.App;

namespace Argos.Tests;

/// <summary>Argos's mark: app.ico (the program, window and taskbar icon) and the tray icons drawn over it.</summary>
public static class AppIconTests
{
    static uint Rgb(System.Drawing.Color c) => (uint)(c.R << 16 | c.G << 8 | c.B);

    static bool Coral(System.Drawing.Color c) => c.A == 255 && c.R > 220 && c.G is > 70 and < 130 && c.B is > 60 and < 125;

    static bool Grey(System.Drawing.Color c) => Math.Abs(c.R - c.G) <= 2 && Math.Abs(c.G - c.B) <= 2;

    [Test]
    public static void The_icon_file_has_every_size_windows_asks_for()
    {
        byte[] ico = AppIconArt.Ico();
        Assert.Equal((0, 1), (BitConverter.ToUInt16(ico, 0), BitConverter.ToUInt16(ico, 2)), "an icon file");
        Assert.Equal(AppIconArt.Sizes.Length, (int)BitConverter.ToUInt16(ico, 4), "frames");
        var sides = Enumerable.Range(0, AppIconArt.Sizes.Length).Select(i => ico[6 + 16 * i] == 0 ? 256 : ico[6 + 16 * i]).Order();
        Assert.Equal(string.Join(", ", AppIconArt.Sizes), string.Join(", ", sides), "a picture for each size");
        using var icon = new System.Drawing.Icon(new MemoryStream(ico), 32, 32);
        Assert.Equal(32, icon.Width, "Windows reads it");
    }

    [Test]
    public static void The_mark_is_round_with_its_coral_middle_at_every_size()
    {
        // 36 is not in the file: it is scaled down from the largest picture.
        foreach (int size in new[] { 16, 20, 24, 32, 36, 48, 256 })
        {
            using var mark = AppIconArt.Mark(size);
            Assert.Equal((size, size), (mark.Width, mark.Height), $"{size}: its size");
            Assert.Equal(0, (int)mark.GetPixel(0, 0).A, $"{size}: a clear corner, the mark is round");
            Assert.True(Coral(mark.GetPixel(size / 2, size / 2)), $"{size}: the coral middle ({mark.GetPixel(size / 2, size / 2)})");
        }
    }

    [Test]
    public static void The_grey_mark_keeps_the_shape_in_greys()
    {
        using var color = AppIconArt.Mark(32);
        using var grey = AppIconArt.Mark(32, gray: true);
        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++)
            {
                var c = color.GetPixel(x, y);
                var g = grey.GetPixel(x, y);
                Assert.Equal((int)c.A, (int)g.A, $"({x}, {y}): the same shape");
                if (g.A > 0)
                    Assert.True(Grey(g), $"({x}, {y}): grey ({g})");
            }
    }

    [Test]
    public static void The_tray_shows_each_state_on_the_mark()
    {
        foreach (int size in new[] { 16, 20, 24, 32 })
        {
            var dot = TrayController.Dot(size);
            int midX = dot.X + dot.Width / 2, midY = dot.Y + dot.Height / 2;
            using var color = AppIconArt.Mark(size);
            using var grey = AppIconArt.Mark(size, gray: true);
            bool Same(System.Drawing.Bitmap a, System.Drawing.Bitmap b) =>
                Enumerable.Range(0, size * size).All(i => a.GetPixel(i % size, i / size) == b.GetPixel(i % size, i / size));

            using (var active = TrayController.DrawBitmap("active", size))
                Assert.True(Same(active, color), $"{size}: active is the mark in color");
            using (var waiting = TrayController.DrawBitmap("waiting", size))
                Assert.True(Same(waiting, grey), $"{size}: waiting is the mark in grey");

            foreach (string kind in new[] { "recording", "saved", "error", "paused" })
            {
                using var tray = TrayController.DrawBitmap(kind, size);
                Assert.Equal((size, size), (tray.Width, tray.Height), $"{size} {kind}: its size");
                // The mark away from the dot: in color while recording or just saved, grey when not keeping the last minutes.
                var mark = kind is "recording" or "saved" ? color : grey;
                Assert.Equal(mark.GetPixel(size / 2, size / 4), tray.GetPixel(size / 2, size / 4), $"{size} {kind}: the mark");
                Assert.Equal(TrayController.ColorFor(kind), Rgb(tray.GetPixel(midX, midY)), $"{size} {kind}: the dot's color");
                var ring = tray.GetPixel(midX, dot.Bottom - 1);
                Assert.True(ring.R < 90 && ring.G < 70 && ring.B < 60, $"{size} {kind}: the dot's dark ring ({ring})");
            }

            // Inside the ring, across the dot's middle: two runs of the brown, the bars.
            using var paused = TrayController.DrawBitmap("paused", size);
            int edge = Math.Max(1, (int)Math.Round(size / 16.0)), bars = 0;
            bool inBar = false;
            for (int x = dot.X + edge; x < dot.Right - edge; x++)
            {
                bool brown = Rgb(paused.GetPixel(x, midY)) == AppIconArt.Brown;
                if (brown && !inBar)
                    bars++;
                inBar = brown;
            }
            Assert.Equal(2, bars, $"{size}: paused has two bars on its dot");
        }
    }
}
