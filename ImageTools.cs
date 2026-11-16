using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ScrollShot
{
    internal static class ImageTools
    {
        public static Bitmap ScaleIfNeeded(Bitmap src, int percent, int maxWidth)
        {
            double f = percent / 100.0;
            if (maxWidth > 0 && src.Width * f > maxWidth) f = (double)maxWidth / src.Width;
            if (f >= 0.999) return null;
            int w = Math.Max(1, (int)Math.Round(src.Width * f));
            int h = Math.Max(1, (int)Math.Round(src.Height * f));
            var dst = new Bitmap(w, h, PixelFormat.Format32bppRgb);
            using (var g = Graphics.FromImage(dst))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(src, new Rectangle(0, 0, w, h), 0, 0, src.Width, src.Height, GraphicsUnit.Pixel);
            }
            return dst;
        }

        public static List<Bitmap> Split(Bitmap src, int maxH, bool smart)
        {
            var list = new List<Bitmap>();
            maxH = Math.Max(1, maxH);
            int[] act = smart && src.Height > maxH ? RowActivity(src) : null;
            int y = 0;
            while (y < src.Height)
            {
                int end = Math.Min(src.Height, y + maxH);
                if (end < src.Height && act != null)
                {
                    int lo = Math.Max(y + 1, y + (int)(maxH * 0.75));
                    int bestRow = end;
                    int bestVal = int.MaxValue;
                    for (int r = end; r >= lo; r--)
                    {
                        if (act[r] < bestVal)
                        {
                            bestVal = act[r];
                            bestRow = r;
                        }
                    }
                    end = bestRow;
                }
                list.Add(src.Clone(new Rectangle(0, y, src.Width, end - y), PixelFormat.Format32bppRgb));
                y = end;
            }
            return list;
        }

        static int[] RowActivity(Bitmap bmp)
        {
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            try
            {
                var act = new int[bmp.Height];
                var row = new byte[data.Stride];
                for (int y = 0; y < bmp.Height; y++)
                {
                    Marshal.Copy(IntPtr.Add(data.Scan0, y * data.Stride), row, 0, data.Stride);
                    int sum = 0;
                    int prev = row[1];
                    for (int x = 1; x < bmp.Width; x += 2)
                    {
                        int g = row[x * 4 + 1];
                        int d = g - prev;
                        sum += d < 0 ? -d : d;
                        prev = g;
                    }
                    act[y] = sum;
                }
                return act;
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        public static byte[] EncodeJpeg(Bitmap bmp, int quality)
        {
            var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            using (var ms = new MemoryStream())
            using (var ep = new EncoderParameters(1))
            {
                ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)quality);
                bmp.Save(ms, codec, ep);
                return ms.ToArray();
            }
        }

        public static byte[] EncodeJpegTarget(Bitmap bmp, int quality, long targetBytes)
        {
            int q = quality;
            var data = EncodeJpeg(bmp, q);
            while (targetBytes > 0 && data.LongLength > targetBytes && q > 30)
            {
                q = Math.Max(30, q - 8);
                data = EncodeJpeg(bmp, q);
            }
            return data;
        }
    }
}
