using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using D = System.Drawing;

namespace ScrollShot
{
    internal sealed class RecordResult
    {
        public string Path;
        public System.Windows.Media.Imaging.BitmapSource Thumb;
        public int Width;
        public int Height;
        public string Message;
    }

    public partial class RecordWindow : Window
    {
        readonly D.Rectangle _region;
        readonly AppSettings _s;
        readonly TaskCompletionSource<RecordResult> _tcs = new TaskCompletionSource<RecordResult>();
        readonly List<long> _stamps = new List<long>();
        readonly string _dir;
        ScrollFrameWindow _frame;
        DispatcherTimer _timer;
        CancellationTokenSource _stop;
        Task _captureTask;
        Stopwatch _clock;
        int _left;
        bool _recording;
        bool _encoding;
        bool _cancelled;
        bool _finished;
        int _outW;
        int _outH;

        internal RecordWindow(D.Rectangle region, AppSettings s)
        {
            _region = region;
            _s = s;
            _dir = Path.Combine(Path.GetTempPath(), "ScrollShot_rec_" + Guid.NewGuid().ToString("N"));
            double k = s.RecordScalePercent / 100.0;
            _outW = Math.Max(16, (int)Math.Round(region.Width * k)) & ~1;
            _outH = Math.Max(16, (int)Math.Round(region.Height * k)) & ~1;
            InitializeComponent();
            Loaded += (a, b) =>
            {
                PlaceNear();
                if (_s.RecordCountdownSec > 0) StartCountdown();
                else BeginRecording();
            };
            Closed += (a, b) =>
            {
                if (_timer != null) _timer.Stop();
                if (_frame != null) _frame.Close();
                if (!_finished)
                {
                    _cancelled = true;
                    if (_stop != null) _stop.Cancel();
                }
                _tcs.TrySetResult(null);
            };
        }

        internal Task<RecordResult> RunAsync()
        {
            _frame = new ScrollFrameWindow(_region, "#FFEF4444");
            _frame.Show();
            Show();
            return _tcs.Task;
        }

        public void RequestStop()
        {
            if (_recording && _stop != null) _stop.Cancel();
        }

        public void RequestCancel()
        {
            if (_encoding) return;
            _cancelled = true;
            if (_stop != null) _stop.Cancel();
            if (!_recording) Close();
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
            _left = _s.RecordCountdownSec;
            BtnStop.Content = Loc.T("bar_now");
            Status.Text = Loc.F("rec_countdown", _left);
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (a, b) =>
            {
                _left--;
                if (_left <= 0)
                {
                    _timer.Stop();
                    BeginRecording();
                }
                else
                {
                    Status.Text = Loc.F("rec_countdown", _left);
                }
            };
            _timer.Start();
        }

        void BeginRecording()
        {
            if (_recording || _cancelled) return;
            if (_timer != null) _timer.Stop();
            _recording = true;
            BtnStop.Content = Loc.T("rec_stop");
            Directory.CreateDirectory(_dir);
            _stop = new CancellationTokenSource();
            _clock = Stopwatch.StartNew();
            var token = _stop.Token;
            _captureTask = Task.Run(() => CaptureLoop(token));
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _timer.Tick += (a, b) =>
            {
                double sec = _clock.Elapsed.TotalSeconds;
                Dot.Opacity = ((int)(sec * 2) % 2 == 0) ? 1.0 : 0.3;
                Status.Text = Loc.F("rec_recording", FormatTime(sec), FormatTime(_s.RecordMaxSeconds));
            };
            _timer.Start();
            _ = FinishWhenDone();
        }

        static string FormatTime(double sec)
        {
            int t = (int)sec;
            return (t / 60).ToString("D2") + ":" + (t % 60).ToString("D2");
        }

