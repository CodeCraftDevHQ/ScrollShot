using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using D = System.Drawing;

namespace ScrollShot
{
    internal sealed class ScrollFrameWindow : Window
    {
        readonly D.Rectangle _region;

        public ScrollFrameWindow(D.Rectangle region, string color)
        {
            _region = region;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ShowActivated = false;
            Left = 0;
            Top = 0;
            Width = 40;
            Height = 40;
            Content = new Border
            {
                BorderBrush = WinUtil.ParseBrush(color, Brushes.Red),
                BorderThickness = new Thickness(3),
                IsHitTestVisible = false
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var h = new WindowInteropHelper(this).Handle;
            WinUtil.SwallowDpiChange(h);
            WinUtil.MakeClickThrough(h);
            WinUtil.ExcludeFromCapture(h);
            int pad = (int)Math.Ceiling(4 * VisualTreeHelper.GetDpi(this).DpiScaleX);
            WinUtil.SetPhysical(h, _region.X - pad, _region.Y - pad, _region.Width + pad * 2, _region.Height + pad * 2, true, true);
        }
    }
}
