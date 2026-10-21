using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Argos.App;

public enum TrayState { Starting, Active, Waiting, Paused, Recording, Error }

/// <summary>Tray icon and menu. The icons are the app's mark with the state drawn over it at startup.</summary>
public sealed class TrayController : IDisposable
{
    readonly NotifyIcon _icon = new();
    readonly Dictionary<string, Icon> _icons = [];
    readonly System.Windows.Forms.Timer _flashTimer = new() { Interval = 1500 };
    TrayState _state = TrayState.Starting;
    string? _menuKey;

    public event Action? SaveClicked, QuickClicked, ScreenshotClicked, PauseClicked, RecordClicked, PauseRecordClicked, FolderClicked, ShareClicked, DeleteLastClicked, SettingsClicked, ExitClicked;

    public TrayController()
    {
        DrawIcons();
        _icon.Icon = _icons["waiting"];
        _icon.Text = Tooltip;
        _icon.Visible = true;
        _icon.DoubleClick += (_, _) => SettingsClicked?.Invoke();
        _flashTimer.Tick += (_, _) => { _flashTimer.Stop(); ApplyIcon(); };
    }

    /// <summary>The tray icon's tooltip: just the name (the icon already tells the state).</summary>
    public static string Tooltip => Text.Get("app");

    /// <summary>Everything the menu's items depend on. The tooltip is not part of it.</summary>
    public static string MenuKey(TrayState state, bool quickEnabled, int quickSeconds, bool paused, bool recording, string language,
        bool canUndo = false, bool canShare = false, bool recordingPaused = false, bool lastIsShot = false) =>
        $"{state}|{quickEnabled}|{quickSeconds}|{paused}|{recording}|{language}|{canUndo}|{canShare}|{recordingPaused}|{lastIsShot}";

