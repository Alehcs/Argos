using Kairos.App;
using Kairos.Engine;

namespace Kairos.Tests;

/// <summary>The installed Kairos (Velopack's folder): what is inside it, which processes are its own, where clips may go.</summary>
public static class InstallPathsTests
{
    [Test]
    public static void A_folder_inside_the_install_is_inside()
    {
        const string root = @"C:\U\AppData\Local\Kairos";
        Assert.True(InstallPaths.IsInside(root, @"C:\U\AppData\Local\Kairos\clips"), "a folder in it");
        Assert.True(InstallPaths.IsInside(root, @"c:\u\appdata\local\KAIROS\"), "the folder itself, any case, trailing slash");
        Assert.True(!InstallPaths.IsInside(root, @"C:\U\AppData\Local\KairosOther\clips"), "a folder that only starts the same");
        Assert.True(!InstallPaths.IsInside(root, @"D:\Clips"), "elsewhere");
    }

    /// <summary>
    /// A downloaded update never goes in because some Kairos started (the settings window, auto captions, a Start menu
    /// click): Velopack would stop the engine in the middle of a recording. The engine alone decides when it goes in.
    /// </summary>
    [Test]
    public static void Starting_Kairos_never_puts_a_downloaded_update_in()
    {
        var hooks = InstallPaths.VelopackHooks();
        var autoApply = typeof(Velopack.VelopackApp).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Single(f => f.FieldType == typeof(bool) && f.Name.Contains("autoApply", StringComparison.OrdinalIgnoreCase));
        Assert.True(!(bool)autoApply.GetValue(hooks)!, "Velopack's apply-on-start is off");
    }

    [Test]
    public static void Uninstall_stops_only_this_installs_processes()
    {
        var running = new (int Id, string? Path)[]
        {
            (1, @"C:\L\Kairos\current\Kairos.exe"), (2, @"D:\Kairos\bin\64bit\Kairos.exe"), (3, null), (9, @"C:\L\Kairos\current\Kairos.exe"),
        };
        Assert.Equal("1", string.Join(",", InstallPaths.OwnProcesses(@"C:\L\Kairos", running, self: 9)), "this install's, not itself, not another copy");
    }

    /// <summary>An uninstall removes the start-with-Windows entry only when it starts this install (not another copy's).</summary>
    [Test]
    public static void Uninstall_removes_only_its_own_autostart()
    {
        Assert.True(Autostart.PointsInto("\"C:\\L\\Kairos\\Kairos.exe\" --background", @"C:\L\Kairos"), "this install's stub");
        Assert.True(!Autostart.PointsInto("\"D:\\Kairos\\bin\\64bit\\Kairos.exe\" --background", @"C:\L\Kairos"), "another copy");
        Assert.True(!Autostart.PointsInto(null, @"C:\L\Kairos"), "none");
    }

    /// <summary>The installed program runs from current\ beside Velopack's Update.exe; anything else is not installed.</summary>
    [Test]
    public static void The_install_root_is_found_from_where_kairos_runs()
    {
        string root = Path.Combine(Path.GetTempPath(), "kairos-tests", "root-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "current"));
            string exe = Path.Combine(root, "current", "Argos.exe");
            Assert.Equal(null, InstallPaths.RootFor(exe), "no Update.exe: not installed");
            File.WriteAllBytes(Path.Combine(root, "Update.exe"), []);
            Assert.Equal(root, InstallPaths.RootFor(exe), "installed");
            Assert.Equal(Path.Combine(root, "Argos.exe"), InstallPaths.StubFor(root), "Start menu and autostart point at the stub");
            Assert.Equal(null, InstallPaths.RootFor(Path.Combine(root, "Argos.exe")), "the stub itself is not the program");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Uninstalling removes the whole install folder, so clips must never be kept inside it.</summary>
    [Test]
    public static void Clips_are_never_kept_inside_the_install()
    {
        string root = Path.Combine(Path.GetTempPath(), "kairos-tests", "install-" + Guid.NewGuid().ToString("N"));
        string elsewhere = Path.Combine(Path.GetTempPath(), "kairos-tests", "clips-" + Guid.NewGuid().ToString("N"));
        try
        {
            Assert.True(!ClipOrganizer.Usable(Path.Combine(root, "clips"), root), "inside the install: refused");
            Assert.True(ClipOrganizer.Usable(elsewhere, root), "elsewhere: kept");
            Assert.True(ClipOrganizer.Usable(elsewhere, installRoot: null), "a development build has no install");
        }
        finally
        {
            foreach (string dir in new[] { root, elsewhere })
                if (Directory.Exists(dir))
                    Directory.Delete(dir, recursive: true);
        }
    }
}
