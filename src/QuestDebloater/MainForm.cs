using System.Drawing;
using System.Reflection;
using System.Text;

namespace QuestDebloater;

public sealed class MainForm : Form
{
    private static readonly Color BgDark = Color.FromArgb(11, 15, 23);
    private static readonly Color Surface = Color.FromArgb(19, 26, 38);
    private static readonly Color SurfaceAlt = Color.FromArgb(26, 36, 54);
    private static readonly Color BorderCol = Color.FromArgb(38, 50, 72);
    private static readonly Color TextPrimary = Color.FromArgb(241, 245, 249);
    private static readonly Color TextMuted = Color.FromArgb(148, 163, 184);
    private static readonly Color AccentBlue = Color.FromArgb(59, 130, 246);
    private static readonly Color SafeGreen = Color.FromArgb(16, 185, 129);
    private static readonly Color WarnAmber = Color.FromArgb(245, 158, 11);
    private static readonly Color CautionOrange = Color.FromArgb(249, 115, 22);
    private static readonly Color DangerRed = Color.FromArgb(239, 68, 68);
    private static readonly Color Purple = Color.FromArgb(139, 92, 246);

    private readonly AdbService _adb;
    private readonly CatalogRoot _catalog;
    private readonly Dictionary<string, bool> _checked = new(StringComparer.OrdinalIgnoreCase);

    private List<LivePackageEntry> _allEntries = new();
    private List<LivePackageEntry> _filteredEntries = new();
    private HeadsetInfo? _headset;
    private string _activeCategory = "All";
    private bool _busy;
    private Task? _initialScanTask;

    private ComboBox _cboDevices = null!;
    private Label _lblModelInfo = null!;
    private Label _lblBattery = null!;
    private Label _lblRam = null!;
    private Label _lblStats = null!;
    private ProgressBar _ramBar = null!;
    private TextBox _txtSearch = null!;
    private DataGridView _grid = null!;
    private RichTextBox _logBox = null!;
    private FlowLayoutPanel _catBar = null!;
    private Button _btnSafe = null!, _btnTelemetry = null!, _btnPcvr = null!, _btnClearSel = null!;
    private Button _btnDisable = null!, _btnEnable = null!, _btnRestoreAll = null!;
    private Panel _rootPanel = null!;

    public MainForm(AdbService adb, CatalogRoot catalog)
    {
        _adb = adb;
        _catalog = catalog;
        Text = "Meta Quest 3 Debloater & Privacy Manager";
        Size = new Size(1440, 955);
        MinimumSize = new Size(1200, 760);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgDark;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 9.5f);