    /// <summary>Updates the icon; rebuilds the menu only when its items change (so an open menu stays open).</summary>
    /// <param name="canUndo">A clip saved in this session can still be sent to the Recycle Bin from the menu.</param>
    /// <param name="canShare">There is a last clip to open in the share panel.</param>
    /// <param name="recordingPaused">The full recording is paused: its menu item resumes it.</param>
    /// <param name="lastIsShot">The last thing saved is a screenshot: the share and delete items say so.</param>
    public void Update(TrayState state, bool quickEnabled, int quickSeconds, bool paused, bool recording, bool canUndo = false,
        bool canShare = false, bool recordingPaused = false, bool lastIsShot = false)
    {
        _state = state;
        _icon.Text = Tooltip;
        if (!_flashTimer.Enabled)
            ApplyIcon();

        string key = MenuKey(state, quickEnabled, quickSeconds, paused, recording, Text.Language, canUndo, canShare, recordingPaused, lastIsShot);
        if (key == _menuKey)
            return;
        _menuKey = key;

        var menu = TrayMenu.Create(Text.IsRightToLeft);
        menu.Items.Add(Text.Get("tray.save"), null, (_, _) => SaveClicked?.Invoke());
        if (quickEnabled)
            menu.Items.Add(Text.Format("tray.quick", Text.Count("count.seconds", quickSeconds)), null, (_, _) => QuickClicked?.Invoke());
        menu.Items.Add(Text.Get("tray.screenshot"), null, (_, _) => ScreenshotClicked?.Invoke());
        // While a full recording runs, pause is for the recording (as on the Status page).
        if (recording)
            menu.Items.Add(Text.Get(recordingPaused ? "tray.resumeRecord" : "tray.pauseRecord"), null, (_, _) => PauseRecordClicked?.Invoke());
        else
            menu.Items.Add(Text.Get(paused ? "tray.resume" : "tray.pause"), null, (_, _) => PauseClicked?.Invoke());
        menu.Items.Add(Text.Get(recording ? "tray.recordStop" : "tray.recordStart"), null, (_, _) => RecordClicked?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Text.Get("tray.openFolder"), null, (_, _) => FolderClicked?.Invoke());
        if (canShare)
            menu.Items.Add(Text.Get(lastIsShot ? "tray.shareLastShot" : "tray.share"), null, (_, _) => ShareClicked?.Invoke());
        if (canUndo)
            menu.Items.Add(Text.Get(lastIsShot ? "tray.deleteLastShot" : "tray.deleteLast"), null, (_, _) => DeleteLastClicked?.Invoke());
        menu.Items.Add(Text.Get("tray.settings"), null, (_, _) => SettingsClicked?.Invoke());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Text.Get("tray.exit"), null, (_, _) => ExitClicked?.Invoke());
        var old = _icon.ContextMenuStrip;
        _icon.ContextMenuStrip = menu;
        old?.Dispose();
    }

    void DrawIcons()
    {
        foreach (string kind in new[] { "active", "waiting", "paused", "recording", "error", "saved" })
            _icons[kind] = DrawIcon(kind);
    }

    /// <summary>
    /// Active and recording use the theme color; saved, error and idle keep their meaning colors. Recording, saved, error
    /// and paused show it as a dot on the mark.
    /// </summary>
    public static uint ColorFor(string kind) => kind switch
    {
        "active" or "recording" => Settings.ThemeColors.Accent,
        "saved" => 0x2ED47A,
        "error" => 0xFFB020,
        _ => 0xA3928F,
    };

    /// <summary>Briefly shows the green "saved" icon.</summary>
    public void FlashSaved()
    {
        _icon.Icon = _icons["saved"];
        _flashTimer.Stop();
        _flashTimer.Start();
    }

    void ApplyIcon() => _icon.Icon = _icons[_state switch
    {
        TrayState.Active => "active",
        TrayState.Paused => "paused",
        TrayState.Recording => "recording",
        TrayState.Error => "error",
        _ => "waiting",
    }];

    static Icon DrawIcon(string kind)
    {
        // Drawn at the size the tray shows icons at this scale, so Windows does not shrink a larger one.
        using var bmp = DrawBitmap(kind, Math.Max(16, SystemInformation.SmallIconSize.Width));
        IntPtr h = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(h).Clone();
        DestroyIcon(h);
        return icon;
    }

    /// <summary>
    /// The tray picture for <paramref name="kind"/>, <paramref name="size"/> px square: the mark in color while Argos keeps
    /// the last minutes, in grey while it does not (waiting, paused, error). Recording, saved and error add a dot in their
    /// color in the corner; paused, a grey dot with two bars.
    /// </summary>
    public static Bitmap DrawBitmap(string kind, int size = 32)
    {
        var bmp = AppIconArt.Mark(size, gray: kind is "waiting" or "paused" or "error");
        if (kind is "active" or "waiting")
            return bmp;
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PixelOffsetMode = PixelOffsetMode.Half; // whole numbers are pixel edges, so the dot and the bars sit on pixels
        // The dot is ringed in the mark's own brown, so it stands apart from the mark at any size.
        var dot = Dot(size);
        int ring = Math.Max(1, (int)Math.Round(size / 16.0));
        using var brown = new SolidBrush(Opaque(AppIconArt.Brown));
        g.FillEllipse(brown, dot);
        using (var fill = new SolidBrush(Opaque(ColorFor(kind))))
            g.FillEllipse(fill, Rectangle.Inflate(dot, -ring, -ring));
        if (kind == "paused")
        {
            int bar = Math.Max(1, (int)Math.Round(dot.Width * 0.16)), gap = Math.Max(1, (int)Math.Round(dot.Width * 0.12));
            int tall = (int)Math.Round(dot.Height * 0.42);
            int left = dot.X + (dot.Width - 2 * bar - gap) / 2, top = dot.Y + (dot.Height - tall) / 2;
            g.FillRectangle(brown, left, top, bar, tall);
            g.FillRectangle(brown, left + bar + gap, top, bar, tall);
        }
        return bmp;
    }

    /// <summary>Where a state's dot goes on a <paramref name="size"/> px tray picture: the bottom right corner.</summary>
    internal static Rectangle Dot(int size)
    {
        int side = (int)Math.Round(size * 0.56);
        return new Rectangle(size - side, size - side, side, side);
    }

    static Color Opaque(uint rgb) => Color.FromArgb(unchecked((int)(0xFF000000 | rgb)));

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.ContextMenuStrip?.Dispose();
        _icon.Dispose();
        _flashTimer.Dispose();
        foreach (var i in _icons.Values)
            i.Dispose();
    }

    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr handle);
}
