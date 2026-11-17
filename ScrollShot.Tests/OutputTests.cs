using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using Xunit;

namespace ScrollShot.Tests
{
    public class OutputTests
    {
        static byte[] DecodeLzw(byte[] data, int pos, int expected)
        {
            int minSize = data[pos++];
            var raw = new List<byte>();
            while (true)
            {
                int n = data[pos++];
                if (n == 0) break;
                for (int i = 0; i < n; i++) raw.Add(data[pos++]);
            }
            int clear = 1 << minSize;
            int eoi = clear + 1;
            var dict = new List<byte[]>();
            Action reset = () =>
            {
                dict.Clear();
                for (int i = 0; i < clear; i++) dict.Add(new[] { (byte)i });
                dict.Add(null);
                dict.Add(null);
            };
            reset();
            int codeSize = minSize + 1;
            long bits = 0;
            int nbits = 0;
            int idx = 0;
            var output = new List<byte>();
            byte[] prev = null;
            while (true)
            {
                while (nbits < codeSize && idx < raw.Count)
                {
                    bits |= (long)raw[idx++] << nbits;
                    nbits += 8;
                }
                if (nbits < codeSize) break;
                int code = (int)(bits & ((1 << codeSize) - 1));
                bits >>= codeSize;
                nbits -= codeSize;
                if (code == clear)
                {
                    reset();
                    codeSize = minSize + 1;
                    prev = null;
                    continue;
                }
                if (code == eoi) break;
                byte[] entry;
                if (code < dict.Count) entry = dict[code];
                else entry = Concat(prev, prev[0]);
                output.AddRange(entry);
                if (prev != null) dict.Add(Concat(prev, entry[0]));
                prev = entry;
                if (dict.Count == (1 << codeSize) && codeSize < 12) codeSize++;
            }
            return output.ToArray();
        }

        static byte[] Concat(byte[] a, byte b)
        {
            var r = new byte[a.Length + 1];
            Array.Copy(a, r, a.Length);
            r[a.Length] = b;
            return r;
        }

        [Theory]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(5000)]
        [InlineData(60000)]
        public void LzwRoundTrip(int length)
        {
            var rnd = new Random(length);
            var px = new byte[length];
            for (int i = 0; i < length; i++) px[i] = (byte)(i % 7 == 0 ? rnd.Next(256) : rnd.Next(4));
            using (var ms = new MemoryStream())
            {
                GifWriter.WriteLzw(ms, px);
                var decoded = DecodeLzw(ms.ToArray(), 0, length);
                Assert.Equal(px, decoded);
            }
        }

        [Fact]
        public void GifHasHeaderSizeAndTrailer()
        {
            using (var ms = new MemoryStream())
            {
                var gif = new GifWriter(ms, 40, 30);
                for (int i = 0; i < 3; i++)
                {
                    using (var b = new Bitmap(40, 30, PixelFormat.Format32bppRgb))
                    {
                        using (var g = Graphics.FromImage(b))
                        {
                            g.Clear(Color.White);
                            g.FillRectangle(Brushes.Red, i * 10, 5, 10, 10);
                        }
                        gif.AddFrame(b, 10);
                    }
                }
                gif.Finish();
                var bytes = ms.ToArray();
                Assert.Equal("GIF89a", Encoding.ASCII.GetString(bytes, 0, 6));
                Assert.Equal(40, bytes[6] | (bytes[7] << 8));
                Assert.Equal(30, bytes[8] | (bytes[9] << 8));
                Assert.Equal(0x3B, bytes[bytes.Length - 1]);
                ms.Position = 0;
                using (var img = Image.FromStream(ms))
                {
                    Assert.Equal(40, img.Width);
                    Assert.Equal(3, img.GetFrameCount(FrameDimension.Time));
                }
            }
        }

