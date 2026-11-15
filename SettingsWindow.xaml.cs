using System.Windows;
using System.Windows.Media;

namespace ScrollShot
{
    public partial class SettingsWindow : Window
    {
        readonly CaptureController _ctl;
        AppSettings _edit;

        internal SettingsWindow(CaptureController ctl)
        {
            _ctl = ctl;
            InitializeComponent();
            Load(ctl.Settings.Clone());
        }

        void Load(AppSettings s)
        {
            _edit = s;
            DataContext = _edit;
            HkRegion.Value = _edit.RegionHotkey;
            HkWindow.Value = _edit.WindowHotkey;
            HkFull.Value = _edit.FullScreenHotkey;
            HkScroll.Value = _edit.ScrollHotkey;
        }

        void OnBrowse(object sender, RoutedEventArgs e)
        {
            var d = new Microsoft.Win32.OpenFolderDialog { InitialDirectory = _edit.SaveFolder };
            if (d.ShowDialog(this) == true)
            {
                _edit.SaveFolder = d.FolderName;
                TxtFolder.Text = d.FolderName;
            }
        }

        bool ValidColor(string text)
        {
            try
            {
                new BrushConverter().ConvertFromString(text);
                return true;
            }
            catch
            {
                return false;
            }
        }

        void OnSave(object sender, RoutedEventArgs e)
        {
            if (!ValidColor(_edit.SelectionColor) || !ValidColor(_edit.FrameColor))
            {
                MessageBox.Show(this, "رنگ واردشده معتبر نیست. نمونه: #FF3B82F6", "ScrollShot");
                return;
            }
            _edit.RegionHotkey = HkRegion.Value ?? new HotkeyDef();
            _edit.WindowHotkey = HkWindow.Value ?? new HotkeyDef();
            _edit.FullScreenHotkey = HkFull.Value ?? new HotkeyDef();
            _edit.ScrollHotkey = HkScroll.Value ?? new HotkeyDef();
            _edit.FloatingX = _ctl.Settings.FloatingX;
            _edit.FloatingY = _ctl.Settings.FloatingY;
            _ctl.Apply(_edit);
            Close();
        }

        void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void OnReset(object sender, RoutedEventArgs e)
        {
            Load(new AppSettings().Normalize());
        }
    }
}
