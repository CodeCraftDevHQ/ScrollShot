using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ScrollShot
{
    internal static class Native
    {
        public const int WM_HOTKEY = 0x0312;
        public const int WM_DPICHANGED = 0x02E0;
        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;
        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOZORDER = 0x0004;
        public const uint SWP_NOACTIVATE = 0x0010;
        public const uint MOD_ALT = 1;
        public const uint MOD_CONTROL = 2;
        public const uint MOD_SHIFT = 4;
        public const uint MOD_WIN = 8;
        public const uint MOD_NOREPEAT = 0x4000;
        public const uint CWP_SKIPINVISIBLE = 1;
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public MOUSEINPUT mi;
        }

        public delegate bool EnumWindowsProc(IntPtr h, IntPtr l);

        [DllImport("user32.dll")] public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mods, uint vk);
        [DllImport("user32.dll")] public static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
        [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint n, INPUT[] inputs, int size);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumWindowsProc cb, IntPtr l);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetClassName(IntPtr h, StringBuilder sb, int max);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(IntPtr h, StringBuilder sb, int max);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] public static extern int GetWindowLong(IntPtr h, int idx);
        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")] public static extern int SetWindowLong(IntPtr h, int idx, int v);
        [DllImport("user32.dll")] public static extern bool SetWindowDisplayAffinity(IntPtr h, uint a);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int cx, int cy, uint flags);
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] public static extern bool ScreenToClient(IntPtr h, ref POINT p);
        [DllImport("user32.dll")] public static extern IntPtr ChildWindowFromPointEx(IntPtr h, POINT p, uint flags);
        [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int attr, out RECT r, int size);
        [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int attr, out int v, int size);

        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr h);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr h, IntPtr dc);
        [DllImport("gdi32.dll")] public static extern bool BitBlt(IntPtr dest, int x, int y, int w, int h, IntPtr src, int sx, int sy, uint rop);

        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);

        public static void PostWheel(IntPtr target, int delta, int x, int y)
        {
            IntPtr wp = new IntPtr(unchecked((int)((uint)(delta & 0xFFFF) << 16)));
            IntPtr lp = new IntPtr(unchecked((int)(((uint)y << 16) | ((uint)x & 0xFFFF))));
            PostMessage(target, 0x020A, wp, lp);
        }

        public static void SendWheel(int delta)
        {
            var input = new INPUT
            {
                type = 0,
                mi = new MOUSEINPUT { mouseData = unchecked((uint)delta), dwFlags = 0x0800 }
            };
            SendInput(1, new[] { input }, Marshal.SizeOf(typeof(INPUT)));
        }
    }
}
