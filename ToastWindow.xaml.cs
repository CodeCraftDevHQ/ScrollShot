using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ScrollShot
{
    public partial class ToastWindow : Window
    {
        readonly OutputResult _res;
        readonly DispatcherTimer _timer;

        internal ToastWindow(OutputResult res, System.Windows.Media.Imaging.BitmapSource thumb, int seconds, int width, int height)
        {
            _res = res;
            InitializeComponent();
            Thumb.Source = thumb;
            bool saved = res.FilePath != null;
            if (saved)
            {
                TitleText.Text = "اسکرین‌شات ذخیره شد";
                Sub.Text = Path.GetFileName(res.FilePath) + "  (" + width + "×" + height + ")";
            }
            else
            {
                TitleText.Text = res.Copied ? "در کلیپ‌بورد کپی شد" : "اسکرین‌شات گرفته شد";
                Sub.Text = res.Error != null ? res.Error : width + "×" + height;
                BtnOpen.Visibility = Visibility.Collapsed;
                BtnFolder.Visibility = Visibility.Collapsed;
                BtnDelete.Visibility = Visibility.Collapsed;
            }
            if (saved && res.Copied) Sub.Text += " • کپی شد";
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
            _timer.Tick += (a, b) => Close();
            MouseEnter += (a, b) => _timer.Stop();
            MouseLeave += (a, b) => _timer.Start();
            Loaded += OnLoaded;
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            var h = new WindowInteropHelper(this).Handle;
            WinUtil.ExcludeFromCapture(h);
            var wa = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            var r = WinUtil.GetRect(h);
            WinUtil.MoveTo(h, wa.Right - r.Width - 12, wa.Bottom - r.Height - 12);
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(180)));
            _timer.Start();
        }

        void OnOpen(object sender, RoutedEventArgs e)
        {
            if (_res.FilePath == null) return;
            try { Process.Start(new ProcessStartInfo(_res.FilePath) { UseShellExecute = true }); } catch { }
            Close();
        }

        void OnOpenMouse(object sender, MouseButtonEventArgs e)
        {
            OnOpen(sender, (RoutedEventArgs)e);
        }

        void OnFolder(object sender, RoutedEventArgs e)
        {
            if (_res.FilePath == null) return;
            try { Process.Start("explorer.exe", "/select,\"" + _res.FilePath + "\""); } catch { }
            Close();
        }

        void OnDelete(object sender, RoutedEventArgs e)
        {
            try { if (_res.FilePath != null) File.Delete(_res.FilePath); } catch { }
            Close();
        }

        void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