        void CaptureLoop(CancellationToken token)
        {
            int interval = 1000 / _s.RecordFps;
            long maxMs = _s.RecordMaxSeconds * 1000L;
            int index = 0;
            var jpeg = GetJpegCodec();
            var ep = new EncoderParameters(1);
            ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 90L);
            while (!token.IsCancellationRequested)
            {
                long start = _clock.ElapsedMilliseconds;
                if (start >= maxMs) break;
                try
                {
                    using (var raw = ScreenGrab.Capture(_region))
                    {
                        if (_s.RecordCursor) ScreenGrab.DrawCursor(raw, _region);
                        Bitmap frame = raw;
                        Bitmap scaled = null;
                        if (_outW != raw.Width || _outH != raw.Height)
                        {
                            scaled = new Bitmap(_outW, _outH, PixelFormat.Format32bppRgb);
                            using (var g = Graphics.FromImage(scaled))
                            {
                                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                                g.DrawImage(raw, new D.Rectangle(0, 0, _outW, _outH), 0, 0, raw.Width, raw.Height, GraphicsUnit.Pixel);
                            }
                            frame = scaled;
                        }
                        try
                        {
                            string file = Path.Combine(_dir, index.ToString("D6") + ".jpg");
                            frame.Save(file, jpeg, ep);
                            lock (_stamps) _stamps.Add(start);
                            index++;
                        }
                        finally
                        {
                            if (scaled != null) scaled.Dispose();
                        }
                    }
                }
                catch
                {
                    break;
                }
                long spent = _clock.ElapsedMilliseconds - start;
                int wait = (int)(interval - spent);
                if (wait > 0) token.WaitHandle.WaitOne(wait);
            }
            _clock.Stop();
        }

        static ImageCodecInfo GetJpegCodec()
        {
            foreach (var c in ImageCodecInfo.GetImageEncoders())
            {
                if (c.FormatID == ImageFormat.Jpeg.Guid) return c;
            }
            return null;
        }

        async Task FinishWhenDone()
        {
            await _captureTask;
            if (_timer != null) _timer.Stop();
            Dot.Opacity = 1;
            if (_cancelled)
            {
                Cleanup();
                _finished = true;
                SafeClose();
                return;
            }
            List<long> stamps;
            lock (_stamps) stamps = new List<long>(_stamps);
            if (stamps.Count < 2)
            {
                Cleanup();
                _finished = true;
                _tcs.TrySetResult(new RecordResult { Message = Loc.T("rec_empty") });
                SafeClose();
                return;
            }
            _encoding = true;
            BtnStop.IsEnabled = false;
            BtnCancel.IsEnabled = false;
            Bar.Visibility = Visibility.Visible;
            Bar.IsIndeterminate = false;
            Bar.Value = 0;
            Status.Text = Loc.T("rec_encoding");
            long totalMs = _clock.ElapsedMilliseconds;
            RecordResult result = null;
            try
            {
                result = await Task.Run(() => Encode(stamps, totalMs));
            }
            catch (Exception ex)
            {
                result = new RecordResult { Message = ex.Message };
            }
            Cleanup();
            _finished = true;
            _tcs.TrySetResult(result);
            SafeClose();
        }

        void Progress(double pct)
        {
            Dispatcher.BeginInvoke(new Action(() => Bar.Value = pct));
        }

        RecordResult Encode(List<long> stamps, long totalMs)
        {
            string baseName = OutputService.ReserveBase(_s, "Record");
            string message = null;
            string path = null;
            bool wantMp4 = _s.RecordFormat == "mp4";
            if (wantMp4)
            {
                string mp4 = Path.Combine(_s.SaveFolder, baseName + ".mp4");
                string err = EncodeMp4(stamps, totalMs, mp4);
                if (err == null) path = mp4;
                else
                {
                    message = Loc.F("rec_fallback", err);
                    try { if (File.Exists(mp4)) File.Delete(mp4); } catch { }
                }
            }
            if (path == null)
            {
                string gif = Path.Combine(_s.SaveFolder, baseName + ".gif");
                EncodeGif(stamps, totalMs, gif);
                path = gif;
            }
            var result = new RecordResult { Path = path, Message = message, Width = _outW, Height = _outH };
            using (var first = new Bitmap(Path.Combine(_dir, "000000.jpg")))
            {
                result.Thumb = OutputService.Thumb(first, 240, 150);
            }
            return result;
        }

        static int DelayFor(List<long> stamps, int i, long totalMs, int fallbackMs)
        {
            long next = i + 1 < stamps.Count ? stamps[i + 1] : Math.Max(totalMs, stamps[i] + fallbackMs);
            return (int)Math.Max(20, next - stamps[i]);
        }

