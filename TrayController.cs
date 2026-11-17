using System;
using System.Drawing;

namespace ScrollShot
{
    internal sealed class DarkColors : System.Windows.Forms.ProfessionalColorTable
    {
        public override Color ToolStripDropDownBackground { get { return Color.FromArgb(43, 43, 43); } }
        public override Color ImageMarginGradientBegin { get { return Color.FromArgb(43, 43, 43); } }
        public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(43, 43, 43); } }
        public override Color ImageMarginGradientEnd { get { return Color.FromArgb(43, 43, 43); } }
        public override Color MenuBorder { get { return Color.FromArgb(75, 75, 75); } }
        public override Color MenuItemBorder { get { return Color.FromArgb(75, 75, 75); } }
        public override Color MenuItemSelected { get { return Color.FromArgb(72, 72, 72); } }
        public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(72, 72, 72); } }
        public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(72, 72, 72); } }
        public override Color SeparatorDark { get { return Color.FromArgb(80, 80, 80); } }
        public override Color SeparatorLight { get { return Color.FromArgb(80, 80, 80); } }
    }

    internal sealed class TrayController : IDisposable
    {
        readonly System.Windows.Forms.NotifyIcon _icon;
        readonly CaptureController _ctl;
        System.Windows.Forms.ContextMenuStrip _menu;

        public TrayController(CaptureController ctl)
        {
            _ctl = ctl;
            _icon = new System.Windows.Forms.NotifyIcon
            {
                Icon = BuildIcon(),
                Text = "ScrollShot",
                Visible = true
            };
            _icon.DoubleClick += (a, b) => _ctl.RunAction(_ctl.Settings.TrayDoubleClick);
            BuildMenu();
            Loc.Changed += BuildMenu;
            ThemeService.Changed += BuildMenu;
        }

        public void Rebuild()
        {
            BuildMenu();
        }

        void BuildMenu()
        {
            bool dark = ThemeService.IsDark;
            var menu = new System.Windows.Forms.ContextMenuStrip
            {
                RightToLeft = Loc.IsRtl ? System.Windows.Forms.RightToLeft.Yes : System.Windows.Forms.RightToLeft.No,
                Font = new Font("Segoe UI", 9.5f),
                ShowImageMargin = false
            };
            if (dark)
            {
                menu.Renderer = new System.Windows.Forms.ToolStripProfessionalRenderer(new DarkColors()) { RoundedEdges = false };
                menu.BackColor = Color.FromArgb(43, 43, 43);
                menu.ForeColor = Color.FromArgb(240, 240, 240);
            }
            Add(menu, "tray_region", () => _ctl.Region());
            Add(menu, "tray_window", () => _ctl.WindowCap());
            Add(menu, "tray_full", () => _ctl.Full());
            Add(menu, "tray_scroll", () => _ctl.Scroll());
            Add(menu, "tray_record", () => _ctl.Record());
            Add(menu, "tray_delayed", () => _ctl.DelayedFull());
            Add(menu, "tray_region_delay", () => _ctl.RegionDelayed());
            Add(menu, "tray_window_delay", () => _ctl.WindowDelayed());
            var profiles = _ctl.Settings.Profiles;
            if (profiles != null && profiles.Count > 0)
            {
                var sub = new System.Windows.Forms.ToolStripMenuItem(Loc.T("tray_profiles"));
                foreach (var p in profiles)
                {
                    string n = p.Name;
                    var it = new System.Windows.Forms.ToolStripMenuItem(n) { Checked = n == _ctl.Settings.ActiveProfile };
                    if (dark)
                    {
                        it.BackColor = Color.FromArgb(43, 43, 43);
                        it.ForeColor = Color.FromArgb(240, 240, 240);
                    }
                    it.Click += (a, b) => _ctl.ApplyProfile(n);
                    sub.DropDownItems.Add(it);
                }
                if (dark)
                {
                    sub.DropDown.BackColor = Color.FromArgb(43, 43, 43);
                    sub.ForeColor = Color.FromArgb(240, 240, 240);
                }
                menu.Items.Add(sub);
            }
            menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            Add(menu, "tray_update", () => _ctl.CheckForUpdates(true));
            Add(menu, "tray_folder", () => _ctl.OpenFolder());
            Add(menu, "tray_settings", () => _ctl.OpenSettings());
            menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            Add(menu, "tray_exit", () => System.Windows.Application.Current.Shutdown());
            var old = _menu;
            _menu = menu;
            _icon.ContextMenuStrip = menu;
            if (old != null) old.Dispose();
        }

        static void Add(System.Windows.Forms.ContextMenuStrip menu, string key, Action action)
        {
            var item = new System.Windows.Forms.ToolStripMenuItem(Loc.T(key));
            item.Padding = new System.Windows.Forms.Padding(4, 4, 4, 4);
            item.Click += (a, b) => action();
            menu.Items.Add(item);
        }

        public void Balloon(string title, string text, bool warning)
        {
            _icon.ShowBalloonTip(4000, title, text, warning ? System.Windows.Forms.ToolTipIcon.Warning : System.Windows.Forms.ToolTipIcon.Info);
        }

        static Icon BuildIcon()
        {
            try
            {
                var info = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/app.ico"));
                if (info != null)
                {
                    using (var st = info.Stream)
                    {
                        return new Icon(st, 32, 32);
                    }
                }
            }
            catch
            {
            }
            using (var bmp = new Bitmap(32, 32))
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                using (var bg = new SolidBrush(Color.FromArgb(255, 59, 130, 246)))
                    g.FillEllipse(bg, 1, 1, 30, 30);
                using (var pen = new Pen(Color.White, 2.5f))
                {
                    g.DrawRectangle(pen, 9, 8, 14, 16);
                    g.DrawLine(pen, 16, 12, 16, 20);
                    g.DrawLine(pen, 13, 17, 16, 20);
                    g.DrawLine(pen, 19, 17, 16, 20);
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        public void Dispose()
        {
            Loc.Changed -= BuildMenu;
            ThemeService.Changed -= BuildMenu;
            _icon.Visible = false;
            _icon.Dispose();
        }
    }
}
