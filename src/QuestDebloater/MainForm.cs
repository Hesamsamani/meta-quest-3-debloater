using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;

namespace QuestDebloater;

public sealed class MainForm : Form
{
    // Layer 1 & 2 Design Tokens: Dark Mode OLED + Tactical Console
    private static readonly Color BgCanvas = Color.FromArgb(7, 10, 18);          // #070A12
    private static readonly Color Surface = Color.FromArgb(15, 22, 36);          // #0F1624
    private static readonly Color SurfaceElevated = Color.FromArgb(22, 32, 51);  // #162033
    private static readonly Color SurfaceInput = Color.FromArgb(11, 16, 27);     // #0B101B
    private static readonly Color BorderSubtle = Color.FromArgb(30, 45, 69);     // #1E2D45
    private static readonly Color BorderHighlight = Color.FromArgb(51, 75, 110); // #334B6E

    private static readonly Color TextPrimary = Color.FromArgb(248, 250, 252);   // #F8FAFC
    private static readonly Color TextSecondary = Color.FromArgb(203, 213, 225); // #CBD5E1
    private static readonly Color TextMuted = Color.FromArgb(100, 116, 139);     // #64748B
    private static readonly Color PackageBlue = Color.FromArgb(147, 197, 253);   // #93C5FD

    private static readonly Color AccentCyan = Color.FromArgb(56, 189, 248);     // #38BDF8
    private static readonly Color AccentBlue = Color.FromArgb(59, 130, 246);     // #3B82F6
    private static readonly Color SafeGreen = Color.FromArgb(16, 185, 129);      // #10B981
    private static readonly Color WarnAmber = Color.FromArgb(245, 158, 11);      // #F59E0B
    private static readonly Color CautionOrange = Color.FromArgb(249, 115, 22);  // #F97316
    private static readonly Color DangerRed = Color.FromArgb(239, 68, 68);       // #EF4444
    private static readonly Color AccentPurple = Color.FromArgb(168, 85, 247);   // #A855F7

    private static readonly Color RowEvenBg = Surface;
    private static readonly Color RowOddBg = Color.FromArgb(12, 18, 30);
    private static readonly Color RowHoverBg = Color.FromArgb(20, 31, 49);
    private static readonly Color RowSelectBg = Color.FromArgb(28, 49, 86);
    private static readonly Color GridLineColor = Color.FromArgb(22, 32, 48);

    private readonly AdbService _adb;
    private readonly CatalogRoot _catalog;
    private readonly Dictionary<string, bool> _checked = new(StringComparer.OrdinalIgnoreCase);

    private List<LivePackageEntry> _allEntries = new();
    private List<LivePackageEntry> _filteredEntries = new();
    private HeadsetInfo? _headset;
    private string _activeCategory = "All";
    private bool _busy;
    private Task? _initialScanTask;
    private int _hoverRowIndex = -1;

    private DevicePickerButton _cboDevices = null!;
    private readonly List<string> _deviceLabels = new();
    private BentoCard _cardHeadset = null!;
    private BentoCard _cardBattery = null!;
    private BentoCard _cardRam = null!;
    private BentoCard _cardBloat = null!;
    private TextBox _txtSearch = null!;
    private Label _lblSearchClear = null!;
    private Panel _searchFrame = null!;
    private DataGridView _grid = null!;
    private RichTextBox _logBox = null!;
    private FlowLayoutPanel _catBar = null!;
    private Button _btnSafe = null!, _btnTelemetry = null!, _btnPcvr = null!, _btnClearSel = null!;
    private Button _btnDisable = null!, _btnEnable = null!, _btnRestoreAll = null!;
    private Label _lblSelectionCount = null!;
    private Label _lblGuardStatus = null!;
    private Panel _rootPanel = null!;

    public MainForm(AdbService adb, CatalogRoot catalog)
    {
        _adb = adb;
        _catalog = catalog;
        Text = "Meta Quest 3 Debloater & Privacy Manager";
        Size = new Size(1480, 990);
        MinimumSize = new Size(1220, 820);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = BgCanvas;
        ForeColor = TextPrimary;
        Font = new Font("Segoe UI", 9.5f);
        DoubleBuffered = true;

        BuildUi();
        Shown += (_, _) =>
        {
            ActiveControl = _grid;
            _initialScanTask = RefreshHeadsetAsync();
        };
    }