        [Fact]
        public void PdfHasStructure()
        {
            using (var bmp = new Bitmap(100, 80, PixelFormat.Format32bppRgb))
            {
                byte[] jpg = ImageTools.EncodeJpeg(bmp, 80);
                var pdf = new PdfBuilder();
                pdf.AddPage(595, 842, 20, 20, 555, 444, jpg, 100, 80);
                pdf.AddPage(595, 842, 20, 20, 555, 444, jpg, 100, 80);
                string path = Path.Combine(Path.GetTempPath(), "ss_test_" + Guid.NewGuid().ToString("N") + ".pdf");
                try
                {
                    pdf.Save(path);
                    string text = Encoding.ASCII.GetString(File.ReadAllBytes(path));
                    Assert.StartsWith("%PDF-1.4", text);
                    Assert.Contains("/Type /Pages", text);
                    Assert.Contains("%%EOF", text);
                    Assert.Contains("/Count 2", text);
                }
                finally
                {
                    if (File.Exists(path)) File.Delete(path);
                }
            }
        }

        [Fact]
        public void SplitKeepsTotalHeight()
        {
            using (var bmp = new Bitmap(100, 2500, PixelFormat.Format32bppRgb))
            {
                var parts = ImageTools.Split(bmp, 1000, true);
                int sum = 0;
                foreach (var p in parts)
                {
                    sum += p.Height;
                    Assert.True(p.Height <= 1000);
                    p.Dispose();
                }
                Assert.Equal(2500, sum);
            }
        }

        [Fact]
        public void TargetSizeIsRespected()
        {
            var rnd = new Random(5);
            using (var bmp = new Bitmap(600, 600, PixelFormat.Format32bppRgb))
            {
                for (int y = 0; y < 600; y += 2)
                    for (int x = 0; x < 600; x++) bmp.SetPixel(x, y, Color.FromArgb(rnd.Next(256), rnd.Next(256), rnd.Next(256)));
                long target = 60 * 1024;
                byte[] data = ImageTools.EncodeJpegTarget(bmp, 90, target);
                Assert.True(data.Length <= target * 1.1);
            }
        }
    }

    public class SettingsTests
    {
        [Fact]
        public void NormalizeClampsValues()
        {
            var s = new AppSettings { RecordFps = 500, RecordMaxSeconds = 0, ScalePercent = 1, MagnifierZoom = 99, Language = "xx", Theme = "?" };
            s.Normalize();
            Assert.Equal(30, s.RecordFps);
            Assert.Equal(3, s.RecordMaxSeconds);
            Assert.Equal(10, s.ScalePercent);
            Assert.Equal(20, s.MagnifierZoom);
            Assert.Equal("en", s.Language);
            Assert.Equal("dark", s.Theme);
        }

        [Fact]
        public void ProfileRoundTrip()
        {
            var s = new AppSettings().Normalize();
            s.Format = "jpg";
            s.ScalePercent = 60;
            s.TrimHeader = true;
            var p = s.CaptureProfile("test");
            var t = new AppSettings().Normalize();
            t.Profiles.Add(p);
            Assert.True(t.ApplyProfile("TEST"));
            Assert.Equal("jpg", t.Format);
            Assert.Equal(60, t.ScalePercent);
            Assert.True(t.TrimHeader);
        }

        [Fact]
        public void CloneIsIndependent()
        {
            var s = new AppSettings().Normalize();
            var c = s.Clone();
            c.Format = "bmp";
            Assert.NotEqual(s.Format, c.Format);
        }
    }

    public class UpdateTests
    {
        [Theory]
        [InlineData("1.2.3", true, 1, 2, 3)]
        [InlineData("v2.0", true, 2, 0, 0)]
        [InlineData("1.4.0-beta.1", true, 1, 4, 0)]
        [InlineData("abc", false, 0, 0, 0)]
        [InlineData("", false, 0, 0, 0)]
        public void ParsesVersions(string text, bool ok, int a, int b, int c)
        {
            Version v;
            Assert.Equal(ok, UpdateService.TryParse(text, out v));
            if (ok)
            {
                Assert.Equal(a, v.Major);
                Assert.Equal(b, v.Minor);
                Assert.Equal(c, v.Build);
            }
        }
    }
}
