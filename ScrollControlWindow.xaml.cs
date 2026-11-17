using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using D = System.Drawing;

namespace ScrollShot
{
    public partial class ScrollControlWindow : Window
    {
        readonly D.Rectangle _region;
        readonly AppSettings _s;
        ScrollStitcher _st;
        bool _horizontal;
        readonly TaskCompletionSource<D.Bitmap> _tcs = new TaskCompletionSource<D.Bitmap>();
        ScrollFrameWindow _frame;
        CancellationTokenSource _cts;
        DispatcherTimer _countdown;
        int _left;
        bool _useMsg;
        IntPtr _target;
        bool _running;
        bool _closing;

        public string EndMessage { get; private set; }

        internal ScrollControlWindow(D.Rectangle region, AppSettings s)
        {
            _region = region;
            _s = s;
            _horizontal = s.ScrollDirection == "horizontal";
            _st = new ScrollStitcher(s, _horizontal);
            InitializeComponent();
            RbV.IsChecked = !_horizontal;
            RbH.IsChecked = _horizontal;
            ChkTop.IsChecked = s.ScrollFromTop;
            var accent = TryFindResource("AccentButtonStyle") as Style;
            if (s.ScrollMode == "manual")
            {
                BtnManual.IsDefault = true;
                if (accent != null) BtnManual.Style = accent;
            }
            else
            {
                BtnAuto.IsDefault = true;
                if (accent != null) BtnAuto.Style = accent;
            }
            Loaded += (a, b) =>
            {
                PlaceNear();
                if (_s.ScrollCountdownSec > 0) StartCountdown();
                else Status.Text = Loc.T("bar_ready");
            };
            Closed += (a, b) =>
            {
                StopCountdown();
                if (!_closing)
                {
                    _closing = true;
                    if (_cts != null) _cts.Cancel();
                    if (!_running) _st.Dispose();
                }
                if (_frame != null) _frame.Close();
                _tcs.TrySetResult(null);
            };
        }

        public Task<D.Bitmap> RunAsync()
        {
            _frame = new ScrollFrameWindow(_region, _s.FrameColor);
            _frame.Show();
            Show();
            return _tcs.Task;
        }

        public void RequestFinish()
        {
            if (_running && _cts != null) _cts.Cancel();
        }

        public void RequestCancel()
        {
            if (!_closing) DoCancel();
        }

        void PlaceNear()
        {
            var h = new WindowInteropHelper(this).Handle;
            WinUtil.ExcludeFromCapture(h);
            var scr = System.Windows.Forms.Screen.FromRectangle(_region).Bounds;
            WinUtil.MoveTo(h, _region.X, _region.Bottom);
            var wr = WinUtil.GetRect(h);
            int x = _region.X + (_region.Width - wr.Width) / 2;
            int y = _region.Bottom + 12;
            if (y + wr.Height > scr.Bottom) y = _region.Y - wr.Height - 12;
            if (y < scr.Y) y = _region.Bottom - wr.Height - 16;
            x = Math.Max(scr.X, Math.Min(x, scr.Right - wr.Width));
            y = Math.Max(scr.Y, Math.Min(y, scr.Bottom - wr.Height));
            WinUtil.MoveTo(h, x, y);
        }

        void StartCountdown()
        {
            _left = _s.ScrollCountdownSec;
            BtnAuto.Content = Loc.T("bar_now");
            ShowCountdown();
            _countdown = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _countdown.Tick += (a, b) =>
            {
                _left--;
                if (_left <= 0)
                {
                    StopCountdown();
                    StartMode(_s.ScrollMode != "manual");
                }
                else
                {
                    ShowCountdown();
                }
            };
            _countdown.Start();
        }

        void ShowCountdown()
        {
            string mode = Loc.T(_s.ScrollMode == "manual" ? "mode_manual" : "mode_auto");
            Status.Text = Loc.F("bar_countdown", mode, _left);
        }

        void StopCountdown()
        {
            if (_countdown != null)
            {
                _countdown.Stop();
                _countdown = null;
            }
        }

