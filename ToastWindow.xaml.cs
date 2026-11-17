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
        readonly CaptureController _ctl;
        bool _menuOpen;

        internal ToastWindow(OutputResult res, System.Windows.Media.Imaging.BitmapSource thumb, int seconds, int width, int height, CaptureController ctl)
        {
            _res = res;
            _ctl = ctl;
            InitializeComponent();
            Thumb.Source = thumb;
            bool saved = res.FilePath != null;
            if (saved)
            {
                TitleText.Text = Loc.T("toast_saved");
                string size = OutputService.FormatSize(res.TotalBytes);
                if (res.FilePaths.Count == 1) Sub.Text = Path.GetFileName(res.FilePath) + "  (" + width + "×" + height + ")  " + size;
                else Sub.Text = Loc.F("toast_files", res.FilePaths.Count) + "  •  " + size;
            }
            else
            {
                TitleText.Text = Loc.T(res.Copied ? "toast_copied" : "toast_taken");
                Sub.Text = res.Error != null ? res.Error : width + "×" + height;
                BtnOpen.Visibility = Visibility.Collapsed;
                BtnFolder.Visibility = Visibility.Collapsed;
                BtnDelete.Visibility = Visibility.Collapsed;
                BtnShare.Visibility = Visibility.Collapsed;
            }
            if (saved && res.Copied) Sub.Text += " • " + Loc.T("toast_copied_suffix");
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(seconds) };
            _timer.Tick += (a, b) => Close();
            MouseEnter += (a, b) => _timer.Stop();
            MouseLeave += (a, b) =>
            {
                if (!_menuOpen) _timer.Start();
            };
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
            foreach (var f in _res.FilePaths)
            {
                try { File.Delete(f); } catch { }
            }
            Close();
        }

        void OnShare(object sender, RoutedEventArgs e)
        {
            string path = _res.FilePath;
            if (path == null) return;
            var m = new System.Windows.Controls.ContextMenu
            {
                FlowDirection = Loc.Flow,
                PlacementTarget = BtnShare
            };
            AddItem(m, Loc.T("sh_copy_path"), () => ShareService.CopyText(path));
            AddItem(m, Loc.T("sh_copy_file"), () => ShareService.CopyFile(path));
            AddItem(m, Loc.T("sh_open_with"), () =>
            {
                try { ShareService.OpenWith(path, _ctl.Settings); } catch (Exception ex) { _ctl.Notify(Loc.T("msg_error"), ex.Message, true); }
            });
            if (!string.IsNullOrWhiteSpace(_ctl.Settings.UploadUrl))
            {
                AddItem(m, Loc.T("sh_upload"), () => { _ = _ctl.UploadAndNotify(path); });
            }
            _menuOpen = true;
            _timer.Stop();
            m.Closed += (a, b) =>
            {
                _menuOpen = false;
                _timer.Start();
            };
            m.IsOpen = true;
        }

        static void AddItem(System.Windows.Controls.ContextMenu m, string header, Action action)
        {
            var item = new System.Windows.Controls.MenuItem { Header = header };
            item.Click += (a, b) => action();
            m.Items.Add(item);
        }

        void OnClose(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