        void EncodeGif(List<long> stamps, long totalMs, string path)
        {
            int fallback = 1000 / _s.RecordFps;
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                var gif = new GifWriter(fs, _outW, _outH);
                for (int i = 0; i < stamps.Count; i++)
                {
                    using (var src = new Bitmap(Path.Combine(_dir, i.ToString("D6") + ".jpg")))
                    using (var frame = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppRgb))
                    {
                        using (var g = Graphics.FromImage(frame)) g.DrawImageUnscaled(src, 0, 0);
                        int cs = (int)Math.Round(DelayFor(stamps, i, totalMs, fallback) / 10.0);
                        gif.AddFrame(frame, cs);
                    }
                    Progress((i + 1) * 100.0 / stamps.Count);
                }
                gif.Finish();
            }
        }

        string FindFfmpeg()
        {
            if (!string.IsNullOrWhiteSpace(_s.FfmpegPath) && File.Exists(_s.FfmpegPath)) return _s.FfmpegPath;
            string local = Path.Combine(AppContext.BaseDirectory, "ffmpeg.exe");
            if (File.Exists(local)) return local;
            return "ffmpeg";
        }

        string EncodeMp4(List<long> stamps, long totalMs, string path)
        {
            int fallback = 1000 / _s.RecordFps;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < stamps.Count; i++)
            {
                sb.Append("file '").Append(i.ToString("D6")).Append(".jpg'\n");
                sb.Append("duration ").Append((DelayFor(stamps, i, totalMs, fallback) / 1000.0).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
            }
            sb.Append("file '").Append((stamps.Count - 1).ToString("D6")).Append(".jpg'\n");
            File.WriteAllText(Path.Combine(_dir, "list.txt"), sb.ToString());
            var psi = new ProcessStartInfo
            {
                FileName = FindFfmpeg(),
                WorkingDirectory = _dir,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            psi.ArgumentList.Add("-y");
            psi.ArgumentList.Add("-f");
            psi.ArgumentList.Add("concat");
            psi.ArgumentList.Add("-safe");
            psi.ArgumentList.Add("0");
            psi.ArgumentList.Add("-i");
            psi.ArgumentList.Add("list.txt");
            psi.ArgumentList.Add("-vf");
            psi.ArgumentList.Add("fps=" + _s.RecordFps + ",scale=trunc(iw/2)*2:trunc(ih/2)*2");
            psi.ArgumentList.Add("-c:v");
            psi.ArgumentList.Add("libx264");
            psi.ArgumentList.Add("-pix_fmt");
            psi.ArgumentList.Add("yuv420p");
            psi.ArgumentList.Add("-crf");
            psi.ArgumentList.Add("23");
            psi.ArgumentList.Add("-movflags");
            psi.ArgumentList.Add("+faststart");
            psi.ArgumentList.Add(path);
            try
            {
                using (var p = Process.Start(psi))
                {
                    var errTask = p.StandardError.ReadToEndAsync();
                    var outTask = p.StandardOutput.ReadToEndAsync();
                    Dispatcher.BeginInvoke(new Action(() => Bar.IsIndeterminate = true));
                    if (!p.WaitForExit(600000))
                    {
                        try { p.Kill(true); } catch { }
                        return "timeout";
                    }
                    if (p.ExitCode != 0)
                    {
                        string e = errTask.Result ?? "";
                        int n = e.TrimEnd().LastIndexOf('\n');
                        return n >= 0 ? e.Substring(n + 1).Trim() : "ffmpeg exit " + p.ExitCode;
                    }
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return "ffmpeg not found";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return File.Exists(path) ? null : "no output";
        }

        void SafeClose()
        {
            try { Close(); } catch { }
        }

        void Cleanup()
        {
            try
            {
                if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
            }
            catch
            {
            }
        }

        void OnStop(object sender, RoutedEventArgs e)
        {
            if (!_recording) BeginRecording();
            else RequestStop();
        }

        void OnCancel(object sender, RoutedEventArgs e)
        {
            RequestCancel();
        }

        void OnDrag(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is System.Windows.Controls.TextBlock || e.OriginalSource is System.Windows.Controls.Border)
            {
                try { DragMove(); } catch { }
            }
        }
    }
}