        void OnDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.TextBlock || e.OriginalSource is System.Windows.Controls.Border)
            {
                try { DragMove(); } catch { }
            }
        }

        void OnTopChanged(object sender, RoutedEventArgs e)
        {
            _s.ScrollFromTop = ChkTop.IsChecked == true;
            SettingsStore.Save(_s);
        }

        void OnDirChanged(object sender, RoutedEventArgs e)
        {
            if (_st == null || _running) return;
            bool h = RbH.IsChecked == true;
            if (h == _horizontal) return;
            _horizontal = h;
            _st.Dispose();
            _st = new ScrollStitcher(_s, _horizontal);
            _s.ScrollDirection = h ? "horizontal" : "vertical";
            SettingsStore.Save(_s);
        }

        void OnAuto(object sender, RoutedEventArgs e) { StartMode(true); }
        void OnManual(object sender, RoutedEventArgs e) { StartMode(false); }

        void OnDone(object sender, RoutedEventArgs e)
        {
            RequestFinish();
        }

        void OnCancel(object sender, RoutedEventArgs e)
        {
            DoCancel();
        }

        void DoCancel()
        {
            _closing = true;
            StopCountdown();
            if (_cts != null) _cts.Cancel();
            if (!_running) _st.Dispose();
            _tcs.TrySetResult(null);
            Close();
        }

        async void StartMode(bool auto)
        {
            if (_running || _closing) return;
            StopCountdown();
            _running = true;
            BtnAuto.IsEnabled = false;
            BtnManual.IsEnabled = false;
            ChkTop.IsEnabled = false;
            RbV.IsEnabled = false;
            RbH.IsEnabled = false;
            BtnDone.IsEnabled = true;
            Bar.Visibility = Visibility.Visible;
            Bar.IsIndeterminate = true;
            _cts = new CancellationTokenSource();
            string endMsg = null;
            try
            {
                endMsg = auto ? await AutoLoop(_cts.Token) : await ManualLoop(_cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Log.Error("scroll loop", ex);
                endMsg = ex.Message;
            }
            if (_closing)
            {
                _st.Dispose();
                return;
            }
            await FinishAsync(endMsg);
        }

        async Task FinishAsync(string msg)
        {
            _closing = true;
            EndMessage = msg;
            BtnDone.IsEnabled = false;
            Status.Text = Loc.T("bar_building");
            D.Bitmap result = null;
            try
            {
                bool horizontal = _horizontal;
                bool trimH = _s.TrimHeader;
                bool trimF = _s.TrimFooter;
                result = await Task.Run(() =>
                {
                    var b = _st.Build(trimH, trimF);
                    if (horizontal) b.RotateFlip(System.Drawing.RotateFlipType.Rotate270FlipNone);
                    return b;
                });
            }
            catch (Exception ex)
            {
                EndMessage = ex.Message;
            }
            _st.Dispose();
            _tcs.TrySetResult(result);
            Close();
        }

        Task<D.Bitmap> GrabAsync()
        {
            bool horizontal = _horizontal;
            return Task.Run(() =>
            {
                var b = ScreenGrab.Capture(_region);
                if (horizontal) b.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone);
                return b;
            });
        }

        async Task<StitchResult> AddAsync(D.Bitmap frame)
        {
            var r = await Task.Run(() => _st.Add(frame));
            if (r == StitchResult.Failed)
            {
                Log.Info("stitch failed: frames=" + _st.FrameCount + " height=" + _st.TotalHeight + " region=" + _region + " horizontal=" + _horizontal);
                if (_s.DebugDumpFrames) Log.DumpFrame(frame, "fail", _st.FrameCount);
            }
            Status.Text = Loc.F("bar_stats", _st.FrameCount, _st.TotalHeight);
            return r;
        }

        async Task<D.Bitmap> WaitStableAsync(CancellationToken ct, int initialDelay)
        {
            await Task.Delay(initialDelay, ct);
            var prev = await GrabAsync();
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < _s.ScrollStableTimeoutMs)
            {
                await Task.Delay(100, ct);
                var cur = await GrabAsync();
                var a = prev;
                bool same = await Task.Run(() => ScrollStitcher.QuickEqual(a, cur));
                if (same)
                {
                    prev.Dispose();
                    return cur;
                }
                prev.Dispose();
                prev = cur;
            }
            return prev;
        }

        void Wheel(int notches, int cx, int cy)
        {
            int dir = _horizontal ? -1 : 1;
            if (_useMsg && _target != IntPtr.Zero)
            {
                int sign = notches < 0 ? -1 : 1;
                for (int i = 0; i < Math.Abs(notches); i++)
                {
                    if (_horizontal) Native.PostHWheel(_target, dir * sign * 120, cx, cy);
                    else Native.PostWheel(_target, sign * 120, cx, cy);
                }
                return;
            }
            Native.SetCursorPos(cx, cy);
            if (_horizontal) Native.SendHWheel(dir * 120 * notches);
            else Native.SendWheel(120 * notches);
        }

        async Task ScrollToTopAsync(CancellationToken ct, int cx, int cy)
        {
            Status.Text = Loc.T("bar_to_top");
            D.Bitmap last = null;
            try
            {
                for (int round = 0; round < 12; round++)
                {
                    for (int i = 0; i < 40; i++)
                    {
                        Wheel(1, cx, cy);
                        await Task.Delay(8, ct);
                    }
                    var cur = await WaitStableAsync(ct, 250);
                    bool same = false;
                    if (last != null)
                    {
                        var l = last;
                        same = await Task.Run(() => ScrollStitcher.QuickEqual(l, cur));
                        last.Dispose();
                    }
                    last = cur;
                    if (same) break;
                }
            }
            finally
            {
                if (last != null) last.Dispose();
            }
        }

        async Task<string> AutoLoop(CancellationToken ct)
        {
            int cx = _region.X + _region.Width / 2;
            int cy = _region.Y + _region.Height / 2;
            _target = ScreenGrab.HandleAt(ScreenGrab.EnumerateWindows(), new D.Point(cx, cy));
            _useMsg = _s.ScrollWithoutMouse && _target != IntPtr.Zero;
            if (_s.ScrollFromTop) await ScrollToTopAsync(ct, cx, cy);
            using (var first = await WaitStableAsync(ct, 150))
            {
                await AddAsync(first);
            }
            double notches = _s.ScrollNotches;
            int noChange = 0;
            int failed = 0;
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                if (_st.LimitReached) return Loc.T("end_height");
                if (_st.FrameCount >= _s.MaxFrames) return Loc.T("end_frames");
                int n = Math.Max(1, (int)Math.Round(notches));
                Wheel(-n, cx, cy);
                StitchResult r;
                using (var frame = await WaitStableAsync(ct, _s.ScrollDelayMs))
                {
                    r = await AddAsync(frame);
                }
                if (r == StitchResult.Added)
                {
                    noChange = 0;
                    failed = 0;
                    double axis = _horizontal ? _region.Width : _region.Height;
                    double ratio = 0.5 * axis / Math.Max(1, _st.LastShift);
                    notches = Math.Max(1, Math.Min(40, notches * Math.Max(0.6, Math.Min(1.6, ratio))));
                }
                else if (r == StitchResult.NoChange)
                {
                    if (_useMsg && _st.FrameCount == 1)
                    {
                        _useMsg = false;
                        noChange = 0;
                        continue;
                    }
                    if (++noChange >= 2) return null;
                }
                else if (r == StitchResult.Failed)
                {
                    failed++;
                    if (failed >= 4) return Loc.T("end_match");
                    Status.Text = Loc.F("bar_adjust", failed);
                    Wheel(n, cx, cy);
                    using (var back = await WaitStableAsync(ct, _s.ScrollDelayMs))
                    {
                    }
                    notches = Math.Max(1, notches * 0.5);
                }
            }
        }

        async Task<string> ManualLoop(CancellationToken ct)
        {
            Bar.IsIndeterminate = false;
            using (var first = await GrabAsync())
            {
                await AddAsync(first);
            }
            Status.Text += "\n" + Loc.T("bar_manual_hint");
            D.Bitmap last = null;
            try
            {
                while (true)
                {
                    await Task.Delay(150, ct);
                    if (_st.LimitReached) return Loc.T("end_height");
                    if (_st.FrameCount >= _s.MaxFrames) return Loc.T("end_frames");
                    var cur = await GrabAsync();
                    var prev = last;
                    bool stable = prev != null && await Task.Run(() => ScrollStitcher.QuickEqual(prev, cur));
                    if (last != null) last.Dispose();
                    last = cur;
                    if (!stable) continue;
                    var r = await AddAsync(cur);
                    if (r == StitchResult.Failed)
                    {
                        Status.Text = Loc.F("bar_stats", _st.FrameCount, _st.TotalHeight) + "\n" + Loc.T("bar_manual_fail");
                    }
                }
            }
            finally
            {
                if (last != null) last.Dispose();
            }
        }
    }
}
