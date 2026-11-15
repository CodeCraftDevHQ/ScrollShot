using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace ScrollShot
{
    internal sealed class WinInfo
    {
        public IntPtr Handle;
        public Rectangle Bounds;
        public string Title;
    }

    internal static class ScreenGrab
    {
        public static Rectangle VirtualBounds()
        {
            return System.Windows.Forms.SystemInformation.VirtualScreen;
        }

        public static Bitmap Capture(Rectangle r)
        {
            var bmp = new Bitmap(r.Width, r.Height, PixelFormat.Format32bppRgb);
            IntPtr screenDc = Native.GetDC(IntPtr.Zero);
            try
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    IntPtr destDc = g.GetHdc();
                    try
                    {
                        Native.BitBlt(destDc, 0, 0, r.Width, r.Height, screenDc, r.X, r.Y, 0x00CC0020u | 0x40000000u);
                    }
                    finally
                    {
                        g.ReleaseHdc(destDc);
                    }
                }
            }
            finally
            {
                Native.ReleaseDC(IntPtr.Zero, screenDc);
            }
            return bmp;
        }

        public static Bitmap Crop(Bitmap src, Rectangle r)
        {
            return src.Clone(r, src.PixelFormat);
        }

        public static System.Windows.Media.Imaging.BitmapSource ToSource(Bitmap bmp)
        {
            var data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppRgb);
            try
            {
                var src = System.Windows.Media.Imaging.BitmapSource.Create(
                    bmp.Width, bmp.Height, 96, 96,
                    System.Windows.Media.PixelFormats.Bgr32, null,
                    data.Scan0, data.Stride * bmp.Height, data.Stride);
                src.Freeze();
                return src;
            }
            finally
            {
                bmp.UnlockBits(data);
            }
        }

        public static List<WinInfo> EnumerateWindows()
        {
            var list = new List<WinInfo>();
            uint self = (uint)Environment.ProcessId;
            var sb = new StringBuilder(256);
            Native.EnumWindows((h, l) =>
            {
                if (!Native.IsWindowVisible(h) || Native.IsIconic(h)) return true;
                uint pid;
                Native.GetWindowThreadProcessId(h, out pid);
                if (pid == self) return true;
                int cloaked;
                if (Native.DwmGetWindowAttribute(h, 14, out cloaked, 4) == 0 && cloaked != 0) return true;
                sb.Clear();
                Native.GetClassName(h, sb, 256);
                string cls = sb.ToString();
                if (cls == "Progman" || cls == "WorkerW") return true;
                Native.RECT rc;
                if (Native.DwmGetWindowAttribute(h, 9, out rc, Marshal.SizeOf(typeof(Native.RECT))) != 0)
                {
                    if (!Native.GetWindowRect(h, out rc)) return true;
                }
                var b = Rectangle.FromLTRB(rc.Left, rc.Top, rc.Right, rc.Bottom);
                if (b.Width < 8 || b.Height < 8) return true;
                sb.Clear();
                Native.GetWindowText(h, sb, 256);
                list.Add(new WinInfo { Handle = h, Bounds = b, Title = sb.ToString() });
                return true;
            }, IntPtr.Zero);
            return list;
        }

        public static WinInfo WindowAt(List<WinInfo> wins, Point p)
        {
            foreach (var w in wins)
            {
                if (w.Bounds.Contains(p)) return w;
            }
            return null;
        }

        public static Rectangle? ChildRectAt(IntPtr top, Point p)
        {
            IntPtr cur = top;
            for (int i = 0; i < 8; i++)
            {
                var pt = new Native.POINT { X = p.X, Y = p.Y };
                Native.ScreenToClient(cur, ref pt);
                IntPtr c = Native.ChildWindowFromPointEx(cur, pt, Native.CWP_SKIPINVISIBLE);
                if (c == IntPtr.Zero || c == cur) break;
                cur = c;
            }
            if (cur == top) return null;
            Native.RECT r;
            if (!Native.GetWindowRect(cur, out r)) return null;
            var rect = Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
            if (rect.Width < 8 || rect.Height < 8) return null;
            return rect;
        }
    }
}
