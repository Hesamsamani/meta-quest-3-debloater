using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace QuestDebloater;

public sealed class AdbService
{
    private static readonly Regex ProtectedRegex = new(
        @"^(android|android\.ext\..*|com\.qualcomm\..*|horizonos\..*|horizon\.platform\..*|com\.meta\.systemui|com\.oculus\.(vrshell|systemux|systemdriver|systemactivities|systemutilities|systemresource|guardian.*|os\..*|vrbtcontrol|micservice|userserver2|ocms|configuration|panelapp\.(settings|library|devicepairing)|firsttimenux|MiramarSetupRetail|maintenanceboot|externalstorage|extrapermissions|companion\.server|identitymanagement\.service|deviceauthserver|updater|q4bservice|.*\.rro))$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> SafeAndroidExemptions = new(StringComparer.OrdinalIgnoreCase)
    {
        "com.android.adservices.api",
        "com.android.ext.adservices.api",
        "com.android.ondevicepersonalization.services",
        "com.android.federatedcompute.services",
        "com.android.managedprovisioning",
        "com.android.providers.tv",
        "com.android.providers.calendar",
        "com.android.healthconnect.controller",
        "com.android.health.connect.backuprestore",
        "com.android.cts.ctsshim",
        "com.android.cts.priv.ctsshim",
        "com.android.bookmarkprovider",
        "com.android.providers.blockednumber",
        "com.android.nearby.halfsheet",
    };

    public string AdbPath { get; }

    public AdbService()
    {
        AdbPath = ResolveAdbPath();
    }

    public static string ResolveProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 7 && dir != null; i++, dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "scripts"))
                && File.Exists(Path.Combine(dir.FullName, "configs", "packages-catalog.json")))
                return dir.FullName;
        }
        return AppContext.BaseDirectory;
    }

    public static CatalogRoot LoadCatalog()
    {
        var diskPath = Path.Combine(ResolveProjectRoot(), "configs", "packages-catalog.json");
        string json;
        if (File.Exists(diskPath))
        {
            json = File.ReadAllText(diskPath, Encoding.UTF8);
        }
        else
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("packages-catalog.json")
                ?? throw new FileNotFoundException("Embedded packages-catalog.json not found");
            using var reader = new StreamReader(stream, Encoding.UTF8);
            json = reader.ReadToEnd();
        }
        return JsonSerializer.Deserialize<CatalogRoot>(json) ?? new CatalogRoot();
    }

    private static string ResolveAdbPath()
    {
        var localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userProf = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "adb.exe"),
            Path.Combine(localApp, "Programs", "VRmoo", "adb", "adb.exe"),
            Path.Combine(localApp, "Android", "Sdk", "platform-tools", "adb.exe"),
            Path.Combine(userProf, "platform-tools", "adb.exe"),
            @"C:\platform-tools\adb.exe",
        };
        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }
        return "adb.exe";
    }

    public static bool IsHardProtected(CatalogPackage pkg)
    {
        if (string.Equals(pkg.Tier, "PROTECTED", StringComparison.OrdinalIgnoreCase))
            return true;
        return IsHardProtected(pkg.Package);
    }

    public static bool IsHardProtected(string pkgName)
    {
        if (SafeAndroidExemptions.Contains(pkgName))
            return false;
        if (pkgName.StartsWith("com.android.", StringComparison.OrdinalIgnoreCase))
            return true;
        return ProtectedRegex.IsMatch(pkgName);
    }

    public (int ExitCode, string StdOut, string StdErr) RunAdb(IEnumerable<string> args, int timeoutMs = 15000)
    {
        var psi = new ProcessStartInfo(AdbPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var proc = Process.Start(psi) ?? throw new InvalidOperationException($"Cannot start {AdbPath}");
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        if (!proc.WaitForExit(timeoutMs))
        {
            try { proc.Kill(true); } catch { }
            return (-1, stdoutTask.Result, "ADB timeout");
        }
        return (proc.ExitCode, stdoutTask.Result, stderrTask.Result);
    }

    public (int ExitCode, string StdOut, string StdErr) Shell(string serial, string cmd, int timeoutMs = 15000) =>
        RunAdb(new[] { "-s", serial, "shell", cmd }, timeoutMs);

    public List<(string Serial, string Label)> ListDevices()
    {
        var list = new List<(string, string)>();
        var (_, stdout, _) = RunAdb(new[] { "devices", "-l" }, 8000);
        foreach (var raw in stdout.Split('\n', '\r'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("List of devices", StringComparison.OrdinalIgnoreCase))
                continue;
            var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || parts[1] != "device")
                continue;
            var serial = parts[0];
            var modelMatch = Regex.Match(line, @"model:(\S+)");
            var label = modelMatch.Success ? $"{modelMatch.Groups[1].Value.Replace('_', ' ')} ({serial})" : serial;
            list.Add((serial, label));
        }
        return list;
    }

    public (HeadsetInfo Info, List<LivePackageEntry> Entries) ScanHeadset(string serial, CatalogRoot catalog)
    {
        var info = new HeadsetInfo { Serial = serial };

        var probeCmd = string.Join(" && ", new[]
        {
            "echo __PROPS__",
            "getprop ro.product.manufacturer",
            "echo __MODEL__",
            "getprop ro.product.model",
            "echo __DEV__",
            "getprop ro.product.device",
            "echo __ANDR__",
            "getprop ro.build.version.release",
            "echo __FW__",
            "getprop ro.build.display.id",
            "echo __HOME__",
            "cmd package resolve-activity --brief -c android.intent.category.HOME -a android.intent.action.MAIN | tail -n 1",
            "echo __GUARDIAN__",
            "settings get secure user_guardian",
            "echo __BAT__",
            "dumpsys battery",
            "echo __MEM__",
            "head -n 8 /proc/meminfo",
            "echo __DF__",
            "df -h /data | tail -n 1",
            "echo __ALL__",
            "pm list packages",
            "echo __SYS__",
            "pm list packages -s",
            "echo __DIS__",
            "pm list packages -d",
            "echo __PROC__",
            "dumpsys meminfo -c | grep -E '^proc,' || true"
        });

        var (_, stdout, _) = Shell(serial, probeCmd, 25000);
        var sections = ParseSections(stdout);

        info.Model = sections.GetValueOrDefault("__MODEL__", "").Trim();
        if (string.IsNullOrWhiteSpace(info.Model)) info.Model = "Meta Quest 3";
        info.DeviceCode = sections.GetValueOrDefault("__DEV__", "").Trim();
        info.AndroidVersion = sections.GetValueOrDefault("__ANDR__", "").Trim();
        info.FirmwareDisplay = sections.GetValueOrDefault("__FW__", "").Trim();
        info.HomeActivity = sections.GetValueOrDefault("__HOME__", "").Trim();
        info.GuardianEnabled = !string.Equals(sections.GetValueOrDefault("__GUARDIAN__", "1").Trim(), "0", StringComparison.Ordinal);

        // Parse battery
        foreach (var line in sections.GetValueOrDefault("__BAT__", "").Split('\n'))
        {
            var t = line.Trim();
            if (t.StartsWith("level:") && int.TryParse(t[6..].Trim(), out int lvl)) info.BatteryLevel = lvl;
            if (t.StartsWith("temperature:") && double.TryParse(t[12..].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out double temp))
                info.BatteryTempC = temp / 10.0;
        }

        // Parse meminfo
        foreach (var line in sections.GetValueOrDefault("__MEM__", "").Split('\n'))
        {
            var m = Regex.Match(line, @"^(MemTotal|MemAvailable):\s+(\d+)\s+kB");
            if (!m.Success) continue;
            double mb = double.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) / 1024.0;
            if (m.Groups[1].Value == "MemTotal") info.MemTotalMb = mb;
            else info.MemAvailableMb = mb;
        }

        // Parse storage df -h
        var dfParts = sections.GetValueOrDefault("__DF__", "").Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (dfParts.Length >= 5)
            info.StorageSummary = $"{dfParts[2]} / {dfParts[1]} ({dfParts[4]})";

        var allPkgs = ParsePackageSet(sections.GetValueOrDefault("__ALL__", ""));
        var sysPkgs = ParsePackageSet(sections.GetValueOrDefault("__SYS__", ""));
        var disPkgs = ParsePackageSet(sections.GetValueOrDefault("__DIS__", ""));

        info.TotalInstalledCount = allPkgs.Count;
        info.DisabledCount = disPkgs.Count;

        // Sum process RSS per package
        var ramByPkg = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in sections.GetValueOrDefault("__PROC__", "").Split('\n'))
        {
            // proc,<adj>,<procName>,<pid>,<pssKb>,...
            var cols = line.Trim().Split(',');
            if (cols.Length < 5 || cols[0] != "proc") continue;
            var procName = cols[2];
            var colonIdx = procName.IndexOf(':');
            var basePkg = colonIdx > 0 ? procName[..colonIdx] : procName;
            if (double.TryParse(cols[4], NumberStyles.Any, CultureInfo.InvariantCulture, out double kb))
            {
                ramByPkg[basePkg] = ramByPkg.GetValueOrDefault(basePkg) + (kb / 1024.0);
            }
        }

        var entries = new List<LivePackageEntry>();
        var catalogSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var spec in catalog.Packages)
        {
            catalogSeen.Add(spec.Package);
            bool installed = allPkgs.Contains(spec.Package);
            bool disabled = disPkgs.Contains(spec.Package);
            entries.Add(new LivePackageEntry
            {
                Spec = spec,
                Installed = installed,
                Enabled = installed && !disabled,
                SystemApp = sysPkgs.Contains(spec.Package),
                RamMb = Math.Round(ramByPkg.GetValueOrDefault(spec.Package), 1),
                Protected = IsHardProtected(spec),
            });
        }

        // Also surface any extra Meta/Oculus/Facebook/Horizon packages on device not in static catalog
        foreach (var pkg in allPkgs.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            if (catalogSeen.Contains(pkg)) continue;
            if (!pkg.StartsWith("com.oculus.", StringComparison.OrdinalIgnoreCase)
                && !pkg.StartsWith("com.meta.", StringComparison.OrdinalIgnoreCase)
                && !pkg.StartsWith("com.facebook.", StringComparison.OrdinalIgnoreCase)
                && !pkg.StartsWith("horizon", StringComparison.OrdinalIgnoreCase))
                continue;

            bool prot = IsHardProtected(pkg);
            bool disabled = disPkgs.Contains(pkg);
            entries.Add(new LivePackageEntry
            {
                Spec = new CatalogPackage
                {
                    Package = pkg,
                    Name = pkg,
                    Category = prot ? "Protected" : "Discovered Meta/OS",
                    Tier = prot ? "PROTECTED" : "CAUTION",
                    Description = prot
                        ? "Horizon OS core component detected on device (protected)."
                        : "Additional Meta/Horizon package discovered on this firmware build."
                },
                Installed = true,
                Enabled = !disabled,
                SystemApp = sysPkgs.Contains(pkg),
                RamMb = Math.Round(ramByPkg.GetValueOrDefault(pkg), 1),
                Protected = prot,
            });
        }

        return (info, entries);
    }

    public (bool Ok, string Message) DisablePackage(string serial, CatalogPackage spec)
    {
        if (IsHardProtected(spec))
            return (false, $"REFUSED (protected OS package): {spec.Package}");

        var (rc, outStr, errStr) = Shell(serial, $"pm disable-user --user 0 {spec.Package} && am force-stop {spec.Package}");
        var combined = (outStr + " " + errStr).Trim();
        if (rc != 0)
            return (false, $"Failed {spec.Package}: {combined}");

        // Safety verify HOME activity stays com.oculus.vrshell
        var (_, homeOut, _) = Shell(serial, "cmd package resolve-activity --brief -c android.intent.category.HOME -a android.intent.action.MAIN | tail -n 1");
        var home = homeOut.Trim();
        if (!home.StartsWith("com.oculus.vrshell/", StringComparison.OrdinalIgnoreCase))
        {
            Shell(serial, $"pm enable {spec.Package}");
            return (false, $"SAFETY ROLLBACK on {spec.Package}: HOME changed to '{home}', re-enabled immediately!");
        }

        return (true, $"Disabled {spec.Package}");
    }

    public (bool Ok, string Message) EnablePackage(string serial, string pkg)
    {
        var (rc, outStr, errStr) = Shell(serial, $"pm enable {pkg}");
        var combined = (outStr + " " + errStr).Trim();
        return rc == 0
            ? (true, $"Enabled {pkg}")
            : (false, $"Enable {pkg} returned rc={rc}: {combined}");
    }

    public (double BeforeAvailMb, double AfterAvailMb, int StoppedCount) FreeBloatMemory(string serial, IEnumerable<LivePackageEntry> entries)
    {
        double before = ReadMemAvailableMb(serial);
        var targets = entries
            .Where(e => e.Installed && !e.Protected && (!e.Enabled || string.Equals(e.Spec.Tier, "SAFE", StringComparison.OrdinalIgnoreCase)))
            .Select(e => $"am force-stop {e.Spec.Package}")
            .ToList();

        if (targets.Count > 0)
        {
            Shell(serial, string.Join("; ", targets), 20000);
        }
        double after = ReadMemAvailableMb(serial);
        return (before, after, targets.Count);
    }

    public double ReadMemAvailableMb(string serial)
    {
        var (_, memOut, _) = Shell(serial, "grep '^MemAvailable:' /proc/meminfo");
        var m = Regex.Match(memOut, @"(\d+)");
        return m.Success ? double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) / 1024.0 : 0;
    }

    public string SaveBackup(string serial, HeadsetInfo info, List<LivePackageEntry> entries, bool isInitialBefore = false)
    {
        var backupDir = Path.Combine(ResolveProjectRoot(), "backups");
        Directory.CreateDirectory(backupDir);
        var snap = new BackupSnapshot
        {
            TimestampUtc = DateTime.UtcNow.ToString("O"),
            Headset = info,
            PreviouslyDisabledPackages = entries.Where(e => e.Installed && !e.Enabled).Select(e => e.Spec.Package).OrderBy(p => p).ToList(),
            CatalogStates = entries.Select(e => new CatalogPackageState
            {
                Package = e.Spec.Package,
                Tier = e.Spec.Tier,
                Installed = e.Installed,
                Enabled = e.Enabled,
                RamMb = e.RamMb
            }).ToList()
        };

        var fileName = isInitialBefore
            ? $"quest3-{serial}-before.json"
            : $"quest3-{serial}-{DateTime.Now:yyyyMMdd-HHmmss}.json";
        var path = Path.Combine(backupDir, fileName);
        if (isInitialBefore && File.Exists(path))
            return path;

        File.WriteAllText(path, JsonSerializer.Serialize(snap, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        return path;
    }

    public string SetRefreshRate(string serial, int hz)
    {
        Shell(serial, $"setprop debug.oculus.refreshRate {hz}");
        return $"Set debug.oculus.refreshRate={hz}";
    }

    public string SetFoveation(string serial, int level, bool dynamic)
    {
        Shell(serial, $"setprop debug.oculus.foveation.level {level} && setprop debug.oculus.foveation.dynamic {(dynamic ? 1 : 0)}");
        return $"Set foveation level={level}, dynamic={(dynamic ? 1 : 0)}";
    }

    public string SetPerfLevel(string serial, int level)
    {
        Shell(serial, $"setprop debug.oculus.cpuLevel {level} && setprop debug.oculus.gpuLevel {level}");
        return $"Set cpuLevel={level}, gpuLevel={level}";
    }

    public string ToggleFpsOverlay(string serial, bool enable)
    {
        var action = enable ? "com.oculus.ovrmonitormetricsservice.ENABLE_OVERLAY" : "com.oculus.ovrmonitormetricsservice.DISABLE_OVERLAY";
        Shell(serial, $"am broadcast -n com.oculus.ovrmonitormetricsservice/.SettingsBroadcastReceiver -a {action}");
        return $"OVR Metrics Overlay {(enable ? "ENABLED" : "DISABLED")}";
    }

    public string ToggleKeepAwake(string serial, bool keepAwake)
    {
        var action = keepAwake ? "com.oculus.vrpowermanager.prox_close" : "com.oculus.vrpowermanager.automation_disable";
        Shell(serial, $"am broadcast -a {action}");
        return $"Proximity Keep-Awake {(keepAwake ? "ENABLED" : "DISABLED")}";
    }

    public string ToggleGuardian(string serial, bool enable)
    {
        Shell(serial, $"settings put secure user_guardian {(enable ? 1 : 0)}");
        return $"Guardian boundary {(enable ? "ENABLED" : "DISABLED")}";
    }

    public string RestartVrShell(string serial)
    {
        Shell(serial, "am force-stop com.oculus.vrshell");
        return "Restarted com.oculus.vrshell";
    }

    private static Dictionary<string, string> ParseSections(string output)
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        string currentKey = "";
        var sb = new StringBuilder();
        foreach (var raw in output.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.StartsWith("__") && line.EndsWith("__"))
            {
                if (currentKey.Length > 0) dict[currentKey] = sb.ToString();
                currentKey = line;
                sb.Clear();
            }
            else if (currentKey.Length > 0)
            {
                sb.AppendLine(line);
            }
        }
        if (currentKey.Length > 0) dict[currentKey] = sb.ToString();
        return dict;
    }

    private static HashSet<string> ParsePackageSet(string text)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in text.Split('\n'))
        {
            var t = raw.Trim();
            if (t.StartsWith("package:", StringComparison.Ordinal))
                set.Add(t[8..].Trim());
        }
        return set;
    }
}
