using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Reflection;
using System.Runtime.InteropServices;
using RBX_AntiAFK.Core;

namespace RBX_AntiAFK;

internal class ModernForm : Form
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
    private const int DWM_WINDOW_CORNER_ROUND = 2;
    private const int DWMSBT_MAINWINDOW = 2;

    private const int TH = 42;
    private const int PD = 16;
    private const int CR = 12;

    private static readonly Color Bg = Color.FromArgb(32, 32, 48);
    private static readonly Color CardBg = Color.FromArgb(42, 42, 62);
    private static readonly Color Brd = Color.FromArgb(60, 60, 85);
    private static readonly Color Cyan = Color.FromArgb(98, 196, 255);
    private static readonly Color T0 = Color.FromArgb(255, 255, 255);
    private static readonly Color T1 = Color.FromArgb(180, 180, 195);
    private static readonly Color T2 = Color.FromArgb(120, 120, 140);
    private static readonly Color Green = Color.FromArgb(76, 209, 55);
    private static readonly Color Red = Color.FromArgb(231, 76, 60);

    public event Action<bool>? RunningStateChanged;

    private readonly KeyPresser _kp;
    private readonly Settings _s;
    private SynchronizationContext? _ui;
    private CancellationTokenSource? _cts;
    private Task? _afkTask;
    private volatile bool _allowNotif = true;
    private volatile int _intDly;

    private volatile bool _run;
    private bool _drag;
    private Point _dPt;
    private float _pulse = 1f;
    private bool _pulseUp;

    private Panel _cardAct = null!;
    private ComboBox _cAct = null!;
    private CheckBox _chkMax = null!;
    private NumericUpDown _numDly = null!;
    private CheckBox _chkHide = null!;
    private Button _btnTst = null!;
    private Form? _svr;
    private Point _lm;

    private static NotifyIcon? _tray;
    private Button _btnRun = null!;
    private Panel _statCard = null!;

    public ModernForm(KeyPresser kp, Settings s)
    {
        _kp = kp; _s = s;
        _ui = SynchronizationContext.Current;

        Text = "Roblox-AntiAFK";
        Size = new Size(380, 480);
        MinimumSize = new Size(340, 420);
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        BackColor = Color.FromArgb(20, 20, 30);
        DoubleBuffered = true;

        var scr = Screen.PrimaryScreen!.WorkingArea;
        Location = new Point((scr.Width - Width) / 2, (scr.Height - Height) / 2);

        Build();
        LoadUI();
        StartPulse();
        FormClosed += (_, _) => _pulseTimer?.Dispose();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int p = DWM_WINDOW_CORNER_ROUND;
            DwmSetWindowAttribute(Handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref p, sizeof(int));
            int m = DWMSBT_MAINWINDOW;
            DwmSetWindowAttribute(Handle, DWMWA_SYSTEMBACKDROP_TYPE, ref m, sizeof(int));
        }
        catch { }
    }

    protected override CreateParams CreateParams
    {
        get { var cp = base.CreateParams; cp.ClassStyle |= 0x20000; return cp; }
    }

    public void SetTray(NotifyIcon t) => _tray = t;

    private void Build()
    {
        var fl = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(PD, PD, PD, PD),
            BackColor = Color.Transparent
        };

        fl.Controls.Add(MakeTitle());
        fl.Controls.Add(MakeStatus());
        fl.Controls.Add(SecLbl("SETTINGS"));
        fl.Controls.Add(MakeAction());
        fl.Controls.Add(MakeMax());
        fl.Controls.Add(SecLbl("TOOLS"));
        fl.Controls.Add(MakeTools());
        fl.Controls.Add(MakeFoot());
        Controls.Add(fl);
    }

    private Panel MakeTitle()
    {
        var p = new Panel { Height = TH, Width = Width - PD * 2, Margin = new Padding(0) };
        p.Paint += (s, e) =>
        {
            var g = e.Graphics; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit; g.SmoothingMode = SmoothingMode.AntiAlias;
            Card(g, p.ClientRectangle, 14);
            using var f = new Font("Segoe UI", 11, FontStyle.Bold);
            g.DrawString("Roblox-AntiAFK", f, Brushes.White, 14, (TH - g.MeasureString("Roblox-AntiAFK", f).Height) / 2);
        };
        p.MouseDown += (_, e) => { _drag = true; _dPt = e.Location; };
        p.MouseMove += (_, e) => { if (_drag) Location = new Point(Location.X + e.X - _dPt.X, Location.Y + e.Y - _dPt.Y); };
        p.MouseUp += (_, _) => _drag = false;
        return p;
    }

    private Panel MakeStatus()
    {
        _statCard = new Panel { Height = 90, Width = Width - PD * 2, Margin = new Padding(0, 8, 0, 8), BackColor = Color.Transparent };
        _statCard.Paint += (s, e) =>
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Card(g, _statCard.ClientRectangle, 14);

            int ds = 14, dx = 16, dy = (_statCard.Height - ds) / 2;
            var dc = _run ? Green : Color.FromArgb((int)(_pulse * 160), 120, 120, 140);
            using var dotBrush = new SolidBrush(dc);
            g.FillEllipse(dotBrush, dx, dy, ds, ds);

            using var f = new Font("Segoe UI", 16, FontStyle.Bold);
            using var f2 = new Font("Segoe UI", 11, FontStyle.Regular);
            var st = _run ? "Running" : "Idle";
            using var sb = new SolidBrush(_run ? Green : T1);
            g.DrawString(st, f, sb, dx + ds + 14, (_statCard.Height - g.MeasureString(st, f).Height) / 2 - 2);
            using var t2Brush = new SolidBrush(T2);
            g.DrawString(_run ? "Anti-AFK active" : "Click Start to begin", f2, t2Brush, dx + ds + 14, (_statCard.Height - g.MeasureString(st, f).Height) / 2 + 16);
        };

        _btnRun = new Button
        {
            Text = "Start",
            Width = 84, Height = 32,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Green,
            Cursor = Cursors.Hand,
            Location = new Point(_statCard.Width - 110, (_statCard.Height - 32) / 2)
        };
        _btnRun.FlatAppearance.BorderSize = 0;
        _btnRun.Click += async (_, _) =>
        {
            if (_run) { try { await StopAfkAsync(); } catch (Exception ex) { Console.WriteLine($"Stop error: {ex}"); } }
            else StartAfk();
        };
        _statCard.Controls.Add(_btnRun);

        RunningStateChanged += r =>
        {
            _btnRun.Text = r ? "Stop" : "Start";
            _btnRun.BackColor = r ? Red : Green;
        };
        return _statCard;
    }

    private static Label SecLbl(string t) => new()
    {
        Text = t,
        Font = new Font("Segoe UI", 9, FontStyle.Bold),
        ForeColor = T2,
        AutoSize = true,
        Margin = new Padding(0, 8, 0, 4)
    };

    private Panel MakeAction()
    {
        _cardAct = new Panel { Height = 48, Width = Width - PD * 2, Margin = new Padding(0, 0, 0, 4) };
        _cardAct.Paint += (s, e) =>
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            Card(g, _cardAct.ClientRectangle, CR);
            using var f = new Font("Segoe UI", 11, FontStyle.Regular);
            using var brush = new SolidBrush(T1);
            g.DrawString("Action Type", f, brush, 14, (_cardAct.Height - g.MeasureString("Action Type", f).Height) / 2);
        };
        _cardAct.MouseEnter += (_, _) => _cardAct.Invalidate();
        _cardAct.MouseLeave += (_, _) => _cardAct.Invalidate();

        _cAct = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 130,
            FlatStyle = FlatStyle.Flat,
            BackColor = CardBg,
            ForeColor = T0,
            Font = new Font("Segoe UI", 10)
        };
        foreach (ActionTypeEnum v in Enum.GetValues(typeof(ActionTypeEnum))) _cAct.Items.Add(v);
        _cAct.SelectedIndexChanged += (_, _) => { _s.ActionType = (ActionTypeEnum)_cAct.SelectedItem!; _s.Save(); };

        var cp = new Panel { Width = 140, Height = 28, Location = new Point(_cardAct.Width - 156, (_cardAct.Height - 28) / 2), BackColor = Color.Transparent };
        _cAct.Location = new Point(0, 0);
        cp.Controls.Add(_cAct);
        _cardAct.Controls.Add(cp);
        return _cardAct;
    }

    private Panel MakeMax()
    {
        var p = new Panel { Height = 80, Width = Width - PD * 2, Margin = new Padding(0, 0, 0, 4) };
        p.Paint += (s, e) => { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; Card(g, p.ClientRectangle, CR); };

        _chkMax = new CheckBox { Text = "Open Roblox for", FlatStyle = FlatStyle.Flat, ForeColor = T1, Font = new Font("Segoe UI", 10), AutoSize = true, Location = new Point(14, 12), BackColor = Color.Transparent };
        _chkMax.CheckedChanged += (_, _) => { _s.EnableWindowMaximization = _chkMax.Checked; _s.Save(); };

        _numDly = new NumericUpDown { Width = 48, Minimum = 1, Maximum = 60, Value = 3, Location = new Point(156, 10), BackColor = CardBg, ForeColor = T0, Font = new Font("Segoe UI", 10) };
        _numDly.ValueChanged += (_, _) => { _s.WindowMaximizationDelaySeconds = (int)_numDly.Value; _s.Save(); };

        var sl = new Label { Text = "sec", ForeColor = T2, Font = new Font("Segoe UI", 10), AutoSize = true, Location = new Point(208, 12), BackColor = Color.Transparent };

        _chkHide = new CheckBox { Text = "Hide window contents", FlatStyle = FlatStyle.Flat, ForeColor = T1, Font = new Font("Segoe UI", 10), AutoSize = true, Location = new Point(14, 44), BackColor = Color.Transparent };
        _chkHide.CheckedChanged += (_, _) => { _s.HideWindowContentsOnMaximizing = _chkHide.Checked; _s.Save(); };

        p.Controls.AddRange([_chkMax, _numDly, sl, _chkHide]);
        return p;
    }

    private Panel MakeTools()
    {
        var p = new Panel { Height = 56, Width = Width - PD * 2, Margin = new Padding(0, 0, 0, 4) };
        p.Paint += (s, e) => { var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias; Card(g, p.ClientRectangle, CR); };

        _btnTst = RndBtn("Test", 80, 32);
        _btnTst.Location = new Point(14, (_btnTst.Height - 32) / 2 + 8);
        _btnTst.Click += async (_, _) => await TestMove();

        var b2 = RndBtn("Screensaver", 96, 32);
        b2.Location = new Point(p.Width - 14 - 96, (_btnTst.Height - 32) / 2 + 8);
        b2.Click += (_, _) => ToggleSvr();

        p.Controls.AddRange([_btnTst, b2]);
        return p;
    }

    private Panel MakeFoot()
    {
        var p = new Panel { Height = 28, Width = Width - PD * 2, Margin = new Padding(0, 6, 0, 0) };

        var l1 = Lnk("Show", 0, 4); l1.Click += (_, _) => ShowRbx();
        var l2 = Lnk("Hide", 56, 4); l2.Click += (_, _) => HideRbx();
        var l3 = Lnk("About", 112, 4); l3.Click += (_, _) => ShowAbout();
        var l4 = Lnk("Exit", 168, 4); l4.Click += async (_, _) => await Exit();

        p.Controls.AddRange([l1, l2, l3, l4]);
        return p;
    }

    private static Label Lnk(string t, int x, int y) => new()
    {
        Text = t, Font = new Font("Segoe UI", 9.5f), ForeColor = Cyan,
        AutoSize = true, Cursor = Cursors.Hand, Location = new Point(x, y), BackColor = Color.Transparent
    };

    private static Button RndBtn(string t, int w, int h)
    {
        var b = new Button
        {
            Text = t, Width = w, Height = h, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = Color.White, BackColor = Cyan, Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        b.Paint += (s, e) =>
        {
            var btn = (Button)s!;
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = new GraphicsPath();
            var r = new Rectangle(0, 0, btn.Width - 1, btn.Height - 1);
            int rr = 8;
            path.AddArc(r.X, r.Y, rr, rr, 180, 90);
            path.AddArc(r.Right - rr, r.Y, rr, rr, 270, 90);
            path.AddArc(r.Right - rr, r.Bottom - rr, rr, rr, 0, 90);
            path.AddArc(r.X, r.Bottom - rr, rr, rr, 90, 90);
            path.CloseFigure();
            using var br = new SolidBrush(btn.BackColor);
            g.FillPath(br, path);
            TextRenderer.DrawText(g, btn.Text, btn.Font, r, btn.ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        };
        return b;
    }

    private void Card(Graphics g, Rectangle r, int rad)
    {
        using var path = new GraphicsPath();
        var rect = new Rectangle(r.X, r.Y, r.Width - 1, r.Height - 1);
        path.AddArc(rect.X, rect.Y, rad, rad, 180, 90);
        path.AddArc(rect.Right - rad, rect.Y, rad, rad, 270, 90);
        path.AddArc(rect.Right - rad, rect.Bottom - rad, rad, rad, 0, 90);
        path.AddArc(rect.X, rect.Bottom - rad, rad, rad, 90, 90);
        path.CloseFigure();
        using var fill = new SolidBrush(CardBg);
        g.FillPath(fill, path);
        using var border = new Pen(Brd, 1);
        g.DrawPath(border, path);
    }

    private System.Windows.Forms.Timer? _pulseTimer;

    private void StartPulse()
    {
        _pulseTimer = new System.Windows.Forms.Timer { Interval = 400 };
        _pulseTimer.Tick += (_, _) =>
        {
            if (_run) return;
            _pulse += _pulseUp ? 0.08f : -0.08f;
            if (_pulse >= 1f) { _pulse = 1f; _pulseUp = false; }
            else if (_pulse <= 0.3f) { _pulse = 0.3f; _pulseUp = true; }
            _statCard?.Invalidate();
        };
        _pulseTimer.Start();
    }

    public void SetRun(bool r)
    {
        _run = r;
        RunningStateChanged?.Invoke(r);
        _btnRun.Text = r ? "Stop" : "Start";
        _btnRun.BackColor = r ? Red : Green;
        _statCard?.Invalidate();
        if (r) _pulseTimer?.Stop(); else { _pulse = 1f; _pulseUp = false; _pulseTimer?.Start(); }
    }

    public bool IsRunning() => _run;

    private void LoadUI()
    {
        _chkMax.Checked = _s.EnableWindowMaximization;
        _numDly.Value = _s.WindowMaximizationDelaySeconds;
        _chkHide.Checked = _s.HideWindowContentsOnMaximizing;
        var idx = _cAct.Items.IndexOf(_s.ActionType);
        if (idx != -1) _cAct.SelectedIndex = idx;
        _kp.KeypressDelay = _s.DelayBetweenKeyPressMilliseconds;
        _kp.InteractionDelay = _intDly = _s.DelayBeforeWindowInteractionMilliseconds;
    }

    public void StartAfk()
    {
        var wins = WinManager.GetAllRobloxWindows();
        if (wins.Count == 0) { MessageBox.Show("Roblox window not found!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }

        _cts = new CancellationTokenSource();
        SetRun(true);
        _afkTask = Task.Run(() => AfkLoop(_cts.Token));
    }

    public async Task StopAfkAsync()
    {
        if (_cts == null) return;
        _cts.Cancel();
        if (_afkTask != null)
        {
            try { await _afkTask; } catch { }
        }
        _cts.Dispose(); _cts = null;
        await Repair();
        SetRun(false);
    }

    public async Task Exit()
    {
        if (_svr != null) CloseSvr();
        await StopAfkAsync();
        _s.Save();
        _tray?.Dispose();
        Application.Exit();
    }

    private async Task AfkLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ProcessIter(ct);

                for (int i = 0; i < 15; i++)
                {
                    await Task.Delay(60000, ct);

                    if (WinManager.GetAllRobloxWindows().Count == 0)
                    {
                        StopAfkInternal();
                        return;
                    }
                }
            }
            catch (OperationCanceledException) { return; }
            catch (Exception ex)
            {
                Console.WriteLine($"AFK error: {ex}");
                try { await Task.Delay(30000, ct); } catch (OperationCanceledException) { return; }
            }
        }
    }

    private void StopAfkInternal()
    {
        Console.WriteLine("No Roblox windows found, stopping...");
        _cts?.Cancel();
        _ui?.Post(_ => { _ = StopAfkAsync(); }, null);
    }

    private async Task ProcessIter(CancellationToken ct)
    {
        var user = WinManager.GetActiveWindow();
        var wins = WinManager.GetAllRobloxWindows();
        
        if (wins.Count == 0)
        {
            await Task.Delay(30000, ct);
            return;
        }

        var s = _s.Clone();
        bool max = s.EnableWindowMaximization;
        int maxSec = s.WindowMaximizationDelaySeconds;
        bool hide = s.HideWindowContentsOnMaximizing;
        var act = s.ActionType;

        if (max && _allowNotif)
        {
            _ui?.Post(_ => _tray?.ShowBalloonTip(2000, "Roblox-AntiAFK", "Roblox is opening soon", ToolTipIcon.Info), null);
            await Task.Delay(3000, ct);
        }

        foreach (var w in wins.Where(w => w.IsValidWindow))
        {
            bool wasMin = w.IsMinimized;
            if (max)
            {
                if (hide) w.SetTransparency(0);
                if (wasMin) w.Restore();
                w.Activate();
                await Task.Delay(TimeSpan.FromSeconds(maxSec), ct);
                if (wasMin) w.Minimize();
            }

            for (int i = 0; i < 3; i++)
            {
                w.Activate();
                await Task.Delay(_intDly, ct);
                if (act == ActionTypeEnum.Jump) await _kp.PressSpaceAsync();
                else await _kp.MoveCameraAsync();
                await Task.Delay(_intDly, ct);
            }

            if (user?.IsValidWindow == true) user.Activate();
            w.SetTransparency(255);
        }
    }

    private async Task Repair()
    {
        await Task.Run(() =>
        {
            try
            {
                foreach (var w in WinManager.GetAllRobloxWindows())
                {
                    try
                    {
                        if (!w.IsVisible) { w.Minimize(); w.Show(); }
                        w.SetTransparency(255);
                    }
                    catch { }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Repair error: {ex}"); }
        });
    }

    public async Task TestMove()
    {
        _btnTst.Enabled = false;
        await Task.Run(async () =>
        {
            try
            {
                var wins = WinManager.GetVisibleRobloxWindows();
                if (wins.Count == 0) return;
                var w = wins.First();
                for (int i = 0; i < 3; i++)
                {
                    if (w.IsMinimized) w.Restore();
                    w.Activate();
                    await Task.Delay(_intDly);
                    if (_s.ActionType == ActionTypeEnum.Jump) await _kp.PressSpaceAsync();
                    else await _kp.MoveCameraAsync();
                    await Task.Delay(_intDly);
                }
            }
            catch (Exception ex) { Console.WriteLine($"Test error: {ex}"); }
            finally { _ui?.Post(_ => _btnTst.Enabled = true, null); }
        });
    }

    private static void ShowRbx()
    {
        var wins = WinManager.GetAllRobloxWindows();
        if (wins.Count == 0) { MessageBox.Show("No Roblox windows found.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        foreach (var w in wins)
        {
            if (w.IsMinimized) w.Restore();
            w.Show();
            w.Activate();
        }
    }

    private static void HideRbx()
    {
        var wins = WinManager.GetAllRobloxWindows();
        if (wins.Count == 0) { MessageBox.Show("No Roblox windows found.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        foreach (var w in wins)
        {
            w.Hide();
        }
    }

    public void ShowAbout()
    {
        using var f = new Form
        {
            Text = "About",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false, MinimizeBox = false,
            StartPosition = FormStartPosition.CenterScreen,
            ClientSize = new Size(380, 200),
            BackColor = Bg, ForeColor = T0
        };

        var fl = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, Padding = new Padding(20), BackColor = Color.Transparent };
        fl.Controls.Add(new Label { Text = "Roblox-AntiAFK", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Cyan, AutoSize = true });
        fl.Controls.Add(new Label { Text = "by NeedSleep.Dev", Font = new Font("Segoe UI", 10), ForeColor = T2, AutoSize = true, Margin = new Padding(0, 0, 0, 2) });
        fl.Controls.Add(new Label { Text = $"v{Assembly.GetExecutingAssembly().GetName().Version}", Font = new Font("Segoe UI", 9.5f), ForeColor = T2, AutoSize = true, Margin = new Padding(0, 0, 0, 4) });

        var link = new Label { Text = "GitHub", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Cyan, AutoSize = true, Cursor = Cursors.Hand };
        link.Click += (_, _) => Process.Start(new ProcessStartInfo { FileName = "https://github.com/needsleepz/Roblox-AntiAFK", UseShellExecute = true });
        fl.Controls.Add(link);

        fl.Controls.Add(new Label { Text = "Based on AntiAFK-Roblox by JunkBeat", Font = new Font("Segoe UI", 8.5f), ForeColor = T2, AutoSize = true, Margin = new Padding(0, 8, 0, 0) });
        var origLink = new Label { Text = "Original", Font = new Font("Segoe UI", 8.5f, FontStyle.Regular), ForeColor = Color.FromArgb(100, 140, 180), AutoSize = true, Cursor = Cursors.Hand, Margin = new Padding(0, 0, 0, 0) };
        origLink.Click += (_, _) => Process.Start(new ProcessStartInfo { FileName = "https://github.com/JunkBeat/AntiAFK-Roblox", UseShellExecute = true });
        fl.Controls.Add(origLink);

        f.Controls.Add(fl);
        f.ShowDialog();
    }

    public void ToggleSvr()
    {
        if (_svr != null) { CloseSvr(); return; }

        _allowNotif = false;
        foreach (var w in WinManager.GetVisibleRobloxWindows()) w.SetNoTopMost();

        _svr = new Form
        {
            BackColor = Color.Black,
            FormBorderStyle = FormBorderStyle.None,
            WindowState = FormWindowState.Maximized,
            TopMost = true,
            ShowInTaskbar = false
        };
        _svr.MouseMove += SvrMove;
        _svr.KeyPress += (_, e) => { if (e.KeyChar == 27) CloseSvr(); };
        _svr.Show();
        _lm = Cursor.Position;
    }

    private void SvrMove(object? sender, MouseEventArgs e)
    {
        var cur = Cursor.Position;
        if (Math.Abs(cur.X - _lm.X) > 15 || Math.Abs(cur.Y - _lm.Y) > 15)
        {
            CloseSvr();
            Task.Run(() => { foreach (var w in WinManager.GetVisibleRobloxWindows()) w.SetTop(); });
        }
    }

    private void CloseSvr()
    {
        if (_svr == null) return;
        _allowNotif = true;
        if (_svr.InvokeRequired)
            _svr.Invoke(() => { _svr.Close(); _svr.Dispose(); _svr = null; });
        else
        { _svr.Close(); _svr.Dispose(); _svr = null; }
    }
}
