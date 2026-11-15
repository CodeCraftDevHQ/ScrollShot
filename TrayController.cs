using System;
using System.Drawing;

namespace ScrollShot
{
    internal sealed class TrayController : IDisposable
    {
        readonly System.Windows.Forms.NotifyIcon _icon;
        readonly CaptureController _ctl;

        public TrayController(CaptureController ctl)
        {
            _ctl = ctl;
            _icon = new System.Windows.Forms.NotifyIcon
            {
                Icon = BuildIcon(),
                Text = "ScrollShot",
                Visible = true
            };
            var menu = new System.Windows.Forms.ContextMenuStrip
            {
                RightToLeft = System.Windows.Forms.RightToLeft.Yes
            };
            menu.Items.Add("گرفتن از ناحیه", null, (a, b) => _ctl.Region());
            menu.Items.Add("گرفتن از پنجره", null, (a, b) => _ctl.WindowCap());
            menu.Items.Add("تمام صفحه", null, (a, b) => _ctl.Full());
            menu.Items.Add("اسکرین‌شات اسکرولی", null, (a, b) => _ctl.Scroll());
            menu.Items.Add("تمام صفحه با تأخیر", null, (a, b) => _ctl.DelayedFull());
            menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            menu.Items.Add("باز کردن پوشه ذخیره", null, (a, b) => _ctl.OpenFolder());
            menu.Items.Add("تنظیمات", null, (a, b) => _ctl.OpenSettings());
            menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());
            menu.Items.Add("خروج", null, (a, b) => System.Windows.Application.Current.Shutdown());
            _icon.ContextMenuStrip = menu;
            _icon.DoubleClick += (a, b) => _ctl.RunAction(_ctl.Settings.TrayDoubleClick);
        }

        public void Balloon(string title, string text, bool warning)
        {
            _icon.ShowBalloonTip(4000, title, text, warning ? System.Windows.Forms.ToolTipIcon.Warning : System.Windows.Forms.ToolTipIcon.Info);
        }

        static Icon BuildIcon()
        {
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
            _icon.Visible = false;
            _icon.Dispose();
        }
    }
}
