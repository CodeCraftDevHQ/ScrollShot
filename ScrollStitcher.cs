using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ScrollShot
{
    internal enum StitchResult { First, Added, NoChange, Failed }

    internal sealed class ScrollStitcher : IDisposable
    {
        readonly int _ignoreLeft;
        readonly int _ignoreRight;
        readonly int _tol;
        readonly int _maxHeight;
        readonly List<Bitmap> _strips = new List<Bitmap>();
        Bitmap _footer;
        int _footerRows;
        int _headerRows;
        bool _footerKnown;
        byte[] _prev;
        int _w;
        int _h;
        int _total;

        public int FrameCount { get; private set; }
        public int LastShift { get; private set; }
        public int TotalHeight { get { return _total; } }
        public bool LimitReached { get { return _total >= _maxHeight; } }

        public ScrollStitcher(AppSettings s) : this(s, false)
        {
        }

        public ScrollStitcher(AppSettings s, bool horizontal)
        {
            _ignoreLeft = horizontal ? s.IgnoreRightPx : s.IgnoreLeftPx;
            _ignoreRight = horizontal ? s.IgnoreLeftPx : s.IgnoreRightPx;
            _tol = s.MatchTolerance;
            _maxHeight = s.MaxHeightPx;
        }

        public StitchResult Add(Bitmap frame)
        {
            if (_prev == null)
            {
                _w = frame.Width;
                _h = frame.Height;
                _prev = Gray(frame);
                _strips.Add(Crop(frame, new Rectangle(0, 0, _w, _h)));
                _total = _h;
                FrameCount = 1;
                return StitchResult.First;
            }
            if (frame.Width != _w || frame.Height != _h) return StitchResult.Failed;

            byte[] cur = Gray(frame);
            if (SameGray(_prev, cur)) return StitchResult.NoChange;

            int d = FindShift(_prev, cur);
            if (d == 0) return StitchResult.NoChange;
            if (d == int.MinValue || d < 0 || d >= _h - 8) return StitchResult.Failed;

            if (!_footerKnown) DetectFooter(cur);

            int bottom = _h - _footerRows;
            int top = bottom - d;
            if (top < 0) return StitchResult.Failed;

            _strips.Add(Crop(frame, new Rectangle(0, top, _w, d)));
            _total += d;

            if (_footerRows > 0)
            {
                if (_footer != null) _footer.Dispose();
                _footer = Crop(frame, new Rectangle(0, _h - _footerRows, _w, _footerRows));
            }

            _prev = cur;
            LastShift = d;
            FrameCount++;
            return StitchResult.Added;
        }

        void DetectFooter(byte[] cur)
        {
            int k = 0;
            int max = _h / 3;
            while (k < max && RowSimilar(_prev, cur, _h - 1 - k)) k++;
            _footerKnown = true;
            _footerRows = k;
            int hk = 0;
            int hmax = _h / 4;
            while (hk < hmax && RowSimilar(_prev, cur, hk)) hk++;
            _headerRows = hk;
            if (k > 0)
            {
                var trimmed = Crop(_strips[0], new Rectangle(0, 0, _w, _h - k));
                _strips[0].Dispose();
                _strips[0] = trimmed;
            }
        }

        bool RowSimilar(byte[] a, byte[] b, int row)
        {
            int x0 = Math.Min(_ignoreLeft, _w / 4);
            int x1 = _w - Math.Min(_ignoreRight, _w / 4);
            long sum = 0;
            int cnt = 0;
            int i = row * _w;
            for (int x = x0; x < x1; x += 3)
            {
                int v = a[i + x] - b[i + x];
                sum += v < 0 ? -v : v;
                cnt++;
            }
            return cnt > 0 && sum <= cnt * 3L;
        }

        bool SameGray(byte[] a, byte[] b)
        {
            int x0 = Math.Min(_ignoreLeft, _w / 4);
            int x1 = _w - Math.Min(_ignoreRight, _w / 4);
            long diffs = 0;
            long n = 0;
            for (int y = 0; y < _h; y++)
            {
                int i = y * _w;
                for (int x = x0; x < x1; x += 2)
                {
                    int v = a[i + x] - b[i + x];
                    if (v < 0) v = -v;
                    if (v > 10) diffs++;
                    n++;
                }
            }
            return diffs <= Math.Max(30L, (long)(n * 0.0004));
        }

        int FindShift(byte[] p, byte[] f)
        {
            int x0 = Math.Min(_ignoreLeft, _w / 4);
            int x1 = _w - Math.Min(_ignoreRight, _w / 4);
            const int step = 2;
            int bh = Math.Max(16, Math.Min(64, _h / 6));
            if (_h < bh * 3) return int.MinValue;
            long samples = (long)((bh + 1) / 2) * ((x1 - x0 + step - 1) / step);

            var scored = new List<KeyValuePair<double, int>>();
            int stepY = Math.Max(4, bh / 2);
            for (int y = 0; y <= _h - bh; y += stepY)
            {
                long t = Texture(p, y, bh, x0, x1);
                if (t < samples) continue;
                double score = (double)t * (_h + y) / _h;
                scored.Add(new KeyValuePair<double, int>(score, y));
            }
            scored.Sort((u, v) => v.Key.CompareTo(u.Key));

            var chosen = new List<int>();
            foreach (var kv in scored)
            {
                bool near = false;
                foreach (int c in chosen)
                {
                    if (Math.Abs(c - kv.Value) < bh) { near = true; break; }
                }
                if (near) continue;
                chosen.Add(kv.Value);
                if (chosen.Count >= 6) break;
            }

            bool zero = false;
            foreach (int yb in chosen)
            {
                long best = long.MaxValue;
                int bestY = -1;
                for (int k = 0; k <= _h - bh; k++)
                {
                    for (int sign = 0; sign < 2; sign++)
                    {
                        if (k == 0 && sign == 1) continue;
                        int y2 = sign == 0 ? yb - k : yb + k;
                        if (y2 < 0 || y2 > _h - bh) continue;
                        long sad = Sad(p, yb, f, y2, bh, x0, x1, step, best);
                        if (sad < best)
                        {
                            best = sad;
                            bestY = y2;
                        }
                    }
                }
                if (bestY < 0) continue;
                if ((double)best / samples > _tol) continue;
                int d = yb - bestY;
                if (d == 0)
                {
                    zero = true;
                    continue;
                }
                if (d > 0 && !Verify(p, f, d, x0, x1)) continue;
                return d;
            }
            return zero ? 0 : int.MinValue;
        }

        long Texture(byte[] p, int y, int bh, int x0, int x1)
        {
            long sum = 0;
            for (int r = 0; r < bh; r += 2)
            {
                int i = (y + r) * _w;
                for (int x = x0; x < x1 - 2; x += 2)
                {
                    int v = p[i + x] - p[i + x + 2];
                    sum += v < 0 ? -v : v;
                }
            }
            return sum;
        }

        long Sad(byte[] p, int py, byte[] f, int fy, int bh, int x0, int x1, int step, long limit)
        {
            long sum = 0;
            for (int r = 0; r < bh; r += 2)
            {
                int pi = (py + r) * _w;
                int fi = (fy + r) * _w;
                for (int x = x0; x < x1; x += step)
                {
                    int v = p[pi + x] - f[fi + x];
                    sum += v < 0 ? -v : v;
                }
                if (sum >= limit) return sum;
            }
            return sum;
        }

        bool Verify(byte[] p, byte[] f, int d, int x0, int x1)
        {
            int ok = 0;
            int n = 0;
            for (int r = d; r < _h; r += 3)
            {
                n++;
                long sum = 0;
                int cnt = 0;
                int pi = r * _w;
                int fi = (r - d) * _w;
                for (int x = x0; x < x1; x += 4)
                {
                    int v = p[pi + x] - f[fi + x];
                    sum += v < 0 ? -v : v;
                    cnt++;
                }
                if (cnt > 0 && sum / cnt <= _tol * 2) ok++;
            }
            return n > 0 && ok * 100 >= n * 55;
        }

        public Bitmap Build()
        {
            return Build(false, false);
        }

        public Bitmap Build(bool trimHeader, bool trimFooter)
        {
            int skipTop = trimHeader ? Math.Max(0, Math.Min(_headerRows, _strips[0].Height - 1)) : 0;
            int footerRows = _footer != null ? _footer.Height : 0;
            int total = _total - skipTop - (trimFooter ? footerRows : 0);
            if (total < 1) total = _total;
            var bmp = new Bitmap(_w, total, PixelFormat.Format32bppRgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                int y = 0;
                for (int i = 0; i < _strips.Count; i++)
                {
                    var s = _strips[i];
                    int skip = i == 0 ? skipTop : 0;
                    int hh = s.Height - skip;
                    g.DrawImage(s, new Rectangle(0, y, s.Width, hh), 0, skip, s.Width, hh, GraphicsUnit.Pixel);
                    y += hh;
                }
                if (_footer != null && !trimFooter)
                {
                    g.DrawImage(_footer, new Rectangle(0, y, _footer.Width, _footer.Height), 0, 0, _footer.Width, _footer.Height, GraphicsUnit.Pixel);
                }
            }
            return bmp;
        }

        public void Dispose()
        {
            foreach (var s in _strips) s.Dispose();
            _strips.Clear();
            if (_footer != null) _footer.Dispose();
            _footer = null;
        }

        static Bitmap Crop(Bitmap src, Rectangle r)
        {
            return src.Clone(r, PixelFormat.Format32bppRgb);
        }

        static byte[] Raw(Bitmap bmp, out int stride)
        {
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            try
            {
                stride = data.Stride;
                var raw = new byte[stride * bmp.Height];
                Marshal.Copy(data.Scan0, raw, 0, raw.Length);
                return raw;
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        static byte[] Gray(Bitmap bmp)
        {
            int stride;
            var raw = Raw(bmp, out stride);
            int w = bmp.Width;
            int h = bmp.Height;
            var g = new byte[w * h];
            for (int y = 0; y < h; y++)
            {
                int ri = y * stride;
                int gi = y * w;
                for (int x = 0; x < w; x++)
                {
                    int i = ri + x * 4;
                    g[gi + x] = (byte)((raw[i] * 29 + raw[i + 1] * 150 + raw[i + 2] * 77) >> 8);
                }
            }
            return g;
        }

        public static bool QuickEqual(Bitmap a, Bitmap b)
        {
            if (a.Width != b.Width || a.Height != b.Height) return false;
            int sa, sb;
            var ra = Raw(a, out sa);
            var rb = Raw(b, out sb);
            long diffs = 0;
            long n = 0;
            for (int y = 0; y < a.Height; y += 3)
            {
                int ia = y * sa;
                int ib = y * sb;
                for (int x = 0; x < a.Width; x += 3)
                {
                    int pa = ia + x * 4;
                    int pb = ib + x * 4;
                    int d = Math.Abs(ra[pa] - rb[pb]) + Math.Abs(ra[pa + 1] - rb[pb + 1]) + Math.Abs(ra[pa + 2] - rb[pb + 2]);
                    if (d > 24) diffs++;
                    n++;
                }
            }
            return diffs <= Math.Max(3L, (long)(n * 0.0005));
        }
    }
}
