using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScrollShot
{
    public sealed class HotkeyBox : TextBox
    {
        HotkeyDef _value;

        public HotkeyDef Value
        {
            get { return _value; }
            set
            {
                _value = value;
                Text = value == null || value.Key == 0 ? "—" : value.ToString();
            }
        }

        public HotkeyBox()
        {
            IsReadOnly = true;
            Cursor = Cursors.Hand;
            FlowDirection = FlowDirection.LeftToRight;
            Width = 180;
            HorizontalAlignment = HorizontalAlignment.Left;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            e.Handled = true;
            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (key == Key.Back || key == Key.Delete)
            {
                Value = new HotkeyDef();
                return;
            }
            if (key == Key.LeftCtrl || key == Key.RightCtrl || key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftShift || key == Key.RightShift || key == Key.LWin || key == Key.RWin ||
                key == Key.None || key == Key.ImeProcessed || key == Key.DeadCharProcessed) return;
            uint mods = 0;
            var m = Keyboard.Modifiers;
            if ((m & ModifierKeys.Control) != 0) mods |= Native.MOD_CONTROL;
            if ((m & ModifierKeys.Alt) != 0) mods |= Native.MOD_ALT;
            if ((m & ModifierKeys.Shift) != 0) mods |= Native.MOD_SHIFT;
            if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin)) mods |= Native.MOD_WIN;
            bool standalone = (key >= Key.F1 && key <= Key.F24) || key == Key.Snapshot;
            if (mods == 0 && !standalone) return;
            int vk = KeyInterop.VirtualKeyFromKey(key);
            if (vk == 0) return;
            Value = new HotkeyDef(mods, (uint)vk);
        }
    }
}
