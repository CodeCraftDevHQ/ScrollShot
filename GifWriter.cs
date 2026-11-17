using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ScrollShot
{
    internal sealed class GifWriter
    {
        static readonly byte[] Palette = BuildPalette();
        static readonly int[] OffR = BuildOffsets(51.0);
        static readonly int[] OffG = BuildOffsets(42.5);
        static readonly int[] OffB = BuildOffsets(51.0);

        readonly Stream _out;
        readonly int _w;
        readonly int _h;
        byte[] _prev;
        bool _headerWritten;
        bool _hasPending;
        int _pX;
        int _pY;
        int _pW;
        int _pH;
        int _pDelay;
        byte[] _pData;

        public GifWriter(Stream output, int width, int height)
        {
            _out = output;
            _w = width;
            _h = height;
        }

        static byte[] BuildPalette()
        {
            var p = new byte[768];
            for (int r = 0; r < 6; r++)
            {
                for (int g = 0; g < 7; g++)
                {
                    for (int b = 0; b < 6; b++)
                    {
                        int idx = r * 42 + g * 6 + b;
                        p[idx * 3] = (byte)(r * 51);
                        p[idx * 3 + 1] = (byte)Math.Round(g * 255.0 / 6.0);
                        p[idx * 3 + 2] = (byte)(b * 51);
                    }
                }
            }
            return p;
        }

        static int[] BuildOffsets(double step)
        {
            int[] m = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
            var o = new int[16];
            for (int i = 0; i < 16; i++) o[i] = (int)Math.Round(((m[i] + 0.5) / 16.0 - 0.5) * step);
            return o;
        }

        static int Clamp(int v)
        {
            return v < 0 ? 0 : v > 255 ? 255 : v;
        }

        byte[] Quantize(Bitmap bmp)
        {
            var data = bmp.LockBits(new Rectangle(0, 0, _w, _h), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            try
            {
                int stride = data.Stride;
                var raw = new byte[stride * _h];
                Marshal.Copy(data.Scan0, raw, 0, raw.Length);
                var idx = new byte[_w * _h];
                for (int y = 0; y < _h; y++)
                {
                    int ri = y * stride;
                    int oi = y * _w;
                    int by = (y & 3) << 2;
                    for (int x = 0; x < _w; x++)
                    {
                        int i = ri + x * 4;
                        int cell = by + (x & 3);
                        int b = Clamp(raw[i] + OffB[cell]);
                        int g = Clamp(raw[i + 1] + OffG[cell]);
                        int r = Clamp(raw[i + 2] + OffR[cell]);
                        int rq = (r * 5 + 127) / 255;
                        int gq = (g * 6 + 127) / 255;
                        int bq = (b * 5 + 127) / 255;
                        idx[oi + x] = (byte)(rq * 42 + gq * 6 + bq);
                    }
                }
                return idx;
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        void W16(int v)
        {
            _out.WriteByte((byte)(v & 0xFF));
            _out.WriteByte((byte)((v >> 8) & 0xFF));
        }

        void WriteHeader()
        {
            _headerWritten = true;
            var sig = new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 };
            _out.Write(sig, 0, sig.Length);
            W16(_w);
            W16(_h);
            _out.WriteByte(0xF7);
            _out.WriteByte(0);
            _out.WriteByte(0);
            _out.Write(Palette, 0, Palette.Length);
            var net = new byte[] { 0x21, 0xFF, 0x0B, 0x4E, 0x45, 0x54, 0x53, 0x43, 0x41, 0x50, 0x45, 0x32, 0x2E, 0x30, 0x03, 0x01, 0x00, 0x00, 0x00 };
            _out.Write(net, 0, net.Length);
        }

        public void AddFrame(Bitmap bmp, int delayCs)
        {
            var cur = Quantize(bmp);
            if (!_headerWritten) WriteHeader();
            if (_prev == null)
            {
                QueueFrame(0, 0, _w, _h, cur, delayCs);
                _prev = cur;
                return;
            }
            int minX = _w, minY = _h, maxX = -1, maxY = -1;
            for (int y = 0; y < _h; y++)
            {
                int o = y * _w;
                for (int x = 0; x < _w; x++)
                {
                    if (cur[o + x] != _prev[o + x])
                    {
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                }
            }
            if (maxX < 0)
            {
                if (_hasPending) _pDelay += delayCs;
                return;
            }
            int bw = maxX - minX + 1;
            int bh = maxY - minY + 1;
            var data = new byte[bw * bh];
            for (int yy = 0; yy < bh; yy++)
            {
                int so = (minY + yy) * _w + minX;
                int d = yy * bw;
                for (int xx = 0; xx < bw; xx++)
                {
                    byte c = cur[so + xx];
                    data[d + xx] = c == _prev[so + xx] ? (byte)255 : c;
                }
            }
            QueueFrame(minX, minY, bw, bh, data, delayCs);
            _prev = cur;
        }

        void QueueFrame(int x, int y, int w, int h, byte[] data, int delayCs)
        {
            FlushPending();
            _hasPending = true;
            _pX = x;
            _pY = y;
            _pW = w;
            _pH = h;
            _pData = data;
            _pDelay = delayCs;
        }

        void FlushPending()
        {
            if (!_hasPending) return;
            _hasPending = false;
            _out.WriteByte(0x21);
            _out.WriteByte(0xF9);
            _out.WriteByte(4);
            _out.WriteByte(0x05);
            W16(Math.Max(2, _pDelay));
            _out.WriteByte(255);
            _out.WriteByte(0);
            _out.WriteByte(0x2C);
            W16(_pX);
            W16(_pY);
            W16(_pW);
            W16(_pH);
            _out.WriteByte(0);
            WriteLzw(_out, _pData);
            _pData = null;
        }

        public void Finish()
        {
            FlushPending();
            _out.WriteByte(0x3B);
        }

        sealed class BitWriter
        {
            public readonly List<byte> Bytes = new List<byte>();
            int _cur;
            int _bits;

            public void Write(int code, int size)
            {
                _cur |= code << _bits;
                _bits += size;
                while (_bits >= 8)
                {
                    Bytes.Add((byte)(_cur & 0xFF));
                    _cur >>= 8;
                    _bits -= 8;
                }
            }

            public void Flush()
            {
                if (_bits > 0)
                {
                    Bytes.Add((byte)(_cur & 0xFF));
                    _cur = 0;
                    _bits = 0;
                }
            }
        }

        internal static void WriteLzw(Stream s, byte[] pixels)
        {
            const int clear = 256;
            const int eoi = 257;
            var bw = new BitWriter();
            var table = new int[4096 * 256];
            int codeSize = 9;
            int next = 258;
            bw.Write(clear, codeSize);
            int prefix = pixels[0];
            for (int i = 1; i < pixels.Length; i++)
            {
                int k = pixels[i];
                int key = (prefix << 8) | k;
                int code = table[key];
                if (code != 0)
                {
                    prefix = code;
                    continue;
                }
                bw.Write(prefix, codeSize);
                if (next >= (1 << codeSize) && codeSize < 12) codeSize++;
                if (next < 4096)
                {
                    table[key] = next++;
                }
                else
                {
                    bw.Write(clear, codeSize);
                    Array.Clear(table, 0, table.Length);
                    codeSize = 9;
                    next = 258;
                }
                prefix = k;
            }
            bw.Write(prefix, codeSize);
            if (next >= (1 << codeSize) && codeSize < 12) codeSize++;
            bw.Write(eoi, codeSize);
            bw.Flush();
            s.WriteByte(8);
            var bytes = bw.Bytes;
            var arr = bytes.ToArray();
            for (int p = 0; p < arr.Length; p += 255)
            {
                int n = Math.Min(255, arr.Length - p);
                s.WriteByte((byte)n);
                s.Write(arr, p, n);
            }
            s.WriteByte(0);
        }
    }
}