    public async Task WaitForInitialLoadAsync()
    {
        while (_initialScanTask == null) await Task.Delay(30);
        await _initialScanTask;
        while (_busy) await Task.Delay(30);
        await Task.Delay(150);
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.F))
        {
            _txtSearch.Visible = true;
            _txtSearch.Focus();
            _txtSearch.SelectAll();
            return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void BuildUi()
    {
        _rootPanel = new Panel { Dock = DockStyle.Fill, BackColor = BgCanvas, Padding = new Padding(14) };
        Controls.Add(_rootPanel);

        // 1. Top Header & Bento Dashboard Cards
        var topPanel = new Panel { Dock = DockStyle.Top, Height = 152, BackColor = Color.Transparent };
        BuildTopDashboard(topPanel);

        // 2. Preset + Search + Category Tabs
        var filterPanel = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = Color.Transparent, Padding = new Padding(0, 6, 0, 4) };
        BuildPresetAndFilterBar(filterPanel);

        // 3. Bottom Action Bar & Console Log
        var bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 166, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0) };
        BuildBottomBar(bottomPanel);

        // 4. Middle Area: Grid (left/fill) + Quest 3 Quick Tweaks Sidebar (right)
        var midPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };

        var rightSidebar = new Panel
        {
            Dock = DockStyle.Right,
            Width = 310,
            BackColor = Surface,
            Padding = new Padding(10, 8, 10, 8)
        };
        AttachBorderPaint(rightSidebar, BorderSubtle);
        BuildTweaksSidebar(rightSidebar);

        var gridHost = new Panel { Dock = DockStyle.Fill, BackColor = Surface, Padding = new Padding(1) };
        AttachBorderPaint(gridHost, BorderSubtle);
        BuildGrid();
        gridHost.Controls.Add(_grid);

        var spacer = new Panel { Dock = DockStyle.Right, Width = 10, BackColor = BgCanvas };
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
        var titleRow = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Surface, Padding = new Padding(14, 8, 12, 8) };
        titleRow.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(BorderSubtle);
            e.Graphics.DrawRectangle(pen, 0, 0, titleRow.Width - 1, titleRow.Height - 1);
            using var accentBrush = new SolidBrush(AccentCyan);
            e.Graphics.FillRectangle(accentBrush, 0, 0, 4, titleRow.Height);
        };

        var leftFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Left,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(2, 4, 0, 0)
        };
        var lblTitle = new Label
        {
            Text = "META QUEST 3 DEBLOATER",
            Font = new Font("Segoe UI", 11.5f, FontStyle.Bold),
            ForeColor = TextPrimary,
            AutoSize = true,
            UseMnemonic = false,
            Margin = new Padding(0, 2, 10, 0)
        };
        var guardBadge = new PillBadgeLabel
        {
            Text = "ADB HOME GUARD ACTIVE",
            Accent = SafeGreen,
            BackColor = Surface,
            Margin = new Padding(0, 2, 10, 0)
        };
        var lblSub = new Label
        {
            Text = "Reversible User-0 Horizon OS Manager",
            Font = new Font("Segoe UI", 8.75f),
            ForeColor = TextMuted,
            AutoSize = true,
            UseMnemonic = false,
            Margin = new Padding(0, 5, 0, 0)
        };
        leftFlow.Controls.AddRange(new Control[] { lblTitle, guardBadge, lblSub });

        var rightFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 1, 0, 0)
        };

        _cboDevices = new DevicePickerButton(_deviceLabels)
        {
            Width = 242,
            Height = 30,
            Margin = new Padding(0, 2, 8, 0)
        };

        var btnScan = MakeButton("Scan Headset", AccentBlue, Color.White, BorderHighlight, 114, 30);
        btnScan.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        btnScan.Click += async (_, _) => await RefreshHeadsetAsync();

        var btnBackup = MakeButton("Backup State", SurfaceElevated, TextPrimary, BorderHighlight, 108, 30);
        btnBackup.Click += async (_, _) => await SaveBackupAsync();

        rightFlow.Controls.AddRange(new Control[] { _cboDevices, btnScan, btnBackup });

        titleRow.Controls.AddRange(new Control[] { rightFlow, leftFlow });

        // 4-column Bento metric row
        var cardsRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };
        for (int i = 0; i < 4; i++) cardsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
        cardsRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _cardHeadset = new BentoCard("HEADSET & HORIZON OS", AccentCyan)
        {
            Margin = new Padding(0, 0, 5, 0),
            PillText = "ADB READY",
            HeroText = "Scanning...",
            SubText = "Waiting for Quest 3 ADB"
        };
        _cardBattery = new BentoCard("POWER & GUARDIAN", SafeGreen)
        {
            Margin = new Padding(5, 0, 5, 0),
            PillText = "GUARDIAN --",
            HeroText = "--%",
            SubText = "Thermals: --",
            ShowProgress = true,
            ProgressRatio = 0
        };
        _cardRam = new BentoCard("HEADSET MEMORY (RAM)", AccentPurple)
        {
            Margin = new Padding(5, 0, 5, 0),
            PillText = "--% USED",
            HeroText = "-- GB free",
            SubText = "Total: -- GB",
            ShowProgress = true,
            ProgressRatio = 0
        };
        _cardBloat = new BentoCard("DEBLOAT & STORAGE", WarnAmber)
        {
            Margin = new Padding(5, 0, 0, 0),
            PillText = "0 / 0 SAFE OFF",
            HeroText = "-- Disabled",
            SubText = "Storage: --",
            ShowProgress = true,
            ProgressRatio = 0
        };

        cardsRow.Controls.Add(_cardHeadset, 0, 0);
        cardsRow.Controls.Add(_cardBattery, 1, 0);
        cardsRow.Controls.Add(_cardRam, 2, 0);
        cardsRow.Controls.Add(_cardBloat, 3, 0);

        parent.Controls.Add(cardsRow);
        parent.Controls.Add(titleRow);
    }

    private void BuildPresetAndFilterBar(Panel parent)
    {
        int safeCount = _catalog.Packages.Count(p => p.Presets.Contains("safe"));
        int telCount = _catalog.Packages.Count(p => p.Presets.Contains("telemetry"));
        int pcvrCount = _catalog.Packages.Count(p => p.Presets.Contains("pcvr"));

        var row1 = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 38, BackColor = Color.Transparent, WrapContents = false };
        var lblPreset = new Label
        {
            Text = "PRESETS",
            ForeColor = TextMuted,
            AutoSize = true,
            UseMnemonic = false,
            Margin = new Padding(0, 8, 8, 0),
            Font = new Font("Segoe UI", 8.25f, FontStyle.Bold)
        };

        _btnSafe = MakeButton($"Safe Recommended ({safeCount})", SafeGreen, Color.FromArgb(4, 24, 16), SafeGreen, 190, 30);
        _btnSafe.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _btnSafe.Click += (_, _) => ApplyPresetSelection("safe");

        _btnTelemetry = MakeButton($"Telemetry & Ads ({telCount})", AccentBlue, Color.White, AccentBlue, 174, 30);
        _btnTelemetry.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _btnTelemetry.Click += (_, _) => ApplyPresetSelection("telemetry");

        _btnPcvr = MakeButton($"PCVR / Minimalist ({pcvrCount})", WarnAmber, Color.FromArgb(24, 14, 2), WarnAmber, 182, 30);
        _btnPcvr.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _btnPcvr.Click += (_, _) => ApplyPresetSelection("pcvr");

        _btnClearSel = MakeButton("Select None", SurfaceElevated, TextSecondary, BorderHighlight, 102, 30);
        _btnClearSel.Click += (_, _) => ApplyPresetSelection("none");

        var btnFreeRam = MakeButton("Free RAM (Kill Bloat)", Color.FromArgb(45, 28, 78), Color.FromArgb(233, 213, 255), AccentPurple, 174, 30);
        btnFreeRam.Font = new Font("Segoe UI", 8.75f, FontStyle.Bold);
        btnFreeRam.Margin = new Padding(10, 2, 8, 2);
        btnFreeRam.Click += async (_, _) => await RunFreeMemoryAsync();

        // Dark Search Container with vector icon & clear button
        _searchFrame = new Panel
        {
            Width = 278,
            Height = 30,
            BackColor = SurfaceInput,
            Cursor = Cursors.IBeam,
            Margin = new Padding(6, 2, 0, 2)
        };
        _searchFrame.Paint += (_, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Color border = _txtSearch != null && _txtSearch.Focused ? AccentCyan : BorderHighlight;
            using var pen = new Pen(border);
            g.DrawRectangle(pen, 0, 0, _searchFrame.Width - 1, _searchFrame.Height - 1);

            // Vector magnifying glass (no emoji)
            using var iconPen = new Pen(TextMuted, 1.5f);
            g.DrawEllipse(iconPen, 9, 9, 9, 9);
            g.DrawLine(iconPen, 16.5f, 16.5f, 20.5f, 20.5f);

            if (_txtSearch != null && !_txtSearch.Focused && _txtSearch.TextLength == 0)
            {
                TextRenderer.DrawText(
                    g,
                    "Filter package, affects, tier...",
                    new Font("Segoe UI", 8.75f),
                    new Rectangle(26, 1, 224, _searchFrame.Height - 2),
                    TextMuted,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis);
            }
        };

        _txtSearch = new TextBox
        {
            Location = new Point(28, 6),
            Size = new Size(224, 18),
            BackColor = SurfaceInput,
            ForeColor = TextPrimary,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 9f),
            Visible = false
        };
        _searchFrame.Click += (_, _) => { _txtSearch.Visible = true; _txtSearch.Focus(); };
        _txtSearch.GotFocus += (_, _) => _searchFrame.Invalidate();
        _txtSearch.LostFocus += (_, _) =>
        {
            if (_txtSearch.TextLength == 0) _txtSearch.Visible = false;
            _searchFrame.Invalidate();
        };
        _txtSearch.TextChanged += (_, _) =>
        {
            _lblSearchClear.Visible = _txtSearch.Text.Length > 0;
            _searchFrame.Invalidate();
            PopulateGrid();
        };

        _lblSearchClear = new Label
        {
            Text = "×",
            ForeColor = TextMuted,
            BackColor = SurfaceInput,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Bounds = new Rectangle(254, 2, 20, 26),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Visible = false,
            UseMnemonic = false
        };
        _lblSearchClear.Click += (_, _) =>
        {
            _txtSearch.Clear();
            _txtSearch.Visible = true;
            _txtSearch.Focus();
        };

        _searchFrame.Controls.Add(_txtSearch);
        _searchFrame.Controls.Add(_lblSearchClear);

        row1.Controls.AddRange(new Control[] { lblPreset, _btnSafe, _btnTelemetry, _btnPcvr, _btnClearSel, btnFreeRam, _searchFrame });

        _catBar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 36, BackColor = Color.Transparent, WrapContents = false };
        var cats = new[] { "All", "Telemetry & Ads", "Social & Horizon", "Bloat & Demos", "Environments", "Caution", "Protected", "Disabled Only" };
        foreach (var cat in cats)
        {
            var b = MakeCategoryTab(cat);
            _catBar.Controls.Add(b);
        }

        parent.Controls.Add(_catBar);
        parent.Controls.Add(row1);
    }

    private Button MakeCategoryTab(string cat)
    {
        bool active = cat == _activeCategory;
        var b = new Button
        {
            Text = cat,
            BackColor = active ? SurfaceElevated : Surface,
            ForeColor = active ? TextPrimary : TextSecondary,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = active ? AccentBlue : BorderSubtle },
            Font = new Font("Segoe UI", 8.5f, active ? FontStyle.Bold : FontStyle.Regular),
            Height = 29,
            AutoSize = true,
            MinimumSize = new Size(76, 29),
            Padding = new Padding(8, 0, 8, 2),
            Margin = new Padding(0, 3, 6, 0),
            UseMnemonic = false,
            Cursor = Cursors.Hand,
            Tag = cat
        };
        b.Paint += (s, e) =>
        {
            if (s is not Button btn || (string?)btn.Tag != _activeCategory) return;
            using var bar = new SolidBrush(AccentCyan);
            e.Graphics.FillRectangle(bar, 1, btn.Height - 3, btn.Width - 2, 2);
        };
        b.MouseEnter += (s, _) =>
        {
            if (s is Button btn && (string?)btn.Tag != _activeCategory)
            {
                btn.BackColor = SurfaceElevated;
                btn.ForeColor = TextPrimary;
            }
        };
        b.MouseLeave += (s, _) =>
        {
            if (s is Button btn && (string?)btn.Tag != _activeCategory)
            {
                btn.BackColor = Surface;
                btn.ForeColor = TextSecondary;
            }
        };
        b.Click += (_, _) =>
        {
            _activeCategory = cat;
            RefreshCategoryTabStyles();
            PopulateGrid();
        };
        return b;
    }

    private void RefreshCategoryTabStyles()
    {
        foreach (Control ctrl in _catBar.Controls)
        {
            if (ctrl is not Button cb) continue;
            bool isActive = string.Equals((string?)cb.Tag, _activeCategory, StringComparison.Ordinal);
            cb.BackColor = isActive ? SurfaceElevated : Surface;
            cb.ForeColor = isActive ? TextPrimary : TextSecondary;
            cb.FlatAppearance.BorderColor = isActive ? AccentBlue : BorderSubtle;
            cb.Font = new Font("Segoe UI", 8.5f, isActive ? FontStyle.Bold : FontStyle.Regular);
            cb.Invalidate();
        }
    }

    private void UpdateCategoryCounts()
    {
        foreach (Control ctrl in _catBar.Controls)
        {
            if (ctrl is not Button cb || cb.Tag is not string cat) continue;
            int n = cat switch
            {
                "All" => _allEntries.Count,
                "Caution" => _allEntries.Count(e => e.Spec.Tier == "CAUTION"),
                "Protected" => _allEntries.Count(e => e.Protected || e.Spec.Tier == "PROTECTED"),
                "Disabled Only" => _allEntries.Count(e => e.Installed && !e.Enabled),
                _ => _allEntries.Count(e => string.Equals(e.Spec.Category, cat, StringComparison.OrdinalIgnoreCase))
            };
            cb.Text = _allEntries.Count > 0 ? $"{cat} ({n})" : cat;
        }
    }

    private void BuildGrid()
    {
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Surface,
            GridColor = GridLineColor,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            ColumnHeadersHeight = 36,
            ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            RowTemplate = { Height = 34 }
        };

        typeof(DataGridView).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)?
            .SetValue(_grid, true, null);

        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = SurfaceElevated,
            ForeColor = TextSecondary,
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
            SelectionBackColor = SurfaceElevated,
            SelectionForeColor = TextSecondary,
            Padding = new Padding(6, 0, 6, 0)
        };
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = RowEvenBg,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9f),
            SelectionBackColor = RowSelectBg,
            SelectionForeColor = TextPrimary,
            Padding = new Padding(6, 0, 6, 0)
        };
        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = RowOddBg,
            ForeColor = TextPrimary,
            Font = new Font("Segoe UI", 9f),
            SelectionBackColor = RowSelectBg,
            SelectionForeColor = TextPrimary,
            Padding = new Padding(6, 0, 6, 0)
        };

        var colChk = new DataGridViewCheckBoxColumn { Name = "Sel", HeaderText = "", Width = 40, AutoSizeMode = DataGridViewAutoSizeColumnMode.None };
        var colName = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Feature", FillWeight = 17, ReadOnly = true };
        var colPkg = new DataGridViewTextBoxColumn
        {
            Name = "Package",
            HeaderText = "Package ID",
            FillWeight = 21,
            ReadOnly = true,
            DefaultCellStyle = { Font = new Font("Consolas", 8.5f), ForeColor = PackageBlue }
        };
        var colCat = new DataGridViewTextBoxColumn
        {
            Name = "Category",
            HeaderText = "Category",
            FillWeight = 11,
            ReadOnly = true,
            DefaultCellStyle = { Font = new Font("Segoe UI", 8.5f), ForeColor = TextSecondary }
        };
        var colAffects = new DataGridViewTextBoxColumn { Name = "Affects", HeaderText = "Affects (Part)", FillWeight = 19, ReadOnly = true };
        var colTier = new DataGridViewTextBoxColumn { Name = "Risk", HeaderText = "Risk", Width = 96, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, ReadOnly = true };
        var colStatus = new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "Status", Width = 104, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, ReadOnly = true };
        var colRam = new DataGridViewTextBoxColumn
        {
            Name = "Ram",
            HeaderText = "RAM",
            Width = 68,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            ReadOnly = true,
            DefaultCellStyle = { Font = new Font("Consolas", 8.5f), Alignment = DataGridViewContentAlignment.MiddleRight, Padding = new Padding(0, 0, 10, 0) }
        };
        var colDesc = new DataGridViewTextBoxColumn
        {
            Name = "Desc",
            HeaderText = "Description",
            FillWeight = 24,
            ReadOnly = true,
            DefaultCellStyle = { ForeColor = TextSecondary }
        };

        _grid.Columns.AddRange(colChk, colName, colPkg, colCat, colAffects, colTier, colStatus, colRam, colDesc);

        _grid.CellMouseEnter += (_, e) =>
        {
            if (e.RowIndex == _hoverRowIndex) return;
            int prev = _hoverRowIndex;
            _hoverRowIndex = e.RowIndex;
            if (prev >= 0 && prev < _grid.RowCount) _grid.InvalidateRow(prev);
            if (_hoverRowIndex >= 0 && _hoverRowIndex < _grid.RowCount) _grid.InvalidateRow(_hoverRowIndex);
        };
        _grid.MouseLeave += (_, _) =>
        {
            if (_hoverRowIndex >= 0 && _hoverRowIndex < _grid.RowCount)
            {
                int prev = _hoverRowIndex;
                _hoverRowIndex = -1;
                _grid.InvalidateRow(prev);
            }
        };

        _grid.CellMouseClick += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _filteredEntries.Count || e.ColumnIndex != 0) return;
            var entry = _filteredEntries[e.RowIndex];
            if (entry.Protected)
            {
                LogMessage($"Protected package locked: {entry.Spec.Package}");
                return;
            }
            bool next = !_checked.GetValueOrDefault(entry.Spec.Package);
            _checked[entry.Spec.Package] = next;
            _grid.Rows[e.RowIndex].Cells[0].Value = next;
            _grid.InvalidateCell(0, e.RowIndex);
            UpdateSelectionCounter();
        };

        _grid.CellPainting += Grid_CellPainting;
    }

    private void Grid_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _filteredEntries.Count || e.Graphics == null) return;
        string colName = _grid.Columns[e.ColumnIndex].Name;
        bool isHovered = e.RowIndex == _hoverRowIndex;
        bool isSelected = _grid.Rows[e.RowIndex].Selected;
        Color rowBg = isSelected ? RowSelectBg : (isHovered ? RowHoverBg : (e.RowIndex % 2 == 0 ? RowEvenBg : RowOddBg));

        if (colName is not ("Sel" or "Risk" or "Status" or "Name" or "Ram"))
        {
            if (isHovered && !isSelected)
                e.CellStyle!.BackColor = RowHoverBg;
            return;
        }

        var entry = _filteredEntries[e.RowIndex];
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        if (colName == "Name")
        {
            if (isHovered && !isSelected) e.CellStyle!.BackColor = RowHoverBg;
            e.CellStyle!.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            return;
        }

        if (colName == "Ram")
        {
            if (isHovered && !isSelected) e.CellStyle!.BackColor = RowHoverBg;
            e.CellStyle!.ForeColor = entry.RamMb >= 100 ? WarnAmber : (entry.RamMb > 0.1 ? AccentCyan : TextMuted);
            return;
        }

        // Paint cell background & horizontal separator manually for custom pill/checkbox columns
        using (var bgBrush = new SolidBrush(rowBg))
            g.FillRectangle(bgBrush, e.CellBounds);
        using (var gridPen = new Pen(GridLineColor))
            g.DrawLine(gridPen, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right - 1, e.CellBounds.Bottom - 1);

        if (colName == "Sel")
        {
            int boxSize = 16;
            int bx = e.CellBounds.X + (e.CellBounds.Width - boxSize) / 2;
            int by = e.CellBounds.Y + (e.CellBounds.Height - boxSize) / 2;
            var boxRect = new Rectangle(bx, by, boxSize, boxSize);

            if (entry.Protected)
            {
                FillRoundedRect(g, Color.FromArgb(40, DangerRed), boxRect, 4);
                DrawRoundedRect(g, Color.FromArgb(100, DangerRed), boxRect, 4);
                // Vector padlock
                using var lockPen = new Pen(DangerRed, 1.4f);
                g.DrawArc(lockPen, bx + 5, by + 3, 6, 6, 180, 180);
                using var bodyBrush = new SolidBrush(DangerRed);
                g.FillRectangle(bodyBrush, bx + 4, by + 8, 8, 5);
            }
            else if (_checked.GetValueOrDefault(entry.Spec.Package))
            {
                FillRoundedRect(g, AccentBlue, boxRect, 4);
                DrawRoundedRect(g, AccentCyan, boxRect, 4);
                using var chkPen = new Pen(Color.White, 2f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
                g.DrawLines(chkPen, new[]
                {
                    new PointF(bx + 3.5f, by + 8.5f),
                    new PointF(bx + 6.8f, by + 11.8f),
                    new PointF(bx + 12.5f, by + 4.8f)
                });
            }
            else
            {
                FillRoundedRect(g, SurfaceInput, boxRect, 4);
                DrawRoundedRect(g, BorderHighlight, boxRect, 4);
            }

            e.Handled = true;
            return;
        }

        if (colName == "Risk")
        {
            Color tint = entry.Spec.Tier switch
            {
                "SAFE" => SafeGreen,
                "MODERATE" => WarnAmber,
                "CAUTION" => CautionOrange,
                _ => DangerRed
            };
            DrawPillInCell(g, e.CellBounds, entry.Spec.Tier, tint, drawDot: false);
            e.Handled = true;
            return;
        }

        if (colName == "Status")
        {
            var (label, tint, hasDot) = !entry.Installed
                ? ("ABSENT", TextMuted, false)
                : (!entry.Enabled
                    ? ("DISABLED", WarnAmber, true)
                    : (entry.Protected ? ("LOCKED", DangerRed, true) : ("ACTIVE", SafeGreen, true)));
            DrawPillInCell(g, e.CellBounds, label, tint, hasDot);
            e.Handled = true;
        }
    }

    private void BuildTweaksSidebar(Panel sidebar)
    {
        var hdrPanel = new Panel { Dock = DockStyle.Top, Height = 24, BackColor = Color.Transparent };
        var hdr = new Label
        {
            Text = "QUEST 3 QUICK TWEAKS",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = AccentCyan,
            Dock = DockStyle.Fill,
            UseMnemonic = false
        };
        hdrPanel.Controls.Add(hdr);

        // Bottom Safety Guard Status Card
        var guardCard = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 76,
            BackColor = SurfaceElevated,
            Padding = new Padding(10, 6, 10, 6)
        };
        guardCard.Paint += (_, e) =>
        {
            using var pen = new Pen(BorderSubtle);
            e.Graphics.DrawRectangle(pen, 0, 0, guardCard.Width - 1, guardCard.Height - 1);
            using var bar = new SolidBrush(SafeGreen);
            e.Graphics.FillRectangle(bar, 0, 0, 3, guardCard.Height);
        };
        var guardTitle = new Label
        {
            Text = "SAFETY & INTEGRITY GUARD",
            Font = new Font("Segoe UI", 7.75f, FontStyle.Bold),
            ForeColor = SafeGreen,
            Dock = DockStyle.Top,
            Height = 16,
            UseMnemonic = false
        };
        _lblGuardStatus = new Label
        {
            Text = "Home: com.oculus.vrshell\r\nMode: User-0 Reversible\r\nLock: Ready",
            Font = new Font("Consolas", 8f),
            ForeColor = TextSecondary,
            Dock = DockStyle.Fill,
            UseMnemonic = false
        };
        guardCard.Controls.Add(_lblGuardStatus);
        guardCard.Controls.Add(guardTitle);

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 2, 0, 0)
        };

        AddTweakSection(flow, "REFRESH RATE OVERRIDE", 3,
            ("72 Hz", () => RunTweak(s => _adb.SetRefreshRate(s, 72))),
            ("90 Hz", () => RunTweak(s => _adb.SetRefreshRate(s, 90))),
            ("120 Hz", () => RunTweak(s => _adb.SetRefreshRate(s, 120))));

        AddTweakSection(flow, "FIXED FOVEATED RENDERING (FFR)", 4,
            ("Off", () => RunTweak(s => _adb.SetFoveation(s, 0, false))),
            ("Med", () => RunTweak(s => _adb.SetFoveation(s, 2, false))),
            ("High", () => RunTweak(s => _adb.SetFoveation(s, 3, false))),
            ("Dyn", () => RunTweak(s => _adb.SetFoveation(s, 2, true))));

        AddTweakSection(flow, "CPU / GPU CLOCK LEVEL", 2,
            ("Auto", () => RunTweak(s => _adb.SetPerfLevel(s, -1))),
            ("Max Level 4", () => RunTweak(s => _adb.SetPerfLevel(s, 4))));

        AddTweakSection(flow, "HUD & POWER DIAGNOSTICS", 2,
            ("FPS HUD On", () => RunTweak(s => _adb.ToggleFpsOverlay(s, true))),
            ("FPS HUD Off", () => RunTweak(s => _adb.ToggleFpsOverlay(s, false))));

        AddTweakSection(flow, "DISPLAY & BOUNDARY", 2,
            ("Keep Awake", () => RunTweak(s => _adb.ToggleKeepAwake(s, true))),
            ("Auto Sleep", () => RunTweak(s => _adb.ToggleKeepAwake(s, false))),
            ("Guardian On", () => RunTweak(s => _adb.ToggleGuardian(s, true))),
            ("Guardian Off", () => RunTweak(s => _adb.ToggleGuardian(s, false))));

        var btnRestartShell = MakeButton("Restart VR Shell (vrshell)", SurfaceElevated, AccentCyan, BorderHighlight, 288, 29);
        btnRestartShell.Font = new Font("Segoe UI", 8.75f, FontStyle.Bold);
        btnRestartShell.Margin = new Padding(0, 2, 0, 2);
        btnRestartShell.Click += async (_, _) => await RunTweak(s => _adb.RestartVrShell(s));
        flow.Controls.Add(btnRestartShell);

        sidebar.Controls.Add(flow);
        sidebar.Controls.Add(guardCard);
        sidebar.Controls.Add(hdrPanel);
    }

    private void AddTweakSection(FlowLayoutPanel parent, string title, int cols, params (string Label, Func<Task> Action)[] buttons)
    {
        const int cardW = 288;
        const int padX = 8;
        const int gap = 5;
        const int btnH = 26;
        int rows = (int)Math.Ceiling(buttons.Length / (double)cols);
        int innerW = cardW - padX * 2;
        int btnW = (innerW - gap * (cols - 1)) / cols;
        int cardH = 24 + rows * btnH + (rows - 1) * gap + 7;

        var card = new Panel
        {
            Size = new Size(cardW, cardH),
            BackColor = SurfaceElevated,
            Margin = new Padding(0, 0, 0, 5)
        };
        AttachBorderPaint(card, BorderSubtle);

        var lbl = new Label
        {
            Text = title,
            ForeColor = TextMuted,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            Location = new Point(padX, 5),
            Size = new Size(innerW, 15),
            UseMnemonic = false
        };
        card.Controls.Add(lbl);

        for (int i = 0; i < buttons.Length; i++)
        {
            int r = i / cols;
            int c = i % cols;
            var (text, action) = buttons[i];
            var b = MakeButton(text, SurfaceInput, TextPrimary, BorderHighlight, btnW, btnH);
            b.Location = new Point(padX + c * (btnW + gap), 23 + r * (btnH + gap));
            b.Font = new Font("Segoe UI", 8.25f, FontStyle.Bold);
            b.Margin = Padding.Empty;
            b.Click += async (_, _) => await action();
            card.Controls.Add(b);
        }

        parent.Controls.Add(card);
    }

    private void BuildBottomBar(Panel parent)
    {
        var actionRow = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 42, BackColor = Color.Transparent };

        _btnDisable = MakeButton("Disable Selected (Reversible)", DangerRed, Color.White, DangerRed, 228, 33);
        _btnDisable.Font = new Font("Segoe UI", 9.25f, FontStyle.Bold);
        _btnDisable.Click += async (_, _) => await ApplySelectionAsync(disable: true);

        _btnEnable = MakeButton("Re-Enable Selected", SafeGreen, Color.FromArgb(4, 24, 16), SafeGreen, 168, 33);
        _btnEnable.Font = new Font("Segoe UI", 9.25f, FontStyle.Bold);
        _btnEnable.Click += async (_, _) => await ApplySelectionAsync(disable: false);

        _btnRestoreAll = MakeButton("Restore All Meta Defaults", SurfaceElevated, TextPrimary, BorderHighlight, 192, 33);
        _btnRestoreAll.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        _btnRestoreAll.Click += async (_, _) => await RestoreAllAsync();

        _lblSelectionCount = new Label
        {
            Text = "0 queued",
            ForeColor = TextPrimary,
            BackColor = Color.FromArgb(24, 38, 62),
            AutoSize = true,
            Padding = new Padding(10, 5, 10, 5),
            Margin = new Padding(12, 4, 8, 0),
            Font = new Font("Consolas", 8.5f, FontStyle.Bold),
            UseMnemonic = false
        };
        AttachBorderPaint(_lblSelectionCount, AccentBlue);

        var lblNote = new Label
        {
            Text = "Uses reversible 'pm disable-user --user 0' • Zero APKs uninstalled • HOME verified after every toggle",
            ForeColor = TextMuted,
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5f),
            Margin = new Padding(6, 9, 0, 0),
            UseMnemonic = false
        };

        actionRow.Controls.AddRange(new Control[] { _btnDisable, _btnEnable, _btnRestoreAll, _lblSelectionCount, lblNote });

        var consoleFrame = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(8, 11, 18), Padding = new Padding(1) };
        AttachBorderPaint(consoleFrame, BorderSubtle);

        var consoleHdr = new Panel { Dock = DockStyle.Top, Height = 23, BackColor = SurfaceElevated, Padding = new Padding(10, 3, 10, 2) };
        consoleHdr.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var dot = new SolidBrush(SafeGreen);
            e.Graphics.FillEllipse(dot, 10, 7, 7, 7);
            using var pen = new Pen(BorderSubtle);
            e.Graphics.DrawLine(pen, 0, consoleHdr.Height - 1, consoleHdr.Width, consoleHdr.Height - 1);
        };
        var consoleTitle = new Label
        {
            Text = "ADB EXECUTION CONSOLE",
            Font = new Font("Segoe UI", 7.75f, FontStyle.Bold),
            ForeColor = TextSecondary,
            Dock = DockStyle.Fill,
            Padding = new Padding(14, 1, 0, 0),
            UseMnemonic = false
        };
        consoleHdr.Controls.Add(consoleTitle);

        _logBox = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(8, 11, 18),
            ForeColor = TextSecondary,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            Font = new Font("Consolas", 8.75f)
        };

        consoleFrame.Controls.Add(_logBox);
        consoleFrame.Controls.Add(consoleHdr);

        parent.Controls.Add(consoleFrame);
        parent.Controls.Add(actionRow);
    }

    public async Task RefreshHeadsetAsync()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            var devices = await Task.Run(() => _adb.ListDevices());
            _deviceLabels.Clear();
            foreach (var d in devices) _deviceLabels.Add(d.Label);
            if (devices.Count == 0)
            {
                _cboDevices.SetDevice("No Quest connected");
                _cardHeadset.UpdateData("No Quest ADB", "Enable Developer Mode & USB", "DISCONNECTED", AccentCyan, null);
                _cardBattery.UpdateData("--%", "Connect Quest 3 via ADB", "NO DEVICE", WarnAmber, 0);
                _cardRam.UpdateData("-- GB free", "Memory unavailable", "--", AccentPurple, 0);
                _cardBloat.UpdateData("-- Disabled", "Storage unavailable", "OFFLINE", WarnAmber, 0);
                LogMessage("No authorized ADB device found. Enable Developer Mode and connect Quest 3.");
                return;
            }

            _cboDevices.SetDevice(devices[0].Label);
            var serial = devices[0].Serial;
            LogMessage($"Scanning headset {serial} via {_adb.AdbPath}...");

            var (info, entries) = await Task.Run(() => _adb.ScanHeadset(serial, _catalog));
            _headset = info;
            _allEntries = entries;

            if (_checked.Count == 0)
            {
                foreach (var e in _allEntries.Where(x => x.Spec.Presets.Contains("safe") && x.Installed && x.Enabled && !x.Protected))
                    _checked[e.Spec.Package] = true;
            }

            UpdateDashboardCards(info, entries);
            UpdateCategoryCounts();
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
        _cardHeadset.UpdateData(
            $"{info.Model} • Android {info.AndroidVersion}",
            $"SN {info.Serial} ({info.DeviceCode})",
            "ADB CONNECTED",
            AccentCyan,
            null);

        string batHero = info.BatteryLevel >= 0 ? $"{info.BatteryLevel}%  ({info.BatteryTempC:F1}°C)" : "--%";
        string guardPill = info.GuardianEnabled ? "GUARDIAN ON" : "GUARDIAN OFF";
        double? batRatio = info.BatteryLevel >= 0 ? info.BatteryLevel / 100.0 : null;
        _cardBattery.UpdateData(
            batHero,
            $"Boundary: {(info.GuardianEnabled ? "Guardian Active" : "Guardian Off")}",
            guardPill,
            info.GuardianEnabled ? SafeGreen : WarnAmber,
            batRatio);

        double freeGb = info.MemAvailableMb / 1024.0;
        double totalGb = info.MemTotalMb / 1024.0;
        double usedRatio = info.MemTotalMb > 0 ? info.MemUsedMb / info.MemTotalMb : 0;
        _cardRam.UpdateData(
            $"{freeGb:F2} GB free",
            $"Available of {totalGb:F2} GB ({info.MemUsedMb:F0} MB used)",
            $"{usedRatio * 100:F0}% USED",
            AccentPurple,
            usedRatio);

        int safeDisabled = entries.Count(e => e.Spec.Tier == "SAFE" && e.Installed && !e.Enabled);
        int safeTotal = entries.Count(e => e.Spec.Tier == "SAFE" && e.Installed);
        double safeRatio = safeTotal > 0 ? (double)safeDisabled / safeTotal : 0;
        _cardBloat.UpdateData(
            $"{info.DisabledCount} / {info.TotalInstalledCount} Disabled",
            $"Storage: {info.StorageSummary}",
            $"{safeDisabled}/{safeTotal} SAFE OFF",
            safeDisabled == safeTotal && safeTotal > 0 ? SafeGreen : WarnAmber,
            safeRatio);

        int lockedCount = entries.Count(e => e.Installed && e.Protected);
        string homeShort = info.HomeActivity.Contains("vrshell", StringComparison.OrdinalIgnoreCase)
            ? "com.oculus.vrshell ✓"
            : info.HomeActivity;
        _lblGuardStatus.Text = $"Home: {homeShort}\r\nMode: User-0 Reversible ✓\r\nLock: {lockedCount} core packages";
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
                || e.Spec.Affects.Contains(q, StringComparison.OrdinalIgnoreCase)
                || e.Spec.Description.Contains(q, StringComparison.OrdinalIgnoreCase);
        }).ToList();

        _hoverRowIndex = -1;
        _grid.Rows.Clear();
        foreach (var e in _filteredEntries)
        {
            bool isChecked = !e.Protected && _checked.GetValueOrDefault(e.Spec.Package);
            int idx = _grid.Rows.Add(
                isChecked,
                e.Spec.Name,
                e.Spec.Package,
                e.Spec.Category,
                e.Spec.Affects,
                e.Spec.Tier,
                e.LiveStatus,
                e.RamMb > 0.1 ? $"{e.RamMb:F0} MB" : "—",
                e.Spec.Description);
            if (e.Protected)
                _grid.Rows[idx].Cells[0].ReadOnly = true;
        }
        UpdateSelectionCounter();
    }

    private void UpdateSelectionCounter()
    {
        int selectedInstalled = _allEntries.Count(e => !e.Protected && e.Installed && _checked.GetValueOrDefault(e.Spec.Package));
        _lblSelectionCount.Text = $"{selectedInstalled} queued";
        _lblSelectionCount.ForeColor = selectedInstalled > 0 ? TextPrimary : TextMuted;
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
        Directory.CreateDirectory(Path.GetFullPath(Path.GetDirectoryName(outPath)!));
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

    private static Button MakeButton(string text, Color bg, Color fg, Color border, int w, int h)
    {
        Color hoverBg = ControlPaint.Light(bg, 0.12f);
        var btn = new Button
        {
            Text = text,
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 1, BorderColor = border, MouseOverBackColor = hoverBg },
            Width = w,
            Height = h,
            UseMnemonic = false,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 6, 2)
        };
        return btn;
    }

    private static void AttachBorderPaint(Control ctrl, Color borderColor)
    {
        ctrl.Paint += (_, e) =>
        {
            using var pen = new Pen(borderColor);
            e.Graphics.DrawRectangle(pen, 0, 0, ctrl.Width - 1, ctrl.Height - 1);
        };
    }

    private static GraphicsPath CreateRoundedRectPath(Rectangle bounds, int radius)
    {
        int d = radius * 2;
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static void FillRoundedRect(Graphics g, Color color, Rectangle rect, int radius)
    {
        using var path = CreateRoundedRectPath(rect, radius);
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }

    private static void DrawRoundedRect(Graphics g, Color color, Rectangle rect, int radius)
    {
        using var path = CreateRoundedRectPath(rect, radius);
        using var pen = new Pen(color);
        g.DrawPath(pen, path);
    }

    private static void DrawPillInCell(Graphics g, Rectangle cellBounds, string text, Color tint, bool drawDot)
    {
        int w = Math.Min(cellBounds.Width - 10, drawDot ? 88 : 80);
        int h = 21;
        var rect = new Rectangle(cellBounds.X + (cellBounds.Width - w) / 2, cellBounds.Y + (cellBounds.Height - h) / 2, w, h);

        FillRoundedRect(g, Color.FromArgb(38, tint), rect, 6);
        DrawRoundedRect(g, Color.FromArgb(95, tint), rect, 6);

        using var font = new Font("Segoe UI", 7.75f, FontStyle.Bold);
        using var textBrush = new SolidBrush(tint);

        if (drawDot)
        {
            using var dotBrush = new SolidBrush(tint);
            g.FillEllipse(dotBrush, rect.X + 8, rect.Y + 7, 6, 6);
            var textRect = new Rectangle(rect.X + 14, rect.Y, rect.Width - 16, rect.Height);
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, font, textBrush, textRect, sf);
        }
        else
        {
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(text, font, textBrush, rect, sf);
        }
    }

    // Custom dark device selector pill + dark popup menu (zero native white chrome)
    private sealed class DevicePickerButton : Control
    {
        private readonly List<string> _labels;
        private string _currentText = "Scanning ADB...";
        private bool _hovered;

        public DevicePickerButton(List<string> labels)
        {
            _labels = labels;
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 8.75f, FontStyle.Bold);
            MouseEnter += (_, _) => { _hovered = true; Invalidate(); };
            MouseLeave += (_, _) => { _hovered = false; Invalidate(); };
            Click += OnPickerClick;
        }

        public void SetDevice(string text)
        {
            _currentText = text;
            Invalidate();
        }

        private void OnPickerClick(object? sender, EventArgs e)
        {
            if (_labels.Count == 0) return;
            var menu = new ContextMenuStrip { BackColor = SurfaceElevated, ForeColor = TextPrimary, ShowImageMargin = false };
            foreach (var label in _labels)
            {
                var item = new ToolStripMenuItem(label) { BackColor = SurfaceElevated, ForeColor = TextPrimary };
                item.Click += (_, _) => SetDevice(label);
                menu.Items.Add(item);
            }
            menu.Show(this, new Point(0, Height + 2));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            FillRoundedRect(g, _hovered ? Color.FromArgb(28, 41, 65) : SurfaceElevated, rect, 6);
            DrawRoundedRect(g, _hovered ? AccentCyan : BorderHighlight, rect, 6);

            bool online = _labels.Count > 0;
            using (var dot = new SolidBrush(online ? SafeGreen : WarnAmber))
                g.FillEllipse(dot, 10, (Height - 7) / 2, 7, 7);

            var txtRect = new Rectangle(22, 0, Width - 46, Height);
            TextRenderer.DrawText(g, _currentText, Font, txtRect, TextPrimary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            using var divPen = new Pen(BorderSubtle);
            g.DrawLine(divPen, Width - 26, 6, Width - 26, Height - 7);

            float cx = Width - 13f;
            float cy = Height / 2f;
            using var chevron = new SolidBrush(TextSecondary);
            g.FillPolygon(chevron, new[]
            {
                new PointF(cx - 4, cy - 2),
                new PointF(cx + 4, cy - 2),
                new PointF(cx, cy + 3)
            });
        }
    }

    private sealed class PillBadgeLabel : Control
    {
        public Color Accent { get; set; } = SafeGreen;

        public PillBadgeLabel()
        {
            DoubleBuffered = true;
            Size = new Size(192, 22);
        }

        private static Color BlendOpaque(Color baseCol, Color accent, float ratio) =>
            Color.FromArgb(
                (int)(baseCol.R + (accent.R - baseCol.R) * ratio),
                (int)(baseCol.G + (accent.G - baseCol.G) * ratio),
                (int)(baseCol.B + (accent.B - baseCol.B) * ratio));

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            FillRoundedRect(g, BlendOpaque(Surface, Accent, 0.16f), rect, 10);
            DrawRoundedRect(g, BlendOpaque(Surface, Accent, 0.55f), rect, 10);

            using var dot = new SolidBrush(Accent);
            g.FillEllipse(dot, 9, (Height - 6) / 2, 6, 6);

            using var f = new Font("Segoe UI", 7.5f, FontStyle.Bold);
            using var br = new SolidBrush(Accent);
            var textRect = new Rectangle(18, 0, Width - 22, Height);
            using var sf = new StringFormat(StringFormatFlags.NoWrap) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString(Text, f, br, textRect, sf);
        }
    }

    private sealed class BentoCard : Panel
    {
        public string CardTitle { get; }
        public Color AccentColor { get; private set; }
        public string PillText { get; set; } = "";
        public string HeroText { get; set; } = "";
        public string SubText { get; set; } = "";
        public bool ShowProgress { get; set; }
        public double? ProgressRatio { get; set; }

        public BentoCard(string title, Color accent)
        {
            CardTitle = title;
            AccentColor = accent;
            Dock = DockStyle.Fill;
            DoubleBuffered = true;
            BackColor = BgCanvas;
        }

        public void UpdateData(string hero, string sub, string pill, Color accent, double? progressRatio)
        {
            HeroText = hero;
            SubText = sub;
            PillText = pill;
            AccentColor = accent;
            ProgressRatio = progressRatio;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var grad = new LinearGradientBrush(bounds, Color.FromArgb(19, 28, 46), Color.FromArgb(11, 17, 30), 90f))
            using (var path = CreateRoundedRectPath(bounds, 8))
            {
                g.FillPath(grad, path);
                g.SetClip(path);
                using var topStripe = new SolidBrush(AccentColor);
                g.FillRectangle(topStripe, 0, 0, Width, 3);
                g.ResetClip();
            }

            DrawRoundedRect(g, BorderSubtle, bounds, 8);

            // Title & Pill row
            using var fTitle = new Font("Segoe UI", 7.75f, FontStyle.Bold);
            using var brMuted = new SolidBrush(TextMuted);
            g.DrawString(CardTitle, fTitle, brMuted, new PointF(12, 10));

            if (!string.IsNullOrEmpty(PillText))
            {
                using var fPill = new Font("Segoe UI", 7.25f, FontStyle.Bold);
                var sz = TextRenderer.MeasureText(PillText, fPill);
                int pw = sz.Width + 12;
                int ph = 18;
                var pillRect = new Rectangle(Width - pw - 10, 8, pw, ph);
                FillRoundedRect(g, Color.FromArgb(32, AccentColor), pillRect, 5);
                DrawRoundedRect(g, Color.FromArgb(90, AccentColor), pillRect, 5);
                using var brPill = new SolidBrush(AccentColor);
                using var sfPill = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(PillText, fPill, brPill, pillRect, sfPill);
            }

            // Hero metric
            using var fHero = new Font("Segoe UI", 12.5f, FontStyle.Bold);
            TextRenderer.DrawText(g, HeroText, fHero, new Rectangle(12, 30, Width - 24, 26), TextPrimary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // Subtext
            using var fSub = new Font("Consolas", 8f);
            TextRenderer.DrawText(g, SubText, fSub, new Rectangle(12, 56, Width - 24, 16), TextSecondary, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            // Custom dark progress bar
            if (ShowProgress && ProgressRatio.HasValue)
            {
                double clamped = Math.Clamp(ProgressRatio.Value, 0.0, 1.0);
                var track = new Rectangle(12, Height - 16, Width - 24, 6);
                FillRoundedRect(g, Color.FromArgb(26, 39, 62), track, 3);
                int fillW = (int)Math.Round(track.Width * clamped);
                if (fillW >= 6)
                {
                    var fill = new Rectangle(track.X, track.Y, fillW, track.Height);
                    FillRoundedRect(g, AccentColor, fill, 3);
                }
            }
        }
    }
}
