using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Argos.App;

/// <summary>
/// Argos's mark. app.ico, inside the program, is made with the logo's other pictures by tools/make-brand.py: the window and
/// taskbar show it as it is, and the tray draws its states over it (<see cref="TrayController.DrawBitmap"/>).
/// </summary>
public static class AppIconArt
{
    /// <summary>app.ico's sizes: what Windows asks for from 100% to 300% scale, and the large ones for Explorer.</summary>
    public static readonly int[] Sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256];

    /// <summary>The mark's own dark brown (its outlines).</summary>
    public const uint Brown = 0x302014;

    /// <summary>app.ico itself: a PNG picture for each size in <see cref="Sizes"/>.</summary>
    public static byte[] Ico()
    {
        using var stream = typeof(AppIconArt).Assembly.GetManifestResourceStream("app.ico")
            ?? throw new InvalidOperationException("The icon is missing from the program.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>
    /// The mark, <paramref name="size"/> px square: app.ico's own picture of that size, or its largest scaled down. In the
    /// greys of its colors' lightness when <paramref name="gray"/>.
    /// </summary>
    public static Bitmap Mark(int size, bool gray = false)
    {
        byte[] ico = Ico();
        // The icon's directory: six bytes, then sixteen for each picture (its side, 0 meaning 256; its length; where it is).
        var pictures = Enumerable.Range(0, BitConverter.ToUInt16(ico, 4)).Select(i => 6 + 16 * i)
            .Select(e => (Side: ico[e] == 0 ? 256 : ico[e], Length: BitConverter.ToInt32(ico, e + 8), Offset: BitConverter.ToInt32(ico, e + 12)))
            .ToList();
        var picture = pictures.Find(p => p.Side == size);
        if (picture.Side == 0)
            picture = pictures.MaxBy(p => p.Side);
        using var png = new MemoryStream(ico, picture.Offset, picture.Length);
        using var source = new Bitmap(png);

        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode = PixelOffsetMode.HighQuality;
        using var look = new ImageAttributes();
        look.SetWrapMode(WrapMode.TileFlipXY); // no faint seam where the scaling reads past the picture's edge
        if (gray)
            look.SetColorMatrix(Grays);
        g.DrawImage(source, new Rectangle(0, 0, size, size), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, look);
        return bmp;
    }

    /// <summary>Each color as the grey of its lightness; the transparency stays.</summary>
    static readonly ColorMatrix Grays = new(
    [
        [0.299f, 0.299f, 0.299f, 0, 0],
        [0.587f, 0.587f, 0.587f, 0, 0],
        [0.114f, 0.114f, 0.114f, 0, 0],
        [0, 0, 0, 1, 0],
        [0, 0, 0, 0, 1],
    ]);
}
