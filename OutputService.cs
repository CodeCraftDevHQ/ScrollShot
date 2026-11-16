using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace ScrollShot
{
    internal sealed class OutputResult
    {
        public List<string> FilePaths = new List<string>();
        public long TotalBytes;
        public bool Copied;
        public string Error;
        public string FilePath { get { return FilePaths.Count > 0 ? FilePaths[0] : null; } }
    }

    internal static class OutputService
    {
        public static string FormatSize(long bytes)
        {
            if (bytes >= 1024L * 1024) return (bytes / 1048576.0).ToString("0.0") + " MB";
            return Math.Max(1, bytes / 1024) + " KB";
        }

        public static OutputResult Process(Bitmap bmp, AppSettings s, string type)
        {
            var res = new OutputResult();
            Bitmap scaled = null;
            try
            {
                scaled = ImageTools.ScaleIfNeeded(bmp, s.ScalePercent, s.MaxWidthPx);
                var work = scaled ?? bmp;
                if (s.SaveToFile)
                {
                    try
                    {
                        SaveAll(work, s, type, res);
                    }
                    catch (Exception ex)
                    {
                        res.Error = ex.Message;
                    }
                }
                if (s.CopyToClipboard) res.Copied = CopyToClipboard(work);
            }
            finally
            {
                if (scaled != null) scaled.Dispose();
            }
            return res;
        }

        static string UniqueBase(AppSettings s, string type)
        {
            var now = DateTime.Now;
            string name = s.FileNamePattern
                .Replace("{date}", now.ToString("yyyy-MM-dd"))
                .Replace("{time}", now.ToString("HH-mm-ss"))
                .Replace("{type}", type);
            foreach (char c in Path.GetInvalidFileNameChars()) name = name.Replace(c.ToString(), "_");
            if (string.IsNullOrWhiteSpace(name)) name = "Screenshot";
            string b = name;
            int n = 1;
            while (Directory.GetFiles(s.SaveFolder, b + "*").Length > 0) b = name + "_" + (n++);
            return b;
        }

        static void SaveAll(Bitmap work, AppSettings s, string type, OutputResult res)
        {
            Directory.CreateDirectory(s.SaveFolder);
            string baseName = UniqueBase(s, type);
            string fmt = s.Format;

            if (fmt == "pdf")
            {
                string p = Path.Combine(s.SaveFolder, baseName + ".pdf");
                SavePdf(work, p, s);
                res.FilePaths.Add(p);
            }
            else
            {
                int splitH = s.SplitEnabled ? s.SplitHeightPx : 0;
                if (fmt == "jpg" && work.Height > 65000 && (splitH == 0 || splitH > 65000)) splitH = 60000;
                if (splitH > 0 && work.Height > splitH)
                {
                    var parts = ImageTools.Split(work, splitH, s.SmartCut);
                    try
                    {
                        for (int i = 0; i < parts.Count; i++)
                        {
                            res.FilePaths.Add(SaveImage(parts[i], Path.Combine(s.SaveFolder, baseName + "_" + (i + 1).ToString("D2")), fmt, s));
                        }
                    }
                    finally
                    {
                        foreach (var part in parts) part.Dispose();
                    }
                }
                else
                {
                    res.FilePaths.Add(SaveImage(work, Path.Combine(s.SaveFolder, baseName), fmt, s));
                }
                if (s.AlsoSavePdf)
                {
                    string p = Path.Combine(s.SaveFolder, baseName + ".pdf");
                    SavePdf(work, p, s);
                    res.FilePaths.Add(p);
                }
            }

            foreach (var f in res.FilePaths)
            {
                try { res.TotalBytes += new FileInfo(f).Length; } catch { }
            }
        }

        static string SaveImage(Bitmap bmp, string pathNoExt, string fmt, AppSettings s)
        {
            string path;
            if (fmt == "jpg")
            {
                path = pathNoExt + ".jpg";
                File.WriteAllBytes(path, ImageTools.EncodeJpegTarget(bmp, s.JpgQuality, s.TargetSizeKB * 1024L));
            }
            else if (fmt == "bmp")
            {
                path = pathNoExt + ".bmp";
                bmp.Save(path, ImageFormat.Bmp);
            }
            else
            {
                path = pathNoExt + ".png";
                bmp.Save(path, ImageFormat.Png);
            }
            return path;
        }

        static void SavePdf(Bitmap img, string path, AppSettings s)
        {
            bool letter = s.PdfPageSize == "letter";
            double pw = letter ? 612 : 595.28;
            double ph = letter ? 792 : 841.89;
            double margin = s.PdfMarginMm * 72.0 / 25.4;
            long target = s.TargetSizeKB * 1024L;
            var pdf = new PdfBuilder();

            if (s.PdfPageMode == "single")
            {
                double wpt = img.Width * 0.75 + 2 * margin;
                double hpt = img.Height * 0.75 + 2 * margin;
                if (wpt <= 14400 && hpt <= 14400)
                {
                    var jpg = ImageTools.EncodeJpegTarget(img, s.PdfJpegQuality, target);
                    pdf.AddPage(wpt, hpt, margin, margin, img.Width * 0.75, img.Height * 0.75, jpg, img.Width, img.Height);
                    pdf.Save(path);
                    return;
                }
            }

            double availW = pw - 2 * margin;
            double availH = ph - 2 * margin;
            double scale = availW / img.Width;
            int sliceH = Math.Max(1, (int)(availH / scale));
            var slices = ImageTools.Split(img, sliceH, s.SmartCut);
            try
            {
                foreach (var sl in slices)
                {
                    long t = target > 0 ? Math.Max(1, target * sl.Height / img.Height) : 0;
                    var jpg = ImageTools.EncodeJpegTarget(sl, s.PdfJpegQuality, t);
                    double dh = sl.Height * scale;
                    pdf.AddPage(pw, ph, margin, ph - margin - dh, availW, dh, jpg, sl.Width, sl.Height);
                }
                pdf.Save(path);
            }
            finally
            {
                foreach (var sl in slices) sl.Dispose();
            }
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
