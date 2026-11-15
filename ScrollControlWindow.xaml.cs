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
        readonly ScrollStitcher _st;
        readonly TaskCompletionSource<D.Bitmap> _tcs = new TaskCompletionSource<D.Bitmap>();
        ScrollFrameWindow _frame;
        CancellationTokenSource _cts;
        DispatcherTimer _countdown;
        int _left;
        bool _running;
        bool _closing;

        public string EndMessage { get; private set; }

        internal ScrollControlWindow(D.Rectangle region, AppSettings s)
        {
            _region = region;
            _s = s;
            _st = new ScrollStitcher(s);
            InitializeComponent();
            ChkTop.IsChecked = s.ScrollFromTop;
            if (s.ScrollMode == "manual") BtnManual.IsDefault = true; else BtnAuto.IsDefault = true;
            Loaded += (a, b) =>
            {
                PlaceNear();
                if (_s.ScrollCountdownSec > 0) StartCountdown();
                else Status.Text = "آماده است. حالت را انتخاب کنید.";
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
            BtnAuto.Content = "شروع همین الان";
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
            string mode = _s.ScrollMode == "manual" ? "دستی" : "خودکار";
            Status.Text = "شروع " + mode + " تا " + _left + " ثانیه دیگر… ابتدا ناحیه را در جای دلخواه قرار دهید.";
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
            Status.Text = "در حال ساخت تصویر نهایی…";
            D.Bitmap result = null;
            try
            {
                result = await Task.Run(() => _st.Build());
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
            return Task.Run(() => ScreenGrab.Capture(_region));
        }

        async Task<StitchResult> AddAsync(D.Bitmap frame)
        {
            var r = await Task.Run(() => _st.Add(frame));
            Status.Text = "فریم‌ها: " + _st.FrameCount + "   |   ارتفاع: " + _st.TotalHeight + " px";
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

        async Task ScrollToTopAsync(CancellationToken ct, int cx, int cy)
        {
            Status.Text = "در حال برگرداندن صفحه به بالا…";
            D.Bitmap last = null;
            try
            {
                for (int round = 0; round < 12; round++)
                {
                    Native.SetCursorPos(cx, cy);
                    for (int i = 0; i < 40; i++)
                    {
                        Native.SendWheel(120);
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
            Native.SetCursorPos(cx, cy);
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
                if (_st.LimitReached) return "به حداکثر ارتفاع رسید.";
                if (_st.FrameCount >= _s.MaxFrames) return "به حداکثر تعداد فریم رسید.";
                int n = Math.Max(1, (int)Math.Round(notches));
                Native.SetCursorPos(cx, cy);
                Native.SendWheel(-120 * n);
                StitchResult r;
                using (var frame = await WaitStableAsync(ct, _s.ScrollDelayMs))
                {
                    r = await AddAsync(frame);
                }
                if (r == StitchResult.Added)
                {
                    noChange = 0;
                    failed = 0;
                    double ratio = 0.5 * _region.Height / Math.Max(1, _st.LastShift);
                    notches = Math.Max(1, Math.Min(40, notches * Math.Max(0.6, Math.Min(1.6, ratio))));
                }
                else if (r == StitchResult.NoChange)
                {
                    if (++noChange >= 2) return null;
                }
                else if (r == StitchResult.Failed)
                {
                    failed++;
                    if (failed >= 4) return "تطبیق تصاویر ناموفق بود. محتوای این ناحیه برای چسباندن مناسب نیست؛ حالت دستی را امتحان کنید.";
                    Status.Text = "تنظیم خودکار گام اسکرول… (تلاش " + failed + ")";
                    Native.SetCursorPos(cx, cy);
                    Native.SendWheel(120 * n);
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
            Status.Text += "\nحالا خودتان صفحه را آرام به پایین اسکرول کنید؛ در پایان «پایان و ذخیره».";
            D.Bitmap last = null;
            try
            {
                while (true)
                {
                    await Task.Delay(150, ct);
                    if (_st.LimitReached) return "به حداکثر ارتفاع رسید.";
                    if (_st.FrameCount >= _s.MaxFrames) return "به حداکثر تعداد فریم رسید.";
                    var cur = await GrabAsync();
                    var prev = last;
                    bool stable = prev != null && await Task.Run(() => ScrollStitcher.QuickEqual(prev, cur));
                    if (last != null) last.Dispose();
                    last = cur;
                    if (!stable) continue;
                    var r = await AddAsync(cur);
                    if (r == StitchResult.Failed)
                    {
                        Status.Text = "فریم‌ها: " + _st.FrameCount + "   |   ارتفاع: " + _st.TotalHeight + " px\nاسکرول به بالا یا خیلی سریع بود؛ دوباره آرام به پایین بروید.";
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
