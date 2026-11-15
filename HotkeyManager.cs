using System;
using System.Collections.Generic;
using System.Windows.Interop;

namespace ScrollShot
{
    internal sealed class HotkeyManager : IDisposable
    {
        readonly HwndSource _src;
        readonly Dictionary<int, Action> _actions = new Dictionary<int, Action>();
        int _next = 1;

        public HotkeyManager()
        {
            var p = new HwndSourceParameters("ScrollShotHotkeys")
            {
                ParentWindow = new IntPtr(-3),
                Width = 0,
                Height = 0,
                WindowStyle = 0
            };
            _src = new HwndSource(p);
            _src.AddHook(Hook);
        }

        IntPtr Hook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Native.WM_HOTKEY)
            {
                Action a;
                if (_actions.TryGetValue(wParam.ToInt32(), out a))
                {
                    handled = true;
                    a();
                }
            }
            return IntPtr.Zero;
        }

        public bool Register(HotkeyDef def, Action action)
        {
            int id = _next++;
            if (!Native.RegisterHotKey(_src.Handle, id, def.Modifiers | Native.MOD_NOREPEAT, def.Key)) return false;
            _actions[id] = action;
            return true;
        }

        public int RegisterTemp(HotkeyDef def, Action action)
        {
            int id = _next++;
            if (!Native.RegisterHotKey(_src.Handle, id, def.Modifiers, def.Key)) return 0;
            _actions[id] = action;
            return id;
        }

        public void Unregister(int id)
        {
            Native.UnregisterHotKey(_src.Handle, id);
            _actions.Remove(id);
        }

        public void Clear()
        {
            foreach (var id in _actions.Keys) Native.UnregisterHotKey(_src.Handle, id);
            _actions.Clear();
        }

        public void Dispose()
        {
            Clear();
            _src.Dispose();
        }
    }
}
