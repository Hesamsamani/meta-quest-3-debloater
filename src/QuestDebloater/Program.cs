using System.Runtime.InteropServices;

namespace QuestDebloater;

internal static class Program
{
    [DllImport("kernel32.dll")]
    private static extern bool AttachConsole(int pid);

    [STAThread]
    private static int Main(string[] args)
    {
        var adb = new AdbService();
        var catalog = AdbService.LoadCatalog();

        if (args.Length == 0)
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(adb, catalog));
            return 0;
        }

        var cmd = args[0].ToLowerInvariant();
        if (cmd == "screenshot")
        {
            var outPath = args.Length > 1 ? args[1] : Path.Combine(AdbService.ResolveProjectRoot(), "docs", "screenshot-gui.png");
            ApplicationConfiguration.Initialize();
            var form = new MainForm(adb, catalog);
            bool saved = false;
            form.Shown += async (_, _) =>
            {
                await form.WaitForInitialLoadAsync();
                Application.DoEvents();
                form.CaptureWindowPng(outPath);
                saved = true;
                form.Close();
            };
            Application.Run(form);
            return saved && File.Exists(outPath) ? 0 : 1;
        }

        if (!Console.IsOutputRedirected) AttachConsole(-1);

        return cmd switch
        {
            "status" => RunStatus(adb, catalog),
            "backup" => RunBackup(adb, catalog),
            "apply" => RunApply(adb, catalog, args),
            "restore" => RunRestore(adb, catalog),
            _ => PrintUsage()
        };
    }

    private static int PrintUsage()
    {
        Console.WriteLine("QuestDebloater.exe                  Launch dark-mode desktop GUI");
        Console.WriteLine("QuestDebloater.exe status           Show Quest 3 specs, RAM, and catalog summary");
        Console.WriteLine("QuestDebloater.exe backup           Save live package & memory snapshot");
        Console.WriteLine("QuestDebloater.exe apply --preset safe|telemetry|pcvr");
        Console.WriteLine("QuestDebloater.exe restore          Re-enable all catalog packages");
        Console.WriteLine("QuestDebloater.exe screenshot [png] Capture live GUI screenshot");
        return 2;
    }

    private static string RequireSerial(AdbService adb)
    {
        var devices = adb.ListDevices();
        if (devices.Count == 0)
            throw new InvalidOperationException($"No authorized ADB headset connected (adb: {adb.AdbPath}).");
        return devices[0].Serial;
    }

    private static int RunStatus(AdbService adb, CatalogRoot catalog)
    {
        var serial = RequireSerial(adb);
        var (info, entries) = adb.ScanHeadset(serial, catalog);
        Console.WriteLine($"Device:    {info.Model} ({info.DeviceCode})  Serial: {info.Serial}");
        Console.WriteLine($"OS:        Android {info.AndroidVersion}  Firmware: {info.FirmwareDisplay}");
        Console.WriteLine($"Battery:   {info.BatteryLevel}% ({info.BatteryTempC:F1} C)  Guardian: {info.GuardianEnabled}");
        Console.WriteLine($"RAM:       {info.MemAvailableMb:F0} MB available / {info.MemTotalMb:F0} MB total");
        Console.WriteLine($"Storage:   {info.StorageSummary}");
        Console.WriteLine($"HOME:      {info.HomeActivity}");
        Console.WriteLine($"Packages:  {info.TotalInstalledCount} total installed, {info.DisabledCount} disabled");
        foreach (var tier in new[] { "SAFE", "MODERATE", "CAUTION", "PROTECTED" })
        {
            var t = entries.Where(e => e.Spec.Tier == tier && e.Installed).ToList();
            Console.WriteLine($"  {tier,-10}: {t.Count(x => !x.Enabled)} disabled / {t.Count} installed");
        }
        return 0;
    }

    private static int RunBackup(AdbService adb, CatalogRoot catalog)
    {
        var serial = RequireSerial(adb);
        var (info, entries) = adb.ScanHeadset(serial, catalog);
        var path = adb.SaveBackup(serial, info, entries, isInitialBefore: false);
        Console.WriteLine($"Backup written: {path}");
        return 0;
    }

    private static int RunApply(AdbService adb, CatalogRoot catalog, string[] args)
    {
        var idx = Array.IndexOf(args, "--preset");
        var preset = (idx >= 0 && idx + 1 < args.Length ? args[idx + 1] : "safe").ToLowerInvariant();
        if (preset is not ("safe" or "telemetry" or "pcvr"))
        {
            Console.Error.WriteLine($"Unknown preset '{preset}'. Use safe | telemetry | pcvr.");
            return 2;
        }

        var serial = RequireSerial(adb);
        var (infoBefore, entries) = adb.ScanHeadset(serial, catalog);
        var beforePath = adb.SaveBackup(serial, infoBefore, entries, isInitialBefore: true);
        Console.WriteLine($"Initial baseline backup: {beforePath}");
        Console.WriteLine($"Before RAM available: {infoBefore.MemAvailableMb:F0} MB / {infoBefore.MemTotalMb:F0} MB");

        var targets = entries
            .Where(e => e.Installed && !e.Protected && e.Enabled && e.Spec.Presets.Contains(preset))
            .ToList();

        Console.WriteLine($"Applying preset '{preset}' ({targets.Count} active packages to disable)...");
        int disabledOk = 0;
        foreach (var t in targets)
        {
            var (ok, msg) = adb.DisablePackage(serial, t.Spec);
            Console.WriteLine("  " + msg);
            if (ok) disabledOk++;
            else if (msg.Contains("SAFETY ROLLBACK", StringComparison.Ordinal)) return 1;
        }

        var (_, postEntries) = adb.ScanHeadset(serial, catalog);
        var (memBeforeStop, memAfterStop, stoppedCount) = adb.FreeBloatMemory(serial, postEntries);
        var (infoAfter, _) = adb.ScanHeadset(serial, catalog);

        Console.WriteLine($"Done: disabled {disabledOk} packages, force-stopped {stoppedCount} bloat targets.");
        Console.WriteLine($"After RAM available:  {infoAfter.MemAvailableMb:F0} MB / {infoAfter.MemTotalMb:F0} MB (net delta {infoAfter.MemAvailableMb - infoBefore.MemAvailableMb:+0;-0;0} MB, kill delta {memAfterStop - memBeforeStop:+0;-0;0} MB)");
        Console.WriteLine($"HOME verified:        {infoAfter.HomeActivity}");
        return 0;
    }

    private static int RunRestore(AdbService adb, CatalogRoot catalog)
    {
        var serial = RequireSerial(adb);
        var (_, entries) = adb.ScanHeadset(serial, catalog);
        var disabled = entries.Where(e => e.Installed && !e.Enabled && !e.Protected).ToList();
        Console.WriteLine($"Restoring {disabled.Count} disabled catalog packages on {serial}...");
        foreach (var item in disabled)
        {
            var (_, msg) = adb.EnablePackage(serial, item.Spec.Package);
            Console.WriteLine("  " + msg);
        }
        return 0;
    }
}
