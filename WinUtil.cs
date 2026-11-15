using System;
using System.Windows.Interop;
using System.Windows.Media;
using D = System.Drawing;

namespace ScrollShot
{
    internal static class WinUtil
    {
        static IntPtr DpiHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Native.WM_DPICHANGED) handled = true;
            return IntPtr.Zero;
        }

        public static void SwallowDpiChange(IntPtr hwnd)
        {
            var src = HwndSource.FromHwnd(hwnd);
            if (src != null) src.AddHook(DpiHook);
        }

        public static void SetPhysical(IntPtr hwnd, int x, int y, int w, int h, bool topmost, bool noActivate)
        {
            uint flags = noActivate ? Native.SWP_NOACTIVATE : 0;
            if (!topmost) flags |= Native.SWP_NOZORDER;
            Native.SetWindowPos(hwnd, topmost ? Native.HWND_TOPMOST : IntPtr.Zero, x, y, w, h, flags);
        }

        public static void MoveTo(IntPtr hwnd, int x, int y)
        {
            Native.SetWindowPos(hwnd, IntPtr.Zero, x, y, 0, 0, Native.SWP_NOSIZE | Native.SWP_NOACTIVATE | Native.SWP_NOZORDER);
        }

        public static D.Rectangle GetRect(IntPtr hwnd)
        {
            Native.RECT r;
            Native.GetWindowRect(hwnd, out r);
            return D.Rectangle.FromLTRB(r.Left, r.Top, r.Right, r.Bottom);
        }

        public static void ExcludeFromCapture(IntPtr hwnd)
        {
            Native.SetWindowDisplayAffinity(hwnd, Native.WDA_EXCLUDEFROMCAPTURE);
        }

        public static void MakeClickThrough(IntPtr hwnd)
        {
            int ex = Native.GetWindowLong(hwnd, Native.GWL_EXSTYLE);
            Native.SetWindowLong(hwnd, Native.GWL_EXSTYLE, ex | Native.WS_EX_TRANSPARENT | Native.WS_EX_NOACTIVATE | Native.WS_EX_TOOLWINDOW);
        }

        public static Brush ParseBrush(string text, Brush fallback)
        {
            try
            {
                var b = (Brush)new BrushConverter().ConvertFromString(text);
                if (b.CanFreeze) b.Freeze();
                return b;
            }
            catch
            {
                return fallback;
            }
        }
    }
}
