using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace ScrollShot
{
    internal sealed class CountdownWindow : Window
    {
        readonly TextBlock _text;

        public CountdownWindow()
        {
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            Topmost = true;
            ShowActivated = false;
            AllowsTransparency = true;
            Background = System.Windows.Media.Brushes.Transparent;
            SizeToContent = SizeToContent.WidthAndHeight;
            _text = new TextBlock
            {
                FontSize = 44,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                MinWidth = 60,
                TextAlignment = TextAlignment.Center
            };
            _text.SetResourceReference(TextBlock.ForegroundProperty, "SystemControlHighlightAccentBrush");
            var border = new Border
            {
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(26, 10, 26, 10),
                Margin = new Thickness(10),
                Child = _text
            };
            border.SetResourceReference(Border.BackgroundProperty, "SystemControlBackgroundAltHighBrush");
            Content = border;
            Loaded += (a, b) =>
            {
                var h = new WindowInteropHelper(this).Handle;
                WinUtil.ExcludeFromCapture(h);
                var wa = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
                var r = WinUtil.GetRect(h);
                WinUtil.MoveTo(h, wa.X + (wa.Width - r.Width) / 2, wa.Y + 24);
            };
        }

        public async Task RunAsync(int seconds)
        {
            Show();
            for (int s = seconds; s > 0; s--)
            {
                _text.Text = s.ToString();
                await Task.Delay(1000);
            }
            Close();
        }
    }
}
