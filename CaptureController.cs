using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using D = System.Drawing;

namespace ScrollShot
{
    internal sealed class CaptureController
    {
        TrayController _tray;
        HotkeyManager _hk;
        FloatingButton _float;
        SettingsWindow _settingsWin;
        ToastWindow _toast;
        bool _busy;
        ScrollControlWindow _activeScroll;

        public AppSettings Settings { get; private set; }

        public CaptureController(AppSettings s)
        {
            Settings = s;
        }

        public void Start(TrayController tray)
        {
            _tray = tray;
            _hk = new HotkeyManager();
            RegisterHotkeys();
            UpdateFloating();
            ApplyStartup();
        }

        public void Apply(AppSettings s)
        {
            Settings = s.Normalize();
            SettingsStore.Save(Settings);
            Loc.SetLanguage(Settings.Language);
            ThemeService.Apply(Settings);
            RegisterHotkeys();
            UpdateFloating();
            ApplyStartup();
        }

        public void SaveSettings()
        {
            SettingsStore.Save(Settings);
        }

        void RegisterHotkeys()
        {
            _hk.Clear();
            var failed = new List<string>();
            Reg(Settings.RegionHotkey, Loc.T("tray_region"), Region, failed);
            Reg(Settings.WindowHotkey, Loc.T("tray_window"), WindowCap, failed);
            Reg(Settings.FullScreenHotkey, Loc.T("tray_full"), Full, failed);
            Reg(Settings.ScrollHotkey, Loc.T("tray_scroll"), Scroll, failed);
            if (failed.Count > 0)
            {
                _tray.Balloon(Loc.T("msg_hotkey_title"), Loc.F("msg_hotkey_text", string.Join(Loc.T("msg_sep"), failed)), true);
            }
        }

        void Reg(HotkeyDef def, string name, Action action, List<string> failed)
        {
            if (def == null || def.Key == 0) return;
            if (!_hk.Register(def, action)) failed.Add(name + " (" + def + ")");
        }

        void UpdateFloating()
        {
            if (_float != null)
            {
                _float.Close();
                _float = null;
            }
            if (Settings.ShowFloatingButton)
            {
                _float = new FloatingButton(this);
                _float.Show();
            }
        }

        void ApplyStartup()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (k == null) return;
                    if (Settings.StartWithWindows) k.SetValue("ScrollShot", "\"" + Environment.ProcessPath + "\"");
                    else k.DeleteValue("ScrollShot", false);
                }
            }
            catch
            {
            }
        }

        public void RunAction(string name)
        {
            switch (name)
            {
                case "window": WindowCap(); break;
                case "full": Full(); break;
                case "scroll": Scroll(); break;
                case "settings": OpenSettings(); break;
                default: Region(); break;
            }
        }

        public void Region() { _ = RunGuarded(() => RegionAsync(false)); }
        public void WindowCap() { _ = RunGuarded(() => RegionAsync(true)); }
        public void Full() { _ = RunGuarded(() => FullAsync(150)); }
        public void Scroll()
        {
            if (_activeScroll != null)
            {
                _activeScroll.RequestFinish();
                return;
            }
            _ = RunGuarded(ScrollAsync);
        }
        public void DelayedFull() { _ = RunGuarded(() => FullAsync(Settings.DelaySeconds * 1000)); }

        public void OpenFolder()
        {
            try
            {
                Directory.CreateDirectory(Settings.SaveFolder);
                Process.Start(new ProcessStartInfo(Settings.SaveFolder) { UseShellExecute = true });
            }
            catch
            {
            }
        }

        public void OpenSettings()
        {
            if (_settingsWin != null)
            {
                _settingsWin.Activate();
                return;
            }
            _settingsWin = new SettingsWindow(this);
            _settingsWin.Closed += (a, b) => _settingsWin = null;
            _settingsWin.Show();
            _settingsWin.Activate();
        }

        async Task RunGuarded(Func<Task> work)
        {
            if (_busy) return;
            _busy = true;
            try
            {
                await work();
            }
            catch (Exception ex)
            {
                _tray.Balloon(Loc.T("msg_error"), ex.Message, true);
            }
            finally
            {
                _busy = false;
            }
        }

        D.Rectangle? PickRegion(bool windowOnly)
        {
            var vb = ScreenGrab.VirtualBounds();
            using (var desktop = ScreenGrab.Capture(vb))
            {
                var wins = ScreenGrab.EnumerateWindows();
                var ov = new RegionOverlay(desktop, vb, wins, Settings, windowOnly);
                ov.ShowDialog();
                return ov.Result;
            }
        }

        async Task RegionAsync(bool windowOnly)
        {
            await Task.Delay(150);
            var vb = ScreenGrab.VirtualBounds();
            using (var desktop = ScreenGrab.Capture(vb))
            {
                var wins = ScreenGrab.EnumerateWindows();
                var ov = new RegionOverlay(desktop, vb, wins, Settings, windowOnly);
                ov.ShowDialog();
                if (ov.Result.HasValue)
                {
                    var r = ov.Result.Value;
                    using (var crop = ScreenGrab.Crop(desktop, new D.Rectangle(r.X - vb.X, r.Y - vb.Y, r.Width, r.Height)))
                    {
                        Deliver(crop, windowOnly ? "Window" : "Region");
                    }
                }
            }
        }

        async Task FullAsync(int delayMs)
        {
            await Task.Delay(Math.Max(delayMs, 50));
            D.Rectangle area = Settings.FullScreenScope == "all"
                ? ScreenGrab.VirtualBounds()
                : System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Control.MousePosition).Bounds;
            using (var bmp = ScreenGrab.Capture(area))
            {
                Deliver(bmp, "Full");
            }
        }

        async Task ScrollAsync()
        {
            await Task.Delay(150);
            var region = PickRegion(false);
            if (!region.HasValue) return;
            await Task.Delay(150);
            var win = new ScrollControlWindow(region.Value, Settings);
            _activeScroll = win;
            int esc = _hk.RegisterTemp(new HotkeyDef(0, 0x1B), () => win.RequestCancel());
            try
            {
                using (var bmp = await win.RunAsync())
                {
                    if (!string.IsNullOrEmpty(win.EndMessage)) _tray.Balloon(Loc.T("tray_scroll"), win.EndMessage, true);
                    if (bmp != null) Deliver(bmp, "Scroll");
                }
            }
            finally
            {
                if (esc != 0) _hk.Unregister(esc);
                _activeScroll = null;
            }
        }

        void Deliver(D.Bitmap bmp, string type)
        {
            var res = OutputService.Process(bmp, Settings, type);
            if (Settings.PlaySound) System.Media.SystemSounds.Asterisk.Play();
            if (Settings.ShowToast)
            {
                if (_toast != null)
                {
                    _toast.Close();
                    _toast = null;
                }
                var thumb = OutputService.Thumb(bmp, 240, 150);
                var t = new ToastWindow(res, thumb, Settings.ToastSeconds, bmp.Width, bmp.Height);
                t.Closed += (a, b) =>
                {
                    if (_toast == t) _toast = null;
                };
                _toast = t;
                t.Show();
            }
            else if (res.Error != null)
            {
                _tray.Balloon(Loc.T("msg_save_error"), res.Error, true);
            }
        }
    }
}
