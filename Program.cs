using System.Diagnostics;
using RBX_AntiAFK.Core;

namespace RBX_AntiAFK;

static class Program
{
    private static readonly KeyPresser KeyPresser = new();
    private static readonly Settings Settings = new();
    private static ModernForm? _form;
    private static NotifyIcon? _tray;
    private static bool _exiting;

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Settings.Load();
        KeyPresser.KeypressDelay = Settings.DelayBetweenKeyPressMilliseconds;
        KeyPresser.InteractionDelay = Settings.DelayBeforeWindowInteractionMilliseconds;

        _form = new ModernForm(KeyPresser, Settings);

        var menu = new ContextMenuStrip();
        menu.Renderer = new ModernRenderer();

        var startItem = new ToolStripMenuItem("Start", null, (_, _) => _form.StartAfk());
        var stopItem = new ToolStripMenuItem("Stop", null, async (_, _) => await _form.StopAfkAsync());
        stopItem.Enabled = false;

        var statusItem = new ToolStripMenuItem("Status: Idle") { Enabled = false, ForeColor = Color.FromArgb(120, 120, 140) };

        menu.Items.AddRange([
            statusItem,
            new ToolStripSeparator(),
            startItem,
            stopItem,
            new ToolStripSeparator(),
            new ToolStripMenuItem("Open Panel", null, (_, _) => { _form.Show(); _form.Activate(); _form.WindowState = FormWindowState.Normal; }),
            new ToolStripMenuItem("Show Roblox", null, (_, _) => Invoke(ShowRbx)),
            new ToolStripMenuItem("Hide Roblox", null, (_, _) => Invoke(HideRbx)),
            new ToolStripSeparator(),
            new ToolStripMenuItem("Exit Roblox", null, async (_, _) => await KillRbx()),
            new ToolStripMenuItem("Screensaver", null, (_, _) => { if (_form != null && _form.IsHandleCreated) _form.ToggleSvr(); else _form?.ToggleSvr(); }),
            new ToolStripMenuItem("Test", null, async (_, _) => await _form.TestMove()),
            new ToolStripSeparator(),
            new ToolStripMenuItem("About", null, (_, _) => _form?.ShowAbout()),
            new ToolStripMenuItem("Exit", null, async (_, _) => { _exiting = true; await _form.Exit(); })
        ]);

        _tray = new NotifyIcon
        {
            Text = "Roblox-AntiAFK",
            Icon = Properties.Resources.NormalIcon,
            Visible = true,
            ContextMenuStrip = menu
        };
        _form.SetTray(_tray);

        _tray.DoubleClick += async (_, _) =>
        {
            if (_form.IsRunning())
                await _form.StopAfkAsync();
            else
                _form.StartAfk();
        };

        _form.FormClosing += (_, e) =>
        {
            if (!_exiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                _form.Hide();
            }
        };

        _form.RunningStateChanged += running =>
        {
            statusItem.Text = running ? "Status: Running" : "Status: Idle";
            statusItem.ForeColor = running ? Color.FromArgb(102, 187, 106) : Color.FromArgb(120, 120, 140);
            startItem.Enabled = !running;
            stopItem.Enabled = running;
            _tray.Icon = running ? Properties.Resources.RunningIcon : Properties.Resources.StopIcon;
        };

        Application.Run();
    }

    private static void Invoke(Action a) { try { a(); } catch (Exception ex) { Console.WriteLine($"Error: {ex}"); } }

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

    private static async Task KillRbx()
    {
        try
        {
            if (_form != null && _form.IsRunning())
            {
                await _form.StopAfkAsync();
            }

            var procs = Process.GetProcessesByName("RobloxPlayerBeta");
            foreach (var p in procs)
            {
                try { p.Kill(); } catch { }
                finally { p.Dispose(); }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Kill error: {ex}");
        }
    }

    private class ModernRenderer : ToolStripProfessionalRenderer
    {
        private static readonly Font MenuTextFont = new("Segoe UI", 9.5f, FontStyle.Regular);

        public ModernRenderer() : base(new ModernColors()) { }
        
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextFont = MenuTextFont;
            e.Item.ForeColor = Color.FromArgb(235, 235, 245);
            base.OnRenderItemText(e);
        }
        
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.FromArgb(32, 32, 48));
            
            if (e.Item.Selected)
            {
                using var brush = new SolidBrush(Color.FromArgb(52, 52, 75));
                g.FillRectangle(brush, new Rectangle(2, 0, e.Item.Width - 3, e.Item.Height));
            }
        }
    }

    private class ModernColors : ProfessionalColorTable
    {
        public override Color MenuItemSelected => Color.FromArgb(52, 52, 75);
        public override Color MenuItemSelectedGradientBegin => Color.FromArgb(52, 52, 75);
        public override Color MenuItemSelectedGradientEnd => Color.FromArgb(52, 52, 75);
        public override Color MenuItemBorder => Color.FromArgb(50, 50, 70);
        public override Color MenuStripGradientBegin => Color.FromArgb(32, 32, 48);
        public override Color MenuStripGradientEnd => Color.FromArgb(32, 32, 48);
        public override Color ToolStripDropDownBackground => Color.FromArgb(32, 32, 48);
        public override Color ImageMarginGradientBegin => Color.FromArgb(32, 32, 48);
        public override Color ImageMarginGradientMiddle => Color.FromArgb(32, 32, 48);
        public override Color ImageMarginGradientEnd => Color.FromArgb(32, 32, 48);
        public override Color SeparatorDark => Color.FromArgb(60, 60, 85);
        public override Color SeparatorLight => Color.FromArgb(60, 60, 85);
        public override Color MenuItemPressedGradientBegin => Color.FromArgb(42, 42, 62);
        public override Color MenuItemPressedGradientEnd => Color.FromArgb(42, 42, 62);
        public override Color CheckBackground => Color.FromArgb(102, 187, 106);
        public override Color CheckSelectedBackground => Color.FromArgb(102, 187, 106);
        public override Color CheckPressedBackground => Color.FromArgb(102, 187, 106);
    }
}
