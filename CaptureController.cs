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
        RecordWindow _activeRecord;

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
            _tray.Rebuild();
        }

        public void Notify(string title, string text, bool warning)
        {
            _tray.Balloon(title, text, warning);
        }

        public async Task UploadAndNotify(string path)
        {
            if (string.IsNullOrWhiteSpace(Settings.UploadUrl)) return;
            Notify(Loc.T("btn_share"), Loc.T("msg_uploading"), false);
            try
            {
                string url = await ShareService.UploadAsync(path, Settings);
                ShareService.CopyText(url);
                Notify(Loc.T("btn_share"), Loc.T("msg_upload_done") + "\n" + url, false);
            }
            catch (Exception ex)
            {
                Notify(Loc.T("btn_share"), Loc.F("msg_upload_fail", ex.Message), true);
            }
        }

        public void ApplyProfile(string name)
        {
            var s = Settings.Clone();
            if (!s.ApplyProfile(name)) return;
            Apply(s);
            _tray.Balloon(Loc.T("tray_profiles"), Loc.F("msg_profile_applied", name), false);
        }

        public void CycleProfile()
        {
            var list = Settings.Profiles;
            if (list == null || list.Count == 0) return;
            int i = list.FindIndex(p => p.Name == Settings.ActiveProfile);
            ApplyProfile(list[(i + 1) % list.Count].Name);
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
            Reg(Settings.RegionDelayHotkey, Loc.T("tray_region_delay"), RegionDelayed, failed);
            Reg(Settings.RecordHotkey, Loc.T("tray_record"), Record, failed);
            Reg(Settings.ProfileHotkey, Loc.T("hk_profile"), CycleProfile, failed);
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
                case "record": Record(); break;
                case "settings": OpenSettings(); break;
                default: Region(); break;
            }
        }

        public void Region() { _ = RunGuarded(() => RegionAsync(false, 150)); }
        public void RegionDelayed() { _ = RunGuarded(() => WithCountdown(() => RegionAsync(false, 150))); }
        public void WindowDelayed() { _ = RunGuarded(() => WithCountdown(() => RegionAsync(true, 150))); }
        public void WindowCap() { _ = RunGuarded(() => RegionAsync(true, 150)); }
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
        public void Record()
        {
            if (_activeRecord != null)
            {
                _activeRecord.RequestStop();
                return;
            }
            _ = RunGuarded(RecordAsync);
        }
        public void DelayedFull() { _ = RunGuarded(() => WithCountdown(() => FullAsync(150))); }

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

        async Task WithCountdown(Func<Task> next)
        {
            int sec = Settings.DelaySeconds;
            if (sec > 0)
            {
                var w = new CountdownWindow();
                await w.RunAsync(sec);
            }
            await next();
        }

        async Task RegionAsync(bool windowOnly, int delayMs)
        {
            await Task.Delay(delayMs);
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

        async Task RecordAsync()
        {
            await Task.Delay(150);
            var region = PickRegion(false);
            if (!region.HasValue) return;
            await Task.Delay(150);
            var win = new RecordWindow(region.Value, Settings);
            _activeRecord = win;
            int esc = _hk.RegisterTemp(new HotkeyDef(0, 0x1B), () => win.RequestCancel());
            try
            {
                var r = await win.RunAsync();
                if (r == null) return;
                if (!string.IsNullOrEmpty(r.Message)) _tray.Balloon(Loc.T("tray_record"), r.Message, r.Path == null);
                if (r.Path != null)
                {
                    var res = new OutputResult();
                    res.FilePaths.Add(r.Path);
                    try { res.TotalBytes = new FileInfo(r.Path).Length; } catch { }
                    ShowResult(res, r.Thumb, r.Width, r.Height);
                }
            }
            finally
            {
                if (esc != 0) _hk.Unregister(esc);
                _activeRecord = null;
            }
        }

        void Deliver(D.Bitmap bmp, string type)
        {
            var res = OutputService.Process(bmp, Settings, type);
            ShowResult(res, OutputService.Thumb(bmp, 240, 150), bmp.Width, bmp.Height);
        }

        void ShowResult(OutputResult res, System.Windows.Media.Imaging.BitmapSource thumb, int width, int height)
        {
            if (Settings.PlaySound) System.Media.SystemSounds.Asterisk.Play();
            if (Settings.ShowToast)
            {
                if (_toast != null)
                {
                    _toast.Close();
                    _toast = null;
                }
                var t = new ToastWindow(res, thumb, Settings.ToastSeconds, width, height, this);
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
            if (Settings.UploadAuto && res.FilePath != null && !string.IsNullOrWhiteSpace(Settings.UploadUrl))
            {
                _ = UploadAndNotify(res.FilePath);
            }
        }
    }
}
