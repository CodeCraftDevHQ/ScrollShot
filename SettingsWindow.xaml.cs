using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScrollShot
{
    public partial class SettingsWindow : Window
    {
        static readonly string[] SwatchColors =
        {
            "#FF3B82F6", "#FF22C55E", "#FFEF4444", "#FFF59E0B",
            "#FF8B5CF6", "#FFEC4899", "#FF14B8A6", "#FF64748B"
        };

        readonly CaptureController _ctl;
        FrameworkElement[] _pages;
        AppSettings _edit;
        bool _ready;
        bool _saved;

        internal SettingsWindow(CaptureController ctl)
        {
            _ctl = ctl;
            InitializeComponent();
            _pages = new FrameworkElement[] { Page0, Page1, Page2, Page3, Page4, Page5 };
            Load(ctl.Settings.Clone());
            BuildSwatches();
            Nav.SelectedIndex = 0;
            _ready = true;
            Closed += (a, b) =>
            {
                if (!_saved)
                {
                    Loc.SetLanguage(_ctl.Settings.Language);
                    ThemeService.Apply(_ctl.Settings);
                }
            };
        }

        void Load(AppSettings s)
        {
            _edit = s;
            DataContext = _edit;
            HkRegion.Value = _edit.RegionHotkey;
            HkWindow.Value = _edit.WindowHotkey;
            HkFull.Value = _edit.FullScreenHotkey;
            HkScroll.Value = _edit.ScrollHotkey;
            HkProfile.Value = _edit.ProfileHotkey;
            RefreshProfiles();
        }

        void BuildSwatches()
        {
            foreach (var hex in SwatchColors)
            {
                string h = hex;
                var b = new Button
                {
                    Width = 30,
                    Height = 30,
                    Margin = new Thickness(0, 0, 8, 8),
                    Padding = new Thickness(0),
                    Background = (Brush)new BrushConverter().ConvertFromString(h)
                };
                b.Click += (a, e) =>
                {
                    AccentBox.Text = h;
                    _edit.AccentColor = h;
                    PreviewTheme();
                };
                Swatches.Children.Add(b);
            }
        }

        string Tag(ComboBox box)
        {
            var item = box.SelectedItem as ComboBoxItem;
            return item == null ? null : item.Tag as string;
        }

        void PreviewTheme()
        {
            string theme = Tag(ThemeBox) ?? _edit.Theme;
            ThemeService.Apply(theme, AccentBox.Text);
        }

        void OnNav(object sender, SelectionChangedEventArgs e)
        {
            if (_pages == null) return;
            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i].Visibility = i == Nav.SelectedIndex ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        void OnLangChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready) return;
            string t = Tag(LangBox);
            if (t != null) Loc.SetLanguage(t);
        }

        void OnThemeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_ready) return;
            PreviewTheme();
        }

        void OnAccentChanged(object sender, RoutedEventArgs e)
        {
            if (!_ready) return;
            PreviewTheme();
        }

        void RefreshProfiles()
        {
            ProfileList.ItemsSource = null;
            ProfileList.ItemsSource = _edit.Profiles;
            ActiveText.Text = Loc.F("pf_active", string.IsNullOrEmpty(_edit.ActiveProfile) ? "—" : _edit.ActiveProfile);
        }

        void OnProfileSave(object sender, RoutedEventArgs e)
        {
            string name = (ProfileName.Text ?? "").Trim();
            if (name.Length == 0) return;
            var p = _edit.CaptureProfile(name);
            int i = _edit.Profiles.FindIndex(x => string.Equals(x.Name, name, System.StringComparison.OrdinalIgnoreCase));
            if (i >= 0) _edit.Profiles[i] = p; else _edit.Profiles.Add(p);
            _edit.ActiveProfile = name;
            RefreshProfiles();
        }

        void OnProfileApply(object sender, RoutedEventArgs e)
        {
            var sel = ProfileList.SelectedItem as NamedProfile;
            if (sel == null) return;
            _edit.ApplyProfile(sel.Name);
            DataContext = null;
            DataContext = _edit;
            RefreshProfiles();
        }

        void OnProfileDelete(object sender, RoutedEventArgs e)
        {
            var sel = ProfileList.SelectedItem as NamedProfile;
            if (sel == null) return;
            _edit.Profiles.Remove(sel);
            if (_edit.ActiveProfile == sel.Name) _edit.ActiveProfile = "";
            RefreshProfiles();
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

        static bool ValidColor(string text)
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
            if (!ValidColor(_edit.SelectionColor) || !ValidColor(_edit.FrameColor) || !ValidColor(_edit.AccentColor))
            {
                MessageBox.Show(this, Loc.T("color_invalid"), "ScrollShot");
                return;
            }
            _edit.RegionHotkey = HkRegion.Value ?? new HotkeyDef();
            _edit.WindowHotkey = HkWindow.Value ?? new HotkeyDef();
            _edit.FullScreenHotkey = HkFull.Value ?? new HotkeyDef();
            _edit.ScrollHotkey = HkScroll.Value ?? new HotkeyDef();
            _edit.ProfileHotkey = HkProfile.Value ?? new HotkeyDef();
            _edit.FloatingX = _ctl.Settings.FloatingX;
            _edit.FloatingY = _ctl.Settings.FloatingY;
            _saved = true;
            _ctl.Apply(_edit);
            Close();
        }

        void OnCancel(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void OnReset(object sender, RoutedEventArgs e)
        {
            var d = new AppSettings().Normalize();
            Load(d);
            Loc.SetLanguage(d.Language);
            ThemeService.Apply(d);
        }
    }
}
