using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ScrollShot
{
    internal sealed class OutputResult
    {
        public string FilePath;
        public bool Copied;
        public string Error;
    }

    internal static class OutputService
    {
        public static OutputResult Process(Bitmap bmp, AppSettings s, string type)
        {
            var res = new OutputResult();
            if (s.SaveToFile)
            {
                try
                {
                    res.FilePath = Save(bmp, s, type);
                }
                catch (Exception ex)
                {
                    res.Error = ex.Message;
                }
            }
            if (s.CopyToClipboard) res.Copied = CopyToClipboard(bmp);
            return res;
        }

        static string Save(Bitmap bmp, AppSettings s, string type)
        {
            Directory.CreateDirectory(s.SaveFolder);
            string fmt = s.Format;
            if (fmt == "jpg" && bmp.Height > 65000) fmt = "png";
            string ext = fmt == "jpg" ? ".jpg" : fmt == "bmp" ? ".bmp" : ".png";
            var now = DateTime.Now;
            string name = s.FileNamePattern
                .Replace("{date}", now.ToString("yyyy-MM-dd"))
                .Replace("{time}", now.ToString("HH-mm-ss"))
                .Replace("{type}", type);
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c.ToString(), "_");
            if (string.IsNullOrWhiteSpace(name)) name = "Screenshot";
            string path = Path.Combine(s.SaveFolder, name + ext);
            int n = 1;
            while (File.Exists(path)) path = Path.Combine(s.SaveFolder, name + "_" + (n++) + ext);

            if (fmt == "jpg")
            {
                var codec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
                using (var ep = new EncoderParameters(1))
                {
                    ep.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)s.JpgQuality);
                    bmp.Save(path, codec, ep);
                }
            }
            else if (fmt == "bmp")
            {
                bmp.Save(path, ImageFormat.Bmp);
            }
            else
            {
                bmp.Save(path, ImageFormat.Png);
            }
            return path;
        }

        static bool CopyToClipboard(Bitmap bmp)
        {
            if ((long)bmp.Width * bmp.Height > 80000000L) return false;
            var src = ScreenGrab.ToSource(bmp);
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    System.Windows.Clipboard.SetImage(src);
                    return true;
                }
                catch
                {
                    System.Threading.Thread.Sleep(80);
                }
            }
            return false;
        }

        public static System.Windows.Media.Imaging.BitmapSource Thumb(Bitmap bmp, int w, int h)
        {
            using (var t = new Bitmap(w, h, PixelFormat.Format32bppRgb))
            using (var g = Graphics.FromImage(t))
            {
                g.Clear(Color.FromArgb(32, 33, 36));
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                double scale;
                int ox = 0;
                int oy = 0;
                if ((long)bmp.Height * w > (long)h * bmp.Width)
                {
                    scale = (double)w / bmp.Width;
                }
                else
                {
                    scale = Math.Min((double)w / bmp.Width, (double)h / bmp.Height);
                    ox = (int)((w - bmp.Width * scale) / 2);
                    oy = (int)((h - bmp.Height * scale) / 2);
                }
                int dw = Math.Max(1, (int)(bmp.Width * scale));
                int dh = Math.Max(1, (int)(bmp.Height * scale));
                g.DrawImage(bmp, new Rectangle(ox, oy, dw, dh), 0, 0, bmp.Width, bmp.Height, GraphicsUnit.Pixel);
                return ScreenGrab.ToSource(t);
            }
        }
    }
}
