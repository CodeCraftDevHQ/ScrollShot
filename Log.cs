using System;
using System.Drawing;
using System.IO;

namespace ScrollShot
{
    internal static class Log
    {
        static readonly object Gate = new object();

        public static string Folder
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScrollShot"); }
        }

        public static string FilePath
        {
            get { return Path.Combine(Folder, "log.txt"); }
        }

        public static void Info(string text)
        {
            Write("INFO", text);
        }

        public static void Error(string where, Exception ex)
        {
            Write("ERROR", where + ": " + ex);
        }

        static void Write(string level, string text)
        {
            try
            {
                lock (Gate)
                {
                    Directory.CreateDirectory(Folder);
                    var fi = new FileInfo(FilePath);
                    if (fi.Exists && fi.Length > 512 * 1024)
                    {
                        string old = FilePath + ".1";
                        if (File.Exists(old)) File.Delete(old);
                        File.Move(FilePath, old);
                    }
                    File.AppendAllText(FilePath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [" + level + "] " + text + Environment.NewLine);
                }
            }
            catch
            {
            }
        }

        public static string DumpFrame(Bitmap bmp, string tag, int index)
        {
            try
            {
                string dir = Path.Combine(Folder, "debug");
                Directory.CreateDirectory(dir);
                var files = new DirectoryInfo(dir).GetFiles("*.png");
                if (files.Length > 60)
                {
                    Array.Sort(files, (a, b) => a.CreationTimeUtc.CompareTo(b.CreationTimeUtc));
                    for (int i = 0; i < files.Length - 40; i++)
                    {
                        try { files[i].Delete(); } catch { }
                    }
                }
                string path = Path.Combine(dir, DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + tag + "_" + index.ToString("D3") + ".png");
                bmp.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                return path;
            }
            catch
            {
                return null;
            }
        }
    }
}