        BuildUi();
        Shown += (_, _) => _initialScanTask = RefreshHeadsetAsync();
    }

    public async Task WaitForInitialLoadAsync()
    {
        while (_initialScanTask == null) await Task.Delay(30);
        await _initialScanTask;
        while (_busy) await Task.Delay(30);
        await Task.Delay(150);
    }

    private void BuildUi()
    {
        _rootPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgDark, Padding = new Padding(14) };
        Controls.Add(_rootPanel);

        // 1. Top Header & Dashboard Cards
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 128, BackColor = Color.Transparent };
        BuildTopDashboard(topPanel);

        // 2. Preset + Filter Toolbar
        var filterPanel = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 6) };
        BuildPresetAndFilterBar(filterPanel);

        // 3. Bottom Action Bar & Console Log
        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 178, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0) };
        BuildBottomBar(bottomPanel);

        // 4. Middle Area: Grid (left/fill) + Quest 3 Quick Tweaks Sidebar (right)
        var midPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        var rightSidebar = new Panel { Dock = DockStyle.Right, Width = 304, BackColor = Surface, Padding = new Padding(12, 10, 12, 10) };
        BuildTweaksSidebar(rightSidebar);

        var gridHost = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(1, 1, 10, 1) };
        BuildGrid();
        gridHost.Controls.Add(_grid);

        var spacer = new Panel { Dock = DockStyle.Right, Width = 10, BackColor = BgDark };
        midPanel.Controls.Add(gridHost);
        midPanel.Controls.Add(spacer);
        midPanel.Controls.Add(rightSidebar);

        _rootPanel.Controls.Add(midPanel);
        _rootPanel.Controls.Add(filterPanel);
        _rootPanel.Controls.Add(topPanel);
        _rootPanel.Controls.Add(bottomPanel);
    }

    private void BuildTopDashboard(Panel parent)
    {
        var titleRow = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = Surface, Padding = new Padding(12, 8, 12, 8) };

        var leftFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 3, 0, 0)
        };
        var lblTitle = new Label
        {
            Text = "META QUEST 3 DEBLOATER & PRIVACY MANAGER",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = TextPrimary,
            AutoSize = true,
            UseMnemonic = false,
            Margin = new Padding(0, 3, 14, 0)
        };
        var lblSub = new Label
        {
            Text = "Reversible Horizon OS Debloating via ADB  •  OS Home Guard Active",
            Font = new Font("Segoe UI", 9f),
            ForeColor = SafeGreen,
            AutoSize = true,
            UseMnemonic = false,
            Margin = new Padding(0, 6, 0, 0)
        };
        leftFlow.Controls.AddRange(new Control[] { lblTitle, lblSub });

        var rightFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 1, 0, 0)
        };
        _cboDevices = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            BackColor = SurfaceAlt,
            ForeColor = TextPrimary,
            FlatStyle = FlatStyle.Flat,
            Width = 225,
            Margin = new Padding(0, 3, 8, 0)
        };
        var btnScan = MakeButton("Scan Headset", AccentBlue, 114, 28);
        btnScan.Click += async (_, _) => await RefreshHeadsetAsync();
        var btnBackup = MakeButton("Backup State", SurfaceAlt, 110, 28);
        btnBackup.Click += async (_, _) => await SaveBackupAsync();
        rightFlow.Controls.AddRange(new Control[] { _cboDevices, btnScan, btnBackup });

        titleRow.Controls.AddRange(new Control[] { rightFlow, leftFlow });

        // Stat cards row
        var cardsRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };
        for (int i = 0; i < 4; i++) cardsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));

        _lblModelInfo = MakeCard(cardsRow, 0, "HEADSET & HORIZON OS", "Scanning ADB...", AccentBlue);
        _lblBattery = MakeCard(cardsRow, 1, "BATTERY & THERMALS", "--", SafeGreen);

        var ramCard = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Margin = new Padding(4, 0, 4, 0), Padding = new Padding(12, 8, 12, 8) };
        var ramTitle = new Label { Text = "HEADSET MEMORY (RAM)", Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = TextMuted, Dock = DockStyle.Top, Height = 18, UseMnemonic = false };
        _lblRam = new Label { Text = "-- / --", Font = new Font("Segoe UI", 10.5f, FontStyle.Bold), ForeColor = Purple, Dock = DockStyle.Top, Height = 26, UseMnemonic = false };
        _ramBar = new ProgressBar { Dock = DockStyle.Bottom, Height = 8, Style = ProgressBarStyle.Continuous, Value = 0 };
        ramCard.Controls.AddRange(new Control[] { _lblRam, ramTitle, _ramBar });
        cardsRow.Controls.Add(ramCard, 2, 0);

        _lblStats = MakeCard(cardsRow, 3, "BLOAT & STORAGE STATUS", "--", WarnAmber);

        parent.Controls.Add(cardsRow);
        parent.Controls.Add(titleRow);
    }

    private static Label MakeCard(TableLayoutPanel parent, int col, string title, string value, Color accent)
    {
        var card = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Margin = new Padding(col == 0 ? 0 : 4, 0, col == 3 ? 0 : 4, 0), Padding = new Padding(12, 8, 12, 8) };
        var lblT = new Label { Text = title, Font = new Font("Segoe UI", 8f, FontStyle.Bold), ForeColor = TextMuted, Dock = DockStyle.Top, Height = 18, UseMnemonic = false };
        var lblV = new Label { Text = value, Font = new Font("Segoe UI", 10f, FontStyle.Bold), ForeColor = accent, Dock = DockStyle.Fill, UseMnemonic = false };
        card.Controls.AddRange(new Control[] { lblV, lblT });
        parent.Controls.Add(card, col, 0);
        return lblV;
    }

    private void BuildPresetAndFilterBar(Panel parent)
    {
        int safeCount = _catalog.Packages.Count(p => p.Presets.Contains("safe"));
        int telCount = _catalog.Packages.Count(p => p.Presets.Contains("telemetry"));
        int pcvrCount = _catalog.Packages.Count(p => p.Presets.Contains("pcvr"));

        var row1 = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, BackColor = Color.Transparent, WrapContents = false };
        var lblPreset = new Label { Text = "Presets:", ForeColor = TextMuted, AutoSize = true, UseMnemonic = false, Margin = new Padding(0, 7, 8, 0), Font = new Font("Segoe UI", 9.5f, FontStyle.Bold) };

        _btnSafe = MakeButton($"Safe Recommended ({safeCount})", SafeGreen, 196, 30);
        _btnSafe.ForeColor = Color.Black;
        _btnSafe.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _btnSafe.Click += (_, _) => ApplyPresetSelection("safe");

        _btnTelemetry = MakeButton($"Telemetry & Ads ({telCount})", AccentBlue, 180, 30);
        _btnTelemetry.Click += (_, _) => ApplyPresetSelection("telemetry");

        _btnPcvr = MakeButton($"PCVR / Minimalist ({pcvrCount})", WarnAmber, 188, 30);
        _btnPcvr.ForeColor = Color.Black;
        _btnPcvr.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _btnPcvr.Click += (_, _) => ApplyPresetSelection("pcvr");

        _btnClearSel = MakeButton("Select None", SurfaceAlt, 106, 30);
        _btnClearSel.Click += (_, _) => ApplyPresetSelection("none");

        var btnFreeRam = MakeButton("Free RAM (Kill Bloat)", Purple, 188, 30);
        btnFreeRam.Margin = new Padding(12, 2, 4, 2);
        btnFreeRam.Click += async (_, _) => await RunFreeMemoryAsync();

        var lblSearch = new Label { Text = "Search:", ForeColor = TextMuted, AutoSize = true, UseMnemonic = false, Margin = new Padding(14, 7, 6, 0) };
        _txtSearch = new TextBox
        {
            BackColor = Surface,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.FixedSingle,
            Width = 225,
            Margin = new Padding(0, 4, 0, 0),
            PlaceholderText = "package, telemetry, social..."
        };
        _txtSearch.TextChanged += (_, _) => PopulateGrid();

        row1.Controls.AddRange(new Control[] { lblPreset, _btnSafe, _btnTelemetry, _btnPcvr, _btnClearSel, btnFreeRam, lblSearch, _txtSearch });

        _catBar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 36, BackColor = Color.Transparent, WrapContents = false };
        var cats = new[] { "All", "Telemetry & Ads", "Social & Horizon", "Bloat & Demos", "Environments", "Caution", "Protected", "Disabled Only" };
        foreach (var cat in cats)
        {
            var b = MakeButton(cat, cat == "All" ? AccentBlue : Surface, 130, 28);
            b.AutoSize = true;
            b.MinimumSize = new Size(92, 28);
            b.Padding = new Padding(8, 0, 8, 0);
            b.Tag = cat;
            b.Click += (_, _) =>
            {
                _activeCategory = cat;
                foreach (Control ctrl in _catBar.Controls)
                {
                    if (ctrl is Button cb) cb.BackColor = (string?)cb.Tag == cat ? AccentBlue : Surface;
                }
                PopulateGrid();
            };
            _catBar.Controls.Add(b);
        }

        parent.Controls.Add(_catBar);
        parent.Controls.Add(row1);
    }

    private void BuildGrid()
    {
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Surface,
            GridColor = BorderCol,
            BorderStyle = BorderStyle.None,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersHeight = 34,
            RowTemplate = { Height = 30 }
        };

        // Enable double buffering to eliminate flicker
        typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?
            .SetValue(_grid, true, null);

        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = SurfaceAlt,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            SelectionBackColor = SurfaceAlt,
            Padding = new Padding(4, 0, 4, 0)
        };
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Surface,
            ForeColor = TextPrimary,
            SelectionBackColor = Color.FromArgb(30, 58, 108),
            SelectionForeColor = TextPrimary,
            Padding = new Padding(4, 0, 4, 0)
        };
        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(22, 30, 44),
            ForeColor = TextPrimary,
            SelectionBackColor = Color.FromArgb(30, 58, 108),
            SelectionForeColor = TextPrimary
        };

        var colChk = new DataGridViewCheckBoxColumn { Name = "Sel", HeaderText = "", Width = 36, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
        var colName = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Feature", FillWeight = 18, ReadOnly = true };
        var colPkg = new DataGridViewTextBoxColumn { Name = "Package", HeaderText = "Package ID", FillWeight = 24, ReadOnly = true };
        var colCat = new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category", FillWeight = 13, ReadOnly = true };
        var colTier = new DataGridViewTextBoxColumn { Name = "Risk", HeaderText = "Risk Tier", FillWeight = 10, ReadOnly = true };
        var colStatus = new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Live Status", FillWeight = 11, ReadOnly = true };
        var colRam = new DataGridViewTextBoxColumn { Name = "Ram", HeaderText = "RAM", FillWeight = 8, ReadOnly = true };
        var colDesc = new DataGridViewTextBoxColumn { Name = "Desc", HeaderText = "Description", FillWeight = 34, ReadOnly = true };

        _grid.Columns.AddRange(colChk, colName, colPkg, colCat, colTier, colStatus, colRam, colDesc);

        _grid.CellContentClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0 && e.RowIndex < _filteredEntries.Count)
            {
                var entry = _filteredEntries[e.RowIndex];
                if (entry.Protected)
                {
                    _grid.CancelEdit();
                    LogMessage($"Protected package locked: {entry.Spec.Package}");
                    return;
                }
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                bool val = Convert.ToBoolean(_grid.Rows[e.RowIndex].Cells[0].Value);
                _checked[entry.Spec.Package] = val;
            }
        };

        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _filteredEntries.Count) return;
            var entry = _filteredEntries[e.RowIndex];
            if (_grid.Columns[e.ColumnIndex].Name == "Risk")
            {
                e.CellStyle!.ForeColor = entry.Spec.Tier switch
                {
                    "SAFE" => SafeGreen,
                    "MODERATE" => WarnAmber,
                    "CAUTION" => CautionOrange,
                    _ => DangerRed
                };
                e.CellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            }
            else if (_grid.Columns[e.ColumnIndex].Name == "Status")
            {
                e.CellStyle!.ForeColor = entry.LiveStatus switch
                {
                    "ACTIVE" => SafeGreen,
                    "DISABLED" => WarnAmber,
                    _ => TextMuted
                };
                e.CellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            }
        };
    }

    private void BuildTweaksSidebar(Panel sidebar)
    {
        var hdr = new Label
        {
            Text = "QUEST 3 QUICK TWEAKS",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = AccentBlue,
            Dock = DockStyle.Top,
            UseMnemonic = false,
            Height = 24
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false
        };

        AddTweakSection(flow, "Refresh Rate Override",
            ("72 Hz", () => RunTweak(s => _adb.SetRefreshRate(s, 72))),
            ("90 Hz", () => RunTweak(s => _adb.SetRefreshRate(s, 90))),
            ("120 Hz", () => RunTweak(s => _adb.SetRefreshRate(s, 120))));

        AddTweakSection(flow, "Fixed Foveated Rendering (FFR)",
            ("Off", () => RunTweak(s => _adb.SetFoveation(s, 0, false))),
            ("Med", () => RunTweak(s => _adb.SetFoveation(s, 2, false))),
            ("High", () => RunTweak(s => _adb.SetFoveation(s, 3, false))),
            ("Dyn", () => RunTweak(s => _adb.SetFoveation(s, 2, true))));

        AddTweakSection(flow, "CPU / GPU Clock Level",
            ("Auto", () => RunTweak(s => _adb.SetPerfLevel(s, -1))),
            ("Max Level 4", () => RunTweak(s => _adb.SetPerfLevel(s, 4))));

        AddTweakSection(flow, "HUD & Power Diagnostics",
            ("FPS HUD On", () => RunTweak(s => _adb.ToggleFpsOverlay(s, true))),
            ("FPS HUD Off", () => RunTweak(s => _adb.ToggleFpsOverlay(s, false))));

        AddTweakSection(flow, "Display & Boundary",
            ("Keep Awake", () => RunTweak(s => _adb.ToggleKeepAwake(s, true))),
            ("Auto Sleep", () => RunTweak(s => _adb.ToggleKeepAwake(s, false))),
            ("Guardian On", () => RunTweak(s => _adb.ToggleGuardian(s, true))),
            ("Guardian Off", () => RunTweak(s => _adb.ToggleGuardian(s, false))));

        var btnRestartShell = MakeButton("Restart VR Shell (vrshell)", SurfaceAlt, 272, 30);
        btnRestartShell.Margin = new Padding(0, 10, 0, 2);
        btnRestartShell.Click += async (_, _) => await RunTweak(s => _adb.RestartVrShell(s));
        flow.Controls.Add(btnRestartShell);

        sidebar.Controls.Add(flow);
        sidebar.Controls.Add(hdr);
    }

    private void AddTweakSection(FlowLayoutPanel parent, string title, params (string Label, Func<Task> Action)[] buttons)
    {
        var lbl = new Label
        {
            Text = title,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            AutoSize = true,
            UseMnemonic = false,
            Margin = new Padding(0, 5, 0, 2)
        };
        parent.Controls.Add(lbl);

        var row = new FlowLayoutPanel { Width = 278, AutoSize = true, WrapContents = true, Margin = new Padding(0) };
        int w = buttons.Length <= 2 ? 132 : (buttons.Length == 3 ? 86 : 132);
        foreach (var (text, action) in buttons)
        {
            var b = MakeButton(text, SurfaceAlt, w, 29);
            b.Click += async (_, _) => await action();
            row.Controls.Add(b);
        }
        parent.Controls.Add(row);
    }

    private void BuildBottomBar(Panel parent)
    {
        var actionRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, BackColor = Color.Transparent };

        _btnDisable = MakeButton("Disable Selected (Reversible)", DangerRed, 235, 34);
        _btnDisable.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _btnDisable.Click += async (_, _) => await ApplySelectionAsync(disable: true);

        _btnEnable = MakeButton("Re-Enable Selected", SafeGreen, 175, 34);
        _btnEnable.ForeColor = Color.Black;
        _btnEnable.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _btnEnable.Click += async (_, _) => await ApplySelectionAsync(disable: false);

        _btnRestoreAll = MakeButton("Restore All Meta Defaults", SurfaceAlt, 200, 34);
        _btnRestoreAll.Click += async (_, _) => await RestoreAllAsync();

        var lblNote = new Label
        {
            Text = "Uses reversible 'pm disable-user --user 0' (never uninstalls) and verifies VR Home after each step.",
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(14, 9, 0, 0)
        };

        actionRow.Controls.AddRange(new Control[] { _btnDisable, _btnEnable, _btnRestoreAll, lblNote });

        _logBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(9, 13, 20),
            ForeColor = Color.FromArgb(203, 213, 225),
            BorderStyle = BorderStyle.FixedSingle,
            ReadOnly = true,
            Font = new Font("Consolas", 9f),
        };

        parent.Controls.Add(_logBox);
        parent.Controls.Add(actionRow);
    }

    public async Task RefreshHeadsetAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            var devices = await Task.Run(() => _adb.ListDevices());
            _cboDevices.Items.Clear();
            foreach (var d in devices) _cboDevices.Items.Add(d.Label);
            if (devices.Count == 0)
            {
                _lblModelInfo.Text = "No Quest connected";
                _lblBattery.Text = "Connect via USB / ADB";
                _lblRam.Text = "--";
                _lblStats.Text = "--";
                LogMessage("No authorized ADB device found. Enable Developer Mode and connect Quest 3.");
                return;
            }
            _cboDevices.SelectedIndex = 0;
            var serial = devices[0].Serial;
            LogMessage($"Scanning headset {serial} via {_adb.AdbPath}...");

            var (info, entries) = await Task.Run(() => _adb.ScanHeadset(serial, _catalog));
            _headset = info;
            _allEntries = entries;

            // Default check unchecked for already-disabled; allow Safe preset selection on first scan
            if (_checked.Count == 0)
            {
                foreach (var e in _allEntries.Where(x => x.Spec.Presets.Contains("safe") && x.Installed && x.Enabled && !x.Protected))
                    _checked[e.Spec.Package] = true;
            }

            UpdateDashboardCards(info, entries);
            PopulateGrid();
            LogMessage($"Scan complete: {info.TotalInstalledCount} packages installed, {info.DisabledCount} disabled, {info.MemAvailableMb:F0} MB RAM available, HOME={info.HomeActivity}");
        }
        catch (Exception ex)
        {
            LogMessage("Scan error: " + ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    private void UpdateDashboardCards(HeadsetInfo info, List<LivePackageEntry> entries)
    {
        _lblModelInfo.Text = $"{info.Model} • Android {info.AndroidVersion}\r\n{info.Serial} ({info.DeviceCode})";
        _lblBattery.Text = info.BatteryLevel >= 0 ? $"{info.BatteryLevel}% • {info.BatteryTempC:F1}°C\r\nGuardian: {(info.GuardianEnabled ? "ON" : "OFF")}" : "--";
        _lblRam.Text = $"{info.MemAvailableMb / 1024.0:F2} GB free / {info.MemTotalMb / 1024.0:F2} GB";
        if (info.MemTotalMb > 0)
            _ramBar.Value = Math.Clamp((int)Math.Round(100.0 * info.MemUsedMb / info.MemTotalMb), 0, 100);

        int safeDisabled = entries.Count(e => e.Spec.Tier == "SAFE" && e.Installed && !e.Enabled);
        int safeTotal = entries.Count(e => e.Spec.Tier == "SAFE" && e.Installed);
        _lblStats.Text = $"Safe Bloat Disabled: {safeDisabled} / {safeTotal}\r\nStorage: {info.StorageSummary}";
    }

    private void PopulateGrid()
    {
        var q = _txtSearch.Text.Trim();
        _filteredEntries = _allEntries.Where(e =>
        {
            bool catMatch = _activeCategory switch
            {
                "All" => true,
                "Caution" => e.Spec.Tier == "CAUTION",
                "Protected" => e.Protected || e.Spec.Tier == "PROTECTED",
                "Disabled Only" => e.Installed && !e.Enabled,
                _ => string.Equals(e.Spec.Category, _activeCategory, StringComparison.OrdinalIgnoreCase)
            };
            if (!catMatch) return false;
            if (q.Length == 0) return true;
            return e.Spec.Package.Contains(q, StringComparison.OrdinalIgnoreCase)
                || e.Spec.Name.Contains(q, StringComparison.OrdinalIgnoreCase)
                || e.Spec.Description.Contains(q, StringComparison.OrdinalIgnoreCase);
        }).ToList();

        _grid.Rows.Clear();
        foreach (var e in _filteredEntries)
        {
            bool isChecked = !e.Protected && _checked.GetValueOrDefault(e.Spec.Package);
            int idx = _grid.Rows.Add(
                isChecked,
                e.Spec.Name,
                e.Spec.Package,
                e.Spec.Category,
                e.Spec.Tier,
                e.LiveStatus,
                e.RamMb > 0.1 ? $"{e.RamMb:F0} MB" : "-",
                e.Spec.Description);
            if (e.Protected)
                _grid.Rows[idx].Cells[0].ReadOnly = true;
        }
    }

    private void ApplyPresetSelection(string preset)
    {
        _checked.Clear();
        if (preset != "none")
        {
            foreach (var e in _allEntries)
            {
                if (!e.Protected && e.Installed && e.Spec.Presets.Contains(preset))
                    _checked[e.Spec.Package] = true;
            }
        }
        PopulateGrid();
        int count = _checked.Values.Count(v => v);
        LogMessage($"Selected preset '{preset}': {count} installed packages queued.");
    }

    private async Task ApplySelectionAsync(bool disable)
    {
        if (_busy || _headset == null) return;
        var selected = _allEntries.Where(e => !e.Protected && e.Installed && _checked.GetValueOrDefault(e.Spec.Package)).ToList();
        if (selected.Count == 0)
        {
            LogMessage("No installed non-protected packages selected.");
            return;
        }

        _busy = true;
        try
        {
            var serial = _headset.Serial;
            if (disable)
            {
                var backupPath = await Task.Run(() => _adb.SaveBackup(serial, _headset, _allEntries, isInitialBefore: true));
                LogMessage($"Baseline backup verified at {backupPath}");
            }

            foreach (var item in selected)
            {
                var (ok, msg) = await Task.Run(() =>
                    disable ? _adb.DisablePackage(serial, item.Spec) : _adb.EnablePackage(serial, item.Spec.Package));
                LogMessage(msg);
                if (!ok && msg.Contains("SAFETY ROLLBACK", StringComparison.Ordinal)) break;
            }
        }
        finally
        {
            _busy = false;
        }
        await RefreshHeadsetAsync();
    }

    private async Task RestoreAllAsync()
    {
        if (_busy || _headset == null) return;
        _busy = true;
        try
        {
            var serial = _headset.Serial;
            var targets = _allEntries.Where(e => e.Installed && !e.Enabled && !e.Protected).ToList();
            LogMessage($"Restoring {targets.Count} disabled packages on {serial}...");
            foreach (var item in targets)
            {
                var (_, msg) = await Task.Run(() => _adb.EnablePackage(serial, item.Spec.Package));
                LogMessage(msg);
            }
        }
        finally
        {
            _busy = false;
        }
        await RefreshHeadsetAsync();
    }

    private async Task SaveBackupAsync()
    {
        if (_busy || _headset == null) return;
        var path = await Task.Run(() => _adb.SaveBackup(_headset.Serial, _headset, _allEntries, isInitialBefore: false));
        LogMessage($"Saved headset snapshot -> {path}");
    }

    private async Task RunFreeMemoryAsync()
    {
        if (_busy || _headset == null) return;
        _busy = true;
        try
        {
            var (before, after, count) = await Task.Run(() => _adb.FreeBloatMemory(_headset.Serial, _allEntries));
            LogMessage($"Stopped {count} bloat/background processes: MemAvailable {before:F0} MB -> {after:F0} MB (delta {after - before:+0;-0;0} MB)");
        }
        finally
        {
            _busy = false;
        }
        await RefreshHeadsetAsync();
    }

    private async Task RunTweak(Func<string, string> action)
    {
        if (_headset == null) return;
        var msg = await Task.Run(() => action(_headset.Serial));
        LogMessage(msg);
    }

    public void CaptureWindowPng(string outPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outPath))!);
        // Select first 4 visible rows cleanly without selection clutter, ensure layout refreshed
        _grid.ClearSelection();
        using var bmp = new Bitmap(_rootPanel.Width, _rootPanel.Height);
        _rootPanel.DrawToBitmap(bmp, new Rectangle(0, 0, _rootPanel.Width, _rootPanel.Height));
        bmp.Save(outPath, System.Drawing.Imaging.ImageFormat.Png);
    }

    private void LogMessage(string text)
    {
        if (InvokeRequired) { Invoke(() => LogMessage(text)); return; }
        _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
        _logBox.SelectionStart = _logBox.TextLength;
        _logBox.ScrollToCaret();
    }

    private static Button MakeButton(string text, Color bg, int w, int h) => new()
    {
        Text = text,
        BackColor = bg,
        ForeColor = TextPrimary,
        FlatStyle = FlatStyle.Flat,
        FlatAppearance = { BorderSize = 1, BorderColor = BorderCol },
        Width = w,
        Height = h,
        UseMnemonic = false,
        Cursor = Cursors.Hand,
        Margin = new Padding(4, 2, 4, 2)
    };
}
