using System;
using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace ScrollShot.Tests
{
    public class StitcherTests
    {
        static Bitmap MakePage(int w, int h, int seed)
        {
            var rnd = new Random(seed);
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppRgb);
            for (int y = 0; y < h; y++)
            {
                int bandColor = rnd.Next(256);
                for (int x = 0; x < w; x++)
                {
                    int v = (bandColor + rnd.Next(120) + (x * 3) % 90) & 0xFF;
                    bmp.SetPixel(x, y, Color.FromArgb(v, (v * 7) & 0xFF, (v * 13) & 0xFF));
                }
            }
            return bmp;
        }

        static Bitmap Window(Bitmap page, int top, int h, Bitmap header, Bitmap footer)
        {
            var f = new Bitmap(page.Width, h, PixelFormat.Format32bppRgb);
            using (var g = Graphics.FromImage(f))
            {
                g.DrawImage(page, new Rectangle(0, 0, page.Width, h), new Rectangle(0, top, page.Width, h), GraphicsUnit.Pixel);
                if (header != null) g.DrawImageUnscaled(header, 0, 0);
                if (footer != null) g.DrawImageUnscaled(footer, 0, h - footer.Height);
            }
            return f;
        }

        static AppSettings Settings()
        {
            var s = new AppSettings();
            s.IgnoreRightPx = 0;
            s.IgnoreLeftPx = 0;
            return s.Normalize();
        }

        [Fact]
        public void StitchesPlainScroll()
        {
            using (var page = MakePage(300, 2000, 1))
            using (var st = new ScrollStitcher(Settings()))
            {
                for (int top = 0; top + 500 <= 2000; top += 300)
                {
                    using (var f = Window(page, top, 500, null, null))
                    {
                        var r = st.Add(f);
                        Assert.True(r == StitchResult.First || r == StitchResult.Added);
                    }
                }
                using (var f = Window(page, 1500, 500, null, null))
                {
                    st.Add(f);
                }
                using (var result = st.Build())
                {
                    Assert.InRange(result.Height, 1995, 2005);
                    Assert.Equal(page.Width, result.Width);
                    for (int y = 0; y < 2000; y += 97)
                    {
                        Assert.Equal(page.GetPixel(10, y).ToArgb(), result.GetPixel(10, y).ToArgb());
                    }
                }
            }
        }

        [Fact]
        public void IdenticalFrameIsNoChange()
        {
            using (var page = MakePage(200, 600, 2))
            using (var st = new ScrollStitcher(Settings()))
            {
                using (var a = Window(page, 0, 400, null, null)) st.Add(a);
                using (var b = Window(page, 0, 400, null, null)) Assert.Equal(StitchResult.NoChange, st.Add(b));
            }
        }

        [Fact]
        public void TrimsStickyHeaderAndFooter()
        {
            using (var page = MakePage(300, 2000, 3))
            using (var header = MakePage(300, 50, 99))
            using (var footer = MakePage(300, 40, 77))
            using (var st = new ScrollStitcher(Settings()))
            {
                for (int top = 0; top + 500 <= 2000; top += 300)
                {
                    using (var f = Window(page, top, 500, header, footer)) st.Add(f);
                }
                using (var f = Window(page, 1500, 500, header, footer)) st.Add(f);
                using (var plain = st.Build(false, false))
                using (var trimmed = st.Build(true, true))
                {
                    Assert.True(trimmed.Height < plain.Height);
                    Assert.InRange(trimmed.Height, 1800, 2000);
                }
            }
        }

        [Fact]
        public void DifferentSizeFrameFails()
        {
            using (var page = MakePage(200, 600, 4))
            using (var st = new ScrollStitcher(Settings()))
            {
                using (var a = Window(page, 0, 300, null, null)) st.Add(a);
                using (var b = Window(page, 0, 250, null, null)) Assert.Equal(StitchResult.Failed, st.Add(b));
            }
        }
    }
}
